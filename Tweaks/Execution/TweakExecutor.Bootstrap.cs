using System;
using System.IO;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static partial class TweakExecutor
{
    private static void DisableDefenderNotifications()
    {
        Log(">> DisableDefenderNotifications");
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications", "DisableNotifications", 1);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Systray", "HideSystray", 1);
    }


    private static void DisableSmartScreen()
    {
        Log(">> DisableSmartScreen");
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
        Log(">> DisableWebContentEvaluation");
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AppHost", "PreventOverride", 0);
    }


    private static void EnableRemoteAssistance()
    {
        SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Remote Assistance", "fAllowToGetHelp", 1);
    }


    private static void EnableRdp()
    {
        Log(">> EnableRdp");
        RunProcess("netsh.exe", "advfirewall firewall set rule group=\"@FirewallAPI.dll,-28752\" new enable=Yes");
        SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0);
    }


    private static void ApplyVisualFxProfile()
    {
        Log(">> ApplyVisualFxProfile");
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
        Log(">> DisableStickyKeys");
        SetString(RegistryHive.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "10");
        LogReg("REG SZ", @"HKU\.DEFAULT\Control Panel\Accessibility\StickyKeys", "Flags", "10");
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(@".DEFAULT\Control Panel\Accessibility\StickyKeys", true);
        key?.SetValue("Flags", "10", RegistryValueKind.String);
        SetDefaultUserString(@"Control Panel\Accessibility\StickyKeys", "Flags", "10");
    }


    private static void DisableEnhancePointerPrecision()
    {
        Log(">> DisableEnhancePointerPrecision");
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
        Log(">> DisableMemoryIntegrity");
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
        Log(">> ApplyProfileRuRuUserLocale");
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
        Log(">> ConfigureRuRuLocale");
        var script = @"
$base = 'C:\ProgramData\DeL1ThiSystem\Wizard'
New-Item -ItemType Directory -Path $base -Force | Out-Null
$log = Join-Path $base 'Wizard.log'
$cacheRoot = Join-Path $base 'LangCache\ru-RU'
New-Item -ItemType Directory -Path $cacheRoot -Force | Out-Null

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

function Invoke-DownloadFile([string]$url, [string]$outFile) {
  try {
    Ensure-ServiceOnline 'bits'
    Start-BitsTransfer -Source $url -Destination $outFile -TransferType Download -Priority Foreground -ErrorAction Stop
    return $true
  } catch {
    try {
      Invoke-WebRequest -Uri $url -OutFile $outFile -UseBasicParsing -ErrorAction Stop
      return $true
    } catch {
      Log 'DOWNLOAD' ('Failed download ' + $outFile + ': ' + $_.Exception.Message)
      return $false
    }
  }
}

function Get-EditionToken([string]$editionId) {
  $token = ($editionId -replace '[^a-zA-Z0-9]', '').ToLowerInvariant()
  if ([string]::IsNullOrWhiteSpace($token)) { return 'professional' }
  return $token
}

function Find-PackageFile([string[]]$roots, [string[]]$patterns) {
  foreach ($root in $roots) {
    if ([string]::IsNullOrWhiteSpace($root) -or -not (Test-Path $root)) { continue }
    $files = Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue
    foreach ($pat in $patterns) {
      $hit = $files | Where-Object { $_.Name -imatch $pat } | Sort-Object Name | Select-Object -First 1
      if ($hit) { return $hit.FullName }
    }
  }
  return ''
}

function Get-MissingPackageKeys([array]$defs, [string[]]$roots) {
  $missing = @()
  foreach ($d in $defs) {
    $found = Find-PackageFile -roots $roots -patterns $d.Patterns
    if ([string]::IsNullOrWhiteSpace($found) -and $d.Hard) { $missing += $d.Key }
    if (-not [string]::IsNullOrWhiteSpace($found)) { Log 'INSTALL' ('Found package ' + $d.Key + ': ' + $found) }
  }
  return $missing
}

function Get-UupCandidateId([string]$targetBuild, [string]$buildMajor, [string]$arch) {
  try {
    $query = [uri]::EscapeDataString(($targetBuild + ' ' + $arch))
    $url = 'https://api.uupdump.net/listid.php?search=' + $query + '&sortByDate=1'
    $resp = Invoke-RestMethod -Uri $url -TimeoutSec 35 -ErrorAction Stop
    $all = @($resp.response.builds.PSObject.Properties | ForEach-Object { $_.Value })
    if ($all.Count -eq 0) { return '' }
    $filtered = $all | Where-Object {
      $_.arch -eq $arch -and
      $_.title -notmatch 'Cumulative Update|\.NET Framework|Dynamic Update|Defender'
    } | Sort-Object created -Descending
    $exact = $filtered | Where-Object { $_.build -eq $targetBuild } | Select-Object -First 1
    if ($exact) { $filtered = @($exact) + @($filtered | Where-Object { $_.uuid -ne $exact.uuid }) }
    foreach ($cand in ($filtered | Select-Object -First 10)) {
      try {
        $langs = Invoke-RestMethod -Uri ('https://api.uupdump.net/listlangs.php?id=' + $cand.uuid) -TimeoutSec 20 -ErrorAction Stop
        if (@($langs.response.langList) -contains 'ru-ru') { return [string]$cand.uuid }
      } catch {}
    }
    $byMajor = $filtered | Where-Object { [string]$_.build -like ($buildMajor + '.*') } | Select-Object -First 1
    if ($byMajor) { return [string]$byMajor.uuid }
    return [string]($filtered | Select-Object -First 1).uuid
  } catch {
    Log 'DOWNLOAD' ('UUP list failed: ' + $_.Exception.Message)
    return ''
  }
}

function Get-UupFileMap([string]$uupId, [string]$editionToken) {
  foreach ($ed in @($editionToken, 'professional', 'core')) {
    if ([string]::IsNullOrWhiteSpace($ed)) { continue }
    $url = 'https://api.uupdump.net/get.php?id=' + $uupId + '&lang=ru-ru&edition=' + $ed
    for ($try = 1; $try -le 3; $try++) {
      try {
        $resp = Invoke-RestMethod -Uri $url -TimeoutSec 45 -ErrorAction Stop
        if ($resp.response.files -and $resp.response.files.PSObject.Properties.Count -gt 0) { return $resp.response.files }
      } catch {
        Log 'DOWNLOAD' ('UUP get failed edition=' + $ed + ' try=' + $try + ': ' + $_.Exception.Message)
        Start-Sleep -Seconds (2 * $try)
      }
    }
  }
  return $null
}

function Download-MissingFromUup([array]$defs, [string[]]$roots, [string]$targetBuild, [string]$buildMajor, [string]$arch, [string]$editionToken, [string]$cachePath) {
  $missing = Get-MissingPackageKeys -defs $defs -roots $roots
  if ($missing.Count -eq 0) { return }
  Log 'DOWNLOAD' ('Required keys missing: ' + ($missing -join ','))
  $uupId = Get-UupCandidateId -targetBuild $targetBuild -buildMajor $buildMajor -arch $arch
  if ([string]::IsNullOrWhiteSpace($uupId)) { return }
  $fileMap = Get-UupFileMap -uupId $uupId -editionToken $editionToken
  if ($null -eq $fileMap) { return }

  foreach ($key in $missing) {
    $def = $defs | Where-Object { $_.Key -eq $key } | Select-Object -First 1
    if (-not $def) { continue }
    $candidate = $null
    foreach ($pat in $def.Patterns) {
      $candidate = $fileMap.PSObject.Properties | Where-Object { $_.Name -imatch $pat } | Select-Object -First 1
      if ($candidate) { break }
    }
    if (-not $candidate) {
      Log 'DOWNLOAD' ('UUP has no file for key=' + $key)
      continue
    }
    $targetFile = Join-Path $cachePath $candidate.Name
    if (Test-Path $targetFile) { continue }
    if (Invoke-DownloadFile -url ([string]$candidate.Value.url) -outFile $targetFile) {
      Log 'DOWNLOAD' ('Downloaded: ' + $candidate.Name)
    }
  }
}

function Install-CapabilityWithSources([string]$cap, [string[]]$sources) {
  try {
    $state = (Get-WindowsCapability -Online -Name $cap -ErrorAction SilentlyContinue).State
    if ($state -eq 'Installed') {
      Log 'INSTALL' ('Capability already installed ' + $cap)
      return
    }
  } catch {}

  foreach ($src in $sources) {
    if ([string]::IsNullOrWhiteSpace($src) -or -not (Test-Path $src)) { continue }
    try {
      Add-WindowsCapability -Online -Name $cap -Source $src -LimitAccess -ErrorAction Stop | Out-String | ForEach-Object {
        if (-not [string]::IsNullOrWhiteSpace($_)) { Log 'INSTALL' $_.TrimEnd() }
      }
      $state = (Get-WindowsCapability -Online -Name $cap -ErrorAction SilentlyContinue).State
      Log 'INSTALL' ('Capability state after source ' + $cap + ': ' + $state + ' source=' + $src)
      if ($state -eq 'Installed') { return }
    } catch {
      Log 'INSTALL' ('Capability source failed ' + $cap + ' source=' + $src + ' err=' + $_.Exception.Message)
    }
  }

  try {
    Add-WindowsCapability -Online -Name $cap -ErrorAction Continue | Out-String | ForEach-Object {
      if (-not [string]::IsNullOrWhiteSpace($_)) { Log 'INSTALL' $_.TrimEnd() }
    }
    $state = (Get-WindowsCapability -Online -Name $cap -ErrorAction SilentlyContinue).State
    Log 'INSTALL' ('Capability state after Microsoft fallback ' + $cap + ': ' + $state)
  } catch {
    Log 'INSTALL' ('Capability Microsoft fallback failed ' + $cap + ': ' + $_.Exception.Message)
  }
}

Log 'INSTALL' 'Locale install started'
Log 'INSTALL' ('OS=' + [Environment]::OSVersion.VersionString)
Ensure-ServiceOnline 'wuauserv'
Ensure-ServiceOnline 'bits'
Ensure-ServiceOnline 'cryptsvc'
Ensure-ServiceOnline 'dosvc'

$currentBuild = [string](Get-ItemPropertyValue -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name 'CurrentBuild' -ErrorAction SilentlyContinue)
if ([string]::IsNullOrWhiteSpace($currentBuild)) {
  $currentBuild = [string](Get-ItemPropertyValue -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name 'CurrentBuildNumber' -ErrorAction SilentlyContinue)
}
$ubr = [string](Get-ItemPropertyValue -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name 'UBR' -ErrorAction SilentlyContinue)
$editionId = [string](Get-ItemPropertyValue -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name 'EditionID' -ErrorAction SilentlyContinue)
$arch = if ([Environment]::Is64BitOperatingSystem) { 'amd64' } else { 'x86' }
$buildFull = if (-not [string]::IsNullOrWhiteSpace($ubr)) { $currentBuild + '.' + $ubr } else { $currentBuild }
$editionToken = Get-EditionToken -editionId $editionId
$cachePath = Join-Path $cacheRoot ($buildFull + '_' + $editionToken + '_' + $arch)
New-Item -ItemType Directory -Path $cachePath -Force | Out-Null
Log 'INSTALL' ('Detected build=' + $buildFull + ' edition=' + $editionId + ' arch=' + $arch)

$localPath = 'C:\Lang\ru-RU'
$sourceRoots = @($cachePath, $localPath) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path $_) }

