using Sabat.Application.Common;
using Sabat.Domain;

namespace Sabat.Application.Contacts;

public sealed class CreateContactRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Language { get; init; } = "ru";

    // Honeypot: real users never fill this field.
    public string? Website { get; init; }
}

public sealed class UpdateContactStatusRequest
{
    public ContactStatus Status { get; init; }
}

public sealed record ContactMessageDto(
    Guid Id,
    string Name,
    string Email,
    string Message,
    string Language,
    ContactStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public interface IContactService
{
    Task<ContactMessageDto> CreateAsync(CreateContactRequest request, CancellationToken cancellationToken);
    Task<PagedResult<ContactMessageDto>> GetListAsync(ContactStatus? status, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken);
    Task<ContactMessageDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ContactMessageDto> UpdateStatusAsync(Guid id, ContactStatus status, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
