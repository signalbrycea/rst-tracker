// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. Licensed under the MIT License, see LICENSE.

using System;
using System.Windows;

namespace RS3Tracker
{
    public partial class App : Application
    {
        public static Catalog Catalog { get; private set; } = new Catalog();
        public static AppState State { get; private set; } = new AppState();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                Catalog = Storage.LoadCatalog();
                State = Storage.LoadState();
                Theme.Apply(State.Theme);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load data: " + ex.Message, "RS3 Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        public static void SaveState()
        {
            try { Storage.SaveState(State); }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save timers: " + ex.Message, "RS3 Tracker", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
