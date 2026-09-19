// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. PolyForm Noncommercial License 1.0.0, see LICENSE.
// Required Notice: Copyright signalbrycea (https://github.com/signalbrycea)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RS3Tracker
{
    public partial class AddTimerWindow : Window
    {
        public TimerEntry? Result { get; private set; }

        sealed class Hit
        {
            public Category Cat = null!;
            public Item It = null!;
            public Stage? St;
            public string Display = "";
        }

        readonly List<Hit> _all = new List<Hit>();
        List<Hit> _shown = new List<Hit>();
        bool _suppress;

        public AddTimerWindow()
        {
            InitializeComponent();
            DarkTitle.Apply(this);
            foreach (var cat in App.Catalog.Categories)
            {
                foreach (var it in cat.Items)
                {
                    if (it.HasStages)
                        foreach (var st in it.Stages!)
                            _all.Add(new Hit { Cat = cat, It = it, St = st, Display = $"{it.Name}, {st.Name.ToLowerInvariant()}    ({cat.Name})" });
                    else
                        _all.Add(new Hit { Cat = cat, It = it, Display = $"{it.Name}    ({cat.Name})" });
                }
            }
            CategoryBox.ItemsSource = App.Catalog.Categories.Select(c => c.Name).ToList();
            Loaded += (_, _) => SearchBox.Focus();
        }

        Category? SelectedCategory => CategoryBox.SelectedIndex >= 0 ? App.Catalog.Categories[CategoryBox.SelectedIndex] : null;
        Item? SelectedItem => SelectedCategory != null && ItemBox.SelectedIndex >= 0 ? SelectedCategory.Items[ItemBox.SelectedIndex] : null;
        Stage? SelectedStage => SelectedItem != null && SelectedItem.HasStages && StageBox.SelectedIndex >= 0 ? SelectedItem.Stages![StageBox.SelectedIndex] : null;

        double SelectedMinutes()
        {
            var it = SelectedItem;
            if (it == null) return 0;
            if (it.HasStages) return SelectedStage?.Minutes ?? 0;
            return it.Minutes ?? 0;
        }

        void Category_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress) return;
            var cat = SelectedCategory;
            ItemBox.ItemsSource = cat?.Items.Select(i => i.Name).ToList();
            ItemBox.SelectedIndex = -1;
            StagePanel.Visibility = Visibility.Collapsed;
            UpdateInfo();
        }

        void Item_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress) return;
            var it = SelectedItem;
            if (it != null && it.HasStages)
            {
                StageBox.ItemsSource = it.Stages!.Select(s => s.Name).ToList();
                StageBox.SelectedIndex = it.Stages!.Count - 1;
                StagePanel.Visibility = Visibility.Visible;
            }
            else
            {
                StagePanel.Visibility = Visibility.Collapsed;
            }
            UpdateInfo();
        }

        void Stage_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress) return;
            UpdateInfo();
        }

        void UpdateInfo()
        {
            var it = SelectedItem;
            var mins = SelectedMinutes();
            AddButton.IsEnabled = it != null && mins > 0;
            if (it == null) { Info.Text = ""; return; }
            var parts = new List<string> { $"{it.Name}: {FmtMinutes(mins)}" };
            if (it.Level != null) parts.Add($"level {it.Level}");
            if (!string.IsNullOrWhiteSpace(it.Note)) parts.Add(it.Note!);
            Info.Text = string.Join(". ", parts);
        }

        static string FmtMinutes(double mins)
        {
            var t = TimeSpan.FromMinutes(mins);
            if (t.TotalHours < 1) return $"{mins:0.#} min";
            if (t.TotalDays < 1) return t.Minutes == 0 ? $"{(int)t.TotalHours} h" : $"{(int)t.TotalHours} h {t.Minutes} min";
            return $"{(int)t.TotalDays} d {t.Hours} h {t.Minutes} min";
        }

        bool _suppressSearch;

        void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressSearch) return;
            var q = SearchBox.Text.Trim();
            if (q.Length == 0) { Results.Visibility = Visibility.Collapsed; Results.ItemsSource = null; _shown.Clear(); return; }
            var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            _shown = _all.Where(h => words.All(w => h.Display.Contains(w, StringComparison.OrdinalIgnoreCase))).Take(80).ToList();
            Results.ItemsSource = _shown.Select(h => h.Display).ToList();
            Results.Visibility = Visibility.Visible;
        }

        void Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && _shown.Count > 0)
            {
                Results.SelectedIndex = 0;
                (Results.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem)?.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && _shown.Count > 0)
            {
                Apply(_shown[Math.Max(0, Results.SelectedIndex)]);
                if (AddButton.IsEnabled) Add_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && Results.Visibility == Visibility.Visible)
            {
                Results.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
        }

        // A mouse pick is final: show it in the search box, close the list, move on to the label.
        // Arrow keys leave the list open so the user can keep browsing (SelectionChanged already applies each one).
        void Results_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Results.SelectedIndex < 0 || Results.SelectedIndex >= _shown.Count) return;
            _suppressSearch = true;
            SearchBox.Text = _shown[Results.SelectedIndex].Display;
            _suppressSearch = false;
            Results.Visibility = Visibility.Collapsed;
            LabelBox.Focus();
        }

        void Results_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Results.SelectedIndex >= 0 && Results.SelectedIndex < _shown.Count) Apply(_shown[Results.SelectedIndex]);
        }

        void Results_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && AddButton.IsEnabled) { Add_Click(sender, e); e.Handled = true; }
        }

        void Apply(Hit h)
        {
            _suppress = true;
            CategoryBox.SelectedIndex = App.Catalog.Categories.IndexOf(h.Cat);
            ItemBox.ItemsSource = h.Cat.Items.Select(i => i.Name).ToList();
            ItemBox.SelectedIndex = h.Cat.Items.IndexOf(h.It);
            if (h.It.HasStages)
            {
                StageBox.ItemsSource = h.It.Stages!.Select(s => s.Name).ToList();
                StageBox.SelectedIndex = h.St != null ? h.It.Stages!.IndexOf(h.St) : h.It.Stages!.Count - 1;
                StagePanel.Visibility = Visibility.Visible;
            }
            else StagePanel.Visibility = Visibility.Collapsed;
            _suppress = false;
            UpdateInfo();
        }

        void Label_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && AddButton.IsEnabled) Add_Click(sender, e);
        }

        void Add_Click(object sender, RoutedEventArgs e)
        {
            var it = SelectedItem;
            var cat = SelectedCategory;
            var mins = SelectedMinutes();
            if (it == null || cat == null || mins <= 0) return;
            Result = new TimerEntry
            {
                Category = cat.Name,
                Item = it.Name,
                Stage = SelectedStage?.Name,
                Minutes = mins,
                Label = LabelBox.Text.Trim(),
            };
            DialogResult = true;
        }

        void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
