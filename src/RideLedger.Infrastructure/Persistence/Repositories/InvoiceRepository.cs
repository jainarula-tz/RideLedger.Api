using FluentResults;
using Microsoft.EntityFrameworkCore;
using RideLedger.Domain.Aggregates;
using RideLedger.Domain.Enums;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;
using RideLedger.Infrastructure.Persistence;
using RideLedger.Infrastructure.Persistence.Mappers;

namespace RideLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Invoice aggregate
/// </summary>
public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly AccountingDbContext _context;
    private readonly InvoiceMapper _mapper;

    public InvoiceRepository(AccountingDbContext context)
    {
        _context = context;
        _mapper = new InvoiceMapper();
    }

    public async Task<Result<Invoice>> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Invoices
            .Include(i => i.LineItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (entity == null)
        {
            return Result.Fail<Invoice>($"Invoice with ID {invoiceId} not found");
        }

        var invoice = _mapper.ToDomain(entity);
        return Result.Ok(invoice);
    }

    public async Task<Invoice?> GetByIdWithLineItemsAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Invoices
            .Include(i => i.LineItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        return entity == null ? null : _mapper.ToDomain(entity);
    }

    public async Task<Result<Invoice>> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Invoices
            .Include(i => i.LineItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);

        if (entity == null)
        {
            return Result.Fail<Invoice>($"Invoice with number {invoiceNumber} not found");
        }

        var invoice = _mapper.ToDomain(entity);
        return Result.Ok(invoice);
    }

    public async Task<Result> AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = _mapper.ToEntity(invoice);
            await _context.Invoices.AddAsync(entity, cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to add invoice: {ex.Message}");
        }
    }

    public async Task<Result> UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = _mapper.ToEntity(invoice);
            _context.Invoices.Update(entity);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to update invoice: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<Invoice>>> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.Invoices
            .Include(i => i.LineItems)
            .Where(i => i.AccountId == accountId.Value)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var invoices = entities.Select(_mapper.ToDomain).ToList();
        return Result.Ok<IEnumerable<Invoice>>(invoices);
    }

    public async Task<(List<Invoice> Invoices, int TotalCount)> SearchAsync(
        Guid? accountId,
        InvoiceStatus? status,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Build query with filters
        var query = _context.Invoices
            .Include(i => i.LineItems)
            .AsQueryable();

        // Apply account filter
        if (accountId.HasValue)
        {
            query = query.Where(i => i.AccountId == accountId.Value);
        }

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        // Apply date range filter (on billing period)
        if (startDate.HasValue)
        {
            query = query.Where(i => i.BillingPeriodStart >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(i => i.BillingPeriodEnd <= endDate.Value.Date);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var entities = await query
            .OrderByDescending(i => i.GeneratedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Map to domain
        var invoices = entities.Select(_mapper.ToDomain).ToList();

        return (invoices, totalCount);
    }
}
