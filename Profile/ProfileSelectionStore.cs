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

        var model = new ProfileSelectionData
        {
            ThemeChoice = string.IsNullOrWhiteSpace(themeChoice) ? "dark" : themeChoice,
            SelectedIds = selectedIds?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>(),
            UpdatedUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ProfileInitPaths.SelectionFile, json, new UTF8Encoding(false));
        File.WriteAllText(ProfileInitPaths.OwnerUserFile, Environment.UserName ?? string.Empty, new UTF8Encoding(false));

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

    public static void ClearLauncher()
    {
        try
        {
            using var runLm = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            runLm?.DeleteValue(RunValueName, false);
        }
        catch
        {
        }

        try
        {
            using var runCu = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            runCu?.DeleteValue(RunValueName, false);
        }
        catch
        {
        }
    }

    private static void EnsureRunEntry()
    {
        try
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(exe))
                return;

            var cmd = $"\"{exe}\" --force-run";
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
        var launcherLog = ProfileInitPaths.LauncherLogFile;
        var content = $@"
$ErrorActionPreference = 'SilentlyContinue'
$exe = '{exe.Replace("'", "''")}'
$ownerFile = '{ProfileInitPaths.OwnerUserFile.Replace("'", "''")}'
$launcherLog = '{launcherLog.Replace("'", "''")}'

function Log([string]$s) {{
  try {{
    New-Item -ItemType Directory -Path (Split-Path -Path $launcherLog -Parent) -Force | Out-Null
    Add-Content -Path $launcherLog -Encoding UTF8 -Value ('[{{0}}] [PROFILE] {{1}}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $s)
  }} catch {{}}
}}

if (-not (Test-Path -LiteralPath $exe)) {{ Log 'Skip: exe not found'; exit 0 }}
if (Get-Process -Name 'DeL1ThiSystem.ConfigurationWizard' -ErrorAction SilentlyContinue) {{ Log 'Skip: already running'; exit 0 }}

$applied = 0
try {{
  $applied = (Get-ItemProperty -Path 'HKCU:\Software\DeL1ThiSystem\Wizard' -Name 'ProfileInitApplied' -ErrorAction SilentlyContinue).ProfileInitApplied
}} catch {{}}
if ($applied -eq 1) {{ Log 'Skip: profile already applied for current user'; exit 0 }}

$ownerUser = ''
try {{
  if (Test-Path -LiteralPath $ownerFile) {{
    $ownerUser = (Get-Content -LiteralPath $ownerFile -ErrorAction SilentlyContinue | Select-Object -First 1).Trim()
  }}
}} catch {{}}

if (-not [string]::IsNullOrWhiteSpace($ownerUser) -and ($ownerUser -eq $env:USERNAME)) {{
  Log 'Skip: owner user session'
  exit 0
}}

Log 'Starting wizard process'
Start-Process -FilePath $exe -ArgumentList '--force-run'
";
        File.WriteAllText(ProfileInitPaths.LauncherScript, content.Trim() + Environment.NewLine, new UTF8Encoding(false));
    }
}
