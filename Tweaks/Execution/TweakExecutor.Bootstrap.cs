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
    }


    private static void ConfigureRuRuLocale()
    {
        var installScript = @"
 $logPath = 'C:\ProgramData\DeL1ThiSystem\Wizard\LocaleInstall.log'
 function Log([string]$s) { ('[{0}] {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $s) | Out-File -FilePath $logPath -Append -Encoding UTF8 }
 Log 'Start locale install'
Write-Output 'Начало настройки локализации...'

# Ensure Windows Update service is running
Write-Output 'Запуск Windows Update сервиса...'
Set-Service -Name 'wuauserv' -StartupType Manual -ErrorAction SilentlyContinue
Start-Service -Name 'wuauserv' -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Try to install language pack via Install-Language if available (preferred on Win10/11)
Write-Output 'Попытка установить языковой пакет ru-RU...'
$installSucceeded = $false

# 0) Offline install from $OEM$ if present
# $OEM$\$1\Lang\ru-RU is copied to C:\Lang\ru-RU at install time.
$oemPath = 'C:\Lang\ru-RU'
if (-not (Test-Path $oemPath)) { $oemPath = 'C:\$OEM$\$1\Lang\ru-RU' }
if (-not (Test-Path $oemPath)) { $oemPath = $null }
if ($oemPath) {
    Write-Output ('Найден офлайн пакет: ' + $oemPath)
    Log ('Offline path: ' + $oemPath)
    try {
        $packages = Get-ChildItem -Path $oemPath -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.Extension -in '.cab', '.esd' }
        foreach ($pkg in $packages) {
            $pkgPath = $pkg.FullName
            Write-Output ('DISM Add-Package: ' + $pkgPath)
            Log ('DISM Add-Package: ' + $pkgPath)
            & dism.exe /Online /Add-Package /PackagePath:""$pkgPath"" /NoRestart 2>&1 | Out-File -FilePath $logPath -Append -Encoding UTF8
            Log ('DISM Add-Package exit: ' + $LASTEXITCODE)
            if ($LASTEXITCODE -ne 0) { throw ('DISM Add-Package failed: ' + $pkgPath) }
        }
        $capsToAdd = @(
            'Language.Basic~~~ru-RU~0.0.1.0',
            'Language.Handwriting~~~ru-RU~0.0.1.0',
            'Language.OCR~~~ru-RU~0.0.1.0',
            'Language.Speech~~~ru-RU~0.0.1.0',
            'Language.TextToSpeech~~~ru-RU~0.0.1.0'
        )
        foreach ($cap in $capsToAdd) {
            Write-Output ('DISM Add-Capability: ' + $cap)
            Log ('DISM Add-Capability: ' + $cap)
            & dism.exe /Online /Add-Capability /CapabilityName:$cap /Source:""$oemPath"" /LimitAccess /NoRestart 2>&1 | Out-File -FilePath $logPath -Append -Encoding UTF8
            Log ('DISM Add-Capability exit: ' + $LASTEXITCODE)
        }
        $installSucceeded = $true
        Write-Output 'Офлайн установка завершена'
        Log 'Offline install completed'
    } catch {
        Write-Output ('Офлайн установка ошибка: ' + $_)
        Log ('Offline install error: ' + $_)
    }
}

$installCmd = Get-Command Install-Language -ErrorAction SilentlyContinue
if ($installCmd) {
    try {
        Install-Language -Language 'ru-RU' -CopyToSettings -ErrorAction Stop | Out-Null
        $installSucceeded = $true
        Write-Output 'Install-Language: успешно'
    } catch {
        Write-Output ('Install-Language ошибка: ' + $_)
    }
}
if (-not $installSucceeded) {
    Write-Output 'Install-Language недоступна или завершилась с ошибкой, использую Add-WindowsCapability'
    $capsToAdd = @(
        'Language.Basic~~~ru-RU~0.0.1.0',
        'Language.Handwriting~~~ru-RU~0.0.1.0',
        'Language.OCR~~~ru-RU~0.0.1.0',
        'Language.Speech~~~ru-RU~0.0.1.0',
        'Language.TextToSpeech~~~ru-RU~0.0.1.0'
    )
    foreach ($cap in $capsToAdd) {
        $result = Add-WindowsCapability -Online -Name $cap 2>&1
        Write-Output ('Add-WindowsCapability ' + $cap + ': ' + $result)
        Log ('Add-WindowsCapability ' + $cap + ': ' + $result)
    }
}

# Report capability state
try {
    $capState = Get-WindowsCapability -Online -Name 'Language.Basic~~~ru-RU~0.0.1.0' | Select-Object -ExpandProperty State
    Write-Output ('Состояние Language.Basic ru-RU: ' + $capState)
    Log ('Language.Basic state: ' + $capState)
} catch {
    Write-Output ('Не удалось получить состояние Language.Basic: ' + $_)
    Log ('Language.Basic state error: ' + $_)
}

# Verify language pack is installed
$caps = dism.exe /Online /Get-Capabilities 2>&1
if ($caps -match 'ru-RU') {
    Write-Output 'Языковой пакет успешно установлен'
    Log 'Capabilities list contains ru-RU'
} else {
    Write-Output 'ВНИМАНИЕ: Языковой пакет не найден в списке возможностей'
    Log 'Capabilities list does not contain ru-RU'
}
Log 'End locale install'
";


        RunPowerShellDetached(installScript, tag: "configure_ru_ru_locale.install");

        var applyScript = @"
$logPath = 'C:\ProgramData\DeL1ThiSystem\Wizard\LocaleApply.log'
function Log([string]$s) { ('[{0}] {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $s) | Out-File -FilePath $logPath -Append -Encoding UTF8 }
Log 'Start locale apply'
$list = Get-WinUserLanguageList
$ruLang = $list | Where-Object { $_.LanguageTag -eq 'ru-RU' }
if (-not $ruLang) {
    Write-Output 'ru-RU не в списке языков, добавляю...'
    $newLang = New-WinUserLanguageList 'ru-RU'
    $list.Insert(0, $newLang[0])
} else {
    Write-Output 'ru-RU уже в списке, переставляю на первое место...'
    $list.Remove($ruLang)
    $list.Insert(0, $ruLang)
}
Set-WinUserLanguageList $list -Force
Write-Output 'Список языков обновлен'

Write-Output 'Установка Display Language...'
# Set through registry for Display Language
New-Item -Path 'HKCU:\Control Panel\Desktop' -Force -ErrorAction SilentlyContinue | Out-Null
Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'PreferredUILanguageFallback' -Value 'ru-RU' -Force
Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'UILanguage' -Value 'ru-RU' -Force

# Also set in HKLM for system-wide Display Language  
New-Item -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\Language' -Force -ErrorAction SilentlyContinue | Out-Null
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\Language' -Name 'Default' -Value '0419' -Force -ErrorAction SilentlyContinue

# Set via powershell if available
try {
    Set-WinUILanguageOverride -Language ru-RU
    Write-Output 'Set-WinUILanguageOverride выполнена успешно'
} catch {
    Write-Output 'Set-WinUILanguageOverride не доступна или ошибка: $_'
}

Set-WinSystemLocale ru-RU -ErrorAction SilentlyContinue
Set-Culture ru-RU -ErrorAction SilentlyContinue
Set-WinHomeLocation -GeoId 203 -ErrorAction SilentlyContinue

# Copy settings to welcome screen and new user accounts
try {
    Copy-UserInternationalSettingsToSystem -WelcomeScreen $true -NewUser $true -ErrorAction Stop
    Write-Output 'Copy-UserInternationalSettingsToSystem выполнена успешно'
} catch {
    Write-Output 'Copy-UserInternationalSettingsToSystem не доступна или ошибка: $_'
}

Write-Output 'Установка временной зоны...'
tzutil /s 'Russian Standard Time'

Write-Output 'Установка UTF-8 кодировки...'
# Enable UTF-8 support (Windows 10/11 feature)
# Method 1: Registry for UTF-8 beta feature
New-Item -Path 'HKCU:\Control Panel\International\User Override' -Force -ErrorAction SilentlyContinue | Out-Null
Set-ItemProperty -Path 'HKCU:\Control Panel\International\User Override' -Name 'GdiCharSet' -Value '204' -Force
Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'iCurrDigits' -Value '2' -Force

# Method 2: Set system codepages (requires restart)
$regPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\CodePage'
try {
    Set-ItemProperty -Path $regPath -Name 'ACP' -Value '65001' -Force
    Set-ItemProperty -Path $regPath -Name 'OEMCP' -Value '65001' -Force  
    Set-ItemProperty -Path $regPath -Name 'MACCP' -Value '65001' -Force
    Write-Output 'Системные кодовые страницы обновлены (требуется перезагрузка для полного применения)'
} catch {
    Write-Output 'Ошибка при установке системных кодовых страниц: $_'
}

# Method 3: Enable UTF-8 beta feature if available
try {
    $regPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Globalization\Intl'
    New-Item -Path $regPath -Force -ErrorAction SilentlyContinue | Out-Null
    Set-ItemProperty -Path $regPath -Name 'UseUtf8Locale' -Value '1' -Force
    Write-Output 'UTF-8 beta feature включена'
} catch {
    Write-Output 'UTF-8 beta feature не доступна: $_'
}

Write-Output 'Установка реестра пользователя...'
if (-not (Test-Path 'HKCU:\Control Panel\International')) {
    New-Item -Path 'HKCU:\Control Panel\International' -Force | Out-Null
}
Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'LocaleName' -Value 'ru-RU' -Force
Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'iCountry' -Value '7' -Force
Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'iLanguage' -Value '0419' -Force
Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'sLanguage' -Value 'RUS' -Force

if (-not (Test-Path 'HKCU:\Keyboard Layout\Preload')) {
    New-Item -Path 'HKCU:\Keyboard Layout\Preload' -Force | Out-Null
}
Set-ItemProperty -Path 'HKCU:\Keyboard Layout\Preload' -Name '1' -Value '00000419' -Force

Write-Output 'Локализация установлена. ТРЕБУЕТСЯ ПЕРЕЗАГРУЗКА!'
Log 'End locale apply'
";
        RunPowerShell(applyScript);

        var applyDeferredScript = @"
$logPath = 'C:\ProgramData\DeL1ThiSystem\Wizard\LocaleApplyDeferred.log'
function Log([string]$s) { ('[{0}] {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $s) | Out-File -FilePath $logPath -Append -Encoding UTF8 }
$taskName = 'DeL1ThiSystem\ApplyLocaleRuRu'
Log 'Deferred apply start'

try {
  $pkg = Get-WindowsPackage -Online | Where-Object { $_.PackageName -match 'LanguagePack' -and $_.PackageName -match 'ru-ru' -and $_.State -eq 'Installed' } | Select-Object -First 1
  if (-not $pkg) { Log 'Client Language Pack not installed yet'; exit 0 }
  Log ('Client Language Pack installed: ' + $pkg.PackageName)

  $list = Get-WinUserLanguageList
  $ruLang = $list | Where-Object { $_.LanguageTag -eq 'ru-RU' }
  if (-not $ruLang) {
      $newLang = New-WinUserLanguageList 'ru-RU'
      $list.Insert(0, $newLang[0])
  } else {
      $list.Remove($ruLang)
      $list.Insert(0, $ruLang)
  }
  Set-WinUserLanguageList $list -Force

  New-Item -Path 'HKCU:\Control Panel\Desktop' -Force -ErrorAction SilentlyContinue | Out-Null
  Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'PreferredUILanguageFallback' -Value 'ru-RU' -Force
  Set-ItemProperty -Path 'HKCU:\Control Panel\International' -Name 'UILanguage' -Value 'ru-RU' -Force
  New-Item -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\Language' -Force -ErrorAction SilentlyContinue | Out-Null
  Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Nls\Language' -Name 'Default' -Value '0419' -Force -ErrorAction SilentlyContinue
  try { Set-WinUILanguageOverride -Language ru-RU } catch { }
  Log 'Applied UI language override'

  try {
    schtasks /Delete /TN $taskName /F >$null 2>$null
    Log 'Deferred task removed'
  } catch { }
} catch {
  Log ('Deferred apply error: ' + $_)
}
";

        var scheduleScript = @"
$taskName = 'DeL1ThiSystem\ApplyLocaleRuRu'
$path = Join-Path $env:ProgramData 'DeL1ThiSystem\Wizard\ApplyLocaleRuRu.ps1'
New-Item -ItemType Directory -Path (Split-Path $path) -Force | Out-Null
$ps = @'
" + applyDeferredScript + @"
'@
$ps | Set-Content -LiteralPath $path -Encoding UTF8
$tr = 'powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File ""' + $path + '""'
schtasks /Delete /TN $taskName /F >$null 2>$null
$user = $env:USERNAME
if ([string]::IsNullOrWhiteSpace($user)) { $user = 'Administrator' }
schtasks /Create /F /TN $taskName /RU $user /RL HIGHEST /SC ONLOGON /TR $tr | Out-Null
";
        RunPowerShell(scheduleScript);
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
