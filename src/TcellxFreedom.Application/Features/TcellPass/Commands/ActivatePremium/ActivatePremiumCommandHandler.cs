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
                $"Вы уже являетесь Премиум-пользователем. Срок действия: {pass.PremiumExpiresAt.Value:yyyy-MM-dd}.");

        var paid = await tcellPassService.ProcessPremiumPaymentAsync(request.UserId, PremiumPrice, cancellationToken);
        if (!paid)
            return new Response<ActivatePremiumResultDto>(HttpStatusCode.PaymentRequired,
                $"Недостаточно средств на балансе. Стоимость Премиум: {PremiumPrice} сомони.");

        var expiresAt = DateTime.UtcNow.AddMonths(1);
        pass.ActivatePremium(expiresAt);
        await passRepository.UpdateAsync(pass, cancellationToken);

        return new Response<ActivatePremiumResultDto>(new ActivatePremiumResultDto(
            ExpiresAt: expiresAt,
            Message: $"Премиум активирован! Действует до {expiresAt:yyyy-MM-dd}."
        ));
    }
}
