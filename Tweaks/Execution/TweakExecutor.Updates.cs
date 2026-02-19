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

    private static void PauseWindowsUpdate()
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


    private static void DisableConsumerFeatures(string osFamily)
    {
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableSoftLanding", 1);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableThirdPartySuggestions", 1);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", 2);
        foreach (var name in ContentDeliveryValues)
            SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", name, 0);
        ApplyDefaultUserContentDelivery();
    }


    private static void DisableWidgetsAndNews()
    {
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowWidgets", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds", "EnableFeeds", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Feeds", "ShellFeedsTaskbarViewMode", 2);
        SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Feeds", "ShellFeedsTaskbarViewMode", 2);
    }

}