$packageDefs = @(
  @{ Key = 'lp'; Hard = $true; Patterns = @('^Microsoft-Windows-Client-LanguagePack-Package.*ru[-_]ru.*\.(cab|esd)$') },
  @{ Key = 'basic'; Hard = $true; Patterns = @('^Microsoft-Windows-LanguageFeatures-Basic-ru-ru-Package.*\.cab$') },
  @{ Key = 'shell'; Hard = $true; Patterns = @('^Microsoft-Windows-Required-ShellExperiences-Desktop-Package\.ESD$') },
  @{ Key = 'edition'; Hard = $true; Patterns = @('^' + [regex]::Escape($editionToken) + '_ru-ru\.esd$', '^[a-z0-9]+_ru-ru\.esd$') },
  @{ Key = 'ux'; Hard = $true; Patterns = @('^Microsoft-Windows-UserExperience-Desktop-Package.*\.(cab|esd)$') },
  @{ Key = 'modernAll'; Hard = $true; Patterns = @('^Microsoft\.ModernApps\.Client\.All\.esd$') },
  @{ Key = 'modernEdition'; Hard = $true; Patterns = @('^Microsoft\.ModernApps\.Client\.' + [regex]::Escape($editionToken) + '\.esd$', '^Microsoft\.ModernApps\.Client\.(professional|core|enterprise|education)\.esd$') },
  @{ Key = 'hand'; Hard = $true; Patterns = @('^Microsoft-Windows-LanguageFeatures-Handwriting-ru-ru-Package.*\.cab$') },
  @{ Key = 'ocr'; Hard = $true; Patterns = @('^Microsoft-Windows-LanguageFeatures-OCR-ru-ru-Package.*\.cab$') },
  @{ Key = 'tts'; Hard = $true; Patterns = @('^Microsoft-Windows-LanguageFeatures-TextToSpeech-ru-ru-Package.*\.cab$') }
)

