using FluentValidation;
using Vara.Api.Models.DTOs;

namespace Vara.Api.Validators;

public class AnalyzeKeywordRequestValidator : AbstractValidator<AnalyzeKeywordRequest>
{
    public AnalyzeKeywordRequestValidator()
    {
        RuleFor(x => x.Keyword)
            .NotEmpty().WithMessage("Keyword is required.")
            .MaximumLength(255).WithMessage("Keyword must not exceed 255 characters.");

        RuleFor(x => x.Niche)
            .MaximumLength(100).WithMessage("Niche must not exceed 100 characters.")
            .When(x => x.Niche is not null);
    }
}
