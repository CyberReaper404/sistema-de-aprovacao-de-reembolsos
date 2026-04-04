using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reembolso.Application.Abstractions;

namespace Reembolso.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        return Ok(await _dashboardService.GetSummaryAsync(from, to, cancellationToken));
    }

    [HttpGet("by-category")]
    public async Task<IActionResult> ByCategory([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        return Ok(await _dashboardService.GetByCategoryAsync(from, to, cancellationToken));
    }

    [HttpGet("by-status")]
    public async Task<IActionResult> ByStatus([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        return Ok(await _dashboardService.GetByStatusAsync(from, to, cancellationToken));
    }
}

