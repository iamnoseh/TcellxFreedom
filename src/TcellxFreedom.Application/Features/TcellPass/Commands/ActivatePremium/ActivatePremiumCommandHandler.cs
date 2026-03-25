using System.Net;
using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Application.Interfaces;

namespace TcellxFreedom.Application.Features.TcellPass.Commands.ActivatePremium;

public sealed class ActivatePremiumCommandHandler(
    IUserTcellPassRepository passRepository,
    ITcellPassService tcellPassService)
    : IRequestHandler<ActivatePremiumCommand, Response<ActivatePremiumResultDto>>
{
    private const decimal PremiumPrice = 19m;

    public async Task<Response<ActivatePremiumResultDto>> Handle(ActivatePremiumCommand request, CancellationToken cancellationToken)
    {
        var pass = await passRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (pass is null)
        {
            pass = UserTcellPass.Create(request.UserId);
            await passRepository.CreateAsync(pass, cancellationToken);
        }

        if (pass.Tier == UserTier.Premium && pass.PremiumExpiresAt.HasValue && pass.PremiumExpiresAt.Value > DateTime.UtcNow)
            return new Response<ActivatePremiumResultDto>(HttpStatusCode.BadRequest,
                $"Шумо аллакай Премиум ҳастед. Муҳлат: {pass.PremiumExpiresAt.Value:yyyy-MM-dd}.");

        var paid = await tcellPassService.ProcessPremiumPaymentAsync(request.UserId, PremiumPrice, cancellationToken);
        if (!paid)
            return new Response<ActivatePremiumResultDto>(HttpStatusCode.PaymentRequired,
                $"Балансатон кофӣ нест. Нархи Премиум: {PremiumPrice} сомонӣ.");

        var expiresAt = DateTime.UtcNow.AddMonths(1);
        pass.ActivatePremium(expiresAt);
        await passRepository.UpdateAsync(pass, cancellationToken);

        return new Response<ActivatePremiumResultDto>(new ActivatePremiumResultDto(
            ExpiresAt: expiresAt,
            Message: $"Премиум фаъол шуд! То {expiresAt:yyyy-MM-dd} амал мекунад."
        ));
    }
}
