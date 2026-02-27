using FluentValidation;

namespace Vara.Api.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator) => _validator = validator;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return Results.BadRequest();

        var result = await _validator.ValidateAsync(argument);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => char.ToLowerInvariant(g.Key[0]) + g.Key[1..],
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}
