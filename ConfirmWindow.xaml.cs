// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. PolyForm Noncommercial License 1.0.0, see LICENSE.
// Required Notice: Copyright signalbrycea (https://github.com/signalbrycea)

using System.Windows;

namespace RS3Tracker
{
    // Small themed Yes / No box. Ask() returns true only when Yes was clicked;
    // Escape, No and the close button all count as No.
    public partial class ConfirmWindow : Window
    {
        public ConfirmWindow(string message)
        {
            InitializeComponent();
            DarkTitle.Apply(this);
            Message.Text = message;
        }

        public static bool Ask(Window owner, string message)
        {
            var w = new ConfirmWindow(message) { Owner = owner };
            return w.ShowDialog() == true;
        }

        void Yes_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
        void No_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}