$missingBefore = Get-MissingPackageKeys -defs $packageDefs -roots $sourceRoots
if ($missingBefore.Count -gt 0) {
  Log 'INSTALL' ('Missing required local keys: ' + ($missingBefore -join ','))
}

Download-MissingFromUup -defs $packageDefs -roots $sourceRoots -targetBuild $buildFull -buildMajor $currentBuild -arch $arch -editionToken $editionToken -cachePath $cachePath

$sourceRoots = @($cachePath, $localPath) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path $_) }
$missingAfterUup = Get-MissingPackageKeys -defs $packageDefs -roots $sourceRoots
if ($missingAfterUup.Count -gt 0) {
  Log 'INSTALL' ('Still missing after UUP fallback: ' + ($missingAfterUup -join ','))
  try {
    $cmd = Get-Command Install-Language -ErrorAction SilentlyContinue
    if ($cmd) {
      Log 'INSTALL' 'Install-Language available, running Microsoft fallback'
      Install-Language -Language 'ru-RU' -CopyToSettings -ErrorAction Stop | Out-String | ForEach-Object { if (-not [string]::IsNullOrWhiteSpace($_)) { Log 'INSTALL' $_.TrimEnd() } }
      Log 'INSTALL' 'Install-Language completed'
    } else {
      Log 'INSTALL' 'Install-Language not available'
    }
  } catch {
    Log 'INSTALL' ('Install-Language error: ' + $_.Exception.Message)
  }
}

