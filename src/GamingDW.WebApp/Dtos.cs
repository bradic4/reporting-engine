namespace GamingDW.WebApp;

// ── Auth ──
public record LoginRequest(string Username, string Password);

// ── Staff Management ──
public record StaffRequest(
    string? Username, string? Password, string? Title,
    bool CanViewReports, bool CanEditReports,
    bool CanSetTargets, bool CanViewLive,
    bool CanManageStaff, bool IsActive = true
);

// ── Daily Reports ──
public record DailyReportRequest(
    string Date,              // "2026-02-19"
    int Registrations,
    int FTDs,
    decimal Deposits,
    decimal Withdrawals,
    decimal GGR,
    int ActivePlayers,
    int Sessions,
    decimal BonusCost,
    string? Notes
);

// ── KPI Targets ──
public record KpiTargetRequest(
    string Period,            // "daily", "weekly", "monthly"
    string PeriodStart,       // "2026-02-19"
    string MetricName,        // "Registrations", "GGR", etc.
    decimal TargetValue
);

// ═══════════════════════════════════════
// RESPONSE DTOs
// ═══════════════════════════════════════

/// <summary>Single daily report returned from the API.</summary>
public record DailyReportDto(
    int Id, string Date, int Registrations, int FTDs,
    decimal Deposits, decimal Withdrawals, decimal GGR,
    int ActivePlayers, int Sessions, decimal BonusCost,
    decimal NetRevenue, string? Notes, string CreatedBy,
    DateTime CreatedAt, DateTime? UpdatedAt
);

/// <summary>Aggregated summary for a date range (used in comparison).</summary>
public record ReportSummaryDto(
    int Days, int Registrations, int FTDs,
    decimal Deposits, decimal Withdrawals, decimal GGR,
    int ActivePlayers, int Sessions, decimal BonusCost,
    decimal NetRevenue
);

/// <summary>Side-by-side comparison of two periods.</summary>
public record ComparePeriodsDto(ReportSummaryDto Period1, ReportSummaryDto Period2);

/// <summary>Staff user without sensitive fields like PasswordHash.</summary>
public record StaffUserDto(
    int Id, string Username, string Title, bool IsActive, DateTime CreatedAt,
    bool CanViewReports, bool CanEditReports, bool CanSetTargets,
    bool CanViewLive, bool CanManageStaff
);

/// <summary>KPI target returned from the API.</summary>
public record KpiTargetDto(
    int Id, string Period, string PeriodStart,
    string MetricName, decimal TargetValue,
    string CreatedBy, DateTime CreatedAt
);

/// <summary>Progress of a single metric against its target.</summary>
public record TargetProgressItemDto(string MetricName, decimal Target, decimal Actual, decimal ProgressPct);

/// <summary>Daily target progress check result.</summary>
public record TargetProgressDto(string Date, IEnumerable<TargetProgressItemDto> Progress);

/// <summary>Live dashboard metrics snapshot.</summary>
public record LiveMetricsDto(
    DateTime Timestamp, int Sessions, int ActivePlayers,
    decimal Deposits, decimal Withdrawals, decimal Bets,
    decimal Wins, decimal GGR, int Plays
);

/// <summary>Dashboard stats counters.</summary>
public record StatsDto(int Reports, int Targets, int Staff);

/// <summary>Audit log entry returned from the API.</summary>
public record AuditLogDto(
    int Id, string Action, string EntityType, int? EntityId,
    int? UserId, string? Username, string? OldValues,
    string? NewValues, string? IpAddress, DateTime Timestamp
);

/// <summary>Paginated result wrapper.</summary>
public record PagedResult<T>(IEnumerable<T> Data, PaginationMeta Meta);

/// <summary>Pagination metadata.</summary>
public record PaginationMeta(int Page, int PageSize, int TotalCount, int TotalPages);
