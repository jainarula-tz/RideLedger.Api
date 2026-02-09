using FluentResults;
using Microsoft.EntityFrameworkCore;
using RideLedger.Domain.Aggregates;
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
}
