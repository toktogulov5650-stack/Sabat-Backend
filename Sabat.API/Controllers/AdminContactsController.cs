using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabat.API.Security;
using Sabat.Application.Common;
using Sabat.Application.Contacts;
using Sabat.Domain;

namespace Sabat.API.Controllers;

[ApiController]
[Authorize(Policy = AdminApiKeyOptions.SessionPolicyName)]
[Route("api/admin/contacts")]
public sealed class AdminContactsController(IContactService contactService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ContactMessageDto>> GetList(
        [FromQuery] ContactStatus? status = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        contactService.GetListAsync(status, from, to, page, pageSize, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ContactMessageDto> GetById(Guid id, CancellationToken cancellationToken) =>
        contactService.GetByIdAsync(id, cancellationToken);

    [HttpPatch("{id:guid}/status")]
    public Task<ContactMessageDto> SetStatus(
        Guid id,
        UpdateContactStatusRequest request,
        CancellationToken cancellationToken) =>
        contactService.UpdateStatusAsync(id, request.Status, cancellationToken);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await contactService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
