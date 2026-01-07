using System.Collections.Generic;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public sealed class TweaksCatalogJson
{
    public int Version { get; set; } = 1;
    public List<TweakGroupJson> Groups { get; set; } = new();
}

public sealed class TweakGroupJson
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Note { get; set; }
    public List<TweakItemJson> Items { get; set; } = new();
}

public sealed class TweakItemJson
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> AppliesTo { get; set; } = new();
    public bool Default { get; set; } = false;
    public string Stage { get; set; } = "tweak";
    public bool OobePreferred { get; set; } = false;
}
