using System;
using System.IO;

namespace DeL1ThiSystem.ConfigurationWizard.Profile;

public static class ProfileInitPaths
{
    public static string BaseDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "DeL1ThiSystem",
        "Wizard");

    public static string SelectionFile => Path.Combine(BaseDir, "profile_selection.json");
    public static string LauncherScript => Path.Combine(BaseDir, "RunProfileInit.ps1");
}
