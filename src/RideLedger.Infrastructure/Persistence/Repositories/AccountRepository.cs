using FluentResults;
using Microsoft.EntityFrameworkCore;
using RideLedger.Domain.Aggregates;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;
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
            await _context.SaveChangesAsync(cancellationToken);
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
            var entity = _mapper.ToEntity(account);
            _context.Accounts.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
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
}
