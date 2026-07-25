using TruthGate_Web.Services;

namespace TruthGate_Web.Tests;

public sealed class IpnsVersionRetentionTests
{
    [Theory]
    [InlineData("site", "site-v001", 1, false)]
    [InlineData("site", "site-v001-connected", 1, true)]
    [InlineData("My-Site", "my-site-v042-CONNECTED", 42, true)]
    public void TryParseEntryName_RecognizesPointerAndConnectedNames(
        string trackedName,
        string entryName,
        int expectedVersion,
        bool expectedConnected)
    {
        var parsed = IpnsVersionRetention.TryParseEntryName(
            trackedName,
            entryName,
            out var version,
            out var connected);

        Assert.True(parsed);
        Assert.Equal(expectedVersion, version);
        Assert.Equal(expectedConnected, connected);
    }

    [Theory]
    [InlineData("site", "site-v")]
    [InlineData("site", "site-v001-extra")]
    [InlineData("site", "other-v001")]
    [InlineData("site", "site-v001-connected-extra")]
    public void TryParseEntryName_RejectsUnmanagedNames(string trackedName, string entryName)
        => Assert.False(IpnsVersionRetention.TryParseEntryName(trackedName, entryName, out _, out _));

    [Fact]
    public void GroupVersions_PairsPointerAndConnectedFolders()
    {
        var children = Children(
            ("site-v001", "pointer-1"),
            ("site-v001-connected", "target-1"),
            ("site-v002", "pointer-2"),
            ("site-v002-connected", "target-2"));

        var pairs = IpnsVersionRetention.GroupVersions("site", children);

        Assert.Equal(2, pairs.Count);
        Assert.Equal("pointer-1", pairs[0].Pointer?.Cid);
        Assert.Equal("target-1", pairs[0].Connected?.Cid);
        Assert.Equal("pointer-2", pairs[1].Pointer?.Cid);
        Assert.Equal("target-2", pairs[1].Connected?.Cid);
        Assert.Equal(3, IpnsVersionRetention.ComputeNextVersion("site", children.Keys));
    }

    [Fact]
    public void BuildPrunePlan_RemovesOldPointerAndConnectedPair()
    {
        var pairs = IpnsVersionRetention.GroupVersions(
            "site",
            Children(
                ("site-v001", "pointer-1"),
                ("site-v001-connected", "target-1"),
                ("site-v002", "pointer-2"),
                ("site-v002-connected", "target-2")));

        var plan = IpnsVersionRetention.BuildPrunePlan(
            pairs,
            retainedVersion: 2,
            new Dictionary<int, string?> { [1] = "target-1", [2] = "target-2" });

        Assert.Contains("/production/pinned/site-v001", plan.PathsToRemove);
        Assert.Contains("/production/pinned/site-v001-connected", plan.PathsToRemove);
        Assert.DoesNotContain("/production/pinned/site-v002", plan.PathsToRemove);
        Assert.Contains("pointer-1", plan.CidsToUnpin);
        Assert.Contains("target-1", plan.CidsToUnpin);
        Assert.DoesNotContain("pointer-2", plan.CidsToUnpin);
        Assert.DoesNotContain("target-2", plan.CidsToUnpin);
    }

    [Fact]
    public void BuildPrunePlan_DoesNotUnpinTargetSharedWithRetainedVersion()
    {
        var pairs = IpnsVersionRetention.GroupVersions(
            "site",
            Children(
                ("site-v001", "pointer-1"),
                ("site-v001-connected", "shared-target"),
                ("site-v002", "pointer-2"),
                ("site-v002-connected", "shared-target")));

        var plan = IpnsVersionRetention.BuildPrunePlan(
            pairs,
            retainedVersion: 2,
            new Dictionary<int, string?> { [1] = "shared-target", [2] = "shared-target" });

        Assert.Contains("pointer-1", plan.CidsToUnpin);
        Assert.DoesNotContain("shared-target", plan.CidsToUnpin);
    }

    [Fact]
    public void BuildPrunePlan_RemovesOrphanConnectedFolderOutsideRetainedVersion()
    {
        var pairs = IpnsVersionRetention.GroupVersions(
            "site",
            Children(
                ("site-v002", "pointer-2"),
                ("site-v002-connected", "target-2"),
                ("site-v003-connected", "orphan-target")));

        var latest = IpnsVersionRetention.GetLatestPointerPair(pairs);
        Assert.NotNull(latest);
        Assert.Equal(2, latest.Version);

        var plan = IpnsVersionRetention.BuildPrunePlan(
            pairs,
            latest.Version,
            new Dictionary<int, string?> { [2] = "target-2", [3] = "orphan-target" });

        Assert.Contains("/production/pinned/site-v003-connected", plan.PathsToRemove);
        Assert.Contains("orphan-target", plan.CidsToUnpin);
    }

    [Theory]
    [InlineData("{"tgp":1,"current":"bafy-current"}", "bafy-current")]
    [InlineData("{"tgp":1,"current":"/ipfs/bafy-current"}", "bafy-current")]
    [InlineData("{"tgp":2,"current":"bafy-current"}", null)]
    [InlineData("not-json", null)]
    public void TryReadTgpCurrentCid_ParsesVersionOnePointer(string json, string? expected)
        => Assert.Equal(expected, IpnsVersionRetention.TryReadTgpCurrentCid(json));

    [Fact]
    public void TryReadLegacyTargetCid_PreservesSidecarCompatibility()
    {
        const string json = "{"Kind":"tgp-meta","PointerCid":"pointer","TargetCid":"/ipfs/legacy-target"}";
        Assert.Equal("legacy-target", IpnsVersionRetention.TryReadLegacyTargetCid(json));
    }

    private static Dictionary<string, (string Cid, string Path)> Children(
        params (string Name, string Cid)[] entries)
        => entries.ToDictionary(
            entry => entry.Name,
            entry => (entry.Cid, $"/production/pinned/{entry.Name}"),
            StringComparer.OrdinalIgnoreCase);
}
