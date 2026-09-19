// RS3 Timers (RS3Tracker)
// Copyright (c) 2026 signalbrycea. PolyForm Noncommercial License 1.0.0, see LICENSE.
// Required Notice: Copyright signalbrycea (https://github.com/signalbrycea)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace RS3Tracker
{
    // A row on the Buyers or Resets tab. Nothing to start or reset: the next instant comes from the
    // game clock, which is UTC on every world. The countdown is the same for everyone; the clock label
    // is converted to this machine's local time. A clock may carry a checklist (items in the catalog);
    // ticks are saved against the reset instant they belong to and clear when the reset rolls over.
    public class ClockRow : INotifyPropertyChanged
    {
        public Clock Clock { get; }
        public ObservableCollection<CheckRow> Items { get; } = new ObservableCollection<CheckRow>();

        public ClockRow(Clock clock)
        {
            Clock = clock;
            foreach (var it in clock.Items ?? new List<ClockItem>()) Items.Add(new CheckRow(this, it));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string Name => Clock.Name;
        public string Note => Clock.Note ?? "";
        public bool HasItems => Items.Count > 0;

        string _remaining = "", _sub = "", _progress = "";
        bool _allDone;
        public string Remaining { get => _remaining; private set { if (_remaining != value) { _remaining = value; Changed(nameof(Remaining)); } } }
        public string SubText { get => _sub; private set { if (_sub != value) { _sub = value; Changed(nameof(SubText)); } } }
        public string Progress { get => _progress; private set { if (_progress != value) { _progress = value; Changed(nameof(Progress)); } } }
        public bool AllDone { get => _allDone; private set { if (_allDone != value) { _allDone = value; Changed(nameof(AllDone)); } } }

        CheckState State
        {
            get
            {
                if (!App.State.Checks.TryGetValue(Clock.Name, out var cs)) { cs = new CheckState(); App.State.Checks[Clock.Name] = cs; }
                return cs;
            }
        }

        public void Tick(DateTime nowUtc)
        {
            var next = FixedClock.Next(Clock, nowUtc);
            Remaining = TimerRow.Fmt(next - nowUtc);
            SubText = "resets " + TimerRow.ClockAt(next);
            if (!HasItems) return;

            // Rollover: the ticks belong to the previous reset window, so they clear.
            var cycleStart = FixedClock.Last(Clock, nowUtc);
            var st = State;
            if (st.CycleStart != cycleStart)
            {
                st.CycleStart = cycleStart;
                st.Done.Clear();
                foreach (var it in Items) it.Refresh();
                App.SaveState();
            }
            UpdateProgress();
        }

        internal bool IsDone(string item) => State.Done.Contains(item);

        internal void SetDone(string item, bool done)
        {
            var st = State;
            if (done) { if (!st.Done.Contains(item)) st.Done.Add(item); }
            else st.Done.Remove(item);
            App.SaveState();
            UpdateProgress();
        }

        void UpdateProgress()
        {
            var done = Items.Count(i => i.IsDone);
            Progress = $"{done} of {Items.Count} done";
            AllDone = done == Items.Count && Items.Count > 0;
        }
    }

    // One tick box under a reset row.
    public class CheckRow : INotifyPropertyChanged
    {
        readonly ClockRow _owner;
        public ClockItem Item { get; }
        public CheckRow(ClockRow owner, ClockItem item) { _owner = owner; Item = item; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name => Item.Name;
        public string? Note => string.IsNullOrWhiteSpace(Item.Note) ? null : Item.Note;

        public bool IsDone
        {
            get => _owner.IsDone(Item.Name);
            set
            {
                if (value == IsDone) return;
                _owner.SetDone(Item.Name, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDone)));
            }
        }

        public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDone)));
    }

    public static class FixedClock
    {
        // Any Wednesday works as the weekly anchor; the game's weekly reset is Wednesday 00:00 UTC.
        static readonly DateTime WeeklyAnchor = new DateTime(2024, 3, 13, 0, 0, 0, DateTimeKind.Utc);

        // Next reset instant (UTC) strictly after nowUtc.
        public static DateTime Next(Clock c, DateTime nowUtc) => Boundary(c, nowUtc, 1);

        // Most recent reset instant (UTC) at or before nowUtc: the start of the current window.
        public static DateTime Last(Clock c, DateTime nowUtc) => Boundary(c, nowUtc, 0);

        static DateTime Boundary(Clock c, DateTime nowUtc, int offset)
        {
            switch ((c.Kind ?? "").Trim().ToLowerInvariant())
            {
                case "weekly": return Cycle(nowUtc, WeeklyAnchor, 7, offset);
                case "monthly":
                    return new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(offset);
                case "cycle": return Cycle(nowUtc, ParseAnchor(c.Anchor), Math.Max(1, c.Days ?? 1), offset);
                default: return Cycle(nowUtc, WeeklyAnchor, 1, offset);   // daily, 00:00 UTC
            }
        }

        static DateTime Cycle(DateTime nowUtc, DateTime anchorUtc, int days, int offset)
        {
            var span = TimeSpan.FromDays(days);
            var cycles = Math.Floor((nowUtc - anchorUtc) / span);
            return anchorUtc + span * (cycles + offset);
        }

        static DateTime ParseAnchor(string? text)
        {
            if (DateTime.TryParseExact(text ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d))
                return DateTime.SpecifyKind(d, DateTimeKind.Utc);
            return WeeklyAnchor;
        }
    }
}
