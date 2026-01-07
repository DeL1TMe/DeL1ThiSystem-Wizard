using System.Collections.ObjectModel;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public sealed class TweakNode
{
    // Fluent-builder convenience ctor (used by TweaksCatalog).
    public TweakNode(string id, string title, string? description = null)
    {
        Id = id;
        Title = title;
        Description = description ?? string.Empty;
    }

    // Parameterless ctor kept for serializers / XAML designers.
    public TweakNode() { }

    /// <summary>
    /// Fluent helper: adds a child node and returns the current node.
    /// </summary>
    public TweakNode Add(TweakNode child)
    {
        Children.Add(child);
        return this;
    }

    /// <summary>
    /// Fluent helper: adds multiple children and returns the current node.
    /// </summary>
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

    // Metadata
    public string AppliesTo { get; set; } = ""; // "10,11"
    public string Stage { get; set; } = "tweak";

    public ObservableCollection<TweakNode> Children { get; } = new();
}
