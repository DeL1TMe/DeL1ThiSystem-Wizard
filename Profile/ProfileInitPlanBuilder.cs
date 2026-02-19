using System;
using System.Collections.Generic;
using System.Linq;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard.Profile;

public static class ProfileInitPlanBuilder
{
    public static (string ThemeChoice, (string Id, string Title)[] Steps) Build(string osFamily)
    {
        var selection = ProfileSelectionStore.Load();
        if (selection == null)
            return ("dark", Array.Empty<(string, string)>());

        var catalog = TweaksCatalogLoader.LoadAsNodes(osFamily)
            .ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);

        var steps = new List<(string Id, string Title)>();
        foreach (var id in selection.SelectedIds)
        {
            if (!ProfileTweakPolicy.IsProfileApplicable(id))
                continue;
            if (!catalog.TryGetValue(id, out var node))
                continue;
            if (!node.IsEnabled)
                continue;
            steps.Add((id, node.Title));
        }

        if (!steps.Any(s => string.Equals(s.Id, "ui.color_theme", StringComparison.OrdinalIgnoreCase)))
            steps.Insert(0, ("ui.color_theme", "Применяем тему"));

        return (selection.ThemeChoice, steps.DistinctBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToArray());
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
