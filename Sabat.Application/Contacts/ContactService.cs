using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Sabat.Application.Abstractions;
using Sabat.Application.Common;
using Sabat.Domain;

namespace Sabat.Application.Contacts;

public sealed class ContactService(ISabatDbContext dbContext, TimeProvider timeProvider) : IContactService
{
    public async Task<ContactMessageDto> CreateAsync(
        CreateContactRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        var entity = new ContactMessage
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Message = request.Message.Trim(),
            Language = SupportedLanguages.Normalize(request.Language),
            Status = ContactStatus.New,
            CreatedAt = timeProvider.GetUtcNow()
        };

        dbContext.ContactMessages.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<PagedResult<ContactMessageDto>> GetListAsync(
        ContactStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.ContactMessages.AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(message => message.Status == status.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(message => message.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(message => message.CreatedAt <= to.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(message => message.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(message => new ContactMessageDto(
                message.Id,
                message.Name,
                message.Email,
                message.Message,
                message.Language,
                message.Status,
                message.CreatedAt,
                message.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ContactMessageDto>(items, page, pageSize, totalCount);
    }

    public async Task<ContactMessageDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ContactMessages.AsNoTracking()
            .SingleOrDefaultAsync(message => message.Id == id, cancellationToken);
        return entity is null
            ? throw new NotFoundException("Contact message was not found.")
            : ToDto(entity);
    }

    public async Task<ContactMessageDto> UpdateStatusAsync(
        Guid id,
        ContactStatus status,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(status))
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["Unknown contact status."]
            });
        }

        var entity = await dbContext.ContactMessages.SingleOrDefaultAsync(
            message => message.Id == id,
            cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException("Contact message was not found.");
        }

        entity.Status = status;
        entity.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ContactMessages.SingleOrDefaultAsync(
            message => message.Id == id,
            cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException("Contact message was not found.");
        }

        dbContext.ContactMessages.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(CreateContactRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length is < 2 or > 120)
        {
            errors["name"] = ["Name must contain between 2 and 120 characters."];
        }

        if (!IsValidEmail(request.Email))
        {
            errors["email"] = ["A valid email address is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Trim().Length is < 10 or > 5_000)
        {
            errors["message"] = ["Message must contain between 10 and 5000 characters."];
        }

        if (!SupportedLanguages.Contains(request.Language))
        {
            errors["language"] = ["Supported languages are ru, ky and en."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
        {
            return false;
        }

        try
        {
            return new MailAddress(email).Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static ContactMessageDto ToDto(ContactMessage message) => new(
        message.Id,
        message.Name,
        message.Email,
        message.Message,
        message.Language,
        message.Status,
        message.CreatedAt,
        message.UpdatedAt);
}
