namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public sealed class TweakNode
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string GroupId { get; set; } = "";
    public string GroupTitle { get; set; } = "";

    public bool IsChecked { get; set; } = false;
    public bool IsEnabled { get; set; } = true;

    public string AppliesTo { get; set; } = "";
    public string Stage { get; set; } = "tweak";
}
