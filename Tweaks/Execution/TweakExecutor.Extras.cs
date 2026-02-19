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

    private static void InstallToolbox()
    {
        var logPath = Path.Combine(BaseDir, "Install-Toolbox.log");
        void LogToolbox(string message)
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n");
            }
            catch
            {
            }
        }

        LogToolbox("Start InstallToolbox.");
        LogToolbox($"InternetAvailable={IsInternetAvailable()}");

        var script = $@"
$ErrorActionPreference = 'Continue'
function Log([string]$s) {{ (""[{{0}}] {{1}}"" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $s) | Out-File -FilePath '{logPath}' -Append -Encoding UTF8 }}
Log 'Starting toolbox install script.'
try {{
  $ps = Start-Process powershell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command ""irm https://system.del1t.me/setup_ghost.ps1 | iex""' -PassThru -WindowStyle Hidden
  $ps.WaitForExit()
  Log (""Toolbox installer exit code: {{0}}"" -f $ps.ExitCode)
}} catch {{
  Log (""Toolbox installer failed: {{0}}"" -f $_.Exception.Message)
}}

$toolboxPath = ""C:\Ghost Toolbox\toolbox.updater.x64.exe""
Log (""Toolbox path exists: {{0}}"" -f (Test-Path $toolboxPath))

if (Test-Path $toolboxPath) {{
  try {{
    $WScriptShell = New-Object -ComObject WScript.Shell
    $desktopPath = [Environment]::GetFolderPath(""Desktop"")
    $shortcutPath = ""$desktopPath\Toolbox.lnk""
    $shortcut = $WScriptShell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $toolboxPath
    $shortcut.WorkingDirectory = ""C:\Ghost Toolbox""
    $shortcut.IconLocation = $toolboxPath
    $shortcut.Save()
    Log (""Shortcut created: {{0}}"" -f $shortcutPath)
  }} catch {{
    Log (""Shortcut creation failed: {{0}}"" -f $_.Exception.Message)
  }}
}}
";

        RunPowerShell(script);
        LogToolbox("End InstallToolbox.");
    }


    private static void ActivateHwid()
    {
        var marker = Path.Combine(BaseDir, "hwid_activated.marker");
        var hwidCmd = Path.Combine(BaseDir, "HWID_Activation.cmd");
        var helperPs = Path.Combine(BaseDir, "ActivateWhenOnline.ps1");
        var taskName = @"DeL1ThiSystem\HWIDActivation";

        if (File.Exists(marker))
        {
            Log("HWID: marker exists, skipping.");
            return;
        }

        EnsureHwidFiles(hwidCmd, helperPs);

        if (!File.Exists(hwidCmd))
        {
            Log("HWID: script missing, cannot activate.");
            return;
        }

        if (IsInternetAvailable())
        {
            Log("HWID: internet OK, running activation...");
            var args = $"/c \"\"{hwidCmd}\" /HWID <nul\"";
            var exitCode = RunProcessWithTimeout("cmd.exe", args, 180000, out var timedOut);
            if (timedOut)
            {
                Log("HWID: activation timeout -> killed.");
                return;
            }
            Log($"HWID: activation finished. ExitCode={exitCode}");
            try { File.WriteAllText(marker, "ok", Encoding.ASCII); } catch { }
            RunProcess("schtasks.exe", $"/Change /TN \"{taskName}\" /Disable");
            return;
        }

        Log("HWID: no internet, creating retry task.");
        var psExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            @"System32\WindowsPowerShell\v1.0\powershell.exe");
        var taskArgs =
            $"/Create /F /TN \"{taskName}\" /RU \"SYSTEM\" /RL HIGHEST /SC ONLOGON /DELAY 0000:30 " +
            $"/TR \"\\\"{psExe}\\\" -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \\\"{helperPs}\\\"\"";
        RunProcess("schtasks.exe", taskArgs);
    }


    private static void EnsureHwidFiles(string hwidCmd, string helperPs)
    {
        try { Directory.CreateDirectory(BaseDir); } catch { }

        if (!File.Exists(hwidCmd))
        {
            var content = DecodeHwidActivation();
            if (!string.IsNullOrWhiteSpace(content))
                File.WriteAllText(hwidCmd, content, Encoding.ASCII);
        }

        if (!File.Exists(helperPs))
            File.WriteAllText(helperPs, GetActivateWhenOnlineScript(), Encoding.UTF8);
    }


    private static string? DecodeHwidActivation()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(HwidActivationData.Base64))
                return null;
            var bytes = Convert.FromBase64String(HwidActivationData.Base64);
            return Encoding.ASCII.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }


    private static string GetActivateWhenOnlineScript()
    {
        return @"
$ErrorActionPreference = 'SilentlyContinue'

$Brand = 'DeL1ThiSystem'
$Base  = Join-Path $env:ProgramData ""$Brand\Wizard""
$Log   = Join-Path $Base 'ActivateWhenOnline.log'
$Marker = Join-Path $Base 'hwid_activated.marker'
$Hwid = Join-Path $Base 'HWID_Activation.cmd'

if (-not (Test-Path -LiteralPath $Base)) { New-Item -ItemType Directory -Path $Base -Force | Out-Null }

function Log([string]$s){
  (""[{0}] {1}"" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $s) | Out-File -FilePath $Log -Append -Encoding UTF8
}

function Test-Internet {
  try { return [bool](Test-NetConnection -ComputerName '1.1.1.1' -InformationLevel Quiet -WarningAction SilentlyContinue) }
  catch { return $false }
}

try {
  if (Test-Path -LiteralPath $Marker) { Log 'Marker exists -> already activated. Exiting.'; exit 0 }
  if (-not (Test-Path -LiteralPath $Hwid)) { Log 'HWID_Activation.cmd not found. Exiting.'; exit 0 }

  Log 'Waiting for internet...'
  $deadline = (Get-Date).AddHours(6)
  while ((Get-Date) -lt $deadline) {
    if (Test-Internet) { break }
    Start-Sleep -Seconds 10
  }
  if (-not (Test-Internet)) { Log 'Internet not available within deadline -> exit.'; exit 0 }

  Log 'Internet OK. Running HWID activation (timeout 180s)...'
  $args = '/c ""' + $Hwid + '"" /HWID <nul'
  $p = Start-Process -FilePath ""$env:SystemRoot\System32\cmd.exe"" -ArgumentList $args -PassThru -WindowStyle Hidden
  if (-not $p.WaitForExit(180000)) {
    try { $p.Kill() } catch {}
    Log 'HWID activation timeout -> killed'
    exit 0
  }
  Log (""HWID activation finished. ExitCode={0}"" -f $p.ExitCode)

  New-Item -ItemType File -Path $Marker -Force | Out-Null
  try { schtasks /Change /TN ""$Brand\HWIDActivation"" /Disable | Out-Null } catch {}
} catch {
  Log (""Unhandled: {0}"" -f $_.Exception.Message)
}
";
    }

}
