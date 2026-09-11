using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabat.API.Security;
using Sabat.Application.Abstractions;
using Sabat.Application.Common;
using Sabat.Application.News;
using Sabat.Domain;

namespace Sabat.API.Controllers;

[ApiController]
[Authorize(Policy = AdminApiKeyOptions.PolicyName)]
[Route("api/admin/news")]
public sealed class AdminNewsController(INewsService newsService, IFileStorage fileStorage) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AdminNewsDto>> GetList(
        [FromQuery] string? lang = null,
        [FromQuery] NewsStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        newsService.GetAdminListAsync(lang, status, page, pageSize, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<AdminNewsDto> GetById(Guid id, CancellationToken cancellationToken) =>
        newsService.GetAdminByIdAsync(id, cancellationToken);

    [HttpPost]
    [ProducesResponseType<AdminNewsDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AdminNewsDto>> Create(
        UpsertNewsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await newsService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public Task<AdminNewsDto> Update(
        Guid id,
        UpsertNewsRequest request,
        CancellationToken cancellationToken) =>
        newsService.UpdateAsync(id, request, cancellationToken);

    [HttpPatch("{id:guid}/status")]
    public Task<AdminNewsDto> SetStatus(
        Guid id,
        SetNewsStatusRequest request,
        CancellationToken cancellationToken) =>
        newsService.SetStatusAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await newsService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("images")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<object>> UploadImage(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["file"] = ["An image file is required."]
            });
        }

        await using var stream = file.OpenReadStream();
        var url = await fileStorage.SaveNewsImageAsync(
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);
        return Ok(new { url });
    }
}
