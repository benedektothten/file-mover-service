using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FileMoverService.UI;

public static class ConfigService
{
    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "..", "service", "appsettings.json");

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static List<WatchFolderEntry> Load()
    {
        if (!File.Exists(ConfigPath))
            return [];

        var json = File.ReadAllText(ConfigPath);
        var root = JsonNode.Parse(json);
        var folders = root?["AppSettings"]?["WatchFolders"]?.AsArray();

        if (folders == null)
            return [];

        return folders.Select(f => new WatchFolderEntry
        {
            SourceFolder = f?["SourceFolder"]?.GetValue<string>() ?? string.Empty,
            TargetFolder = f?["TargetFolder"]?.GetValue<string>() ?? string.Empty,
            Extensions = string.Join(", ", f?["Extensions"]?.AsArray()
                .Select(e => e?.GetValue<string>() ?? string.Empty) ?? [])
        }).ToList();
    }

    public static void Save(IEnumerable<WatchFolderEntry> entries)
    {
        var json = File.ReadAllText(ConfigPath);
        var root = JsonNode.Parse(json)!;

        var foldersArray = new JsonArray();
        foreach (var entry in entries)
        {
            var exts = entry.Extensions
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => e.StartsWith('.') ? e : $".{e}")
                .ToList();

            var node = new JsonObject
            {
                ["SourceFolder"] = entry.SourceFolder,
                ["TargetFolder"] = entry.TargetFolder,
                ["Extensions"] = new JsonArray(exts.Select(e => JsonValue.Create(e)).ToArray())
            };
            foldersArray.Add(node);
        }

        root["AppSettings"]!["WatchFolders"] = foldersArray;
        File.WriteAllText(ConfigPath, root.ToJsonString(WriteOptions));
    }
}
