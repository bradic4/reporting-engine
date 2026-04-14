namespace GamingDW.WebApp.Reports;

public record ConversionSummary(
    int TotalRegistered,
    int MadeFirstDeposit,
    double ConversionRate,
    double AvgDaysToDeposit
);

public record ConversionCohort(string RegistrationWeek, int Registered, int Deposited, double ConversionPct);

public static class ConversionReport
{
    /// <summary>
    /// Overall conversion: Registration → First Deposit funnel.
    /// Uses LEFT JOIN between Users and the first Deposit per user.
    /// </summary>
    public static string SummarySql => """
        SELECT
            COUNT(*) AS TotalRegistered,
            COUNT(fd.FirstDeposit) AS MadeFirstDeposit,
            ROUND(COUNT(fd.FirstDeposit) * 100.0 / COUNT(*), 2) AS ConversionRate,
            ROUND(AVG(
                CASE WHEN fd.FirstDeposit IS NOT NULL
                     THEN julianday(fd.FirstDeposit) - julianday(u.RegistrationDate)
                END
            ), 1) AS AvgDaysToDeposit
        FROM Users u
        LEFT JOIN (
            SELECT UserId, MIN(Timestamp) AS FirstDeposit
            FROM Transactions
            WHERE Type = 'Deposit'
            GROUP BY UserId
        ) fd ON u.Id = fd.UserId
        """;

    /// <summary>
    /// Conversion by registration week cohort.
    /// Shows how each weekly cohort converts over time.
    /// </summary>
    public static string ByCohortSql => """
        SELECT
            strftime('%Y-W%W', u.RegistrationDate) AS RegistrationWeek,
            COUNT(*) AS Registered,
            COUNT(fd.FirstDeposit) AS Deposited,
            ROUND(COUNT(fd.FirstDeposit) * 100.0 / COUNT(*), 2) AS ConversionPct
        FROM Users u
        LEFT JOIN (
            SELECT UserId, MIN(Timestamp) AS FirstDeposit
            FROM Transactions
            WHERE Type = 'Deposit'
            GROUP BY UserId
        ) fd ON u.Id = fd.UserId
        GROUP BY strftime('%Y-W%W', u.RegistrationDate)
        ORDER BY RegistrationWeek
        """;
}
