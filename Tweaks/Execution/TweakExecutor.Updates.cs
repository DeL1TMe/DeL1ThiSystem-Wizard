using System;
using System.Globalization;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static partial class TweakExecutor
{

    // ── privacy.disable_tracking ───────────────────────────────────
    // Combines: consumer_features, widgets, copilot, cortana, search_suggestions

    private static void DisableTrackingAndAds(string osFamily)
    {
        // Consumer features / ad recommendations
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableSoftLanding", 1);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableThirdPartySuggestions", 1);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", 2);

        foreach (var name in ContentDeliveryValues)
            SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", name, 0);
        ApplyDefaultUserContentDelivery();

        // Widgets and news feed
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowWidgets", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds", "EnableFeeds", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Feeds", "ShellFeedsTaskbarViewMode", 2);
        SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Feeds", "ShellFeedsTaskbarViewMode", 2);

        // Copilot
        DisableCopilotEverywhere();

        // Cortana
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCortanaButton", 0);
        SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCortanaButton", 0);

        // Web search suggestions
        SetDword(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1);
        SetDefaultUserDword(@"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowSearchHighlights", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", 0);
        SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", 1);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchUseWeb", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchPrivacy", 3);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent", 0);
        SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0);
        SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent", 0);
    }


    private static void DisableCopilotEverywhere()
    {
        const string policyKey = @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot";
        SetDword(RegistryHive.LocalMachine, policyKey, "TurnOffWindowsCopilot", 1);
        SetDword(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
        SetDefaultUserDword(@"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);

        const string shellCopilotBingChat = @"Software\Microsoft\Windows\Shell\Copilot\BingChat";
        SetDword(RegistryHive.CurrentUser, shellCopilotBingChat, "IsUserEligible", 0);
        SetDword(RegistryHive.CurrentUser, shellCopilotBingChat, "CopilotButtonVisibility", 0);
        SetDefaultUserDword(shellCopilotBingChat, "IsUserEligible", 0);
        SetDefaultUserDword(shellCopilotBingChat, "CopilotButtonVisibility", 0);

        const string explorerAdvanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        SetDword(RegistryHive.CurrentUser, explorerAdvanced, "ShowCopilotButton", 0);
        SetDefaultUserDword(explorerAdvanced, "ShowCopilotButton", 0);
    }


    // ── privacy.pause_updates ──────────────────────────────────────
    // Sets pause + creates scheduled task for auto-renewal on logon

    private static void PauseWindowsUpdate()
    {
        SetPauseDates();
        CreatePauseRenewalTask();
    }

    private static void SetPauseDates()
    {
        var now = DateTime.UtcNow;
        var start = now.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture);
        var end = now.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture);

        const string key = @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings";
        SetString(RegistryHive.LocalMachine, key, "PauseFeatureUpdatesStartTime", start);
        SetString(RegistryHive.LocalMachine, key, "PauseFeatureUpdatesEndTime", end);
        SetString(RegistryHive.LocalMachine, key, "PauseQualityUpdatesStartTime", start);
        SetString(RegistryHive.LocalMachine, key, "PauseQualityUpdatesEndTime", end);
        SetString(RegistryHive.LocalMachine, key, "PauseUpdatesStartTime", start);
        SetString(RegistryHive.LocalMachine, key, "PauseUpdatesExpiryTime", end);
    }

    private static void CreatePauseRenewalTask()
    {
        var script = @"
$taskName = 'DeL1ThiSystem\PauseUpdatesRenew';
$ps = @'
$now = [DateTime]::UtcNow
$start = $now.ToString('yyyy-MM-ddTHH:mm:ssK')
$end = $now.AddDays(7).ToString('yyyy-MM-ddTHH:mm:ssK')
$key = 'HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings'
New-Item -Path $key -Force -ErrorAction SilentlyContinue | Out-Null
Set-ItemProperty -Path $key -Name PauseFeatureUpdatesStartTime -Value $start -Type String -Force
Set-ItemProperty -Path $key -Name PauseFeatureUpdatesEndTime -Value $end -Type String -Force
Set-ItemProperty -Path $key -Name PauseQualityUpdatesStartTime -Value $start -Type String -Force
Set-ItemProperty -Path $key -Name PauseQualityUpdatesEndTime -Value $end -Type String -Force
Set-ItemProperty -Path $key -Name PauseUpdatesStartTime -Value $start -Type String -Force
Set-ItemProperty -Path $key -Name PauseUpdatesExpiryTime -Value $end -Type String -Force
'@;
$path = Join-Path $env:ProgramData 'DeL1ThiSystem\Wizard\PauseUpdatesRenew.ps1';
New-Item -ItemType Directory -Path (Split-Path $path) -Force | Out-Null;
$ps | Set-Content -LiteralPath $path -Encoding UTF8;
$tr = 'powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File ""' + $path + '""';
schtasks /Delete /TN $taskName /F >$null 2>$null;
schtasks /Create /F /TN $taskName /RU SYSTEM /RL HIGHEST /SC ONLOGON /TR $tr | Out-Null;
";
        RunPowerShell(script);
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
