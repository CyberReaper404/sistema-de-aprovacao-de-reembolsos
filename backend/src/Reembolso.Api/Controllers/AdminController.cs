using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reembolso.Application.Abstractions;
using Reembolso.Application.Dtos.Admin;

namespace Reembolso.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await _adminService.GetUsersAsync(page, pageSize, cancellationToken));

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.CreateUserAsync(request, cancellationToken));

    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.UpdateUserAsync(id, request, cancellationToken));

    [HttpPost("users/{id:guid}/revoke-sessions")]
    public async Task<IActionResult> RevokeSessions(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.RevokeUserSessionsAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("cost-centers")]
    public async Task<IActionResult> GetCostCenters(CancellationToken cancellationToken)
        => Ok(await _adminService.GetCostCentersAsync(cancellationToken));

    [HttpPost("cost-centers")]
    public async Task<IActionResult> CreateCostCenter([FromBody] CreateCostCenterRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.CreateCostCenterAsync(request, cancellationToken));

    [HttpPut("cost-centers/{id:guid}")]
    public async Task<IActionResult> UpdateCostCenter(Guid id, [FromBody] UpdateCostCenterRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.UpdateCostCenterAsync(id, request, cancellationToken));

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        => Ok(await _adminService.GetCategoriesAsync(cancellationToken));

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.CreateCategoryAsync(request, cancellationToken));

    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.UpdateCategoryAsync(id, request, cancellationToken));

    [HttpGet("audit-entries")]
    public async Task<IActionResult> GetAuditEntries([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
        => Ok(await _adminService.GetAuditEntriesAsync(page, pageSize, cancellationToken));
}

