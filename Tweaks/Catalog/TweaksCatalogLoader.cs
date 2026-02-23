using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static class TweaksCatalogLoader
{
    private const string TweaksCatalogResource = "DeL1ThiSystem.ConfigurationWizard.Resources.tweaks_catalog.json";
    private const string BootstrapStepsResource = "DeL1ThiSystem.ConfigurationWizard.Resources.bootstrap_steps.json";

    public static ObservableCollection<TweakNode> LoadAsNodes(string osFamily)
    {
        var json = ReadEmbeddedText(TweaksCatalogResource);
        var model = JsonSerializer.Deserialize<TweaksCatalogJson>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new TweaksCatalogJson();

        var root = new ObservableCollection<TweakNode>();

        foreach (var g in model.Groups)
        {
            foreach (var it in g.Items)
            {
                bool compatible = it.AppliesTo == null || it.AppliesTo.Count == 0 || it.AppliesTo.Contains(osFamily);
                var description = BuildDescription(it, osFamily, compatible);
                root.Add(new TweakNode
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
        }

        if (!root.Any(n => string.Equals(n.Id, "system.configure_ru_ru_locale_utf8", StringComparison.OrdinalIgnoreCase)))
        {
            root.Add(new TweakNode
            {
                Id = "system.configure_ru_ru_locale_utf8",
                Title = "Установить русский язык системы + UTF-8",
                Description = "Скачивает и применяет ru-RU (Display Language, формат, раскладки) и включает UTF-8 для non-Unicode программ.",
                IsChecked = true,
                IsEnabled = true,
                AppliesTo = "10,11",
                Stage = "tweak"
            });
        }

        if (!root.Any(n => string.Equals(n.Id, "perf.memory_integrity_disable", StringComparison.OrdinalIgnoreCase)))
        {
            root.Add(new TweakNode
            {
                Id = "perf.memory_integrity_disable",
                Title = "Отключить целостность памяти (Core Isolation)",
                Description = "Отключает Memory Integrity (HVCI).",
                IsChecked = true,
                IsEnabled = true,
                AppliesTo = "10,11",
                Stage = "tweak"
            });
        }

        return root;
    }

    public static (string Title, (string Id, string Title)[] Steps) LoadBootstrapSteps()
    {
        var json = ReadEmbeddedText(BootstrapStepsResource);
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
        return item.Description ?? string.Empty;
    }
}
