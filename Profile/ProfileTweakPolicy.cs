using System;
using System.Collections.Generic;

namespace DeL1ThiSystem.ConfigurationWizard.Profile;

public static class ProfileTweakPolicy
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "ui.color_theme",
        "shell.classic_context_menu",
        "shell.show_file_extensions",
        "shell.hide_task_view",
        "shell.meet_now_disable",
        "shell.search_box_mode",
        "shell.start_tiles_clear",
        "shell.explorer_launch_to_this_pc",
        "shell.desktop_icons_minimal",
        "shell.taskbar_clear_pins",
        "shell.taskbar_end_task",
        "shell.tray_show_all_icons",
        "shell.remove_edge_desktop_shortcut",
        "shell.win11_start_recommended_disable"
    };

    public static bool IsProfileApplicable(string id) => Allowed.Contains(id);
}
