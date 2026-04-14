using GamingDW.Core.Models;

namespace GamingDW.DataGenerator.Generators;

/// <summary>
/// Simulates realistic player activity: sessions, transactions, and gameplay.
/// Users are classified into behavior profiles that determine their activity patterns.
/// </summary>
public static class ActivityGenerator
{
    private static readonly string[] Devices = ["Desktop", "Mobile", "Tablet"];

    private enum PlayerProfile { Churned, Casual, Regular, Whale }

    /// <summary>
    /// Assigns a player profile based on weighted distribution:
    /// 40% Churned, 30% Casual, 20% Regular, 10% Whale
    /// </summary>
    private static PlayerProfile GetProfile(Random rng)
    {
        var roll = rng.NextDouble();
        return roll switch
        {
            < 0.40 => PlayerProfile.Churned,
            < 0.70 => PlayerProfile.Casual,
            < 0.90 => PlayerProfile.Regular,
            _ => PlayerProfile.Whale
        };
    }

    /// <summary>
    /// Generates all activity data for a list of users.
    /// Returns sessions, transactions, and gameplay logs in bulk.
    /// </summary>
    public static (List<UserSession> Sessions, List<Transaction> Transactions, List<GameplayLog> GameplayLogs)
        GenerateAll(List<User> users, Random rng)
    {
        var sessions = new List<UserSession>();
        var transactions = new List<Transaction>();
        var gameplayLogs = new List<GameplayLog>();

        foreach (var user in users)
        {
            var profile = GetProfile(rng);
            var (s, t, g) = GenerateForUser(user, profile, rng);
            sessions.AddRange(s);
            transactions.AddRange(t);
            gameplayLogs.AddRange(g);
        }

        return (sessions, transactions, gameplayLogs);
    }

    private static (List<UserSession>, List<Transaction>, List<GameplayLog>)
        GenerateForUser(User user, PlayerProfile profile, Random rng)
    {
        var sessions = new List<UserSession>();
        var transactions = new List<Transaction>();
        var gameplay = new List<GameplayLog>();

        var now = DateTime.UtcNow;
        var daysActive = (int)(now - user.RegistrationDate).TotalDays;
        if (daysActive < 1) daysActive = 1;

        switch (profile)
        {
            case PlayerProfile.Churned:
                GenerateChurned(user, rng, sessions, daysActive);
                break;
            case PlayerProfile.Casual:
                GenerateCasual(user, rng, sessions, transactions, gameplay, daysActive);
                break;
            case PlayerProfile.Regular:
                GenerateRegular(user, rng, sessions, transactions, gameplay, daysActive);
                break;
            case PlayerProfile.Whale:
                GenerateWhale(user, rng, sessions, transactions, gameplay, daysActive);
                break;
        }

        return (sessions, transactions, gameplay);
    }

    /// <summary>
    /// Churned: 0-2 sessions, no deposits, no gameplay.
    /// These users registered but never engaged.
    /// </summary>
    private static void GenerateChurned(User user, Random rng, List<UserSession> sessions, int daysActive)
    {
        int sessionCount = rng.Next(0, 3); // 0, 1, or 2 sessions
        for (int i = 0; i < sessionCount; i++)
        {
            var login = user.RegistrationDate.AddMinutes(rng.Next(0, Math.Min(daysActive, 3) * 24 * 60));
            var duration = TimeSpan.FromMinutes(rng.Next(1, 15)); // Very short sessions
            sessions.Add(new UserSession
            {
                UserId = user.Id,
                LoginTime = login,
                LogoutTime = login + duration,
                Device = Devices[rng.Next(Devices.Length)]
            });
        }
    }

