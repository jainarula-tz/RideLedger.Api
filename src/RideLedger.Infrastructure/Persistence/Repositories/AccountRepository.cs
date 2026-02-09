using FluentResults;
using Microsoft.EntityFrameworkCore;
using RideLedger.Domain.Aggregates;
using RideLedger.Domain.Enums;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;
using RideLedger.Infrastructure.Persistence.Entities;
using RideLedger.Infrastructure.Persistence.Mappers;

namespace RideLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// INFRASTRUCTURE LAYER - Repository Implementation
/// Implements IAccountRepository for Account aggregate persistence
/// Maps between domain and persistence entities
/// </summary>
public sealed class AccountRepository : IAccountRepository
{
    private readonly AccountingDbContext _context;
    private readonly AccountMapper _mapper;

    public AccountRepository(AccountingDbContext context)
    {
        _context = context;
        _mapper = new AccountMapper();
    }

    public async Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == id.Value, cancellationToken);

        return entity == null ? null : _mapper.ToDomain(entity);
    }

    public async Task<Account?> GetByIdWithLedgerEntriesAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        // Use AsNoTracking for read operations - UpdateAsync will load with tracking if needed
        var entity = await _context.Accounts
            .Include(a => a.LedgerEntries)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == id.Value, cancellationToken);

        return entity == null ? null : _mapper.ToDomainWithEntries(entity);
    }

    public async Task<Result> AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = _mapper.ToEntity(account);
            await _context.Accounts.AddAsync(entity, cancellationToken);
            // Don't call SaveChangesAsync here - UnitOfWork handles it
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to add account: {ex.Message}");
        }
    }

    public async Task<Result> UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        try
        {
            // Always load fresh from database with ledger entries to ensure we have latest data
            // Note: Can't reuse tracked entity from GetByIdWithLedgerEntriesAsync because 
            // each HTTP request gets a new DbContext scope
            var existingEntity = await _context.Accounts
                .Include(a => a.LedgerEntries)
                .FirstOrDefaultAsync(a => a.AccountId == account.Id.Value, cancellationToken);

            if (existingEntity == null)
            {
                return Result.Fail("Account not found");
            }

            // Update account properties
            existingEntity.Name = account.Name;
            existingEntity.Status = account.Status;
            existingEntity.UpdatedAt = account.UpdatedAtUtc;

            // Add only new ledger entries (ones not already in database)
            var existingEntryIds = existingEntity.LedgerEntries.Select(e => e.LedgerEntryId).ToHashSet();
            var newEntries = account.LedgerEntries
                .Where(e => !existingEntryIds.Contains(e.Id))
                .Select(e => new LedgerEntryEntity
                {
                    LedgerEntryId = e.Id,
                    AccountId = account.Id.Value,
                    TenantId = e.TenantId, // FIX: Set tenant ID from domain entity
                    LedgerAccount = e.LedgerAccount,
                    DebitAmount = e.DebitAmount?.Amount,
                    CreditAmount = e.CreditAmount?.Amount,
                    Currency = e.DebitAmount?.Currency ?? e.CreditAmount?.Currency ?? "USD",
                    SourceType = e.SourceType,
                    SourceReferenceId = e.SourceReferenceId,
                    TransactionDate = e.TransactionDate,
                    CreatedAt = e.CreatedAtUtc, // FIX: Use domain entity's creation time
                    CreatedBy = e.CreatedBy // FIX: Use domain entity's creator
                })
                .ToList();

            if (newEntries.Any())
            {
                await _context.LedgerEntries.AddRangeAsync(newEntries, cancellationToken);
            }

            // Don't call SaveChangesAsync here - UnitOfWork handles it
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to update account: {ex.Message}");
        }
    }

    public async Task<bool> ExistsAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .AnyAsync(a => a.AccountId == id.Value, cancellationToken);
    }

    public async Task<(List<Account> Accounts, int TotalCount)> SearchAsync(
        string? searchTerm,
        AccountType? type,
        AccountStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Build query with filters
        var query = _context.Accounts
            .Include(a => a.LedgerEntries)
            .AsQueryable();

        // Apply search term filter (search in name or account ID)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchTermLower = searchTerm.ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(searchTermLower) ||
                a.AccountId.ToString().ToLower().Contains(searchTermLower));
        }

        // Apply type filter
        if (type.HasValue)
        {
            query = query.Where(a => a.Type == type.Value);
        }

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var entities = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Map to domain
        var accounts = entities.Select(e => _mapper.ToDomainWithEntries(e)).ToList();

        return (accounts, totalCount);
    }
}