$sourceRoots = @($cachePath, $localPath) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path $_) }
$installOrder = @{ lp = 1; basic = 2; shell = 3; ux = 4; modernAll = 5; modernEdition = 6; edition = 7; hand = 8; ocr = 9; tts = 10 }
$installList = @()
foreach ($def in $packageDefs) {
  $pkg = Find-PackageFile -roots $sourceRoots -patterns $def.Patterns
  if (-not [string]::IsNullOrWhiteSpace($pkg)) {
    $installList += [pscustomobject]@{
      Key = $def.Key
      Path = $pkg
      Rank = if ($installOrder.ContainsKey($def.Key)) { $installOrder[$def.Key] } else { 999 }
    }
  }
}

$installList = $installList | Sort-Object Rank, Path -Unique
foreach ($pkg in $installList) {
  $code = Run-Dism 'INSTALL' @('/Online','/Add-Package',('/PackagePath:' + $pkg.Path),'/NoRestart')
  Log 'INSTALL' ('Add-Package result key=' + $pkg.Key + ' exit=' + $code)
}

$missingAfter = Get-MissingPackageKeys -defs $packageDefs -roots $sourceRoots
if ($missingAfter.Count -gt 0) {
  Log 'INSTALL' ('Required keys still missing after fallback: ' + ($missingAfter -join ','))
}

foreach ($cap in @('Language.Basic~~~ru-RU~0.0.1.0','Language.Handwriting~~~ru-RU~0.0.1.0','Language.OCR~~~ru-RU~0.0.1.0','Language.Speech~~~ru-RU~0.0.1.0','Language.TextToSpeech~~~ru-RU~0.0.1.0')) {
  Install-CapabilityWithSources -cap $cap -sources $sourceRoots
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
        Log(">> ConfigureAutoTimeZone");
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

    private static void CleanupRuRuLocalPackages()
    {
        Log(">> CleanupRuRuLocalPackages");
        var script = @"
$base = 'C:\ProgramData\DeL1ThiSystem\Wizard'
New-Item -ItemType Directory -Path $base -Force | Out-Null
$log = Join-Path $base 'Wizard.log'

function Log([string]$phase, [string]$msg) {
  Add-Content -Path $log -Encoding UTF8 -Value ('[{0}] [{1}] {2}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $phase, $msg)
}

function Remove-PathSafe([string]$path) {
  try {
    if (Test-Path $path) {
      Remove-Item -Path $path -Recurse -Force -ErrorAction Stop
      Log 'CLEANUP' ('Removed: ' + $path)
    } else {
      Log 'CLEANUP' ('Not found: ' + $path)
    }
  } catch {
    Log 'CLEANUP' ('Remove failed: ' + $path + ' err=' + $_.Exception.Message)
  }
}

Log 'CLEANUP' 'RU package cleanup started'
Remove-PathSafe 'C:\Lang\ru-RU'
Remove-PathSafe 'C:\ProgramData\DeL1ThiSystem\Wizard\LangCache\ru-RU'
Log 'CLEANUP' 'RU package cleanup finished'
";

        RunPowerShell(script);
    }


    private static void DisableRestoreAndCleanup()
    {
        Log(">> DisableRestoreAndCleanup");
        RunPowerShell("try { Disable-ComputerRestore -Drive \"$env:SystemDrive\\\" } catch {}");
        if (Directory.Exists(@"C:\Windows.old"))
            RunProcess("cmd.exe", "/c rmdir /s /q C:\\Windows.old");
        else
            Log("Windows.old not found, skip cleanup.");
    }


    private static void SetPowercfgNeverSleep()
    {
        Log(">> SetPowercfgNeverSleep");
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
