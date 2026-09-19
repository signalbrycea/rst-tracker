// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. PolyForm Noncommercial License 1.0.0, see LICENSE.
// Required Notice: Copyright signalbrycea (https://github.com/signalbrycea)

using System.Windows;

namespace RS3Tracker
{
    public partial class SettingsWindow : Window
    {
        bool _ready;

        public SettingsWindow()
        {
            InitializeComponent();
            DarkTitle.Apply(this);
            VolumeSlider.Value = App.State.Volume * 100;
            VolumeText.Text = $"{(int)VolumeSlider.Value}%";
            MuteBox.IsChecked = App.State.Muted;
            TopBox.IsChecked = App.State.AlwaysOnTop;
            ConfirmBox.IsChecked = App.State.ConfirmReset;
            CatalogPath.Text = "Timer list: " + (Storage.CatalogPathUsed ?? "(built in)");
            StatePath.Text = "Saved timers: " + Storage.StatePath;
            var ver = typeof(SettingsWindow).Assembly.GetName().Version;
            AboutText.Text = $"RS3 Timers {ver?.Major}.{ver?.Minor}  (c) 2026 signalbrycea  PolyForm Noncommercial 1.0.0";
            _ready = true;
        }

        void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_ready) return;
            App.State.Volume = VolumeSlider.Value / 100.0;
            VolumeText.Text = $"{(int)VolumeSlider.Value}%";
            App.SaveState();
        }

        void Mute_Changed(object sender, RoutedEventArgs e)
        {
            if (!_ready) return;
            App.State.Muted = MuteBox.IsChecked == true;
            App.SaveState();
        }

        void Top_Changed(object sender, RoutedEventArgs e)
        {
            if (!_ready) return;
            App.State.AlwaysOnTop = TopBox.IsChecked == true;
            if (Owner != null) Owner.Topmost = App.State.AlwaysOnTop;
            App.SaveState();
        }

        void Confirm_Changed(object sender, RoutedEventArgs e)
        {
            if (!_ready) return;
            App.State.ConfirmReset = ConfirmBox.IsChecked == true;
            App.SaveState();
        }

        // Test always plays, even when muted, so the slider can be checked.
        void Test_Click(object sender, RoutedEventArgs e)
        {
            try { Sound.Play(App.State.Volume); } catch { }
        }

        void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
