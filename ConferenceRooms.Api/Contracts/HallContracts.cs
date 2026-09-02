using System.ComponentModel.DataAnnotations;

namespace ConferenceRooms.Api.Contracts;

public sealed record HallServiceRequest(
    Guid? Id,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [Range(typeof(decimal), "0", "9999999999", ParseLimitsInInvariantCulture = true)] decimal Price);

public sealed record CreateHallRequest(
    [Required, StringLength(150, MinimumLength = 1)] string Name,
    [Range(1, 100000)] int Capacity,
    [Range(typeof(decimal), "0.01", "9999999999", ParseLimitsInInvariantCulture = true)] decimal BaseHourlyRate,
    IReadOnlyCollection<HallServiceRequest>? Services);

public sealed record UpdateHallRequest(
    [StringLength(150, MinimumLength = 1)] string? Name,
    [Range(1, 100000)] int? Capacity,
    [Range(typeof(decimal), "0.01", "9999999999", ParseLimitsInInvariantCulture = true)] decimal? BaseHourlyRate,
    IReadOnlyCollection<HallServiceRequest>? Services);

public sealed record HallServiceResponse(Guid Id, string Name, decimal Price);

public sealed record HallResponse(
    Guid Id,
    string Name,
    int Capacity,
    decimal BaseHourlyRate,
    IReadOnlyCollection<HallServiceResponse> Services);
