using System;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard;

public static class OsInfo
{
    public static string DetectOsFamily()
    {
        // Returns "10" or "11" (fallback "10")
        try
        {
            // Windows 11 typically reports 10.0 build >= 22000
            int build = Environment.OSVersion.Version.Build;
            if (build >= 22000) return "11";

            // Extra safety: registry ProductName
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var productName = (key?.GetValue("ProductName") as string) ?? "";
            if (productName.Contains("Windows 11", StringComparison.OrdinalIgnoreCase)) return "11";
        }
        catch { }
        return "10";
    }
}
