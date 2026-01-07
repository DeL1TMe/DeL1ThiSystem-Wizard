using System;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard;

public static class OsInfo
{
    public static string DetectOsFamily()
    {
        try
        {
            int build = Environment.OSVersion.Version.Build;
            if (build >= 22000) return "11";

            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var productName = (key?.GetValue("ProductName") as string) ?? "";
            if (productName.Contains("Windows 11", StringComparison.OrdinalIgnoreCase)) return "11";
        }
        catch { }
        return "10";
    }
}
