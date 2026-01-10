using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static class TweaksJsonLoader
{
    public static ObservableCollection<TweakNode> LoadAsNodes(string osFamily)
    {
        var json = ReadEmbeddedText("DeL1ThiSystem.ConfigurationWizard.Resources.tweaks_catalog.json");
        var model = JsonSerializer.Deserialize<TweaksCatalogJson>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new TweaksCatalogJson();

        var root = new ObservableCollection<TweakNode>();

        foreach (var g in model.Groups)
        {
            var groupNode = new TweakNode
            {
                Id = g.Id,
                Title = g.Title,
                Description = g.Note ?? "",
                IsGroup = true
            };

            foreach (var it in g.Items)
            {
                bool compatible = it.AppliesTo == null || it.AppliesTo.Count == 0 || it.AppliesTo.Contains(osFamily);
                var description = BuildDescription(it, osFamily, compatible);
                groupNode.Children.Add(new TweakNode
                {
                    Id = it.Id,
                    Title = it.Title,
                    Description = description,
                    IsChecked = compatible && it.Default,
                    IsEnabled = compatible,
                    AppliesTo = string.Join(",", it.AppliesTo ?? new()),
                    Stage = it.Stage
                });
            }

            root.Add(groupNode);
        }

        return root;
    }

    public static (string Title, (string Id, string Title)[] Steps) LoadBootstrapSteps()
    {
        var json = ReadEmbeddedText("DeL1ThiSystem.ConfigurationWizard.Resources.bootstrap_steps.json");
        using var doc = JsonDocument.Parse(json);
        var title = doc.RootElement.GetProperty("title").GetString() ?? "Подготовка ОС";

        var steps = doc.RootElement.GetProperty("steps")
            .EnumerateArray()
            .Select(e => (Id: e.GetProperty("id").GetString() ?? "", Title: e.GetProperty("title").GetString() ?? ""))
            .ToArray();

        return (title, steps);
    }

    private static string ReadEmbeddedText(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var s = asm.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    }

    private static string BuildDescription(TweakItemJson item, string osFamily, bool compatible)
    {
        var description = item.Description ?? string.Empty;
        var notes = new List<string>();

        if (!compatible)
        {
            var targetOs = osFamily == "11" ? "Windows 11" : "Windows 10";
            var allowed = item.AppliesTo == null || item.AppliesTo.Count == 0
                ? string.Empty
                : string.Join(", ", item.AppliesTo.Select(x => x == "11" ? "Windows 11" : "Windows 10"));
            var reason = string.IsNullOrWhiteSpace(allowed)
                ? $"Недоступно для {targetOs}."
                : $"Недоступно для {targetOs}. Доступно только для {allowed}.";
            notes.Add(reason);
        }

        if (notes.Count == 0)
            return description;

        if (string.IsNullOrWhiteSpace(description))
            return string.Join("\n", notes);

        return $"{description}\n{string.Join("\n", notes)}";
    }
}
