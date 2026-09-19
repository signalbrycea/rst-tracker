// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. Licensed under the MIT License, see LICENSE.

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace RS3Tracker
{
    // Dark and light palettes. Switching swaps the shared brush resources; XAML uses DynamicResource so open windows update at once.
    public static class Theme
    {
        static readonly Dictionary<string, string> Dark = new Dictionary<string, string>
        {
            ["BgBrush"] = "#1b1b20", ["PanelBrush"] = "#26262e", ["PanelHoverBrush"] = "#31313b", ["BorderBrush"] = "#3a3a46",
            ["TextBrush"] = "#e8e8ec", ["MutedBrush"] = "#9a9aa8", ["AccentBrush"] = "#4fa3ff", ["GreenBrush"] = "#3ddc84",
            ["GreenDimBrush"] = "#1f3a2c", ["RedBrush"] = "#ff6b6b", ["SelectedBrush"] = "#2d4a6b",
        };
        static readonly Dictionary<string, string> Light = new Dictionary<string, string>
        {
            ["BgBrush"] = "#f2f2f5", ["PanelBrush"] = "#ffffff", ["PanelHoverBrush"] = "#e6e6ec", ["BorderBrush"] = "#c9c9d3",
            ["TextBrush"] = "#1b1b20", ["MutedBrush"] = "#5d5d6b", ["AccentBrush"] = "#2f7fd6", ["GreenBrush"] = "#1e9e5a",
            ["GreenDimBrush"] = "#dcf4e6", ["RedBrush"] = "#d64545", ["SelectedBrush"] = "#cfe0f5",
        };

        public static bool IsDark => App.State.Theme != "light";

        public static void Apply(string name)
        {
            var palette = name == "light" ? Light : Dark;
            foreach (var kv in palette)
            {
                var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(kv.Value));
                b.Freeze();
                Application.Current.Resources[kv.Key] = b;
            }
            foreach (Window w in Application.Current.Windows) DarkTitle.Set(w, name != "light");
        }

        public static void Toggle()
        {
            App.State.Theme = IsDark ? "light" : "dark";
            Apply(App.State.Theme);
            App.SaveState();
        }
    }
}
