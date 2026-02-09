using RideLedger.Domain.Aggregates;
using RideLedger.Domain.Entities;
using RideLedger.Domain.Enums;
using RideLedger.Domain.ValueObjects;
using RideLedger.Infrastructure.Persistence.Entities;
using System.Reflection;

namespace RideLedger.Infrastructure.Persistence.Mappers;

/// <summary>
/// INFRASTRUCTURE LAYER - Mapper
/// Maps between Account domain aggregate and AccountEntity persistence model
/// Uses reflection to set private fields in domain aggregate (DDD pattern)
/// </summary>
public sealed class AccountMapper
{
    public Account ToDomain(AccountEntity entity)
    {
        // Use reflection to create Account without going through factory method
        // This is necessary when reconstituting from database
        var account = (Account)Activator.CreateInstance(typeof(Account), nonPublic: true)!;
        
        SetPrivateProperty(account, "Id", AccountId.Create(entity.AccountId));
        SetPrivateProperty(account, "Name", entity.Name);
        SetPrivateProperty(account, "Type", entity.Type);
        SetPrivateProperty(account, "Status", entity.Status);
        SetPrivateProperty(account, "TenantId", entity.TenantId);
        SetPrivateProperty(account, "CreatedAtUtc", entity.CreatedAt);
        SetPrivateProperty(account, "UpdatedAtUtc", entity.UpdatedAt);

        return account;
    }

    public Account ToDomainWithEntries(AccountEntity entity)
    {
        var account = ToDomain(entity);

        // Reconstitute ledger entries
        var ledgerEntries = entity.LedgerEntries
            .Select(MapLedgerEntry)
            .ToList();

        var ledgerEntriesField = typeof(Account)
            .GetField("_ledgerEntries", BindingFlags.NonPublic | BindingFlags.Instance);
        ledgerEntriesField!.SetValue(account, ledgerEntries);

        return account;
    }

    public AccountEntity ToEntity(Account domain)
    {
        return new AccountEntity
        {
            AccountId = domain.Id.Value,
            Name = domain.Name,
            Type = domain.Type,
            Status = domain.Status,
            TenantId = domain.TenantId,
            CreatedAt = domain.CreatedAtUtc,
            UpdatedAt = domain.UpdatedAtUtc
        };
    }

    private LedgerEntry MapLedgerEntry(LedgerEntryEntity entity)
    {
        var entry = (LedgerEntry)Activator.CreateInstance(typeof(LedgerEntry), nonPublic: true)!;
        
        SetPrivateProperty(entry, "Id", entity.LedgerEntryId);
        SetPrivateProperty(entry, "LedgerAccount", entity.LedgerAccount);
        
        if (entity.DebitAmount.HasValue)
        {
            SetPrivateProperty(entry, "DebitAmount", Money.Create(entity.DebitAmount.Value, entity.Currency));
        }
        
        if (entity.CreditAmount.HasValue)
        {
            SetPrivateProperty(entry, "CreditAmount", Money.Create(entity.CreditAmount.Value, entity.Currency));
        }
        
        SetPrivateProperty(entry, "SourceType", entity.SourceType);
        SetPrivateProperty(entry, "SourceReferenceId", entity.SourceReferenceId);
        SetPrivateProperty(entry, "TransactionDate", entity.TransactionDate);
        SetPrivateProperty(entry, "CreatedAt", entity.CreatedAt);

        return entry;
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
