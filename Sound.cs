// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. Licensed under the MIT License, see LICENSE.

using System;
using System.IO;
using System.Windows.Media;

namespace RS3Tracker
{
    // Generates a short chime as a WAV once, then plays it through WPF's MediaPlayer so the volume slider works.
    public static class Sound
    {
        static readonly MediaPlayer Player = new MediaPlayer();
        static string? _path;

        public static void Play(double volume)
        {
            if (_path == null) _path = WriteChime();
            Player.Open(new Uri(_path));
            Player.Volume = Math.Clamp(volume, 0, 1);
            Player.Play();
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