    /// <summary>
    /// Casual: 3-10 sessions over first 2 weeks, 1-3 small deposits, light gameplay.
    /// </summary>
    private static void GenerateCasual(User user, Random rng,
        List<UserSession> sessions, List<Transaction> transactions,
        List<GameplayLog> gameplay, int daysActive)
    {
        int activeDays = Math.Min(daysActive, 14);
        int sessionCount = rng.Next(3, 11);
        var device = Devices[rng.Next(Devices.Length)];

        for (int i = 0; i < sessionCount; i++)
        {
            var login = user.RegistrationDate.AddMinutes(rng.Next(0, activeDays * 24 * 60));
            var duration = TimeSpan.FromMinutes(rng.Next(10, 90));
            sessions.Add(new UserSession
            {
                UserId = user.Id,
                LoginTime = login,
                LogoutTime = login + duration,
                Device = device
            });

            // Some gameplay during session
            int plays = rng.Next(1, 3);
            for (int j = 0; j < plays; j++)
            {
                var bet = Math.Round((decimal)(rng.NextDouble() * 5 + 0.5), 2); // $0.50 - $5.50
                var isWof = rng.NextDouble() < 0.6;
                var win = isWof
                    ? (rng.NextDouble() < 0.3 ? bet * (decimal)(rng.NextDouble() * 3 + 1) : 0m)
                    : (rng.NextDouble() < 0.4 ? bet * 2m : 0m);

                gameplay.Add(new GameplayLog
                {
                    UserId = user.Id,
                    GameType = isWof ? GameType.WheelOfFortune : GameType.BattleArena,
                    BetAmount = bet,
                    WinAmount = Math.Round(win, 2),
                    Timestamp = login.AddMinutes(rng.Next(1, (int)duration.TotalMinutes + 1))
                });
            }
        }

        // 1-3 deposits
        int deposits = rng.Next(1, 4);
        for (int i = 0; i < deposits; i++)
        {
            var depTime = user.RegistrationDate.AddMinutes(rng.Next(0, activeDays * 24 * 60));
            transactions.Add(new Transaction
            {
                UserId = user.Id,
                Amount = Math.Round((decimal)(rng.NextDouble() * 40 + 10), 2), // $10 - $50
                Type = TransactionType.Deposit,
                Timestamp = depTime
            });
        }

        // Maybe 1 withdrawal
        if (rng.NextDouble() < 0.3)
        {
            var wTime = user.RegistrationDate.AddDays(rng.Next(1, Math.Max(2, activeDays + 1)));
            transactions.Add(new Transaction
            {
                UserId = user.Id,
                Amount = Math.Round((decimal)(rng.NextDouble() * 20 + 5), 2),
                Type = TransactionType.Withdrawal,
                Timestamp = wTime
            });
        }
    }

    /// <summary>
    /// Regular: daily sessions for weeks, consistent deposits, mixed game types.
    /// </summary>
    private static void GenerateRegular(User user, Random rng,
        List<UserSession> sessions, List<Transaction> transactions,
        List<GameplayLog> gameplay, int daysActive)
    {
        int activeDays = Math.Min(daysActive, 60);
        var device = Devices[rng.Next(Devices.Length)];

        // Almost daily sessions
        for (int day = 0; day < activeDays; day++)
        {
            if (rng.NextDouble() < 0.2) continue; // Skip ~20% of days

            int sessionsPerDay = rng.Next(1, 3);
            for (int s = 0; s < sessionsPerDay; s++)
            {
                var login = user.RegistrationDate.AddDays(day).AddHours(rng.Next(8, 23));
                var duration = TimeSpan.FromMinutes(rng.Next(30, 180));
                sessions.Add(new UserSession
                {
                    UserId = user.Id,
                    LoginTime = login,
                    LogoutTime = login + duration,
                    Device = device
                });

                // More gameplay per session
                int plays = rng.Next(1, 4);
                for (int j = 0; j < plays; j++)
                {
                    var bet = Math.Round((decimal)(rng.NextDouble() * 15 + 2), 2); // $2 - $17
                    var isWof = rng.NextDouble() < 0.5;
                    var win = isWof
                        ? (rng.NextDouble() < 0.35 ? bet * (decimal)(rng.NextDouble() * 5 + 1) : 0m)
                        : (rng.NextDouble() < 0.45 ? bet * (decimal)(rng.NextDouble() * 2 + 1.5) : 0m);

                    gameplay.Add(new GameplayLog
                    {
                        UserId = user.Id,
                        GameType = isWof ? GameType.WheelOfFortune : GameType.BattleArena,
                        BetAmount = bet,
                        WinAmount = Math.Round(win, 2),
                        Timestamp = login.AddMinutes(rng.Next(1, (int)duration.TotalMinutes + 1))
                    });
                }
            }
        }

        // Weekly deposits
        int depositCount = activeDays / 7 + rng.Next(1, 4);
        for (int i = 0; i < depositCount; i++)
        {
            var depTime = user.RegistrationDate.AddDays(rng.Next(0, activeDays));
            transactions.Add(new Transaction
            {
                UserId = user.Id,
                Amount = Math.Round((decimal)(rng.NextDouble() * 80 + 20), 2), // $20 - $100
                Type = TransactionType.Deposit,
                Timestamp = depTime
            });
        }

        // Occasional withdrawals
        int withdrawals = rng.Next(1, depositCount / 2 + 1);
        for (int i = 0; i < withdrawals; i++)
        {
            var wTime = user.RegistrationDate.AddDays(rng.Next(1, Math.Max(2, activeDays + 1)));
            transactions.Add(new Transaction
            {
                UserId = user.Id,
                Amount = Math.Round((decimal)(rng.NextDouble() * 50 + 10), 2),
                Type = TransactionType.Withdrawal,
                Timestamp = wTime
            });
        }
    }

