// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. PolyForm Noncommercial License 1.0.0, see LICENSE.
// Required Notice: Copyright signalbrycea (https://github.com/signalbrycea)

using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace RS3Tracker
{
    public static class Storage
    {
        static readonly JsonSerializerOptions Opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static string BaseDir => AppContext.BaseDirectory;
        public static string? CatalogPathUsed { get; private set; }
        public static string StatePath { get; private set; } = Path.Combine(AppContext.BaseDirectory, "rs3tracker-state.json");

        // Looks for an editable copy next to the exe (or up the tree when run from bin/), else the embedded one.
        public static Catalog LoadCatalog()
        {
            var dir = new DirectoryInfo(BaseDir);
            for (int i = 0; i < 5 && dir != null; i++, dir = dir.Parent)
            {
                foreach (var candidate in new[] { Path.Combine(dir.FullName, "timers.json"), Path.Combine(dir.FullName, "data", "timers.json") })
                {
                    if (File.Exists(candidate))
                    {
                        CatalogPathUsed = candidate;
                        return JsonSerializer.Deserialize<Catalog>(File.ReadAllText(candidate), Opts) ?? new Catalog();
                    }
                }
            }
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("timers.json")
                ?? throw new FileNotFoundException("Embedded timers.json missing");
            CatalogPathUsed = "(built in)";
            return JsonSerializer.Deserialize<Catalog>(stream, Opts) ?? new Catalog();
        }

        static string AppDataStatePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RS3Tracker", "state.json");

        public static AppState LoadState()
        {
            var local = Path.Combine(BaseDir, "rs3tracker-state.json");
            var path = File.Exists(local) ? local : (File.Exists(AppDataStatePath) ? AppDataStatePath : local);
            StatePath = path;
            if (!File.Exists(path)) return new AppState();
            try { return JsonSerializer.Deserialize<AppState>(File.ReadAllText(path), Opts) ?? new AppState(); }
            catch { return new AppState(); }
        }

        public static void SaveState(AppState state)
        {
            var json = JsonSerializer.Serialize(state, Opts);
            try
            {
                File.WriteAllText(StatePath, json);
            }
            catch (Exception) when (StatePath != AppDataStatePath)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AppDataStatePath)!);
                StatePath = AppDataStatePath;
                File.WriteAllText(StatePath, json);
            }
        }
    }
}
