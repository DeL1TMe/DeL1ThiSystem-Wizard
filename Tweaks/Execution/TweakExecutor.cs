using System;
using System.IO;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static partial class TweakExecutor
{
    private static readonly object InternetGateSync = new();
    private static Action? _waitInternetGate;

    private static readonly string BaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "DeL1ThiSystem",
        "Wizard");

    private static readonly string LogPath = Path.Combine(BaseDir, "Wizard.log");
    private static int _procSeq = 0;

    private static readonly string[] ContentDeliveryValues =
    {
        "ContentDeliveryAllowed",
        "FeatureManagementEnabled",
        "OEMPreInstalledAppsEnabled",
        "PreInstalledAppsEnabled",
        "PreInstalledAppsEverEnabled",
        "SilentInstalledAppsEnabled",
        "SoftLandingEnabled",
        "SubscribedContentEnabled",
        "SubscribedContent-310093Enabled",
        "SubscribedContent-338387Enabled",
        "SubscribedContent-338388Enabled",
        "SubscribedContent-338389Enabled",
        "SubscribedContent-338393Enabled",
        "SubscribedContent-353694Enabled",
        "SubscribedContent-353696Enabled",
        "SubscribedContent-353698Enabled",
        "SystemPaneSuggestionsEnabled"
    };

    private static readonly string[] AppxSelectors =
    {
        "Microsoft.Microsoft3DViewer",
        "Microsoft.BingSearch",
        "Clipchamp.Clipchamp",
        "Microsoft.Copilot",
        "Microsoft.549981C3F5F10",
        "Microsoft.Windows.DevHome",
        "A025C540.Yandex.Music",
        "MicrosoftCorporationII.MicrosoftFamily",
        "Microsoft.WindowsFeedbackHub",
        "Microsoft.Edge.GameAssist",
        "Microsoft.GetHelp",
        "Microsoft.Getstarted",
        "Microsoft.BingNews",
        "Microsoft.MicrosoftOfficeHub",
        "Microsoft.Office.OneNote",
        "Microsoft.OutlookForWindows",
        "Microsoft.MSPaint",
        "Microsoft.People",
        "Microsoft.PowerAutomateDesktop",
        "MicrosoftCorporationII.QuickAssist",
        "Microsoft.SkypeApp",
        "Microsoft.MicrosoftSolitaireCollection",
        "Microsoft.MicrosoftStickyNotes",
        "MicrosoftTeams",
        "MSTeams",
        "Microsoft.Todos",
        "Microsoft.Wallet",
        "Microsoft.BingWeather",
        "Microsoft.Xbox.TCUI",
        "Microsoft.XboxApp",
        "Microsoft.XboxGameOverlay",
        "Microsoft.XboxGamingOverlay",
        "Microsoft.XboxIdentityProvider",
        "Microsoft.XboxSpeechToTextOverlay",
        "Microsoft.GamingApp",
        "Microsoft.ZuneVideo",
        "Microsoft.WindowsStore",
        "Microsoft.StorePurchaseApp"
    };

    private static readonly string[] CapabilitySelectors =
    {
        "OneCoreUAP.OneSync",
        "App.Support.QuickAssist",
        "App.StepsRecorder"
    };

    private static readonly string[] FeatureSelectors =
    {
        "Recall"
    };

    public static void SetInternetGate(Action? waitInternetGate)
    {
        lock (InternetGateSync)
        {
            _waitInternetGate = waitInternetGate;
        }
    }

    private static void WaitInternetGateIfNeeded()
    {
        Action? gate;
        lock (InternetGateSync)
        {
            gate = _waitInternetGate;
        }

        if (gate == null)
            return;

        try
        {
            gate();
        }
        catch (Exception ex)
        {
            Log($"INTERNET GATE ERROR: {ex.Message}");
        }
    }
}
