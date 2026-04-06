using Reembolso.Application.Common;
using Reembolso.Application.Dtos.Payments;

namespace Reembolso.Application.Abstractions;

public interface IPaymentService
{
    Task<PagedResult<PaymentListItemResponse>> GetPagedAsync(PaymentListQuery query, CancellationToken cancellationToken);

    Task<PaymentDetailResponse> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken);
}
