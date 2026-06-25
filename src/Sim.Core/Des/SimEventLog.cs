using System.Text;

namespace Sim.Core.Des;

public sealed record SimEventLogEntry(
    long OccurredAtMs,
    string EventId,
    string EventType,
    int Sequence);

public sealed class SimEventLog
{
    private readonly List<SimEventLogEntry> _entries = [];

    public IReadOnlyList<SimEventLogEntry> Entries => _entries;

    public void Append(long occurredAtMs, string eventId, string eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        _entries.Add(new SimEventLogEntry(
            occurredAtMs,
            eventId,
            eventType,
            _entries.Count));
    }

    public string ToDeterministicText()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < _entries.Count; index++)
        {
            var entry = _entries[index];
            if (index > 0)
            {
                builder.AppendLine();
            }

            builder.Append(entry.Sequence);
            builder.Append('|');
            builder.Append(entry.OccurredAtMs);
            builder.Append('|');
            builder.Append(entry.EventId);
            builder.Append('|');
            builder.Append(entry.EventType);
        }

        return builder.ToString();
    }
}
