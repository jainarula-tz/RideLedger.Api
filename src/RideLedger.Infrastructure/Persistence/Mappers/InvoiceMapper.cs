using RideLedger.Domain.Aggregates;
using RideLedger.Domain.Entities;
using RideLedger.Domain.ValueObjects;
using RideLedger.Infrastructure.Persistence.Entities;
using System.Reflection;
using System.Text.Json;

namespace RideLedger.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper between Invoice domain aggregate and InvoiceEntity persistence model
/// Uses reflection to set private fields in domain aggregate
/// </summary>
public sealed class InvoiceMapper
{
    public Invoice ToDomain(InvoiceEntity entity)
    {
        // Use reflection to create Invoice without going through factory method
        var invoice = (Invoice)Activator.CreateInstance(typeof(Invoice), nonPublic: true)!;

        SetPrivateProperty(invoice, "Id", entity.Id);
        SetPrivateProperty(invoice, "InvoiceNumber", entity.InvoiceNumber);
        SetPrivateProperty(invoice, "AccountId", AccountId.Create(entity.AccountId));
        SetPrivateProperty(invoice, "BillingFrequency", entity.BillingFrequency);
        SetPrivateProperty(invoice, "BillingPeriodStart", entity.BillingPeriodStart);
        SetPrivateProperty(invoice, "BillingPeriodEnd", entity.BillingPeriodEnd);
        SetPrivateProperty(invoice, "GeneratedAtUtc", entity.GeneratedAtUtc);
        SetPrivateProperty(invoice, "Status", entity.Status);
        SetPrivateProperty(invoice, "TotalPaymentsApplied", Money.Create(entity.TotalPaymentsApplied, entity.Currency));
        SetPrivateProperty(invoice, "TenantId", entity.TenantId);

        // Reconstitute line items
        var lineItems = entity.LineItems
            .Select(MapLineItem)
            .ToList();

        var lineItemsField = typeof(Invoice)
            .GetField("_lineItems", BindingFlags.NonPublic | BindingFlags.Instance);
        lineItemsField!.SetValue(invoice, lineItems);

        return invoice;
    }

    public InvoiceEntity ToEntity(Invoice domain)
    {
        return new InvoiceEntity
        {
            Id = domain.Id,
            InvoiceNumber = domain.InvoiceNumber,
            AccountId = domain.AccountId.Value,
            TenantId = domain.TenantId,
            BillingFrequency = domain.BillingFrequency,
            BillingPeriodStart = domain.BillingPeriodStart,
            BillingPeriodEnd = domain.BillingPeriodEnd,
            GeneratedAtUtc = domain.GeneratedAtUtc,
            Status = domain.Status,
            Subtotal = domain.Subtotal.Amount,
            TotalPaymentsApplied = domain.TotalPaymentsApplied.Amount,
            Currency = "USD",
            CreatedAtUtc = DateTime.UtcNow,
            LineItems = domain.LineItems.Select(li => new InvoiceLineItemEntity
            {
                Id = li.Id,
                InvoiceId = domain.Id,
                RideId = li.RideId,
                ServiceDate = li.ServiceDate,
                Amount = li.Amount.Amount,
                Description = li.Description,
                LedgerEntryIds = JsonSerializer.Serialize(li.LedgerEntryIds)
            }).ToList()
        };
    }

    private InvoiceLineItem MapLineItem(InvoiceLineItemEntity entity)
    {
        var lineItem = (InvoiceLineItem)Activator.CreateInstance(typeof(InvoiceLineItem), nonPublic: true)!;

        SetPrivateProperty(lineItem, "Id", entity.Id);
        SetPrivateProperty(lineItem, "InvoiceId", entity.InvoiceId);
        SetPrivateProperty(lineItem, "RideId", entity.RideId);
        SetPrivateProperty(lineItem, "ServiceDate", entity.ServiceDate);
        SetPrivateProperty(lineItem, "Amount", Money.Create(entity.Amount, "USD"));
        SetPrivateProperty(lineItem, "Description", entity.Description);

        // Deserialize ledger entry IDs from JSON
        var ledgerEntryIds = JsonSerializer.Deserialize<List<Guid>>(entity.LedgerEntryIds) ?? new List<Guid>();
        var ledgerEntryIdsField = typeof(InvoiceLineItem)
            .GetField("_ledgerEntryIds", BindingFlags.NonPublic | BindingFlags.Instance);
        ledgerEntryIdsField!.SetValue(lineItem, ledgerEntryIds);

        return lineItem;
    }

    private void SetPrivateProperty(object obj, string propertyName, object? value)
    {
        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null && property.CanWrite)
        {
            property.SetValue(obj, value);
        }
        else
        {
            // Try to set via backing field
            var field = obj.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }
}
