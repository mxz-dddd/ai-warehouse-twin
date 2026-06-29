using System.Collections.Generic;
using System.Globalization;
using AIWarehouseTwin.Playback;

namespace AIWarehouseTwin.UI
{
    public static class EventListFormatter
    {
        public static string[] Format(IReadOnlyList<TimelineEvent> events)
        {
            var rows = new string[events.Count];
            for (var index = 0; index < events.Count; index++)
            {
                var item = events[index];
                rows[index] =
                    $"{item.AtMs.ToString(CultureInfo.InvariantCulture)} ms | {item.Flow} | {item.EventType} | {item.EventId}";
            }

            return rows;
        }
    }
}
