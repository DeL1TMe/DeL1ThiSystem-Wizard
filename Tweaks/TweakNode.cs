using System.Collections.ObjectModel;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public sealed class TweakNode
{
    public TweakNode(string id, string title, string? description = null)
    {
        Id = id;
        Title = title;
        Description = description ?? string.Empty;
    }

    public TweakNode() { }

    public TweakNode Add(TweakNode child)
    {
        Children.Add(child);
        return this;
    }

    public TweakNode Add(params TweakNode[] children)
    {
        foreach (var c in children)
            Children.Add(c);
        return this;
    }

    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    public bool IsGroup { get; set; } = false;

    public bool IsChecked { get; set; } = false;
    public bool IsEnabled { get; set; } = true;

    public string AppliesTo { get; set; } = "";
    public string Stage { get; set; } = "tweak";

    public ObservableCollection<TweakNode> Children { get; } = new();
}
