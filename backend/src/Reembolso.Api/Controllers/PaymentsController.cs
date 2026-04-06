using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reembolso.Application.Abstractions;
using Reembolso.Application.Dtos.Payments;
using Reembolso.Domain.Enums;

namespace Reembolso.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? costCenterId = null,
        [FromQuery] PaymentMethod? paymentMethod = null,
        [FromQuery] DateTimeOffset? paidFrom = null,
        [FromQuery] DateTimeOffset? paidTo = null,
        [FromQuery] string? requestNumber = null,
        [FromQuery] string sort = "paidAt:desc",
        CancellationToken cancellationToken = default)
    {
        var query = new PaymentListQuery(page, pageSize, costCenterId, paymentMethod, paidFrom, paidTo, requestNumber, sort);
        return Ok(await _paymentService.GetPagedAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.GetByIdAsync(id, cancellationToken));
    }
}