    /// <summary>
    /// Whale: multiple sessions per day, large deposits, high bet amounts.
    /// </summary>
    private static void GenerateWhale(User user, Random rng,
        List<UserSession> sessions, List<Transaction> transactions,
        List<GameplayLog> gameplay, int daysActive)
    {
        int activeDays = Math.Min(daysActive, 90);
        var device = Devices[rng.Next(Devices.Length)];

        // Multiple sessions most days
        for (int day = 0; day < activeDays; day++)
        {
            if (rng.NextDouble() < 0.1) continue; // Skip very few days

            int sessionsPerDay = rng.Next(2, 5);
            for (int s = 0; s < sessionsPerDay; s++)
            {
                var login = user.RegistrationDate.AddDays(day).AddHours(rng.Next(6, 24));
                var duration = TimeSpan.FromMinutes(rng.Next(60, 300));
                sessions.Add(new UserSession
                {
                    UserId = user.Id,
                    LoginTime = login,
                    LogoutTime = login + duration,
                    Device = device
                });

                // Heavy gameplay
                int plays = rng.Next(2, 6);
                for (int j = 0; j < plays; j++)
                {
                    var bet = Math.Round((decimal)(rng.NextDouble() * 200 + 20), 2); // $20 - $220
                    var isWof = rng.NextDouble() < 0.4;
                    var win = isWof
                        ? (rng.NextDouble() < 0.3 ? bet * (decimal)(rng.NextDouble() * 10 + 1) : 0m)
                        : (rng.NextDouble() < 0.4 ? bet * (decimal)(rng.NextDouble() * 3 + 1.5) : 0m);

                    gameplay.Add(new GameplayLog
                    {
                        UserId = user.Id,
                        GameType = isWof ? GameType.WheelOfFortune : GameType.BattleArena,
                        BetAmount = bet,
                        WinAmount = Math.Round(win, 2),
                        Timestamp = login.AddMinutes(rng.Next(1, (int)duration.TotalMinutes + 1))
                    });
                }
            }
        }

        // Frequent large deposits
        int depositCount = activeDays / 3 + rng.Next(2, 8);
        for (int i = 0; i < depositCount; i++)
        {
            var depTime = user.RegistrationDate.AddDays(rng.Next(0, activeDays));
            transactions.Add(new Transaction
            {
                UserId = user.Id,
                Amount = Math.Round((decimal)(rng.NextDouble() * 900 + 100), 2), // $100 - $1000
                Type = TransactionType.Deposit,
                Timestamp = depTime
            });
        }

        // Some large withdrawals
        int withdrawals = rng.Next(2, depositCount / 2 + 1);
        for (int i = 0; i < withdrawals; i++)
        {
            var wTime = user.RegistrationDate.AddDays(rng.Next(1, Math.Max(2, activeDays + 1)));
            transactions.Add(new Transaction
            {
                UserId = user.Id,
                Amount = Math.Round((decimal)(rng.NextDouble() * 400 + 50), 2),
                Type = TransactionType.Withdrawal,
                Timestamp = wTime
            });
        }
    }
}
