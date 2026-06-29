using System;
using System.Globalization;

namespace AIWarehouseTwin.Playback
{
    public sealed class TimelineEvent
    {
        private TimelineEvent(
            string rawLine,
            string flow,
            int sequence,
            long atMs,
            string eventId,
            string eventType)
        {
            RawLine = rawLine;
            Flow = flow;
            Sequence = sequence;
            AtMs = atMs;
            EventId = eventId;
            EventType = eventType;
        }

        public string RawLine { get; }

        public string Flow { get; }

        public int Sequence { get; }

        public long AtMs { get; }

        public string EventId { get; }

        public string EventType { get; }

        public static TimelineEvent Parse(string rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                throw new ArgumentException("Timeline event log line cannot be empty.", nameof(rawLine));
            }

            var parts = rawLine.Split('|');
            if (parts.Length != 5)
            {
                throw new FormatException($"Timeline event log line must have 5 pipe-delimited fields: {rawLine}");
            }

            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence))
            {
                throw new FormatException($"Timeline event sequence is not an integer: {rawLine}");
            }

            if (!long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var atMs))
            {
                throw new FormatException($"Timeline event time is not an integer: {rawLine}");
            }

            if (atMs < 0)
            {
                throw new FormatException($"Timeline event time cannot be negative: {rawLine}");
            }

            return new TimelineEvent(rawLine, parts[0], sequence, atMs, parts[3], parts[4]);
        }
    }
}
