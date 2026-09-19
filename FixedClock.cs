// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. PolyForm Noncommercial License 1.0.0, see LICENSE.
// Required Notice: Copyright signalbrycea (https://github.com/signalbrycea)

using System;
using System.ComponentModel;
using System.Globalization;

namespace RS3Tracker
{
    // A row on the Buyers or Resets tab. Nothing to start or reset: the next instant comes from the
    // game clock, which is UTC on every world. The countdown is the same for everyone; the clock label
    // is converted to this machine's local time.
    public class ClockRow : INotifyPropertyChanged
    {
        public Clock Clock { get; }
        public ClockRow(Clock clock) { Clock = clock; }

        public event PropertyChangedEventHandler? PropertyChanged;
        void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string Name => Clock.Name;
        public string Note => Clock.Note ?? "";

        string _remaining = "", _sub = "";
        public string Remaining { get => _remaining; private set { if (_remaining != value) { _remaining = value; Changed(nameof(Remaining)); } } }
        public string SubText { get => _sub; private set { if (_sub != value) { _sub = value; Changed(nameof(SubText)); } } }

        public void Tick(DateTime nowUtc)
        {
            var next = FixedClock.Next(Clock, nowUtc);
            Remaining = TimerRow.Fmt(next - nowUtc);
            SubText = "resets " + TimerRow.ClockAt(next);
        }
    }

    public static class FixedClock
    {
        // Any Wednesday works as the weekly anchor; the game's weekly reset is Wednesday 00:00 UTC.
        static readonly DateTime WeeklyAnchor = new DateTime(2024, 3, 13, 0, 0, 0, DateTimeKind.Utc);

        // Next reset instant (UTC) strictly after nowUtc.
        public static DateTime Next(Clock c, DateTime nowUtc)
        {
            switch ((c.Kind ?? "").Trim().ToLowerInvariant())
            {
                case "weekly": return NextCycle(nowUtc, WeeklyAnchor, 7);
                case "monthly":
                    return new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
                case "cycle": return NextCycle(nowUtc, ParseAnchor(c.Anchor), Math.Max(1, c.Days ?? 1));
                default: return NextCycle(nowUtc, WeeklyAnchor, 1);   // daily, 00:00 UTC
            }
        }

        static DateTime NextCycle(DateTime nowUtc, DateTime anchorUtc, int days)
        {
            var span = TimeSpan.FromDays(days);
            var cycles = Math.Floor((nowUtc - anchorUtc) / span);
            return anchorUtc + span * (cycles + 1);
        }

        static DateTime ParseAnchor(string? text)
        {
            if (DateTime.TryParseExact(text ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d))
                return DateTime.SpecifyKind(d, DateTimeKind.Utc);
            return WeeklyAnchor;
        }
    }
}
