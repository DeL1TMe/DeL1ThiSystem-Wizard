using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard.Profile;

public static class ProfileInitPlanBuilder
{
    private static readonly (string Id, string Title)[] ForcedUserBaseline =
    [
        ("profile.sticky_keys_disable", "Отключить Sticky Keys (профиль)"),
        ("profile.enhance_pointer_precision_disable", "Отключить Enhance pointer precision (профиль)"),
        ("profile.wallpaper_quality_100", "Качество обоев 100% (профиль)")
    ];

    public static (string ThemeChoice, (string Id, string Title)[] Steps) Build(string osFamily)
    {
        var selection = ProfileSelectionStore.Load();
        var themeChoice = selection?.ThemeChoice ?? "dark";

        var catalog = TweaksCatalogLoader.LoadAsNodes(osFamily)
            .ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);
        var bootstrap = TweaksCatalogLoader.LoadBootstrapSteps();

        var steps = new List<(string Id, string Title)>();
        var debug = new List<string>
        {
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ProfileInitPlan build started",
            $"OS={osFamily}",
            $"Mode=StateBasedAllProfileTweaks"
        };

        foreach (var node in catalog.Values)
        {
            var id = node.Id;
            if (!ProfileTweakPolicy.IsProfileApplicable(id))
            {
                debug.Add($"SKIP not profile-applicable: {id}");
                continue;
            }

            if (!node.IsEnabled)
            {
                debug.Add($"SKIP disabled in catalog: {id}");
                continue;
            }

            steps.Add((id, node.Title));
            debug.Add($"ADD {id}");
        }

        foreach (var step in bootstrap.Steps)
        {
            if (!ProfileTweakPolicy.IsProfileApplicable(step.Id))
            {
                debug.Add($"SKIP bootstrap not profile-applicable: {step.Id}");
                continue;
            }

            steps.Add((step.Id, step.Title));
            debug.Add($"ADD bootstrap {step.Id}");
        }

        foreach (var forced in ForcedUserBaseline)
        {
            if (!ProfileTweakPolicy.IsProfileApplicable(forced.Id))
            {
                debug.Add($"SKIP forced not profile-applicable: {forced.Id}");
                continue;
            }

            steps.Add(forced);
            debug.Add($"ADD forced {forced.Id}");
        }

        if (!steps.Any(s => string.Equals(s.Id, "ui.color_theme", StringComparison.OrdinalIgnoreCase)))
        {
            steps.Insert(0, ("ui.color_theme", "Применяем тему"));
            debug.Add("ADD ui.color_theme (forced)");
        }

        var result = steps.DistinctBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToArray();
        debug.Add($"ResultSteps={result.Length}");
        TryWriteDebugLog(debug);

        return (themeChoice, result);
    }

    private static void TryWriteDebugLog(IEnumerable<string> lines)
    {
        try
        {
            Directory.CreateDirectory(ProfileInitPaths.BaseDir);
            var path = Path.Combine(ProfileInitPaths.BaseDir, "Wizard.log");
            var mapped = lines.Select(l => $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [PROFILE-PLAN] {l}");
            File.AppendAllLines(path, mapped, new System.Text.UTF8Encoding(false));
        }
        catch
        {
        }
    }

    private static IEnumerable<TSource> DistinctBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey> comparer)
    {
        var set = new HashSet<TKey>(comparer);
        foreach (var item in source)
        {
            if (set.Add(keySelector(item)))
                yield return item;
        }
    }
}
