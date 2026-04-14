namespace GamingDW.WebApp.Reports;

public record FinancialSummary(
    decimal TotalDeposits,
    decimal TotalWithdrawals,
    decimal NetRevenue,
    decimal TotalBets,
    decimal TotalWins,
    decimal GGR
);

public record FinancialByGame(string GameType, decimal TotalBets, decimal TotalWins, decimal GGR, int TotalPlays);

public record FinancialByDay(string Date, decimal Deposits, decimal Withdrawals, decimal GGR);

public static class FinancialReport
{
    /// <summary>
    /// Overall financial summary.
    /// GGR = Total Bets - Total Wins (from player perspective: house revenue).
    /// </summary>
    public static string SummarySql => """
        SELECT
            COALESCE((SELECT SUM(Amount) FROM Transactions WHERE Type = 'Deposit'), 0) AS TotalDeposits,
            COALESCE((SELECT SUM(Amount) FROM Transactions WHERE Type = 'Withdrawal'), 0) AS TotalWithdrawals,
            COALESCE((SELECT SUM(Amount) FROM Transactions WHERE Type = 'Deposit'), 0)
                - COALESCE((SELECT SUM(Amount) FROM Transactions WHERE Type = 'Withdrawal'), 0) AS NetRevenue,
            COALESCE((SELECT SUM(BetAmount) FROM GameplayLogs), 0) AS TotalBets,
            COALESCE((SELECT SUM(WinAmount) FROM GameplayLogs), 0) AS TotalWins,
            COALESCE((SELECT SUM(BetAmount) FROM GameplayLogs), 0)
                - COALESCE((SELECT SUM(WinAmount) FROM GameplayLogs), 0) AS GGR
        """;

    /// <summary>
    /// GGR breakdown by game type.
    /// </summary>
    public static string ByGameSql => """
        SELECT
            GameType,
            SUM(BetAmount) AS TotalBets,
            SUM(WinAmount) AS TotalWins,
            SUM(BetAmount) - SUM(WinAmount) AS GGR,
            COUNT(*) AS TotalPlays
        FROM GameplayLogs
        GROUP BY GameType
        ORDER BY GGR DESC
        """;

    /// <summary>
    /// Daily financial trend: deposits, withdrawals, and GGR per day.
    /// </summary>
    public static string ByDaySql => """
        WITH DailyFinance AS (
            SELECT
                DATE(Timestamp) AS Date,
                SUM(CASE WHEN Type = 'Deposit' THEN Amount ELSE 0 END) AS Deposits,
                SUM(CASE WHEN Type = 'Withdrawal' THEN Amount ELSE 0 END) AS Withdrawals
            FROM Transactions
            GROUP BY DATE(Timestamp)
        ),
        DailyGGR AS (
            SELECT
                DATE(Timestamp) AS Date,
                SUM(BetAmount) - SUM(WinAmount) AS GGR
            FROM GameplayLogs
            GROUP BY DATE(Timestamp)
        )
        SELECT
            df.Date,
            df.Deposits,
            df.Withdrawals,
            COALESCE(dg.GGR, 0) AS GGR
        FROM DailyFinance df
        LEFT JOIN DailyGGR dg ON df.Date = dg.Date
        ORDER BY df.Date DESC
        """;
}
