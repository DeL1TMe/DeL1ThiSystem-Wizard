using System.Collections.Generic;

namespace DeL1ThiSystem.ConfigurationWizard;

public sealed class WizardState
{
    // Selection state for tweaks (key: tweak id)
    public Dictionary<string, bool> Tweaks { get; } = new();

    // Theme choice: "light" / "dark"
    public string ThemeChoice { get; set; } = "dark";

    // Detected OS family: "10" / "11"
    public string OsFamily { get; set; } = OsInfo.DetectOsFamily();

    // Prevent double-run of bootstrap stage
    public bool BootstrapApplied { get; set; } = false;
}
