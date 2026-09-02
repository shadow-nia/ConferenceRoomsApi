using ConferenceRooms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRooms.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Halls.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Halls.AddRange(
            CreateHall("Зал А", 50, 2000m),
            CreateHall("Зал B", 100, 3500m),
            CreateHall("Зал C", 30, 1500m));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Hall CreateHall(string name, int capacity, decimal hourlyRate)
    {
        var hall = new Hall
        {
            Name = name,
            Capacity = capacity,
            BaseHourlyRate = hourlyRate
        };

        hall.Services.AddRange(
            new HallAdditionalService { Name = "Проєктор", Price = 500m },
            new HallAdditionalService { Name = "Wi-Fi", Price = 300m },
            new HallAdditionalService { Name = "Звук", Price = 700m });

        return hall;
    }
}
