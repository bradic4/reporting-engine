namespace GamingDW.WebApp.Reports;

public record RetentionEntry(string CohortDate, int Day0Users, int Day1, int Day3, int Day7, int Day14, int Day30);

public static class RetentionReport
{
    /// <summary>
    /// Day-over-day retention analysis.
    /// For each daily cohort (users who played on Day 0), counts how many returned on Day 1, 3, 7, 14, 30.
    /// Uses self-join on UserSessions comparing login dates.
    /// </summary>
    public static string RetentionSql => """
        WITH DailyActive AS (
            SELECT DISTINCT UserId, DATE(LoginTime) AS ActiveDate
            FROM UserSessions
        ),
        Cohorts AS (
            SELECT
                d0.ActiveDate AS CohortDate,
                d0.UserId,
                CAST(julianday(d1.ActiveDate) - julianday(d0.ActiveDate) AS INTEGER) AS DaysSince
            FROM DailyActive d0
            LEFT JOIN DailyActive d1 ON d0.UserId = d1.UserId
                AND d1.ActiveDate >= d0.ActiveDate
                AND julianday(d1.ActiveDate) - julianday(d0.ActiveDate) <= 30
        )
        SELECT
            CohortDate,
            COUNT(DISTINCT CASE WHEN DaysSince = 0 THEN UserId END) AS Day0Users,
            COUNT(DISTINCT CASE WHEN DaysSince = 1 THEN UserId END) AS Day1,
            COUNT(DISTINCT CASE WHEN DaysSince = 3 THEN UserId END) AS Day3,
            COUNT(DISTINCT CASE WHEN DaysSince = 7 THEN UserId END) AS Day7,
            COUNT(DISTINCT CASE WHEN DaysSince = 14 THEN UserId END) AS Day14,
            COUNT(DISTINCT CASE WHEN DaysSince = 30 THEN UserId END) AS Day30
        FROM Cohorts
        GROUP BY CohortDate
        HAVING Day0Users >= 10
        ORDER BY CohortDate DESC
        LIMIT 30
        """;
}
