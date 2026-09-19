// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. Licensed under the MIT License, see LICENSE.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace RS3Tracker
{
    // One row on screen. Wraps a TimerEntry and exposes display text the XAML binds to.
    public class TimerRow : INotifyPropertyChanged
    {
        public TimerEntry Entry { get; }
        public TimerRow(TimerEntry entry) { Entry = entry; }

        public event PropertyChangedEventHandler? PropertyChanged;
        void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string Title => Entry.Stage != null ? $"{Entry.Item}, {Entry.Stage.ToLowerInvariant()}" : Entry.Item;
        // Hover text: the full title (the row trims long ones) and the full label, or the hint when there is none.
        public string TitleTip => Title + Environment.NewLine + Entry.Category;
        public string LabelTip => string.IsNullOrWhiteSpace(Entry.Label) ? "Your own label, for example north patch or yak pen. Click to type, Enter to save." : Entry.Label;
        public string CategoryShort => Entry.Category.Replace("Farming: ", "").Replace("Player-owned farm: ", "PoF ");

        public string Label
        {
            get => Entry.Label;
            set
            {
                var v = value ?? "";
                if (v == Entry.Label) return;
                Entry.Label = v;
                Changed(nameof(Label));
                Changed(nameof(LabelTip));
                App.SaveState();
            }
        }

        string _remaining = "", _sub = "";
        Brush _time = Brushes.White, _row = Brushes.Transparent, _border = Brushes.Transparent;
        public string Remaining { get => _remaining; private set { if (_remaining != value) { _remaining = value; Changed(nameof(Remaining)); } } }
        public string SubText { get => _sub; private set { if (_sub != value) { _sub = value; Changed(nameof(SubText)); } } }
        public Brush TimeBrush { get => _time; private set { if (_time != value) { _time = value; Changed(nameof(TimeBrush)); } } }
        public Brush RowBrush { get => _row; private set { if (_row != value) { _row = value; Changed(nameof(RowBrush)); } } }
        public Brush BorderBrushValue { get => _border; private set { if (_border != value) { _border = value; Changed(nameof(BorderBrushValue)); } } }

        public bool IsDone { get; private set; }
        public bool IsRunning => Entry.EndUtc != null;

        // Start only works on an idle or finished row. A running timer cannot be restarted by a stray click; Reset it first.
        bool _canStart = true;
        public bool CanStart { get => _canStart; private set { if (_canStart != value) { _canStart = value; Changed(nameof(CanStart)); } } }
        public string StartTip => CanStart ? "Start the timer" : "Running. Use Reset first if you really want to start over.";

        static Brush Res(string key) => (Brush)Application.Current.Resources[key];

        // Refreshes the display. Returns true the first time this timer is seen finished, so the caller can alert once.
        public bool Tick(DateTime nowUtc)
        {
            CanStart = Entry.EndUtc == null || Entry.EndUtc.Value <= nowUtc;
            Changed(nameof(StartTip));
            if (Entry.EndUtc == null)
            {
                IsDone = false;
                Remaining = Fmt(TimeSpan.FromMinutes(Entry.Minutes));
                SubText = "not started";
                TimeBrush = Res("MutedBrush"); RowBrush = Res("PanelBrush"); BorderBrushValue = Res("BorderBrush");
                return false;
            }
            var left = Entry.EndUtc.Value - nowUtc;
            if (left <= TimeSpan.Zero)
            {
                IsDone = true;
                Remaining = "READY";
                SubText = "since " + Clock(Entry.EndUtc.Value);
                TimeBrush = Res("GreenBrush"); RowBrush = Res("GreenDimBrush"); BorderBrushValue = Res("GreenBrush");
                if (!Entry.Notified) { Entry.Notified = true; return true; }
                return false;
            }
            IsDone = false;
            Remaining = Fmt(left);
            SubText = "ready " + ClockAt(Entry.EndUtc.Value);
            TimeBrush = Res("TextBrush"); RowBrush = Res("PanelBrush"); BorderBrushValue = Res("BorderBrush");
            return false;
        }

        static string Clock(DateTime utc)
        {
            var local = utc.ToLocalTime();
            return local.Date == DateTime.Today ? local.ToString("HH:mm") : local.ToString("ddd HH:mm");
        }

        static string ClockAt(DateTime utc) => utc.ToLocalTime().Date == DateTime.Today ? "at " + Clock(utc) : Clock(utc);

        static string Fmt(TimeSpan t)
        {
            var secs = (long)Math.Ceiling(t.TotalSeconds);
            var days = secs / 86400; secs %= 86400;
            var h = secs / 3600; var m = (secs % 3600) / 60; var s = secs % 60;
            return days > 0 ? $"{days}d {h:00}:{m:00}:{s:00}" : $"{h:00}:{m:00}:{s:00}";
        }
    }

    public partial class MainWindow : Window
    {
        readonly ObservableCollection<TimerRow> _rows = new ObservableCollection<TimerRow>();
        readonly DispatcherTimer _tick;
        Forms.NotifyIcon? _tray;

        public MainWindow()
        {
            InitializeComponent();
            DarkTitle.Apply(this);
            Width = App.State.WindowWidth;
            Height = App.State.WindowHeight;
            Topmost = App.State.AlwaysOnTop;
            UpdateThemeButton();
            Rows.ItemsSource = _rows;
            foreach (var e in App.State.Timers) _rows.Add(new TimerRow(e));

            var icon = MakeIcon();
            Icon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            SetupTray(icon);

            _tick = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _tick.Tick += (_, _) => Tick();
            Tick();
            _tick.Start();

            StateChanged += (_, _) => { if (WindowState == WindowState.Minimized) { Hide(); ShowInTaskbar = false; } };
            Closing += OnClosing;
        }

        void Tick()
        {
            var now = DateTime.UtcNow;
            var finished = new List<TimerRow>();
            foreach (var r in _rows) if (r.Tick(now)) finished.Add(r);
            if (finished.Count > 0) { App.SaveState(); Notify(finished); }
            Resort();
            var running = _rows.Count(r => r.IsRunning && !r.IsDone);
            var ready = _rows.Count(r => r.IsDone);
            Summary.Text = _rows.Count == 0 ? "" : $"{running} running, {ready} ready, {_rows.Count - running - ready} idle";
            EmptyHint.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // Ready rows first, then soonest to finish, then idle ones. Moves only what changed so editing a label is not disturbed.
        void Resort()
        {
            var ordered = _rows
                .OrderBy(r => r.IsDone ? 0 : (r.IsRunning ? 1 : 2))
                .ThenBy(r => r.Entry.EndUtc ?? DateTime.MaxValue)
                .ThenBy(r => r.Title).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (!ReferenceEquals(_rows[i], ordered[i])) _rows.Move(_rows.IndexOf(ordered[i]), i);
            }
        }

        void Notify(List<TimerRow> finished)
        {
            if (!App.State.Muted) { try { Sound.Play(App.State.Volume); } catch { } }
            var lines = finished.Select(r => r.Title + (string.IsNullOrWhiteSpace(r.Label) ? "" : $" ({r.Label})"));
            _tray?.ShowBalloonTip(8000, finished.Count == 1 ? "Ready" : $"{finished.Count} timers ready", string.Join("\n", lines), Forms.ToolTipIcon.Info);
        }

        static TimerRow RowOf(object sender) => (TimerRow)((FrameworkElement)sender).DataContext;

        void Add_Click(object sender, RoutedEventArgs e)
        {
            var w = new AddTimerWindow { Owner = this };
            if (w.ShowDialog() == true && w.Result != null)
            {
                var entry = w.Result;
                var now = DateTime.UtcNow;
                entry.StartUtc = now;
                entry.EndUtc = now.AddMinutes(entry.Minutes);
                App.State.Timers.Add(entry);
                _rows.Add(new TimerRow(entry));
                App.SaveState();
                Tick();
            }
        }

        void Start_Click(object sender, RoutedEventArgs e)
        {
            var r = RowOf(sender);
            if (!r.CanStart) return;
            var now = DateTime.UtcNow;
            r.Entry.StartUtc = now;
            r.Entry.EndUtc = now.AddMinutes(r.Entry.Minutes);
            r.Entry.Notified = false;
            App.SaveState();
            Tick();
        }

        void Reset_Click(object sender, RoutedEventArgs e)
        {
            var r = RowOf(sender);
            // Only ask when there is a running or finished timer to lose; an idle row resets silently.
            if (App.State.ConfirmReset && r.Entry.StartUtc != null)
            {
                var name = string.IsNullOrWhiteSpace(r.Entry.Label) ? r.Entry.Item : r.Entry.Label;
                if (!ConfirmWindow.Ask(this, "Are you sure you want to reset the timer?" + Environment.NewLine + Environment.NewLine + name)) return;
            }
            r.Entry.StartUtc = null;
            r.Entry.EndUtc = null;
            r.Entry.Notified = false;
            App.SaveState();
            Tick();
        }

        void Remove_Click(object sender, RoutedEventArgs e)
        {
            var r = RowOf(sender);
            _rows.Remove(r);
            App.State.Timers.Remove(r.Entry);
            App.SaveState();
            Tick();
        }

        void Theme_Click(object sender, RoutedEventArgs e)
        {
            Theme.Toggle();
            UpdateThemeButton();
        }

        void UpdateThemeButton()
        {
            ThemeButton.Content = Theme.IsDark ? "☀" : "☾";
            ThemeButton.ToolTip = Theme.IsDark ? "Switch to light mode" : "Switch to dark mode";
        }

        void Settings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow { Owner = this }.ShowDialog();
            Topmost = App.State.AlwaysOnTop;
        }

        void Label_GotFocus(object sender, RoutedEventArgs e) => ((TextBox)sender).SelectAll();

        void Label_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                var tb = (TextBox)sender;
                if (e.Key == Key.Enter) tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                else tb.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
                Keyboard.ClearFocus();
                FocusManager.SetFocusedElement(this, this);
                e.Handled = true;
            }
        }

        // Tray icon: minimise hides the window, double click brings it back, balloon tips are the toasts.
        void SetupTray(Drawing.Icon icon)
        {
            _tray = new Forms.NotifyIcon { Icon = icon, Text = "RS3 Timers", Visible = true };
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Open", null, (_, _) => Restore());
            menu.Items.Add("Exit", null, (_, _) => Close());
            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += (_, _) => Restore();
            _tray.BalloonTipClicked += (_, _) => Restore();
        }

        void Restore()
        {
            Show();
            ShowInTaskbar = true;
            WindowState = WindowState.Normal;
            Activate();
        }

        void OnClosing(object? sender, CancelEventArgs e)
        {
            if (WindowState == WindowState.Normal) { App.State.WindowWidth = Width; App.State.WindowHeight = Height; }
            App.SaveState();
            if (_tray != null) { _tray.Visible = false; _tray.Dispose(); _tray = null; }
        }

        static Drawing.Icon MakeIcon()
        {
            var bmp = new Drawing.Bitmap(32, 32);
            using (var g = Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Drawing.Color.Transparent);
                using var fill = new Drawing.SolidBrush(Drawing.Color.FromArgb(0x3d, 0xdc, 0x84));
                g.FillEllipse(fill, 1, 1, 30, 30);
                using var pen = new Drawing.Pen(Drawing.Color.FromArgb(0x1b, 0x1b, 0x20), 3);
                g.DrawLine(pen, 16, 16, 16, 7);
                g.DrawLine(pen, 16, 16, 23, 16);
            }
            return Drawing.Icon.FromHandle(bmp.GetHicon());
        }
    }
}
