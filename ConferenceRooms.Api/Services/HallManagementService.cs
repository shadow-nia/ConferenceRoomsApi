using ConferenceRooms.Api.Contracts;
using ConferenceRooms.Api.Data;
using ConferenceRooms.Api.Domain;
using ConferenceRooms.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRooms.Api.Services;

public sealed class HallManagementService(AppDbContext dbContext)
{
    public async Task<IReadOnlyCollection<HallResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var halls = await dbContext.Halls
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Include(x => x.Services)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return halls.Select(Map).ToArray();
    }

    public async Task<HallResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var hall = await dbContext.Halls
            .AsNoTracking()
            .Include(x => x.Services)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Hall was not found.");

        return Map(hall);
    }

    public async Task<HallResponse> CreateAsync(CreateHallRequest request, CancellationToken cancellationToken)
    {
        if (request.Services?.Any(x => x.Id.HasValue) == true)
        {
            throw new BusinessValidationException("Service IDs must be omitted when creating a hall.");
        }

        var services = ValidateAndNormalizeServices(request.Services ?? []);
        var name = request.Name.Trim();

        if (await dbContext.Halls.AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken))
        {
            throw new ConflictException("A hall with this name already exists.");
        }

        var hall = new Hall
        {
            Name = name,
            Capacity = request.Capacity,
            BaseHourlyRate = request.BaseHourlyRate,
            Services = services.Select(x => new HallAdditionalService
            {
                Name = x.Name,
                Price = x.Price
            }).ToList()
        };

        dbContext.Halls.Add(hall);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(hall);
    }

    public async Task<HallResponse> UpdateAsync(Guid id, UpdateHallRequest request, CancellationToken cancellationToken)
    {
        if (request.Name is null && request.Capacity is null && request.BaseHourlyRate is null && request.Services is null)
        {
            throw new BusinessValidationException("At least one field must be provided for update.");
        }

        var hall = await dbContext.Halls
            .Include(x => x.Services)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Hall was not found.");

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (await dbContext.Halls.AnyAsync(
                    x => x.Id != id && x.Name.ToLower() == name.ToLower(),
                    cancellationToken))
            {
                throw new ConflictException("A hall with this name already exists.");
            }

            hall.Name = name;
        }

        hall.Capacity = request.Capacity ?? hall.Capacity;
        hall.BaseHourlyRate = request.BaseHourlyRate ?? hall.BaseHourlyRate;

        if (request.Services is not null)
        {
            ReplaceServices(hall, ValidateAndNormalizeServices(request.Services));
        }

        hall.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(hall);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var hall = await dbContext.Halls
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Hall was not found.");

        hall.IsActive = false;
        hall.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<HallResponse>> FindAvailableAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int capacity,
        CancellationToken cancellationToken)
    {
        RentalPriceCalculator.ValidateInterval(from, to);
        if (capacity <= 0)
        {
            throw new BusinessValidationException("Capacity must be greater than zero.");
        }

        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        var halls = await dbContext.Halls
            .AsNoTracking()
            .Where(hall => hall.IsActive && hall.Capacity >= capacity)
            .Where(hall => !hall.Bookings.Any(booking =>
                booking.Status == BookingStatus.Active &&
                booking.StartAt < toUtc &&
                booking.EndAt > fromUtc))
            .Include(x => x.Services)
            .OrderBy(x => x.BaseHourlyRate)
            .ToListAsync(cancellationToken);

        return halls.Select(Map).ToArray();
    }

    private static List<HallServiceRequest> ValidateAndNormalizeServices(
        IEnumerable<HallServiceRequest> services)
    {
        var normalized = services
            .Select(x => x with { Name = x.Name.Trim() })
            .ToList();

        if (normalized.Any(x => string.IsNullOrWhiteSpace(x.Name)))
        {
            throw new BusinessValidationException("Service name cannot be empty.");
        }

        if (normalized.Any(x => x.Price < 0))
        {
            throw new BusinessValidationException("Service price cannot be negative.");
        }

        if (normalized.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Count)
        {
            throw new BusinessValidationException("Service names must be unique within a hall.");
        }

        if (normalized.Where(x => x.Id.HasValue).Select(x => x.Id).Distinct().Count() !=
            normalized.Count(x => x.Id.HasValue))
        {
            throw new BusinessValidationException("Service IDs must be unique.");
        }

        return normalized;
    }

    private static void ReplaceServices(Hall hall, IReadOnlyCollection<HallServiceRequest> requests)
    {
        var existingById = hall.Services.ToDictionary(x => x.Id);
        var requestedExistingIds = requests.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();

        if (requestedExistingIds.Any(id => !existingById.ContainsKey(id)))
        {
            throw new BusinessValidationException("One or more service IDs do not belong to this hall.");
        }

        hall.Services.RemoveAll(x => !requestedExistingIds.Contains(x.Id));

        foreach (var request in requests)
        {
            if (request.Id.HasValue)
            {
                var service = existingById[request.Id.Value];
                service.Name = request.Name;
                service.Price = request.Price;
            }
            else
            {
                hall.Services.Add(new HallAdditionalService
                {
                    Name = request.Name,
                    Price = request.Price
                });
            }
        }
    }

    private static HallResponse Map(Hall hall) => new(
        hall.Id,
        hall.Name,
        hall.Capacity,
        hall.BaseHourlyRate,
        hall.Services
            .OrderBy(x => x.Name)
            .Select(x => new HallServiceResponse(x.Id, x.Name, x.Price))
            .ToArray());
}
