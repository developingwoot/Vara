using FluentValidation;
using Vara.Api.Models.DTOs;

namespace Vara.Api.Validators;

public class AddChannelRequestValidator : AbstractValidator<AddChannelRequest>
{
    public AddChannelRequestValidator()
    {
        RuleFor(x => x.HandleOrUrl)
            .NotEmpty().WithMessage("Channel handle or URL is required.")
            .MaximumLength(200).WithMessage("Handle or URL must not exceed 200 characters.");
    }
}
