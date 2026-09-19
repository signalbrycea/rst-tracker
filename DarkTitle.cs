// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. PolyForm Noncommercial License 1.0.0, see LICENSE.
// Required Notice: Copyright signalbrycea (https://github.com/signalbrycea)

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RS3Tracker
{
    // Asks Windows 10/11 for a dark or light title bar on a window, following the app theme.
    public static class DarkTitle
    {
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public static void Apply(Window w)
        {
            w.SourceInitialized += (_, _) => Set(w, App.State.Theme == "dark");
        }

        public static void Set(Window w, bool dark)
        {
            try
            {
                var hwnd = new WindowInteropHelper(w).Handle;
                if (hwnd == IntPtr.Zero) return;
                int on = dark ? 1 : 0;
                if (DwmSetWindowAttribute(hwnd, 20, ref on, sizeof(int)) != 0) DwmSetWindowAttribute(hwnd, 19, ref on, sizeof(int));
            }
            catch { }
        }
    }
}
