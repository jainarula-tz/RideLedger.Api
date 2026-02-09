using FluentResults;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Domain.Errors;

/// <summary>
/// Domain errors related to Invoice operations
/// </summary>
public static class InvoiceErrors
{
    public static Error NotFound(string invoiceNumber) =>
        new Error($"Invoice with number '{invoiceNumber}' was not found")
            .WithMetadata("ErrorCode", "INVOICE_NOT_FOUND")
            .WithMetadata("InvoiceNumber", invoiceNumber);

    public static Error NoBillableItems(AccountId accountId, DateTime startDate, DateTime endDate) =>
        new Error($"No billable items found for account '{accountId}' between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}")
            .WithMetadata("ErrorCode", "INVOICE_NO_BILLABLE_ITEMS")
            .WithMetadata("AccountId", accountId.Value)
            .WithMetadata("StartDate", startDate)
            .WithMetadata("EndDate", endDate);

    public static Error InvalidDateRange(DateTime startDate, DateTime endDate) =>
        new Error($"Invalid date range: start date ({startDate:yyyy-MM-dd}) must be before end date ({endDate:yyyy-MM-dd})")
            .WithMetadata("ErrorCode", "INVOICE_INVALID_DATE_RANGE")
            .WithMetadata("StartDate", startDate)
            .WithMetadata("EndDate", endDate);

    public static Error AlreadyExists(string invoiceNumber) =>
        new Error($"Invoice with number '{invoiceNumber}' already exists")
            .WithMetadata("ErrorCode", "INVOICE_ALREADY_EXISTS")
            .WithMetadata("InvoiceNumber", invoiceNumber);

    public static Error Immutable(string invoiceNumber) =>
        new Error($"Invoice '{invoiceNumber}' is immutable and cannot be modified")
            .WithMetadata("ErrorCode", "INVOICE_IMMUTABLE")
            .WithMetadata("InvoiceNumber", invoiceNumber);
}
