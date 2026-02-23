using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static partial class TweakExecutor
{
    private const string ToolboxExePath = @"C:\Ghost Toolbox\toolbox.updater.x64.exe";
    private const string AomeiExePath = @"C:\Program Files (x86)\AOMEI\AOMEI Backupper\8.0.0\Backupper.exe";
    private const string UninstallToolExePath = @"C:\Program Files\Uninstall Tool\UninstallTool.exe";
    private const string SevenZipExePath = @"C:\Program Files\7-Zip\7zFM.exe";
    private const string RustDeskExePath = @"C:\Users\Public\Desktop\RustDesk.exe";


    private static void RemoveAppxPackages()
    {
        RunPowerShell(BuildUwpRemovalScript(0));
        RemoveWindowsStore();
        DisableCortana();
        DisableCopilotEverywhere();
        CreateUwpAutoRemovalTasks();
    }


    private static void CreateUwpAutoRemovalTasks()
    {
        var cleanupTasks = @"
$tasks = @('DeL1ThiSystem\AutoRemoveUwpShellCore','DeL1ThiSystem\AutoRemoveUwpLicensing');
foreach ($t in $tasks) {
  schtasks /Delete /TN $t /F >$null 2>$null
}";
        var removalScript = BuildUwpRemovalScript(180) + cleanupTasks;

        var script = $@"
$root = Join-Path $env:ProgramData 'DeL1ThiSystem\Wizard';
$path = Join-Path $root 'AutoRemoveUwp.ps1';
New-Item -ItemType Directory -Path $root -Force | Out-Null;

$ps = @'
{removalScript}
'@;

$ps | Set-Content -LiteralPath $path -Encoding UTF8;

$tr = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File ""' + $path + '""';

$task1 = 'DeL1ThiSystem\AutoRemoveUwpShellCore';
$task2 = 'DeL1ThiSystem\AutoRemoveUwpLicensing';

schtasks /Delete /TN $task1 /F >$null 2>$null;
schtasks /Delete /TN $task2 /F >$null 2>$null;

$mo1 = ""*[System[Provider[@Name='Microsoft-Windows-Shell-Core'] and (EventID=62144)]]"";
$mo2 = ""*[System[Provider[@Name='Microsoft-Client-Licensing-Platform'] and (EventID=116)] and EventData[Data[@Name='PackageName']='Microsoft.Windows.DevHome_8wekyb3d8bbwe' or Data[@Name='PackageName']='A025C540.Yandex.Music_vfvw9svesycw6' or Data[@Name='PackageName']='Microsoft.OutlookForWindows_8wekyb3d8bbwe']]"";

schtasks /Create /F /TN $task1 /RU SYSTEM /RL HIGHEST /SC ONEVENT /EC 'Microsoft-Windows-Shell-Core/Operational' /MO $mo1 /TR $tr | Out-Null;
schtasks /Create /F /TN $task2 /RU SYSTEM /RL HIGHEST /SC ONEVENT /EC 'Microsoft-Client-Licensing-Platform/Admin' /MO $mo2 /TR $tr | Out-Null;
";
        RunPowerShell(script);
    }


    private static string BuildUwpRemovalScript(int delaySeconds)
    {
        var selectors = string.Join(", ", AppxSelectors.Select(s => $"'{s}'"));
        var delay = delaySeconds > 0 ? $"Start-Sleep -Seconds {delaySeconds}\n" : string.Empty;
        return $@"
{delay}$selectors = @({selectors});
$escaped = $selectors | ForEach-Object {{ [regex]::Escape($_) }};
$pattern = '^(' + ($escaped -join '|') + ')';
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator);
Write-Output ('UWP removal: Admin=' + $isAdmin + ', User=' + $env:USERNAME);
$prov = Get-AppxProvisionedPackage -Online;
$provMatches = $prov | Where-Object {{ $_.DisplayName -match $pattern -or $_.PackageName -match $pattern }};
Write-Output ('UWP provisioned matches: ' + $provMatches.Count);
if ($provMatches.Count -gt 0) {{
  Write-Output ('UWP provisioned sample: ' + (($provMatches | Select-Object -First 10 -ExpandProperty PackageName) -join '; '));
}}
foreach ($p in $provMatches) {{
  Remove-AppxProvisionedPackage -Online -PackageName $p.PackageName -ErrorAction Continue | Out-Null;
}}

$installedAll = @();
try {{ $installedAll = Get-AppxPackage -AllUsers -ErrorAction Stop }} catch {{
  Write-Output ('Get-AppxPackage -AllUsers failed: ' + $_.Exception.Message);
  $installedAll = @()
}}
$installedCurrent = Get-AppxPackage -ErrorAction SilentlyContinue;
$installed = @($installedAll + $installedCurrent) | Sort-Object PackageFullName -Unique;
$hasAllUsers = (Get-Command Remove-AppxPackage).Parameters.ContainsKey('AllUsers');
$installedMatches = $installed | Where-Object {{ $_.Name -match $pattern -or $_.PackageFamilyName -match $pattern }};
Write-Output ('UWP installed matches: ' + $installedMatches.Count);
if ($installedMatches.Count -gt 0) {{
  Write-Output ('UWP installed sample: ' + (($installedMatches | Select-Object -First 10 -ExpandProperty PackageFullName) -join '; '));
}}
foreach ($pkg in $installedMatches) {{
  if ($hasAllUsers) {{ Remove-AppxPackage -AllUsers -Package $pkg.PackageFullName -ErrorAction Continue }}
  else {{ Remove-AppxPackage -Package $pkg.PackageFullName -ErrorAction Continue }}
}}

$outlook = 'Microsoft.OutlookForWindows';
Get-AppxPackage -AllUsers -Name $outlook | ForEach-Object {{
  Write-Output ('Remove installed (Outlook): ' + $_.PackageFullName);
  if ($hasAllUsers) {{ Remove-AppxPackage -AllUsers -Package $_.PackageFullName -ErrorAction Continue }}
  else {{ Remove-AppxPackage -Package $_.PackageFullName -ErrorAction Continue }}
}}
Get-AppxProvisionedPackage -Online | Where-Object {{ $_.DisplayName -eq $outlook -or $_.PackageName -match ('^' + [regex]::Escape($outlook)) }} | ForEach-Object {{
  Write-Output ('Remove provisioned (Outlook): ' + $_.PackageName);
  Remove-AppxProvisionedPackage -Online -PackageName $_.PackageName -ErrorAction Continue | Out-Null;
}}";
    }


    private static void RemoveCapabilities()
    {
        var selectors = string.Join(",", CapabilitySelectors.Select(s => $"'{s}'"));
        var script =
            "$selectors = @(" + selectors + ");" +
            "Get-WindowsCapability -Online | " +
            "Where-Object { $_.State -notin @('NotPresent','Removed') -and $selectors -contains (($_.Name -split '~')[0]) } | " +
            "Remove-WindowsCapability -Online -ErrorAction Continue;";
        RunPowerShell(script);
    }


    private static void RemoveFeatures()
    {
        var selectors = string.Join(",", FeatureSelectors.Select(s => $"'{s}'"));
        var script =
            "$selectors = @(" + selectors + ");" +
            "Get-WindowsOptionalFeature -Online | " +
            "Where-Object { $_.State -notin @('Disabled','DisabledWithPayloadRemoved') -and $selectors -contains $_.FeatureName } | " +
            "Disable-WindowsOptionalFeature -Online -Remove -NoRestart -ErrorAction Continue;";
        RunPowerShell(script);
    }


    private static void RemoveOneDriveArtifacts()
    {
        TryDelete(@"C:\Users\Default\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\OneDrive.lnk");
        var oneDrive32 = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\\System32\\OneDriveSetup.exe");
        var oneDrive64 = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\\SysWOW64\\OneDriveSetup.exe");
        if (File.Exists(oneDrive32))
            RunProcess("cmd.exe", "/c \"" + oneDrive32 + "\" /uninstall");
        else
            Log($"OneDriveSetup missing: {oneDrive32}");
        if (File.Exists(oneDrive64))
            RunProcess("cmd.exe", "/c \"" + oneDrive64 + "\" /uninstall");
        else
            Log($"OneDriveSetup missing: {oneDrive64}");
        if (File.Exists(@"C:\Windows\System32\OneDriveSetup.exe"))
            ForceDelete(@"C:\Windows\System32\OneDriveSetup.exe");
        if (File.Exists(@"C:\Windows\SysWOW64\OneDriveSetup.exe"))
            ForceDelete(@"C:\Windows\SysWOW64\OneDriveSetup.exe");
    }


    private static void MakeEdgeUninstallable()
    {
        var paths = new[]
        {
            @"C:\Windows\System32\IntegratedServicesRegionPolicySet.json",
            @"C:\Windows\SysWOW64\IntegratedServicesRegionPolicySet.json"
        };

        bool updatedAny = false;
        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                Log($"Edge policy missing: {path}");
                continue;
            }

            if (TryPatchEdgeUninstallPolicy(path))
                updatedAny = true;
        }

        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "UninstallAllowed", 1);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "AllowUninstall", 1);

        const string edgeUninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge";
        SetDwordIfKeyExists(RegistryHive.LocalMachine, RegistryView.Registry64, edgeUninstallKey, "NoRemove", 0);
        SetDwordIfKeyExists(RegistryHive.LocalMachine, RegistryView.Registry64, edgeUninstallKey, "SystemComponent", 0);
        SetDwordIfKeyExists(RegistryHive.LocalMachine, RegistryView.Registry32, edgeUninstallKey, "NoRemove", 0);
        SetDwordIfKeyExists(RegistryHive.LocalMachine, RegistryView.Registry32, edgeUninstallKey, "SystemComponent", 0);

        if (!updatedAny)
            Log("Edge policy not updated: GUID not found or file not writable.");
    }


    private static bool TryPatchEdgeUninstallPolicy(string path)
    {
        const string edgeGuid = "{1bca278a-5d11-4acf-ad2f-f9ab6d7f93a6}";
        try
        {
            EnsureFileWritable(path);
            var node = JsonNode.Parse(File.ReadAllText(path));
            var policies = node?["policies"]?.AsArray();
            if (policies == null)
                return false;

            bool updated = false;
            foreach (var item in policies)
            {
                if ((string?)item?["guid"] == edgeGuid)
                {
                    if (item is JsonObject obj)
                    {
                        obj["defaultState"] = "enabled";
                        obj["conditions"] = new JsonObject();
                    }
                    else
                    {
                        item!["defaultState"] = "enabled";
                    }
                    updated = true;
                }
            }

            if (!updated)
            {
                Log($"Edge policy GUID not found in: {path}");
                return false;
            }

            var json = node!.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception ex)
        {
            Log($"Edge policy update failed ({path}): {ex.Message}");
            return false;
        }
    }

    private static void RemoveWindowsStore()
    {
        var script = @"
$names = @('Microsoft.WindowsStore','Microsoft.StorePurchaseApp');
$prov = Get-AppxProvisionedPackage -Online;
foreach ($n in $names) {
  $prov | Where-Object { $_.DisplayName -eq $n } | ForEach-Object {
    Remove-AppxProvisionedPackage -Online -PackageName $_.PackageName -ErrorAction Continue | Out-Null;
  }
}
$installed = Get-AppxPackage -AllUsers;
$hasAllUsers = (Get-Command Remove-AppxPackage).Parameters.ContainsKey('AllUsers');
foreach ($n in $names) {
  $installed | Where-Object { $_.Name -like ($n + '*') -or $_.PackageFamilyName -like ($n + '*') } | ForEach-Object {
    if ($hasAllUsers) { Remove-AppxPackage -AllUsers -Package $_.PackageFullName -ErrorAction Continue }
    else { Remove-AppxPackage -Package $_.PackageFullName -ErrorAction Continue }
  }
}
";
        RunPowerShell(script);
    }

    private static void DisableCortana()
    {
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCortanaButton", 0);
    }


    private static void InstallAppsFromFolder()
    {
        const string appsPath = @"C:\Apps";
        var logPath = Path.Combine(BaseDir, "Wizard.log");

        void LogApp(string message)
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n", new UTF8Encoding(false));
            }
            catch
            {
            }
        }

        try { Directory.CreateDirectory(BaseDir); } catch { }

        if (!Directory.Exists(appsPath))
        {
            LogApp($"Apps folder not found: {appsPath}");
            EnsureCommonDesktopShortcuts();
            return;
        }

        LogApp($"InternetAvailable(start)={IsInternetAvailable()}");
        if (IsInternetAvailable())
        {
            LogApp("Internet OK. Installing 7-Zip (if missing) and downloading RustDesk (if missing).");
            InstallSevenZipIfMissing();
            DownloadRustDesk();
        }
        else
        {
            LogApp("Internet not available. Skip 7-Zip and RustDesk download.");
        }
        LogApp($"InternetAvailable(after-download)={IsInternetAvailable()}");

        var mapPath = Path.Combine(appsPath, "install-args.json");
        var argMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(mapPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(mapPath));
                foreach (var prop in doc.RootElement.EnumerateObject())
                    argMap[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                LogApp($"Failed to read install-args.json: {ex.Message}");
            }
        }

        var files = Directory.GetFiles(appsPath)
            .Select(path => new FileInfo(path))
            .Where(f => f.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                        || f.Extension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Name.Equals("install-apps.ps1", StringComparison.OrdinalIgnoreCase)
                        && !f.Name.Equals("install-args.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Name)
            .ToList();

        foreach (var f in files)
        {
            var name = f.Name;
            var ext = f.Extension.ToLowerInvariant();
            var args = argMap.TryGetValue(name, out var custom) ? custom : string.Empty;

            string fileName;
            string finalArgs;

            if (ext == ".msi")
            {
                fileName = "msiexec.exe";
                if (string.IsNullOrWhiteSpace(args))
                    finalArgs = $"/i \"{f.FullName}\" /qn /norestart";
                else if (args.TrimStart().StartsWith("/i", StringComparison.OrdinalIgnoreCase))
                    finalArgs = args;
                else
                    finalArgs = $"/i \"{f.FullName}\" {args}";
            }
            else
            {
                fileName = f.FullName;
                if (string.IsNullOrWhiteSpace(args))
                {
                    if (name.StartsWith("Uninstall.Tool", StringComparison.OrdinalIgnoreCase))
                        finalArgs = "/S /I";
                    else
                        finalArgs = "/S";
                }
                else
                {
                    finalArgs = args;
                }
            }

            if (!ShouldRunInstaller(name))
            {
                LogApp($"Skip installer (already installed): {name}");
                continue;
            }

            try
            {
                LogApp($"Running: {name} | Args: {finalArgs}");
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = finalArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit();
                LogApp($"ExitCode ({name}): {proc?.ExitCode}");
            }
            catch (Exception ex)
            {
                LogApp($"ERROR running {name}: {ex}");
            }
        }

        EnsureCommonDesktopShortcuts();
        LogApp("=== Install-Apps finished ===");
    }

    private static void EnsureCommonDesktopShortcuts()
    {
        var script = @"
try {
  $w = New-Object -ComObject WScript.Shell
  $publicDesktop = [Environment]::GetFolderPath('CommonDesktopDirectory')
  if ([string]::IsNullOrWhiteSpace($publicDesktop)) { $publicDesktop = 'C:\Users\Public\Desktop' }
  if ([string]::IsNullOrWhiteSpace($publicDesktop)) { return }
  New-Item -ItemType Directory -Path $publicDesktop -Force | Out-Null

  $userDesktop = [Environment]::GetFolderPath('Desktop')

  function Remove-UserDuplicate([string]$name, [string]$target) {
    if ([string]::IsNullOrWhiteSpace($userDesktop)) { return }
    $userShortcut = Join-Path $userDesktop ($name + '.lnk')
    if (-not (Test-Path -LiteralPath $userShortcut)) { return }
    try {
      $existing = $w.CreateShortcut($userShortcut)
      if ($existing -and $existing.TargetPath -eq $target) {
        Remove-Item -LiteralPath $userShortcut -Force -ErrorAction SilentlyContinue
      }
    } catch {}
  }

  function Ensure-Shortcut([string]$name, [string]$target, [string]$workDir) {
    if (-not (Test-Path -LiteralPath $target)) { return }
    $shortcutPath = Join-Path $publicDesktop ($name + '.lnk')
    if (Test-Path -LiteralPath $shortcutPath) {
      try {
        $existing = $w.CreateShortcut($shortcutPath)
        if ($existing -and $existing.TargetPath -eq $target) {
          Remove-UserDuplicate -name $name -target $target
          return
        }
      } catch {}
      Remove-Item -LiteralPath $shortcutPath -Force -ErrorAction SilentlyContinue
    }
    $lnk = $w.CreateShortcut($shortcutPath)
    $lnk.TargetPath = $target
    $lnk.WorkingDirectory = $workDir
    $lnk.IconLocation = $target
    $lnk.Save()
    Remove-UserDuplicate -name $name -target $target
  }

  Ensure-Shortcut -name 'Toolbox' -target 'C:\Ghost Toolbox\toolbox.updater.x64.exe' -workDir 'C:\Ghost Toolbox'
  Ensure-Shortcut -name 'Uninstall Tool' -target 'C:\Program Files\Uninstall Tool\UninstallTool.exe' -workDir 'C:\Program Files\Uninstall Tool'
  Ensure-Shortcut -name 'AOMEI Backupper' -target 'C:\Program Files (x86)\AOMEI\AOMEI Backupper\8.0.0\Backupper.exe' -workDir 'C:\Program Files (x86)\AOMEI\AOMEI Backupper\8.0.0'

  foreach ($legacy in @('UninstallTool.lnk')) {
    foreach ($desktopPath in @($publicDesktop, $userDesktop)) {
      if ([string]::IsNullOrWhiteSpace($desktopPath)) { continue }
      $legacyPath = Join-Path $desktopPath $legacy
      if (Test-Path -LiteralPath $legacyPath) { Remove-Item -LiteralPath $legacyPath -Force -ErrorAction SilentlyContinue }
    }
  }
} catch {
}
";
        RunPowerShell(script);
    }


    private static void InstallSevenZipIfMissing()
    {
        if (File.Exists(SevenZipExePath))
            return;

        var script = @"
if (-not (Test-Path -LiteralPath 'C:\Program Files\7-Zip\7zFM.exe')) {
  $url = 'https://www.7-zip.org/a/7z2301-x64.exe'
  $installer = Join-Path $env:TEMP '7z.exe'
  try {
    Invoke-WebRequest -Uri $url -OutFile $installer -UseBasicParsing
    Start-Process -FilePath $installer -ArgumentList '/S' -Wait
  } catch {
  }
}
";
        RunPowerShell(script);
    }


    private static void DownloadRustDesk()
    {
        if (File.Exists(RustDeskExePath))
            return;

        var script = @"
try {
  $releaseUrl = 'https://api.github.com/repos/rustdesk/rustdesk/releases/latest'
  $releaseData = Invoke-RestMethod -Uri $releaseUrl -UseBasicParsing
  $asset = $releaseData.assets | Where-Object { ($_.name -match 'x86_64') -and ($_.name -match 'exe') } | Select-Object -First 1
  if (-not $asset) { throw 'No asset found' }
  $publicDesktop = 'C:\Users\Public\Desktop'
  $outputFile = Join-Path $publicDesktop 'RustDesk.exe'
  Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $outputFile -UseBasicParsing
} catch {
}
";
        RunPowerShell(script);
    }

    private static bool ShouldRunInstaller(string installerName)
    {
        if (installerName.IndexOf("aomei", StringComparison.OrdinalIgnoreCase) >= 0 ||
            installerName.IndexOf("backupper", StringComparison.OrdinalIgnoreCase) >= 0)
            return !File.Exists(AomeiExePath);

        if (installerName.IndexOf("uninstall", StringComparison.OrdinalIgnoreCase) >= 0 &&
            installerName.IndexOf("tool", StringComparison.OrdinalIgnoreCase) >= 0)
            return !File.Exists(UninstallToolExePath);

        if (installerName.IndexOf("7z", StringComparison.OrdinalIgnoreCase) >= 0 ||
            installerName.IndexOf("7-zip", StringComparison.OrdinalIgnoreCase) >= 0)
            return !File.Exists(SevenZipExePath);

        if (installerName.IndexOf("rustdesk", StringComparison.OrdinalIgnoreCase) >= 0)
            return !File.Exists(RustDeskExePath);

        return true;
    }

}
