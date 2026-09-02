namespace ConferenceRooms.Api.Domain;

public static class BookingInterval
{
    public static bool Overlaps(
        DateTimeOffset firstStart,
        DateTimeOffset firstEnd,
        DateTimeOffset secondStart,
        DateTimeOffset secondEnd) =>
        firstStart < secondEnd && firstEnd > secondStart;
}
