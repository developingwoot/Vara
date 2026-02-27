using FluentValidation;
using Vara.Api.Models.DTOs;

namespace Vara.Api.Validators;

public class SaveVideoRequestValidator : AbstractValidator<SaveVideoRequest>
{
    public SaveVideoRequestValidator()
    {
        RuleFor(x => x.YoutubeId)
            .NotEmpty().WithMessage("YouTube video ID is required.")
            .MaximumLength(11).WithMessage("YouTube video ID must not exceed 11 characters.");
    }
}
