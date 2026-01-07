using System.Collections.Generic;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks
{
    public static class TweaksCatalog
    {
        public static IReadOnlyList<TweakNode> Build()
        {
            return new List<TweakNode>
            {
        new TweakNode("group.debloat", "Удаление и очистка", "Удаление UWP, Capabilities/Features и прочее.")
            .Add(
        new TweakNode("debloat.appx", "Удаление UWP-приложений", "Удаляет предустановленные Appx (Store, Xbox, Widgets и др.).")
            .Add(
            new TweakNode("debloat.appx.clipchamp.clipchamp", "Удалить Clipchamp", "Package: Clipchamp.Clipchamp")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.549981c3f5f10", "Удалить Cortana", "Package: Microsoft.549981C3F5F10")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.copilot", "Удалить Copilot", "Package: Microsoft.Copilot")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.gethelp", "Удалить Get Help", "Package: Microsoft.GetHelp")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.getstarted", "Удалить Tips (Get Started)", "Package: Microsoft.Getstarted")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.microsoftofficehub", "Удалить Office Hub", "Package: Microsoft.MicrosoftOfficeHub")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.microsoftsolitairecollection", "Удалить Microsoft Solitaire", "Package: Microsoft.MicrosoftSolitaireCollection")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.microsoftstickynotes", "Удалить Sticky Notes", "Package: Microsoft.MicrosoftStickyNotes")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.mspaint", "Удалить Paint", "Package: Microsoft.MSPaint")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.people", "Удалить People", "Package: Microsoft.People")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.powerautomatedesktop", "Удалить Power Automate", "Package: Microsoft.PowerAutomateDesktop")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.todos", "Удалить Microsoft To Do", "Package: Microsoft.Todos")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.windowsalarms", "Удалить Alarms & Clock", "Package: Microsoft.WindowsAlarms")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.windowscamera", "Удалить Camera", "Package: Microsoft.WindowsCamera")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.windowsfeedbackhub", "Удалить Feedback Hub", "Package: Microsoft.WindowsFeedbackHub")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.windowsmaps", "Удалить Maps", "Package: Microsoft.WindowsMaps")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.windowssoundrecorder", "Удалить Sound Recorder", "Package: Microsoft.WindowsSoundRecorder")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.windowsterminal", "Удалить Windows Terminal", "Package: Microsoft.WindowsTerminal")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.xboxapp", "Удалить Xbox", "Package: Microsoft.XboxApp")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.xboxgameoverlay", "Удалить Xbox Game Overlay", "Package: Microsoft.XboxGameOverlay")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.xboxgamingoverlay", "Удалить Xbox Gaming Overlay", "Package: Microsoft.XboxGamingOverlay")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.xboxidentityprovider", "Удалить Xbox Identity Provider", "Package: Microsoft.XboxIdentityProvider")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.xboxspeechtotextoverlay", "Удалить Xbox Speech to Text Overlay", "Package: Microsoft.XboxSpeechToTextOverlay")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.yourphone", "Удалить Phone Link", "Package: Microsoft.YourPhone")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.zunemusic", "Удалить Groove Music", "Package: Microsoft.ZuneMusic")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.zunevideo", "Удалить Movies & TV", "Package: Microsoft.ZuneVideo")
            )
            .Add(
            new TweakNode("debloat.appx.microsoftwindows.client.webexperience", "Удалить Widgets Runtime", "Package: MicrosoftWindows.Client.WebExperience")
            )
            .Add(
            new TweakNode("debloat.appx.microsoft.windowsstore", "Удалить Microsoft Store", "Package: Microsoft.WindowsStore")
            )
            )
            .Add(
        new TweakNode("debloat.capabilities", "Удаление возможностей (Capabilities)", "Удаляет OneSync, Quick Assist, Steps Recorder.")
            .Add(
            new TweakNode("debloat.capability.onesync", "Удалить OneSync Capability", "Capability: OneCoreUAP.OneSync")
            )
            .Add(
            new TweakNode("debloat.capability.quickassist", "Удалить Quick Assist", "Capability: App.Support.QuickAssist")
            )
            .Add(
            new TweakNode("debloat.capability.stepsrecorder", "Удалить Steps Recorder", "Capability: App.StepsRecorder")
            )
            )
            .Add(
        new TweakNode("debloat.features", "Удаление компонентов (Features)", "Удаляет/отключает системные Features.")
            .Add(
            new TweakNode("debloat.feature.recall", "Отключить / удалить Recall", "Windows Feature: Recall")
            )
            )
            .Add(
        new TweakNode("debloat.edge_uninstallable", "Сделать Microsoft Edge удаляемым", "Скрипт: MakeEdgeUninstallable.ps1")
            ),

        new TweakNode("group.updates", "Обновления Windows", "Политики и пауза обновлений.")
            .Add(
        new TweakNode("updates.pause", "Пауза и политика обновлений", "PauseWindowsUpdate (PauseWindowsUpdate.ps1/.xml)")
            )
            .Add(
        new TweakNode("updates.disable_chat", "Отключить автоустановку Chat", "HKLM: ConfigureChatAutoInstall=0")
            ),

        new TweakNode("group.privacy", "Конфиденциальность и рекомендации", "Реклама, поиск, новости.")
            .Add(
        new TweakNode("privacy.disable_ads", "Отключить consumer features и рекламу", "DisableWindowsConsumerFeatures=1, ContentDeliveryManager=0")
            )
            .Add(
        new TweakNode("privacy.disable_search_suggestions", "Отключить Bing suggestions в поиске", "DisableSearchBoxSuggestions=1")
            )
            .Add(
        new TweakNode("privacy.disable_news", "Отключить новости/интересы", "AllowNewsAndInterests=0")
            ),

        new TweakNode("group.system", "Системные параметры", "UAC, RDP, SmartScreen и др.")
            .Add(
        new TweakNode("system.bypass_tpm_cpu", "Bypass TPM/CPU check (Win11)", "AllowUpgradesWithUnsupportedTPMOrCPU=1")
            )
            .Add(
        new TweakNode("system.bypass_nro", "Bypass NRO (локальная учётная запись)", "OOBE BypassNRO=1")
            )
            .Add(
        new TweakNode("system.disable_smartscreen", "Отключить SmartScreen", "SmartScreenEnabled=Off + Edge SmartScreen off")
            )
            .Add(
        new TweakNode("system.disable_defender_notifications", "Отключить уведомления Windows Security", "Policy DisableNotifications=1 + HideSystray=1")
            )
            .Add(
        new TweakNode("system.disable_uac", "Отключить UAC", "EnableLUA=0")
            )
            .Add(
        new TweakNode("system.long_paths", "Включить Long Paths", "LongPathsEnabled=1")
            )
            .Add(
        new TweakNode("system.enable_rdp", "Включить RDP", "fDenyTSConnections=0")
            )
            .Add(
        new TweakNode("system.disable_fast_startup", "Отключить быстрый запуск", "HiberbootEnabled=0")
            )
            .Add(
        new TweakNode("system.prevent_device_encryption", "Отключить Device Encryption", "PreventDeviceEncryption=1")
            )
            .Add(
        new TweakNode("system.sticky_keys_off", "Отключить Sticky Keys", "Flags=10 (Default + DefaultUser)")
            ),

        new TweakNode("group.shell", "Пуск и панель задач", "Пины, трее, параметры Проводника.")
            .Add(
        new TweakNode("shell.start_pins", "Настроить Пуск (Pins)", "SetStartPins.ps1")
            )
            .Add(
        new TweakNode("shell.taskbar_end_task", "Включить End task на панели задач", "TaskbarEndTask=1")
            )
            .Add(
        new TweakNode("shell.hide_task_view", "Скрыть кнопку Task View", "ShowTaskViewButton=0")
            )
            .Add(
        new TweakNode("shell.show_file_ext", "Показывать расширения файлов", "HideFileExt=0")
            )
            .Add(
        new TweakNode("shell.show_all_tray_icons", "Показывать все значки в трее", "ShowAllTrayIcons.ps1/.xml")
            ),

        new TweakNode("group.ui", "Оформление", "Цветовая тема, обои.")
            .Add(
        new TweakNode("ui.color_theme", "Настроить цветовую тему", "SetColorTheme.ps1")
            )
            .Add(
        new TweakNode("ui.wallpaper", "Установить обои и экран блокировки", "GetWallpaper/GetLockScreenImage + SetWallpaper.ps1")
            ),

        new TweakNode("group.components", "Дополнительно", "Установка ПО, Toolbox, активация.")
            .Add(
        new TweakNode("components.apps", "Установка ПО", "AOMEI Backuper, Uninstall Tool, RustDesk.")
            )
            .Add(
        new TweakNode("components.toolbox", "Установка Toolbox", "Удобный инструмент для дальнейшей настройки системы.")
            )
            .Add(
        new TweakNode("components.hwid", "Активация Windows (HWID)", "Требуется доступ к интернету.")
            )
            };
        }
    }
}
