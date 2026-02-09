using FluentValidation;
using RideLedger.Application.Commands.Charges;

namespace RideLedger.Application.Validators;

/// <summary>
/// APPLICATION LAYER - Validation
/// FluentValidation validator for RecordChargeCommand
/// </summary>
public sealed class RecordChargeCommandValidator : AbstractValidator<RecordChargeCommand>
{
    public RecordChargeCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.RideId)
            .NotEmpty()
            .WithMessage("Ride ID is required")
            .MaximumLength(100)
            .WithMessage("Ride ID cannot exceed 100 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .PrecisionScale(19, 4, true)
            .WithMessage("Amount cannot have more than 4 decimal places");

        RuleFor(x => x.ServiceDate)
            .NotEmpty()
            .WithMessage("Service date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Service date cannot be in the future");

        RuleFor(x => x.FleetId)
            .NotEmpty()
            .WithMessage("Fleet ID is required")
            .MaximumLength(50)
            .WithMessage("Fleet ID cannot exceed 50 characters");
    }
}
