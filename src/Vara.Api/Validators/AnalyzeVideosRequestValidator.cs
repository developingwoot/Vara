using FluentValidation;
using Vara.Api.Models.DTOs;

namespace Vara.Api.Validators;

public class AnalyzeVideosRequestValidator : AbstractValidator<AnalyzeVideosRequest>
{
    public AnalyzeVideosRequestValidator()
    {
        RuleFor(x => x.Keyword)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.SampleSize)
            .InclusiveBetween(1, 50);
    }
}
