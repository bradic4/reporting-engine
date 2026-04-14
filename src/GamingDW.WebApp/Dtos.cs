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
