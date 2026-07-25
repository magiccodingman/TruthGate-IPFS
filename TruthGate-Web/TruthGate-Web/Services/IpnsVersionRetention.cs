using System.Globalization;
using System.Text.Json;

namespace TruthGate_Web.Services;

internal static class IpnsVersionRetention
{
    private const string ConnectedSuffix = "-connected";

    internal sealed record VersionEntry(
        int Version,
        string Name,
        string Path,
        string Cid,
        bool IsConnected);

    internal sealed class VersionPair
    {
        internal VersionPair(int version) => Version = version;

        internal int Version { get; }
        internal VersionEntry? Pointer { get; set; }
        internal VersionEntry? Connected { get; set; }
    }

    internal sealed record PrunePlan(
        IReadOnlyList<string> PathsToRemove,
        IReadOnlyList<string> CidsToUnpin);

    internal static bool TryParseEntryName(
        string trackedName,
        string entryName,
        out int version,
        out bool isConnected)
    {
        version = 0;
        isConnected = false;

        if (string.IsNullOrWhiteSpace(trackedName) || string.IsNullOrWhiteSpace(entryName))
            return false;

        var prefix = $"{trackedName}-v";
        if (!entryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var versionText = entryName[prefix.Length..];
        if (versionText.EndsWith(ConnectedSuffix, StringComparison.OrdinalIgnoreCase))
        {
            isConnected = true;
            versionText = versionText[..^ConnectedSuffix.Length];
        }

        return versionText.Length > 0
               && int.TryParse(
                   versionText,
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out version);
    }

    internal static IReadOnlyList<VersionPair> GroupVersions(
        string trackedName,
        IReadOnlyDictionary<string, (string Cid, string Path)> children)
    {
        var pairs = new SortedDictionary<int, VersionPair>();

        foreach (var child in children)
        {
            if (!TryParseEntryName(trackedName, child.Key, out var version, out var isConnected))
                continue;

            if (!pairs.TryGetValue(version, out var pair))
            {
                pair = new VersionPair(version);
                pairs.Add(version, pair);
            }

            var entry = new VersionEntry(
                version,
                child.Key,
                child.Value.Path,
                child.Value.Cid,
                isConnected);

            if (isConnected)
                pair.Connected = entry;
            else
                pair.Pointer = entry;
        }

        return pairs.Values.ToArray();
    }

    internal static VersionPair? GetLatestPointerPair(
        string trackedName,
        IReadOnlyDictionary<string, (string Cid, string Path)> children)
        => GetLatestPointerPair(GroupVersions(trackedName, children));

    internal static VersionPair? GetLatestPointerPair(IEnumerable<VersionPair> pairs)
        => pairs
            .Where(pair => pair.Pointer is not null)
            .OrderByDescending(pair => pair.Version)
            .FirstOrDefault();

    internal static int ComputeNextVersion(string trackedName, IEnumerable<string> childNames)
    {
        var max = 0;
        foreach (var childName in childNames)
        {
            if (TryParseEntryName(trackedName, childName, out var version, out _))
                max = Math.Max(max, version);
        }

        return max + 1;
    }

    internal static PrunePlan BuildPrunePlan(
        IReadOnlyList<VersionPair> pairs,
        int retainedVersion,
        IReadOnlyDictionary<int, string?> targetCidsByVersion)
    {
        var retained = pairs.FirstOrDefault(pair => pair.Version == retainedVersion)
                       ?? throw new ArgumentOutOfRangeException(
                           nameof(retainedVersion),
                           retainedVersion,
                           "The retained version must exist in the version set.");

        var retainedCids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddCid(retainedCids, retained.Pointer?.Cid);
        AddCid(retainedCids, retained.Connected?.Cid);
        if (targetCidsByVersion.TryGetValue(retainedVersion, out var retainedTarget))
            AddCid(retainedCids, retainedTarget);

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in pairs.Where(pair => pair.Version != retainedVersion))
        {
            AddPath(paths, pair.Pointer?.Path);
            AddPath(paths, pair.Connected?.Path);

            AddCandidateCid(cids, retainedCids, pair.Pointer?.Cid);
            AddCandidateCid(cids, retainedCids, pair.Connected?.Cid);
            if (targetCidsByVersion.TryGetValue(pair.Version, out var targetCid))
                AddCandidateCid(cids, retainedCids, targetCid);
        }

        return new PrunePlan(paths.ToArray(), cids.ToArray());
    }

    internal static string? TryReadTgpCurrentCid(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("tgp", out var tgp)
                || tgp.ValueKind != JsonValueKind.Number
                || tgp.GetInt32() != 1
                || !root.TryGetProperty("current", out var current)
                || current.ValueKind != JsonValueKind.String)
                return null;

            return NormalizeCid(current.GetString());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static string? TryReadLegacyTargetCid(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("TargetCid", out var target)
                || target.ValueKind != JsonValueKind.String)
                return null;

            return NormalizeCid(target.GetString());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? NormalizeCid(string? value)
    {
        var cid = value?.Trim();
        if (string.IsNullOrWhiteSpace(cid)) return null;
        return cid.StartsWith("/ipfs/", StringComparison.OrdinalIgnoreCase)
            ? cid[6..]
            : cid;
    }

    private static void AddPath(ISet<string> paths, string? path)
    {
        if (!string.IsNullOrWhiteSpace(path)) paths.Add(path);
    }

    private static void AddCid(ISet<string> cids, string? cid)
    {
        if (!string.IsNullOrWhiteSpace(cid)) cids.Add(cid.Trim());
    }

    private static void AddCandidateCid(ISet<string> candidates, ISet<string> retained, string? cid)
    {
        if (string.IsNullOrWhiteSpace(cid)) return;
        cid = cid.Trim();
        if (!retained.Contains(cid)) candidates.Add(cid);
    }
}
