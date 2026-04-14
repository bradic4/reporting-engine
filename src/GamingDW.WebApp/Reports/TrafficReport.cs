namespace GamingDW.WebApp.Reports;

public record TrafficEntry(string Period, int UniqueSessions, int UniqueUsers);

public static class TrafficReport
{
    /// <summary>
    /// Traffic by day: unique sessions and unique users per day.
    /// SQL: COUNT(*) for sessions, COUNT(DISTINCT UserId) for unique users, grouped by date.
    /// </summary>
    public static string ByDaySql => """
        SELECT
            DATE(LoginTime) AS Period,
            COUNT(*) AS UniqueSessions,
            COUNT(DISTINCT UserId) AS UniqueUsers
        FROM UserSessions
        GROUP BY DATE(LoginTime)
        ORDER BY Period DESC
        """;

    /// <summary>
    /// Traffic by hour: useful for identifying peak hours.
    /// </summary>
    public static string ByHourSql => """
        SELECT
            strftime('%H', LoginTime) AS Period,
            COUNT(*) AS UniqueSessions,
            COUNT(DISTINCT UserId) AS UniqueUsers
        FROM UserSessions
        GROUP BY strftime('%H', LoginTime)
        ORDER BY Period
        """;
}
