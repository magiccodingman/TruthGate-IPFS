from pathlib import Path


def replace(path: str, old: str, new: str) -> None:
    file = Path(path)
    text = file.read_text(encoding="utf-8-sig")
    if old in text:
        file.write_text(text.replace(old, new), encoding="utf-8")


simple_prompt = "TruthGate-Web/TruthGate-Web/Components/Shared/SimplePromptDialog.razor"
replace(simple_prompt, '<MudForm @ref="_form" Validation="Validate">', '<MudForm @ref="_form">')
replace(
    simple_prompt,
    'Required="@Required"\n                           Lines=',
    'Required="@Required"\n                           Validation="@(new Func<string?, string?>(ValidateValue))"\n                           Lines=',
)
replace(
    simple_prompt,
    '''    private IEnumerable<string> Validate()
    {
        _error = null;
        if (Required && string.IsNullOrWhiteSpace(_value))
        {
            _error = "Please enter a value.";
            yield return _error;
        }
    }
''',
    '''    private string? ValidateValue(string? value)
    {
        _error = null;
        if (Required && string.IsNullOrWhiteSpace(value))
            return _error = "Please enter a value.";

        return null;
    }
''',
)
replace(simple_prompt, "await _form.Validate();", "await _form.ValidateAsync();")

add_pinned = "TruthGate-Web/TruthGate-Web/Components/Shared/AddPinnedItemDialog.razor"
replace(
    add_pinned,
    '<MudForm @ref="_form" Model="_model" Validation="Validate">',
    '<MudForm @ref="_form" Model="_model">',
)

add_ipns = "TruthGate-Web/TruthGate-Web/Components/Shared/AddOrEditIpnsDialog.razor"
replace(
    add_ipns,
    '<MudForm @ref="_form" Model="_model" Validation="Validate">',
    '<MudForm @ref="_form" Model="_model">',
)
replace(
    add_ipns,
    'Required="true"\n                       Disabled="_isEdit" />',
    'Required="true"\n                       Validation="@(new Func<string?, string?>(ValidateName))"\n                       Disabled="_isEdit" />',
)
replace(
    add_ipns,
    'Label="IPNS Key (k51… or /ipns/k51…)"\n                       @bind-Value="_model.Key"\n                       Immediate="true"\n                       Required="true"\n                       Validation="@(new Func<string?, string?>(ValidateName))"',
    'Label="IPNS Key (k51… or /ipns/k51…)"\n                       @bind-Value="_model.Key"\n                       Immediate="true"\n                       Required="true"\n                       Validation="@(new Func<string?, string?>(ValidateKey))"',
)
replace(
    add_ipns,
    '''    private IEnumerable<string> Validate()
    {
        _error = null;

        if (string.IsNullOrWhiteSpace(_model.Name))
            yield return "Name is required.";
        if (string.IsNullOrWhiteSpace(_model.Key))
            yield return "IPNS key is required.";

        var leaf = IpfsGateway.ToSafeLeaf(_model.Name);
        if (leaf is null)
            yield return "Invalid name.";
        if (!_model.Key.StartsWith("k51") && !_model.Key.StartsWith("/ipns/"))
            yield return "IPNS Key should look like k51… or /ipns/k51…";
    }
''',
    '''    private string? ValidateName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Name is required.";

        return IpfsGateway.ToSafeLeaf(value) is null ? "Invalid name." : null;
    }

    private static string? ValidateKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "IPNS key is required.";

        return value.StartsWith("k51", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("/ipns/k51", StringComparison.OrdinalIgnoreCase)
            ? null
            : "IPNS Key should look like k51… or /ipns/k51…";
    }
''',
)
replace(add_ipns, "private async void Submit()", "private async Task Submit()")
replace(add_ipns, "await _form.Validate();", "await _form.ValidateAsync();")
replace(
    add_ipns,
    '<MudGrid Spacing="2" AlignItems="AlignItems.Center">',
    '<MudGrid Spacing="2" Style="align-items:center;">',
)

search_ip = "TruthGate-Web/TruthGate-Web/Components/Pages/Settings/Shared/SearchIpDialog.razor"
replace(search_ip, '<MudForm @ref="_form" Validation="Validate">', '<MudForm @ref="_form">')
replace(
    search_ip,
    'Required="true"\n                               @onkeydown=',
    'Required="true"\n                               Validation="@(new Func<string?, string?>(ValidateIp))"\n                               @onkeydown=',
)
replace(
    search_ip,
    '''    private IEnumerable<string> Validate()
    {
        _error = null;
        if (string.IsNullOrWhiteSpace(_ip))
        {
            _error = "Please enter an IP address.";
            yield return _error;
            yield break;
        }
        if (!IPAddress.TryParse(_ip, out _))
        {
            _error = "Invalid IP format (v4 or v6 required).";
            yield return _error;
        }
    }
''',
    '''    private string? ValidateIp(string? value)
    {
        _error = null;
        if (string.IsNullOrWhiteSpace(value))
            return _error = "Please enter an IP address.";

        if (!IPAddress.TryParse(value, out _))
            return _error = "Invalid IP format (v4 or v6 required).";

        return null;
    }
''',
)
replace(search_ip, "await _form.Validate();", "await _form.ValidateAsync();")
replace(search_ip, 'Sortable="false"', 'SortMode="SortMode.None"')

ip_bans = "TruthGate-Web/TruthGate-Web/Components/Pages/Settings/IpBansPage.razor"
replace(ip_bans, 'Sortable="false"', 'SortMode="SortMode.None"')

domains = "TruthGate-Web/TruthGate-Web/Components/Pages/Settings/Domains.razor"
replace(
    domains,
    'Class="truncate-text" Title="@context.RedirectUrl"',
    'Class="truncate-text" title="@context.RedirectUrl"',
)
replace(
    domains,
    'Color="Color.Primary" Variant="Variant.Outlined" StartIcon="@Icons.Material.Filled.Link"',
    'Color="Color.Primary" Variant="Variant.Outlined" Icon="@Icons.Material.Filled.Link"',
)
replace(
    domains,
    'Color="Color.Primary" Variant="Variant.Outlined" StartIcon="@Icons.Material.Filled.Fingerprint"',
    'Color="Color.Primary" Variant="Variant.Outlined" Icon="@Icons.Material.Filled.Fingerprint"',
)
