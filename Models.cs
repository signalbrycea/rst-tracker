// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. PolyForm Noncommercial License 1.0.0, see LICENSE.
// Required Notice: Copyright signalbrycea (https://github.com/signalbrycea)

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RS3Tracker
{
    // Catalog: what can be timed. Read from data/timers.json (embedded copy is the fallback).
    public class Catalog
    {
        [JsonPropertyName("categories")] public List<Category> Categories { get; set; } = new List<Category>();
        [JsonPropertyName("clocks")] public List<Clock> Clocks { get; set; } = new List<Clock>();
    }

    // Fixed-clock reset: group = buyers | weekly | monthly (which tab and heading it sits under),
    // kind = daily | weekly | monthly | cycle (cycle needs anchor "yyyy-MM-dd" and days). All 00:00 UTC.
    public class Clock
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("group")] public string Group { get; set; } = "";
        [JsonPropertyName("kind")] public string Kind { get; set; } = "daily";
        [JsonPropertyName("anchor")] public string? Anchor { get; set; }
        [JsonPropertyName("days")] public int? Days { get; set; }
        [JsonPropertyName("note")] public string? Note { get; set; }
    }

    public class Category
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("items")] public List<Item> Items { get; set; } = new List<Item>();
    }

    public class Item
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("minutes")] public double? Minutes { get; set; }
        [JsonPropertyName("level")] public int? Level { get; set; }
        [JsonPropertyName("note")] public string? Note { get; set; }
        [JsonPropertyName("stages")] public List<Stage>? Stages { get; set; }

        public bool HasStages => Stages != null && Stages.Count > 0;
    }

    public class Stage
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("minutes")] public double Minutes { get; set; }
    }

    // State: the timers on screen plus settings. Saved on every change.
    public class AppState
    {
        public List<TimerEntry> Timers { get; set; } = new List<TimerEntry>();
        public double Volume { get; set; } = 0.6;
        public bool Muted { get; set; } = false;
        public bool AlwaysOnTop { get; set; } = false;
        public bool ConfirmReset { get; set; } = true;
        public bool ConfirmRemove { get; set; } = true;
        public string Theme { get; set; } = "dark";
        public string Tab { get; set; } = "Farming";   // last open tab: Farming, Buyers or Resets
        public double WindowWidth { get; set; } = 620;
        public double WindowHeight { get; set; } = 520;
    }

    public class TimerEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Category { get; set; } = "";
        public string Item { get; set; } = "";
        public string? Stage { get; set; }
        public string Label { get; set; } = "";
        public double Minutes { get; set; }
        public DateTime? StartUtc { get; set; }
        public DateTime? EndUtc { get; set; }
        public bool Notified { get; set; }
    }
}
