using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Reembolso.Application.Abstractions;
using Reembolso.Application.Dtos.Reimbursements;
using Reembolso.Domain.Enums;

namespace Reembolso.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reimbursements")]
public sealed class ReimbursementsController : ControllerBase
{
    private readonly IReimbursementService _service;

    public ReimbursementsController(IReimbursementService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] RequestStatus? status = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? costCenterId = null,
        [FromQuery] DateOnly? expenseDateFrom = null,
        [FromQuery] DateOnly? expenseDateTo = null,
        [FromQuery] DateTimeOffset? createdFrom = null,
        [FromQuery] DateTimeOffset? createdTo = null,
        [FromQuery] string? requestNumber = null,
        [FromQuery] bool createdByMe = false,
        [FromQuery] string sort = "createdAt:desc",
        CancellationToken cancellationToken = default)
    {
        var query = new ReimbursementListQuery(page, pageSize, status, categoryId, costCenterId, expenseDateFrom, expenseDateTo, createdFrom, createdTo, requestNumber, createdByMe, sort);
        return Ok(await _service.GetPagedAsync(query, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReimbursementRequest request, CancellationToken cancellationToken)
    {
        var response = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDraft(Guid id, [FromBody] UpdateReimbursementDraftRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.UpdateDraftAsync(id, request, cancellationToken));
    }

    [EnableRateLimiting("critical")]
    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        await _service.SubmitAsync(id, cancellationToken);
        return NoContent();
    }

    [EnableRateLimiting("critical")]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveReimbursementRequest request, CancellationToken cancellationToken)
    {
        await _service.ApproveAsync(id, request, cancellationToken);
        return NoContent();
    }

    [EnableRateLimiting("critical")]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectReimbursementRequest request, CancellationToken cancellationToken)
    {
        await _service.RejectAsync(id, request, cancellationToken);
        return NoContent();
    }

    [EnableRateLimiting("critical")]
    [HttpPost("{id:guid}/payment")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        await _service.RecordPaymentAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/attachments")]
    public async Task<IActionResult> GetAttachments(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetAttachmentsAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/attachments")]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> AddAttachment(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await _service.AddAttachmentAsync(id, file.FileName, file.ContentType, file.Length, stream, cancellationToken));
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        await _service.DeleteAttachmentAsync(id, attachmentId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await _service.DownloadAttachmentAsync(id, attachmentId, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("{id:guid}/workflow-actions")]
    public async Task<IActionResult> GetWorkflowActions(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetWorkflowActionsAsync(id, cancellationToken));
    }
}

