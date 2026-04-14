using GamingDW.Core.Models;

namespace GamingDW.DataGenerator.Generators;

/// <summary>
/// Generates realistic fake users with varied registration dates.
/// </summary>
public static class UserGenerator
{
    private static readonly string[] Countries = [
        "SRB", "USA", "GBR", "DEU", "FRA", "BRA", "JPN", "CAN", "AUS", "ITA",
        "ESP", "NLD", "SWE", "NOR", "POL", "CZE", "HRV", "BIH", "MNE", "MKD"
    ];

    private static readonly string[] FirstNames = [
        "Luka", "Marko", "Nikola", "Stefan", "Aleksandar", "Milan", "Ivan", "Petar",
        "Ana", "Maja", "Jelena", "Milica", "Teodora", "Sara", "Mina", "Katarina",
        "James", "Emma", "Oliver", "Sophie", "Lucas", "Mia", "Noah", "Isabella",
        "Felix", "Luna", "Leo", "Zara", "Kai", "Aria", "Max", "Ella"
    ];

    private static readonly string[] LastNames = [
        "Jovanovic", "Petrovic", "Nikolic", "Markovic", "Djordjevic", "Stojanovic",
        "Ilic", "Stankovic", "Pavlovic", "Milosevic", "Smith", "Johnson", "Williams",
        "Brown", "Jones", "Miller", "Davis", "Wilson", "Anderson", "Taylor",
        "Mueller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer", "Wagner"
    ];

    public static List<User> Generate(int count, Random rng)
    {
        var users = new List<User>(count);
        var startDate = DateTime.UtcNow.AddDays(-90); // Last 90 days

        for (int i = 0; i < count; i++)
        {
            var regDate = startDate.AddMinutes(rng.Next(0, 90 * 24 * 60));
            var firstName = FirstNames[rng.Next(FirstNames.Length)];
            var lastName = LastNames[rng.Next(LastNames.Length)];
            var suffix = rng.Next(1, 9999);

            users.Add(new User
            {
                Username = $"{firstName}{lastName}{suffix}",
                RegistrationDate = regDate,
                Country = Countries[rng.Next(Countries.Length)],
                Status = rng.NextDouble() < 0.05 ? UserStatus.Banned : UserStatus.Active
            });
        }

        return users;
    }
}
