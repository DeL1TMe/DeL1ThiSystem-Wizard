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
    private static void DisableDefenderNotifications()
    {
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications", "DisableNotifications", 1);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Systray", "HideSystray", 1);
    }


    private static void DisableSmartScreen()
    {
        SetString(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "Off");
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "ServiceEnabled", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "NotifyMalicious", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "NotifyPasswordReuse", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "NotifyUnsafeApp", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Edge", "SmartScreenEnabled", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Edge", "SmartScreenPuaEnabled", 0);
    }


    private static void DisableWebContentEvaluation()
    {
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AppHost", "PreventOverride", 0);
    }


    private static void EnableRemoteAssistance()
    {
        SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Remote Assistance", "fAllowToGetHelp", 1);
    }


    private static void EnableRdp()
    {
        RunProcess("netsh.exe", "advfirewall firewall set rule group=\"@FirewallAPI.dll,-28752\" new enable=Yes");
        SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0);
    }


    private static void ApplyVisualFxProfile()
    {
        var key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
        SetDword(RegistryHive.LocalMachine, $@"{key}\ControlAnimations", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\AnimateMinMax", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\TaskbarAnimations", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\DWMAeroPeekEnabled", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\MenuAnimation", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\TooltipAnimation", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\SelectionFade", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\DWMSaveThumbnailEnabled", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\CursorShadow", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\ListviewShadow", "DefaultValue", 1);
        SetDword(RegistryHive.LocalMachine, $@"{key}\ThumbnailsOrIcon", "DefaultValue", 1);
        SetDword(RegistryHive.LocalMachine, $@"{key}\ListviewAlphaSelect", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\DragFullWindows", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\ComboBoxAnimation", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\FontSmoothing", "DefaultValue", 1);
        SetDword(RegistryHive.LocalMachine, $@"{key}\ListBoxSmoothScrolling", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\DropShadow", "DefaultValue", 1);
    }


    private static void DisableStickyKeys()
    {
        SetString(RegistryHive.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "10");
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(@".DEFAULT\Control Panel\Accessibility\StickyKeys", true);
        key?.SetValue("Flags", "10", RegistryValueKind.String);
        SetDefaultUserString(@"Control Panel\Accessibility\StickyKeys", "Flags", "10");
    }


    private static void DisableEnhancePointerPrecision()
    {
        SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0");
        SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0");
        SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0");
        WithDefaultUserHive(root =>
        {
            using var mouse = root.CreateSubKey(@"Control Panel\Mouse", true);
            if (mouse == null)
                return;
            mouse.SetValue("MouseSpeed", "0", RegistryValueKind.String);
            mouse.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
            mouse.SetValue("MouseThreshold2", "0", RegistryValueKind.String);
        });
    }


    private static void SetWallpaperQuality100()
    {
        SetDword(RegistryHive.CurrentUser, @"Control Panel\Desktop", "JPEGImportQuality", 100);
        SetDefaultUserDword(@"Control Panel\Desktop", "JPEGImportQuality", 100);
        SetString(RegistryHive.CurrentUser, @"Control Panel\Desktop", "WallpaperStyle", "10");
        SetString(RegistryHive.CurrentUser, @"Control Panel\Desktop", "TileWallpaper", "0");
        SetDefaultUserString(@"Control Panel\Desktop", "WallpaperStyle", "10");
        SetDefaultUserString(@"Control Panel\Desktop", "TileWallpaper", "0");
    }

    private static void DisableMemoryIntegrity()
    {
        SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0);
        SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "WasEnabledBy", 0);
        SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0);
    }

    private static void ApplyProfileStickyKeys()
    {
        SetString(RegistryHive.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "10");
    }

    private static void ApplyProfileEnhancePointerPrecision()
    {
        SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0");
        SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0");
        SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0");
    }

    private static void ApplyProfileWallpaperQuality100()
    {
        SetDword(RegistryHive.CurrentUser, @"Control Panel\Desktop", "JPEGImportQuality", 100);
        SetString(RegistryHive.CurrentUser, @"Control Panel\Desktop", "WallpaperStyle", "10");
        SetString(RegistryHive.CurrentUser, @"Control Panel\Desktop", "TileWallpaper", "0");
    }

    private static void ApplyProfileRuRuUserLocale()
    {
        var script = @"
try {
  $list = New-WinUserLanguageList 'ru-RU'
  $en = (New-WinUserLanguageList 'en-US')[0]
  $list.Add($en)
  $list[0].Handwriting = $true
  Set-WinUserLanguageList $list -Force
} catch {}

try { Set-WinUILanguageOverride -Language 'ru-RU' } catch {}
try { Set-Culture 'ru-RU' } catch {}
try { Set-WinHomeLocation -GeoId 203 } catch {}
try { Set-WinDefaultInputMethodOverride -InputTip '0419:00000419' } catch {}

try {
  New-Item -Path 'HKCU:\Control Panel\International' -Force | Out-Null
  Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'LocaleName' -Value 'ru-RU' -Force
  Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'Locale' -Value '00000419' -Force
  Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'iCountry' -Value '7' -Force
  Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'iLanguage' -Value '0419' -Force
  New-Item -Path 'HKCU:\Keyboard Layout\Preload' -Force | Out-Null
  Set-ItemProperty -Path 'HKCU:\Keyboard Layout\Preload' -Name '1' -Value '00000419' -Force
  Set-ItemProperty -Path 'HKCU:\Keyboard Layout\Preload' -Name '2' -Value '00000409' -Force
} catch {}
";

        RunPowerShell(script);
    }


    private static void ConfigureRuRuLocale()
    {
        var script = @"
$base = 'C:\ProgramData\DeL1ThiSystem\Wizard'
New-Item -ItemType Directory -Path $base -Force | Out-Null
$log = Join-Path $base 'Wizard.log'

function Log([string]$phase, [string]$msg) {
  Add-Content -Path $log -Encoding UTF8 -Value ('[{0}] [{1}] {2}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $phase, $msg)
}

function Run-Dism([string]$phase, [string[]]$dismArgs) {
  if (-not $dismArgs -or $dismArgs.Count -eq 0) {
    Log $phase 'DISM skipped: empty args'
    return -1
  }

  $argLine = ($dismArgs -join ' ')
  Log $phase ('DISM ' + $argLine)
  try {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = 'dism.exe'
    $psi.Arguments = $argLine
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $psi
    $p.Start() | Out-Null
    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
    $p.WaitForExit()
    if (-not [string]::IsNullOrWhiteSpace($stdout)) {
      ($stdout -split [Environment]::NewLine) | ForEach-Object { if (-not [string]::IsNullOrWhiteSpace($_)) { Log $phase $_ } }
    }
    if (-not [string]::IsNullOrWhiteSpace($stderr)) {
      ($stderr -split [Environment]::NewLine) | ForEach-Object { if (-not [string]::IsNullOrWhiteSpace($_)) { Log $phase ('ERR ' + $_) } }
    }
    Log $phase ('DISM exit=' + $p.ExitCode)
    return $p.ExitCode
  } catch {
    Log $phase ('DISM error: ' + $_.Exception.Message)
    return -1
  }
}

function Ensure-ServiceOnline([string]$name, [string]$startup = 'Manual') {
  try { Set-Service -Name $name -StartupType $startup -ErrorAction SilentlyContinue } catch {}
  try { Start-Service -Name $name -ErrorAction SilentlyContinue } catch {}
}

function Convert-IanaToWindowsTz([string]$iana) {
  switch ($iana) {
    'Europe/Kaliningrad' { return 'Kaliningrad Standard Time' }
    'Europe/Moscow' { return 'Russian Standard Time' }
    'Europe/Samara' { return 'Russia Time Zone 3' }
    'Asia/Yekaterinburg' { return 'Ekaterinburg Standard Time' }
    'Asia/Omsk' { return 'Omsk Standard Time' }
    'Asia/Krasnoyarsk' { return 'North Asia Standard Time' }
    'Asia/Irkutsk' { return 'North Asia East Standard Time' }
    'Asia/Yakutsk' { return 'Yakutsk Standard Time' }
    'Asia/Vladivostok' { return 'Vladivostok Standard Time' }
    'Asia/Magadan' { return 'Magadan Standard Time' }
    'Asia/Sakhalin' { return 'Sakhalin Standard Time' }
    'Asia/Kamchatka' { return 'Russia Time Zone 11' }
    default { return '' }
  }
}

function Get-WindowsTzFromIp {
  $votes = @()

  try {
    $j = Invoke-RestMethod -Uri 'https://ipapi.co/json/' -TimeoutSec 6 -ErrorAction Stop
    if ($j.timezone) { $votes += [string]$j.timezone }
  } catch {}

  try {
    $j2 = Invoke-RestMethod -Uri 'https://worldtimeapi.org/api/ip' -TimeoutSec 6 -ErrorAction Stop
    if ($j2.timezone) { $votes += [string]$j2.timezone }
  } catch {}

  try {
    $j3 = Invoke-RestMethod -Uri 'http://ip-api.com/json/?fields=timezone' -TimeoutSec 6 -ErrorAction Stop
    if ($j3.timezone) { $votes += [string]$j3.timezone }
  } catch {}

  if ($votes.Count -eq 0) { return '' }

  $bestIana = ($votes | Group-Object | Sort-Object Count -Descending | Select-Object -First 1).Name
  return Convert-IanaToWindowsTz $bestIana
}

function Write-LanguageState([string]$phase, [string]$tag) {
  try {
    $langs = Get-WinUserLanguageList -ErrorAction Stop
    $tags = @()
    foreach ($item in $langs) {
      if ($item.PSObject.Properties.Match('Bcp47').Count -gt 0 -and $item.Bcp47) { $tags += [string]$item.Bcp47 }
      elseif ($item.PSObject.Properties.Match('LanguageTag').Count -gt 0 -and $item.LanguageTag) { $tags += [string]$item.LanguageTag }
      else { $tags += $item.ToString() }
    }
    Log $phase ($tag + ' UserLanguageList=' + ($tags -join ','))
  } catch { Log $phase ($tag + ' UserLanguageList error=' + $_.Exception.Message) }

  try { Log $phase ($tag + ' UILanguageOverride=' + (Get-WinUILanguageOverride)) } catch { Log $phase ($tag + ' UILanguageOverride error=' + $_.Exception.Message) }
  try { Log $phase ($tag + ' Culture=' + (Get-Culture).Name) } catch { Log $phase ($tag + ' Culture error=' + $_.Exception.Message) }
  try { Log $phase ($tag + ' SystemLocale=' + (Get-WinSystemLocale).Name) } catch { Log $phase ($tag + ' SystemLocale error=' + $_.Exception.Message) }
}

Log 'INSTALL' 'Locale install started'
Log 'INSTALL' ('OS=' + [Environment]::OSVersion.VersionString)
Ensure-ServiceOnline 'wuauserv'
Ensure-ServiceOnline 'bits'
Ensure-ServiceOnline 'cryptsvc'

$oemPath = 'C:\Lang\ru-RU'
if (-not (Test-Path $oemPath)) { $oemPath = 'C:\$OEM$\$1\Lang\ru-RU' }
if (Test-Path $oemPath) {
  Log 'INSTALL' ('Offline source path found: ' + $oemPath)
  $packages = Get-ChildItem -Path $oemPath -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match 'Microsoft-Windows-Client-LanguagePack-Package_ru-ru.*\.esd$' -or $_.Name -match 'Microsoft-Windows-LanguageFeatures-Basic-ru-ru-Package.*\.cab$' } |
    Sort-Object Name

  foreach ($pkg in $packages) {
    $code = Run-Dism 'INSTALL' @('/Online','/Add-Package',('/PackagePath:' + $pkg.FullName),'/NoRestart')
    Log 'INSTALL' ('Add-Package result for ' + $pkg.Name + ': ' + $code)
  }

  $caps = @(
    'Language.Basic~~~ru-RU~0.0.1.0',
    'Language.Handwriting~~~ru-RU~0.0.1.0',
    'Language.OCR~~~ru-RU~0.0.1.0',
    'Language.Speech~~~ru-RU~0.0.1.0',
    'Language.TextToSpeech~~~ru-RU~0.0.1.0'
  )
  foreach ($cap in $caps) {
    $code = Run-Dism 'INSTALL' @('/Online','/Add-Capability',('/CapabilityName:' + $cap),('/Source:' + $oemPath),'/LimitAccess','/NoRestart')
    Log 'INSTALL' ('Add-Capability result for ' + $cap + ': ' + $code)
  }
} else {
  Log 'INSTALL' 'Offline source path not found'
}

try {
  $cmd = Get-Command Install-Language -ErrorAction SilentlyContinue
  if ($cmd) {
    Log 'INSTALL' 'Install-Language available, running'
    Install-Language -Language 'ru-RU' -CopyToSettings -ErrorAction Stop | Out-String | ForEach-Object { if (-not [string]::IsNullOrWhiteSpace($_)) { Log 'INSTALL' $_.TrimEnd() } }
    Log 'INSTALL' 'Install-Language completed'
  } else {
    Log 'INSTALL' 'Install-Language not available'
  }
} catch {
  Log 'INSTALL' ('Install-Language error: ' + $_.Exception.Message)
}

foreach ($cap in @('Language.Basic~~~ru-RU~0.0.1.0','Language.Handwriting~~~ru-RU~0.0.1.0','Language.OCR~~~ru-RU~0.0.1.0','Language.Speech~~~ru-RU~0.0.1.0','Language.TextToSpeech~~~ru-RU~0.0.1.0')) {
  try {
    $state = (Get-WindowsCapability -Online -Name $cap -ErrorAction SilentlyContinue).State
    Log 'INSTALL' ('Capability state before fallback ' + $cap + ': ' + $state)
    if ($state -ne 'Installed') {
      Add-WindowsCapability -Online -Name $cap -ErrorAction Continue | Out-String | ForEach-Object { if (-not [string]::IsNullOrWhiteSpace($_)) { Log 'INSTALL' $_.TrimEnd() } }
      $state2 = (Get-WindowsCapability -Online -Name $cap -ErrorAction SilentlyContinue).State
      Log 'INSTALL' ('Capability state after fallback ' + $cap + ': ' + $state2)
    }
  } catch {
    Log 'INSTALL' ('Capability check/add error for ' + $cap + ': ' + $_.Exception.Message)
  }
}

Run-Dism 'INSTALL' @('/Online','/Get-Intl') | Out-Null
Run-Dism 'INSTALL' @('/Online','/Get-Capabilities') | Out-Null
Log 'INSTALL' 'Locale install finished'

Log 'APPLY' 'Locale apply started'
Write-LanguageState 'APPLY' 'Before'

try {
  $list = New-WinUserLanguageList 'ru-RU'
  $list.Add((New-WinUserLanguageList 'en-US')[0])
  $list[0].Handwriting = $true
  Set-WinUserLanguageList $list -Force
  Log 'APPLY' 'Set-WinUserLanguageList done'
} catch { Log 'APPLY' ('Set-WinUserLanguageList error: ' + $_.Exception.Message) }

try { Set-WinUILanguageOverride -Language 'ru-RU'; Log 'APPLY' 'Set-WinUILanguageOverride done' } catch { Log 'APPLY' ('Set-WinUILanguageOverride error: ' + $_.Exception.Message) }
try {
  if (Get-Command Set-SystemPreferredUILanguage -ErrorAction SilentlyContinue) {
    Set-SystemPreferredUILanguage -Language 'ru-RU'
    Log 'APPLY' 'Set-SystemPreferredUILanguage done'
  }
} catch { Log 'APPLY' ('Set-SystemPreferredUILanguage error: ' + $_.Exception.Message) }
try { Set-Culture 'ru-RU'; Log 'APPLY' 'Set-Culture done' } catch { Log 'APPLY' ('Set-Culture error: ' + $_.Exception.Message) }
try { Set-WinHomeLocation -GeoId 203; Log 'APPLY' 'Set-WinHomeLocation done' } catch { Log 'APPLY' ('Set-WinHomeLocation error: ' + $_.Exception.Message) }
try { Set-WinDefaultInputMethodOverride -InputTip '0419:00000419'; Log 'APPLY' 'Set-WinDefaultInputMethodOverride done' } catch { Log 'APPLY' ('Set-WinDefaultInputMethodOverride error: ' + $_.Exception.Message) }
try { Set-WinSystemLocale 'ru-RU'; Log 'APPLY' 'Set-WinSystemLocale done' } catch { Log 'APPLY' ('Set-WinSystemLocale error: ' + $_.Exception.Message) }

try {
  New-Item -Path 'HKCU:\Control Panel\International' -Force | Out-Null
  Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'LocaleName' -Value 'ru-RU' -Force
  Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'Locale' -Value '00000419' -Force
  Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'iCountry' -Value '7' -Force
  Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'iLanguage' -Value '0419' -Force
  Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'sLanguage' -Value 'RUS' -Force
  New-Item -Path 'HKCU:\Control Panel\Desktop' -Force | Out-Null
  Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'PreferredUILanguages' -Value @('ru-RU') -Type MultiString -Force
  Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'PreferredUILanguageFallback' -Value 'ru-RU' -Force
  New-Item -Path 'HKCU:\Keyboard Layout\Preload' -Force | Out-Null
  Set-ItemProperty -Path 'HKCU:\Keyboard Layout\Preload' -Name '1' -Value '00000419' -Force
  Set-ItemProperty -Path 'HKCU:\Keyboard Layout\Preload' -Name '2' -Value '00000409' -Force
  Log 'APPLY' 'HKCU language/input registry written'
} catch {
  Log 'APPLY' ('HKCU language/input registry error: ' + $_.Exception.Message)
}

try {
  New-Item -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\MUI\Settings' -Force | Out-Null
  Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\MUI\Settings' -Name 'PreferredUILanguages' -Value @('ru-RU') -Type MultiString -Force
  New-Item -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\Language' -Force | Out-Null
  Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\Language' -Name 'Default' -Value '0419' -Force
  Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\Language' -Name 'InstallLanguage' -Value '0419' -Force
  New-Item -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\CodePage' -Force | Out-Null
  Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\CodePage' -Name 'ACP' -Value '65001' -Force
  Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\CodePage' -Name 'OEMCP' -Value '65001' -Force
  Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\CodePage' -Name 'MACCP' -Value '65001' -Force
  Log 'APPLY' 'HKLM language registry written'
} catch {
  Log 'APPLY' ('HKLM language registry error: ' + $_.Exception.Message)
}

Run-Dism 'APPLY' @('/Online','/Set-UILang:ru-RU') | Out-Null
Run-Dism 'APPLY' @('/Online','/Set-SysLocale:ru-RU') | Out-Null
Run-Dism 'APPLY' @('/Online','/Set-UserLocale:ru-RU') | Out-Null
Run-Dism 'APPLY' @('/Online','/Set-InputLocale:0419:00000419,0409:00000409') | Out-Null

try {
  if (Get-Command Copy-UserInternationalSettingsToSystem -ErrorAction SilentlyContinue) {
    Copy-UserInternationalSettingsToSystem -WelcomeScreen $true -NewUser $true -ErrorAction Stop
    Log 'APPLY' 'Copy-UserInternationalSettingsToSystem done'
  } else {
    Log 'APPLY' 'Copy-UserInternationalSettingsToSystem not available'
  }
} catch {
  Log 'APPLY' ('Copy-UserInternationalSettingsToSystem error: ' + $_.Exception.Message)
}

try {
  $mount = 'HKU\DefaultUser'
  $ntuser = 'C:\Users\Default\NTUSER.DAT'
  reg.exe load $mount $ntuser | Out-Null
  New-Item -Path 'Registry::HKU\DefaultUser\Control Panel\International' -Force | Out-Null
  Set-ItemProperty -Path 'Registry::HKU\DefaultUser\Control Panel\International' -Name 'LocaleName' -Value 'ru-RU' -Force
  Set-ItemProperty -Path 'Registry::HKU\DefaultUser\Control Panel\International' -Name 'Locale' -Value '00000419' -Force
  New-Item -Path 'Registry::HKU\DefaultUser\Control Panel\Desktop' -Force | Out-Null
  Set-ItemProperty -Path 'Registry::HKU\DefaultUser\Control Panel\Desktop' -Name 'PreferredUILanguages' -Value @('ru-RU') -Type MultiString -Force
  Set-ItemProperty -Path 'Registry::HKU\DefaultUser\Control Panel\Desktop' -Name 'PreferredUILanguageFallback' -Value 'ru-RU' -Force
  New-Item -Path 'Registry::HKU\DefaultUser\Keyboard Layout\Preload' -Force | Out-Null
  Set-ItemProperty -Path 'Registry::HKU\DefaultUser\Keyboard Layout\Preload' -Name '1' -Value '00000419' -Force
  Set-ItemProperty -Path 'Registry::HKU\DefaultUser\Keyboard Layout\Preload' -Name '2' -Value '00000409' -Force
  reg.exe unload $mount | Out-Null
  Log 'APPLY' 'DefaultUser language/input written'
} catch {
  Log 'APPLY' ('DefaultUser write error: ' + $_.Exception.Message)
}

Write-LanguageState 'APPLY' 'After'
Run-Dism 'APPLY' @('/Online','/Get-Intl') | Out-Null
Log 'APPLY' 'Locale apply finished'
";

        RunPowerShell(script);
    }

    private static void ConfigureAutoTimeZone()
    {
        var script = @"
$base = 'C:\ProgramData\DeL1ThiSystem\Wizard'
New-Item -ItemType Directory -Path $base -Force | Out-Null
$log = Join-Path $base 'Wizard.log'

function Log([string]$phase, [string]$msg) {
  Add-Content -Path $log -Encoding UTF8 -Value ('[{0}] [{1}] {2}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $phase, $msg)
}

function Ensure-ServiceOnline([string]$name, [string]$startup = 'Manual') {
  try { Set-Service -Name $name -StartupType $startup -ErrorAction SilentlyContinue } catch {}
  try { Start-Service -Name $name -ErrorAction SilentlyContinue } catch {}
}

function Convert-IanaToWindowsTz([string]$iana) {
  switch ($iana) {
    'Europe/Kaliningrad' { return 'Kaliningrad Standard Time' }
    'Europe/Moscow' { return 'Russian Standard Time' }
    'Europe/Samara' { return 'Russia Time Zone 3' }
    'Asia/Yekaterinburg' { return 'Ekaterinburg Standard Time' }
    'Asia/Omsk' { return 'Omsk Standard Time' }
    'Asia/Krasnoyarsk' { return 'North Asia Standard Time' }
    'Asia/Irkutsk' { return 'North Asia East Standard Time' }
    'Asia/Yakutsk' { return 'Yakutsk Standard Time' }
    'Asia/Vladivostok' { return 'Vladivostok Standard Time' }
    'Asia/Magadan' { return 'Magadan Standard Time' }
    'Asia/Sakhalin' { return 'Sakhalin Standard Time' }
    'Asia/Kamchatka' { return 'Russia Time Zone 11' }
    default { return '' }
  }
}

function Get-WindowsTzFromIp {
  $votes = @()
  try {
    $j = Invoke-RestMethod -Uri 'https://ipapi.co/json/' -TimeoutSec 6 -ErrorAction Stop
    if ($j.timezone) { $votes += [string]$j.timezone }
  } catch {}
  try {
    $j2 = Invoke-RestMethod -Uri 'https://worldtimeapi.org/api/ip' -TimeoutSec 6 -ErrorAction Stop
    if ($j2.timezone) { $votes += [string]$j2.timezone }
  } catch {}
  try {
    $j3 = Invoke-RestMethod -Uri 'http://ip-api.com/json/?fields=timezone' -TimeoutSec 6 -ErrorAction Stop
    if ($j3.timezone) { $votes += [string]$j3.timezone }
  } catch {}
  if ($votes.Count -eq 0) { return '' }
  $bestIana = ($votes | Group-Object | Sort-Object Count -Descending | Select-Object -First 1).Name
  return Convert-IanaToWindowsTz $bestIana
}

Log 'APPLY' 'Auto time zone apply started'
try {
  Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\tzautoupdate' -Name 'Start' -Value 3 -Type DWord -Force
  Ensure-ServiceOnline 'tzautoupdate'
  Ensure-ServiceOnline 'W32Time' 'Automatic'
  Ensure-ServiceOnline 'lfsvc'
  New-Item -Path 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location' -Force | Out-Null
  Set-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location' -Name 'Value' -Value 'Allow' -Force
  New-Item -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location' -Force | Out-Null
  Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location' -Name 'Value' -Value 'Allow' -Force
  Log 'APPLY' 'Auto time zone prerequisites enabled'
} catch { Log 'APPLY' ('Auto time zone setup error: ' + $_.Exception.Message) }

try {
  $tz = Get-WindowsTzFromIp
  if (-not [string]::IsNullOrWhiteSpace($tz)) {
    tzutil /s $tz | Out-Null
    Log 'APPLY' ('Time zone set from IP: ' + $tz)
  } else {
    Log 'APPLY' 'Time zone from IP is unavailable'
  }
} catch { Log 'APPLY' ('Time zone apply error: ' + $_.Exception.Message) }

Log 'APPLY' 'Auto time zone apply finished'
";

        RunPowerShell(script);
    }


    private static void DisableRestoreAndCleanup()
    {
        RunPowerShell("try { Disable-ComputerRestore -Drive \"$env:SystemDrive\\\" } catch {}");
        if (Directory.Exists(@"C:\Windows.old"))
            RunProcess("cmd.exe", "/c rmdir /s /q C:\\Windows.old");
        else
            Log("Windows.old not found, skip cleanup.");
    }


    private static void SetPowercfgNeverSleep()
    {
        RunProcess("powercfg.exe", "-change -standby-timeout-ac 0");
        RunProcess("powercfg.exe", "-change -monitor-timeout-ac 0");
        RunProcess("powercfg.exe", "-change -hibernate-timeout-ac 0");
        RunProcess("powercfg.exe", "-change -standby-timeout-dc 0");
        RunProcess("powercfg.exe", "-change -monitor-timeout-dc 0");
        RunProcess("powercfg.exe", "-change -hibernate-timeout-dc 0");
    }


    private static void ApplyDefaultUserContentDelivery()
    {
        WithDefaultUserHive(root =>
        {
            using var key = root.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", true);
            if (key == null)
                return;
            foreach (var name in ContentDeliveryValues)
                key.SetValue(name, 0, RegistryValueKind.DWord);
        });
    }

}
