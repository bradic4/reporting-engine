using FluentValidation;

namespace GamingDW.WebApp.Validation;

/// <summary>
/// Minimal API endpoint filter that validates request body using FluentValidation.
/// Usage: .AddEndpointFilter&lt;ValidationFilter&lt;DailyReportRequest&gt;&gt;()
/// </summary>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator) => _validator = validator;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var argument = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return await next(ctx);

        var result = await _validator.ValidateAsync(argument);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Results.BadRequest(new { error = "Validation failed", details = errors });
        }

        return await next(ctx);
    }
}
