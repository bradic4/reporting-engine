using System.Diagnostics;
using GamingDW.Core.Data;
using GamingDW.DataGenerator.Generators;

Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║   Gaming Data Warehouse - Data Generator        ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine();

var sw = Stopwatch.StartNew();
var rng = new Random(42); // Fixed seed for reproducibility

// Determine DB path: use first argument, or put the DB in the solution root
string dbPath;
if (args.Length > 0)
{
    dbPath = args[0];
}
else
{
    // Walk up from bin directory to find the solution root
    var dir = AppContext.BaseDirectory;
    while (dir != null && !Directory.GetFiles(dir, "*.slnx").Any() && !Directory.GetFiles(dir, "*.sln").Any())
        dir = Directory.GetParent(dir)?.FullName;
    dbPath = Path.Combine(dir ?? AppContext.BaseDirectory, "GamingDW.db");
}

Console.WriteLine($"Database: {dbPath}");
Console.WriteLine();

GamingDbContext.DefaultDbPath = dbPath;
using var db = new GamingDbContext();
db.Database.EnsureDeleted(); // Start fresh
db.Database.EnsureCreated();
Console.WriteLine("✓ Database created");

// Step 1: Generate users
const int userCount = 5000;
Console.Write($"Generating {userCount:N0} users... ");
var users = UserGenerator.Generate(userCount, rng);
db.Users.AddRange(users);
db.SaveChanges();
Console.WriteLine($"✓ ({sw.Elapsed.TotalSeconds:F1}s)");

// Assign IDs back (EF generates them)
users = db.Users.ToList();

// Step 2: Generate activity
Console.Write("Generating activity data (sessions, transactions, gameplay)... ");
var (sessions, transactions, gameplayLogs) = ActivityGenerator.GenerateAll(users, rng);
Console.WriteLine($"✓ ({sw.Elapsed.TotalSeconds:F1}s)");

// Step 3: Bulk insert in batches
Console.Write($"Inserting {sessions.Count:N0} sessions... ");
foreach (var batch in sessions.Chunk(5000))
{
    db.UserSessions.AddRange(batch);
    db.SaveChanges();
    db.ChangeTracker.Clear();
}
Console.WriteLine($"✓ ({sw.Elapsed.TotalSeconds:F1}s)");

Console.Write($"Inserting {transactions.Count:N0} transactions... ");
foreach (var batch in transactions.Chunk(5000))
{
    db.Transactions.AddRange(batch);
    db.SaveChanges();
    db.ChangeTracker.Clear();
}
Console.WriteLine($"✓ ({sw.Elapsed.TotalSeconds:F1}s)");

Console.Write($"Inserting {gameplayLogs.Count:N0} gameplay logs... ");
foreach (var batch in gameplayLogs.Chunk(5000))
{
    db.GameplayLogs.AddRange(batch);
    db.SaveChanges();
    db.ChangeTracker.Clear();
}
Console.WriteLine($"✓ ({sw.Elapsed.TotalSeconds:F1}s)");

sw.Stop();

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║   Generation Complete!                          ║");
Console.WriteLine("╠══════════════════════════════════════════════════╣");
Console.WriteLine($"║  Users:          {users.Count,8:N0}                      ║");
Console.WriteLine($"║  Sessions:       {sessions.Count,8:N0}                      ║");
Console.WriteLine($"║  Transactions:   {transactions.Count,8:N0}                      ║");
Console.WriteLine($"║  Gameplay Logs:  {gameplayLogs.Count,8:N0}                      ║");
Console.WriteLine($"║  Time:           {sw.Elapsed.TotalSeconds,7:F1}s                      ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
