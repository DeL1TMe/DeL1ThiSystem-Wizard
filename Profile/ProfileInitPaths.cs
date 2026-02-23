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
    public static string OwnerUserFile => Path.Combine(BaseDir, "profile_owner_user.txt");
    public static string LauncherLogFile => Path.Combine(BaseDir, "Wizard.log");
}
