using System.Collections.Generic;

namespace DeL1ThiSystem.ConfigurationWizard;

public sealed class WizardState
{
    public Dictionary<string, bool> Tweaks { get; } = new();

    public string ThemeChoice { get; set; } = "dark";

    public string OsFamily { get; set; } = OsInfo.DetectOsFamily();

    public bool BootstrapApplied { get; set; } = false;
}
