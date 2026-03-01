using System;
using System.IO;
using System.Text;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static partial class TweakExecutor
{

    private static void InstallToolbox()
    {
        var toolboxExe = @"C:\Ghost Toolbox\toolbox.updater.x64.exe";
        var publicDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
        if (string.IsNullOrWhiteSpace(publicDesktop))
            publicDesktop = @"C:\Users\Public\Desktop";
        var toolboxShortcut = string.IsNullOrWhiteSpace(publicDesktop) ? string.Empty : Path.Combine(publicDesktop, "Toolbox.lnk");

        Log("TOOLBOX: Start InstallToolbox.");
        Log($"TOOLBOX: ToolboxExists={File.Exists(toolboxExe)}");
        Log($"TOOLBOX: ToolboxShortcutExists={(!string.IsNullOrWhiteSpace(toolboxShortcut) && File.Exists(toolboxShortcut))}");
        Log($"TOOLBOX: InternetAvailable={IsInternetAvailable()}");

        if (File.Exists(toolboxExe))
        {
            EnsureCommonDesktopShortcuts();
            Log("TOOLBOX: Toolbox already installed. Shortcut ensured.");
            return;
        }

var script = $@"
$ErrorActionPreference = 'Continue'
function Log([string]$s) {{ (""[{{0}}] [TOOLBOX] {{1}}"" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $s) | Out-File -FilePath '{LogPath}' -Append -Encoding UTF8 }}
function Test-Internet {{
  try {{ return [bool](Test-NetConnection -ComputerName '1.1.1.1' -InformationLevel Quiet -WarningAction SilentlyContinue) }}
  catch {{ return $false }}
}}
Log 'Starting toolbox install script.'
$attempt = 0
while ($true) {{
  $attempt++
  try {{
    while (-not (Test-Internet)) {{
      Log 'Waiting for internet before toolbox install...'
      Start-Sleep -Seconds 8
    }}
    Log (""Toolbox install attempt: {{0}}"" -f $attempt)
    irm https://system.del1t.me/setup_ghost.ps1 | iex
    Log 'Toolbox installer script executed'
    break
  }} catch {{
    Log (""Toolbox installer failed attempt {{0}}: {{1}}"" -f $attempt, $_.Exception.Message)
    Start-Sleep -Seconds 12
  }}
}}

$toolboxPath = ""C:\Ghost Toolbox\toolbox.updater.x64.exe""
Log (""Toolbox path exists: {{0}}"" -f (Test-Path $toolboxPath))

if (Test-Path $toolboxPath) {{
  Log 'Toolbox executable exists after install.'
}}
";

        RunPowerShell(script);
        EnsureCommonDesktopShortcuts();
        Log("TOOLBOX: End InstallToolbox.");
    }


    private static void ActivateHwid()
    {
        Log(">> ActivateHwid");
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
$Log   = Join-Path $Base 'Wizard.log'
$Marker = Join-Path $Base 'hwid_activated.marker'
$Hwid = Join-Path $Base 'HWID_Activation.cmd'

if (-not (Test-Path -LiteralPath $Base)) { New-Item -ItemType Directory -Path $Base -Force | Out-Null }

function Log([string]$s){
  (""[{0}] [HWID] {1}"" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $s) | Out-File -FilePath $Log -Append -Encoding UTF8
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
