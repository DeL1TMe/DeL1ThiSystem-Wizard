using System;
using System.Collections.Generic;

namespace DeL1ThiSystem.ConfigurationWizard.Profile;

public sealed class ProfileSelectionData
{
    public int Version { get; set; } = 1;
    public string ThemeChoice { get; set; } = "dark";
    public List<string> SelectedIds { get; set; } = new();
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
