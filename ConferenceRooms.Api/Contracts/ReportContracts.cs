namespace ConferenceRooms.Api.Contracts;

public sealed record HallRevenueResponse(
    Guid HallId,
    string HallName,
    int BookingCount,
    decimal Revenue);

public sealed record PopularServiceResponse(
    string Name,
    int TimesSelected,
    decimal Revenue);

public sealed record RevenueReportResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    int BookingCount,
    decimal RoomRevenue,
    decimal ServicesRevenue,
    decimal TotalRevenue,
    IReadOnlyCollection<HallRevenueResponse> ByHall,
    IReadOnlyCollection<PopularServiceResponse> PopularServices);
