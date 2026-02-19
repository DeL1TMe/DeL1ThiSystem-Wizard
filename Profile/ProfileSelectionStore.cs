using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard.Profile;

public static class ProfileSelectionStore
{
    private const string RunValueName = "DeL1ThiSystemProfileInit";

    public static void Save(string themeChoice, IEnumerable<string> selectedIds)
    {
        Directory.CreateDirectory(ProfileInitPaths.BaseDir);

        var ids = selectedIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(ProfileTweakPolicy.IsProfileApplicable)
            .ToList();

        if (!ids.Contains("ui.color_theme", StringComparer.OrdinalIgnoreCase))
            ids.Insert(0, "ui.color_theme");

        var model = new ProfileSelectionData
        {
            ThemeChoice = string.IsNullOrWhiteSpace(themeChoice) ? "dark" : themeChoice,
            SelectedIds = ids,
            UpdatedUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ProfileInitPaths.SelectionFile, json, new UTF8Encoding(false));

        EnsureLauncherScript();
        EnsureRunEntry();
    }

    public static ProfileSelectionData? Load()
    {
        try
        {
            if (!File.Exists(ProfileInitPaths.SelectionFile))
                return null;
            var json = File.ReadAllText(ProfileInitPaths.SelectionFile, Encoding.UTF8);
            return JsonSerializer.Deserialize<ProfileSelectionData>(json);
        }
        catch
        {
            return null;
        }
    }

    public static bool IsAppliedForCurrentUser()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\DeL1ThiSystem\Wizard", true);
            var value = key?.GetValue("ProfileInitApplied");
            return value is int i && i == 1;
        }
        catch
        {
            return false;
        }
    }

    public static void MarkAppliedForCurrentUser()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\DeL1ThiSystem\Wizard", true);
            key?.SetValue("ProfileInitApplied", 1, RegistryValueKind.DWord);
            key?.SetValue("ProfileInitAppliedUtc", DateTime.UtcNow.ToString("O"), RegistryValueKind.String);
        }
        catch
        {
        }
    }

    private static void EnsureRunEntry()
    {
        try
        {
            var ps = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                @"System32\WindowsPowerShell\v1.0\powershell.exe");
            var cmd = $"\"{ps}\" -NoProfile -ExecutionPolicy Bypass -File \"{ProfileInitPaths.LauncherScript}\"";
            using var run = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            run?.SetValue(RunValueName, cmd, RegistryValueKind.String);
        }
        catch
        {
        }
    }

    private static void EnsureLauncherScript()
    {
        var exe = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        var content = $@"
$ErrorActionPreference = 'SilentlyContinue'
$exe = '{exe.Replace("'", "''")}'
$selection = '{ProfileInitPaths.SelectionFile.Replace("'", "''")}'

if (-not (Test-Path -LiteralPath $exe)) {{ exit 0 }}
if (-not (Test-Path -LiteralPath $selection)) {{ exit 0 }}

$applied = 0
try {{
  $applied = (Get-ItemProperty -Path 'HKCU:\Software\DeL1ThiSystem\Wizard' -Name 'ProfileInitApplied' -ErrorAction SilentlyContinue).ProfileInitApplied
}} catch {{}}
if ($applied -eq 1) {{ exit 0 }}

Start-Process -FilePath $exe -ArgumentList '--profile-init'
";
        File.WriteAllText(ProfileInitPaths.LauncherScript, content.Trim() + Environment.NewLine, new UTF8Encoding(false));
    }
}
