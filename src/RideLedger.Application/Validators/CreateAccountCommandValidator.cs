using FluentValidation;
using RideLedger.Application.Commands.Accounts;

namespace RideLedger.Application.Validators;

/// <summary>
/// APPLICATION LAYER - Validation
/// FluentValidation validator for CreateAccountCommand
/// </summary>
public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Account name is required")
            .MaximumLength(200)
            .WithMessage("Account name cannot exceed 200 characters")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Account name cannot be only whitespace");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid account type");
    }
}
