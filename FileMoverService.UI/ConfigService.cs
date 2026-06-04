using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FileMoverService.UI;

public static class ConfigService
{
    internal static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "FileMoverService", "appsettings.json");

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
        var resolvedPath = Path.GetFullPath(ConfigPath);
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath)!);
        Logger.Log($"ConfigService.Save: resolved path = {resolvedPath}, exists = {File.Exists(resolvedPath)}");
        var json = File.ReadAllText(resolvedPath);
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
        File.WriteAllText(resolvedPath, root.ToJsonString(WriteOptions));
    }
}
