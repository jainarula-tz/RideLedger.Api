using FluentValidation;
using RideLedger.Application.Commands.Payments;

namespace RideLedger.Application.Validators;

/// <summary>
/// APPLICATION LAYER - Validation
/// FluentValidation validator for RecordPaymentCommand
/// </summary>
public sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.PaymentReferenceId)
            .NotEmpty()
            .WithMessage("Payment Reference ID is required")
            .MaximumLength(100)
            .WithMessage("Payment Reference ID cannot exceed 100 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .PrecisionScale(19, 4, true)
            .WithMessage("Amount cannot have more than 4 decimal places");

        RuleFor(x => x.PaymentDate)
            .NotEmpty()
            .WithMessage("Payment date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Payment date cannot be in the future");

        RuleFor(x => x.PaymentMode)
            .Must(BeValidPaymentMode)
            .When(x => !string.IsNullOrWhiteSpace(x.PaymentMode))
            .WithMessage("Invalid payment mode. Must be: Cash, Card, or BankTransfer");
    }

    private static bool BeValidPaymentMode(string? paymentMode)
    {
        if (string.IsNullOrWhiteSpace(paymentMode))
            return true;

        return paymentMode is "Cash" or "Card" or "BankTransfer";
    }
}
