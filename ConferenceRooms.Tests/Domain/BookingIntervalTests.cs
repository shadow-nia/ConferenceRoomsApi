using ConferenceRooms.Api.Domain;

namespace ConferenceRooms.Tests.Domain;

public sealed class BookingIntervalTests
{
    [Theory]
    [InlineData(9, 11, 10, 12, true)]
    [InlineData(9, 12, 10, 11, true)]
    [InlineData(9, 11, 9, 11, true)]
    [InlineData(9, 11, 11, 13, false)]
    [InlineData(11, 13, 9, 11, false)]
    public void Overlaps_UsesHalfOpenIntervals(
        int firstStart,
        int firstEnd,
        int secondStart,
        int secondEnd,
        bool expected)
    {
        var actual = BookingInterval.Overlaps(
            At(firstStart),
            At(firstEnd),
            At(secondStart),
            At(secondEnd));

        Assert.Equal(expected, actual);
    }

    private static DateTimeOffset At(int hour) =>
        new(2026, 9, 10, hour, 0, 0, TimeSpan.Zero);
}
