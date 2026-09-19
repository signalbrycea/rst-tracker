// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. PolyForm Noncommercial License 1.0.0, see LICENSE.
// Required Notice: Copyright signalbrycea (https://github.com/signalbrycea)

using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;

namespace RS3Tracker
{
    // Plays the alert: the user's chosen file (State.SoundPath) if it exists, else a short chime generated
    // as a WAV once. Goes through WPF's MediaPlayer so the volume slider works. Any file is cut off after
    // MaxSeconds so a whole song cannot play every time a timer finishes.
    public static class Sound
    {
        public const int MaxSeconds = 10;
        static readonly MediaPlayer Player = new MediaPlayer();
        static readonly DispatcherTimer Cap = new DispatcherTimer { Interval = TimeSpan.FromSeconds(MaxSeconds) };
        static string? _path;

        static Sound()
        {
            Cap.Tick += (_, _) => { Cap.Stop(); Player.Stop(); };
        }

        public static void Play(double volume) => Play(volume, App.State.SoundPath);

        public static void Play(double volume, string? custom)
        {
            var path = !string.IsNullOrWhiteSpace(custom) && File.Exists(custom) ? custom : (_path ??= WriteChime());
            Cap.Stop();
            Player.Stop();
            Player.Open(new Uri(path));
            Player.Volume = Math.Clamp(volume, 0, 1);
            Player.Play();
            Cap.Start();
        }

        static string WriteChime()
        {
            const int rate = 44100;
            double[] notes = { 880, 1174.66, 1567.98 };
            double noteLen = 0.22;
            int perNote = (int)(rate * noteLen);
            int total = perNote * notes.Length + rate / 2;
            var samples = new short[total];
            for (int n = 0; n < notes.Length; n++)
            {
                int start = n * perNote;
                for (int i = 0; i < total - start; i++)
                {
                    double t = i / (double)rate;
                    double env = Math.Exp(-t * 4.0);
                    double v = Math.Sin(2 * Math.PI * notes[n] * t) * env * 0.35;
                    int idx = start + i;
                    int mixed = samples[idx] + (int)(v * short.MaxValue);
                    samples[idx] = (short)Math.Clamp(mixed, short.MinValue, short.MaxValue);
                }
            }
            var path = Path.Combine(Path.GetTempPath(), "rs3tracker-chime.wav");
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var w = new BinaryWriter(fs);
            int dataLen = samples.Length * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + dataLen);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            w.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            w.Write(dataLen);
            foreach (var s in samples) w.Write(s);
            return path;
        }
    }
}
