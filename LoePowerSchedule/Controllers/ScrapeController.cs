using LoePowerSchedule.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoePowerSchedule.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ScrapeController(ImportService importService) : ControllerBase
{
    [HttpGet("start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportFromLoe()
    {
        await importService.ImportAsync();
        return Ok();
    }
}