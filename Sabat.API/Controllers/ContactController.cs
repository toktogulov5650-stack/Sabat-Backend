using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sabat.Application.Contacts;

namespace Sabat.API.Controllers;

[ApiController]
[Route("api/contact")]
public sealed class ContactController(IContactService contactService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("contact")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Submit(
        CreateContactRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            return Accepted();
        }

        var result = await contactService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new
        {
            result.Id,
            message = "Your message has been received."
        });
    }
}
