using Sim.Core.Des;
using Xunit;

namespace Sim.Core.Tests.Des;

public sealed class SimEventLogTests
{
    [Fact]
    public void Append_AssignsSequenceFromZero()
    {
        var log = new SimEventLog();

        log.Append(10, "evt-1", "First");
        log.Append(20, "evt-2", "Second");

        Assert.Equal(0, log.Entries[0].Sequence);
        Assert.Equal(1, log.Entries[1].Sequence);
    }

    [Fact]
    public void ToDeterministicText_ReturnsStableText()
    {
        var log = new SimEventLog();

        log.Append(10, "evt-1", "First");
        log.Append(20, "evt-2", "Second");

        var text = log.ToDeterministicText();

        Assert.Equal("0|10|evt-1|First\n1|20|evt-2|Second", text);
        Assert.DoesNotContain('\r', text);
    }

    [Fact]
    public void ToDeterministicText_ReturnsSameTextAcrossCalls()
    {
        var log = new SimEventLog();
        log.Append(10, "evt-1", "First");

        Assert.Equal(log.ToDeterministicText(), log.ToDeterministicText());
    }
}
