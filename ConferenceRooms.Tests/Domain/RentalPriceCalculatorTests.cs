using ConferenceRooms.Api.Domain;
using ConferenceRooms.Api.Exceptions;

namespace ConferenceRooms.Tests.Domain;

public sealed class RentalPriceCalculatorTests
{
    private readonly RentalPriceCalculator _calculator = new();

    [Theory]
    [InlineData(6, 9, 5400)]
    [InlineData(9, 12, 6000)]
    [InlineData(12, 14, 4600)]
    [InlineData(14, 18, 8000)]
    [InlineData(18, 23, 8000)]
    public void Calculate_AppliesExpectedTariff(int startHour, int endHour, decimal expectedRoomAmount)
    {
        var quote = _calculator.Calculate(
            2000m,
            At(startHour),
            At(endHour),
            []);

        Assert.Equal(expectedRoomAmount, quote.RoomAmount);
    }

    [Fact]
    public void Calculate_SplitsBookingAcrossTariffPeriods()
    {
        var services = new[]
        {
            new HallAdditionalService { Name = "Projector", Price = 500m },
            new HallAdditionalService { Name = "Wi-Fi", Price = 300m }
        };

        var quote = _calculator.Calculate(2000m, At(8), At(13), services);

        Assert.Equal(10100m, quote.RoomAmount);
        Assert.Equal(800m, quote.ServicesAmount);
        Assert.Equal(10900m, quote.TotalAmount);
        Assert.Equal(3, quote.Segments.Count);
    }

    [Fact]
    public void Calculate_PricesPartialHoursProportionally()
    {
        var quote = _calculator.Calculate(2000m, At(12, 30), At(13, 15), []);

        Assert.Equal(1725m, quote.RoomAmount);
    }

    [Fact]
    public void Calculate_RejectsBookingBeforeOpening()
    {
        Assert.Throws<BusinessValidationException>(() =>
            _calculator.Calculate(2000m, At(5), At(7), []));
    }

    [Fact]
    public void Calculate_RejectsBookingAfterClosing()
    {
        Assert.Throws<BusinessValidationException>(() =>
            _calculator.Calculate(2000m, At(22), At(23, 30), []));
    }

    [Fact]
    public void Calculate_RejectsOvernightBooking()
    {
        var start = At(22);
        var end = start.AddHours(8);

        Assert.Throws<BusinessValidationException>(() =>
            _calculator.Calculate(2000m, start, end, []));
    }

    private static DateTimeOffset At(int hour, int minute = 0) =>
        new(2026, 9, 10, hour, minute, 0, TimeSpan.FromHours(3));
}
