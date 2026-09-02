using ConferenceRooms.Api.Exceptions;

namespace ConferenceRooms.Api.Domain;

public sealed class RentalPriceCalculator
{
    private static readonly TariffPeriod[] Tariffs =
    [
        new(new TimeOnly(6, 0), new TimeOnly(9, 0), 0.90m, "Morning (-10%)"),
        new(new TimeOnly(9, 0), new TimeOnly(12, 0), 1.00m, "Standard"),
        new(new TimeOnly(12, 0), new TimeOnly(14, 0), 1.15m, "Peak (+15%)"),
        new(new TimeOnly(14, 0), new TimeOnly(18, 0), 1.00m, "Standard"),
        new(new TimeOnly(18, 0), new TimeOnly(23, 0), 0.80m, "Evening (-20%)")
    ];

    public RentalPriceQuote Calculate(
        decimal baseHourlyRate,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        IEnumerable<HallAdditionalService> selectedServices)
    {
        ValidateInterval(startAt, endAt);

        var segments = new List<RentalPriceSegment>();
        var cursor = startAt;

        while (cursor < endAt)
        {
            var tariff = Tariffs.Single(x =>
                TimeOnly.FromDateTime(cursor.DateTime) >= x.Start &&
                TimeOnly.FromDateTime(cursor.DateTime) < x.End);
            var tariffEnd = new DateTimeOffset(
                cursor.Year,
                cursor.Month,
                cursor.Day,
                tariff.End.Hour,
                tariff.End.Minute,
                0,
                cursor.Offset);
            var segmentEnd = tariffEnd < endAt ? tariffEnd : endAt;
            var hours = (decimal)(segmentEnd - cursor).TotalMinutes / 60m;
            var amount = decimal.Round(
                baseHourlyRate * tariff.Multiplier * hours,
                2,
                MidpointRounding.AwayFromZero);

            segments.Add(new RentalPriceSegment(
                cursor,
                segmentEnd,
                tariff.Name,
                tariff.Multiplier,
                amount));
            cursor = segmentEnd;
        }

        var roomAmount = segments.Sum(x => x.Amount);
        var servicesAmount = selectedServices.Sum(x => x.Price);
        return new RentalPriceQuote(
            roomAmount,
            servicesAmount,
            roomAmount + servicesAmount,
            segments);
    }

    public static void ValidateInterval(DateTimeOffset startAt, DateTimeOffset endAt)
    {
        if (startAt >= endAt)
        {
            throw new BusinessValidationException("Booking start must be earlier than booking end.");
        }

        if (startAt.Offset != endAt.Offset)
        {
            throw new BusinessValidationException("Start and end must use the same UTC offset.");
        }

        if (startAt.Date != endAt.Date)
        {
            throw new BusinessValidationException("A booking must start and end on the same local calendar day.");
        }

        var startTime = TimeOnly.FromDateTime(startAt.DateTime);
        var endTime = TimeOnly.FromDateTime(endAt.DateTime);
        if (startTime < new TimeOnly(6, 0) || endTime > new TimeOnly(23, 0))
        {
            throw new BusinessValidationException("Bookings are available only from 06:00 to 23:00.");
        }
    }

    private sealed record TariffPeriod(TimeOnly Start, TimeOnly End, decimal Multiplier, string Name);
}

public sealed record RentalPriceQuote(
    decimal RoomAmount,
    decimal ServicesAmount,
    decimal TotalAmount,
    IReadOnlyCollection<RentalPriceSegment> Segments);

public sealed record RentalPriceSegment(
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Tariff,
    decimal Multiplier,
    decimal Amount);
