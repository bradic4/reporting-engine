using FluentValidation;

namespace GamingDW.WebApp.Validation;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MaximumLength(50).WithMessage("Username must be at most 50 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(1).WithMessage("Password is required");
    }
}

public class DailyReportRequestValidator : AbstractValidator<DailyReportRequest>
{
    public DailyReportRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required")
            .Must(d => DateOnly.TryParse(d, out _)).WithMessage("Date must be a valid date (yyyy-MM-dd)");

        RuleFor(x => x.Registrations).GreaterThanOrEqualTo(0).WithMessage("Registrations must be >= 0");
        RuleFor(x => x.FTDs).GreaterThanOrEqualTo(0).WithMessage("FTDs must be >= 0");
        RuleFor(x => x.Deposits).GreaterThanOrEqualTo(0).WithMessage("Deposits must be >= 0");
        RuleFor(x => x.Withdrawals).GreaterThanOrEqualTo(0).WithMessage("Withdrawals must be >= 0");
        RuleFor(x => x.GGR).NotNull().WithMessage("GGR is required");
        RuleFor(x => x.ActivePlayers).GreaterThanOrEqualTo(0).WithMessage("ActivePlayers must be >= 0");
        RuleFor(x => x.Sessions).GreaterThanOrEqualTo(0).WithMessage("Sessions must be >= 0");
        RuleFor(x => x.BonusCost).GreaterThanOrEqualTo(0).WithMessage("BonusCost must be >= 0");
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("Notes must be at most 500 characters");
    }
}

public class KpiTargetRequestValidator : AbstractValidator<KpiTargetRequest>
{
    private static readonly string[] ValidPeriods = ["daily", "weekly", "monthly"];
    private static readonly string[] ValidMetrics =
        ["Registrations", "FTDs", "Deposits", "Withdrawals", "GGR", "ActivePlayers", "Sessions", "BonusCost", "NetRevenue"];

    public KpiTargetRequestValidator()
    {
        RuleFor(x => x.Period)
            .NotEmpty().WithMessage("Period is required")
            .Must(p => ValidPeriods.Contains(p)).WithMessage("Period must be: daily, weekly, or monthly");

        RuleFor(x => x.PeriodStart)
            .NotEmpty().WithMessage("PeriodStart is required")
            .Must(d => DateOnly.TryParse(d, out _)).WithMessage("PeriodStart must be a valid date");

        RuleFor(x => x.MetricName)
            .NotEmpty().WithMessage("MetricName is required")
            .Must(m => ValidMetrics.Contains(m)).WithMessage($"MetricName must be one of: {string.Join(", ", ValidMetrics)}");

        RuleFor(x => x.TargetValue).GreaterThan(0).WithMessage("TargetValue must be greater than 0");
    }
}

public class StaffRequestValidator : AbstractValidator<StaffRequest>
{
    public StaffRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MaximumLength(50).WithMessage("Username must be at most 50 characters");

        RuleFor(x => x.Title)
            .MaximumLength(100).WithMessage("Title must be at most 100 characters");
    }
}
