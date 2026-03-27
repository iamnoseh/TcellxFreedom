using System.Net;
using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.TcellPass.Commands.ClaimReward;

public sealed class ClaimRewardCommandHandler(
    IUserTcellPassRepository passRepository,
    ILevelRewardRepository rewardRepository,
    IUserLevelRewardRepository userRewardRepository)
    : IRequestHandler<ClaimRewardCommand, Response<ClaimRewardResultDto>>
{
    public async Task<Response<ClaimRewardResultDto>> Handle(ClaimRewardCommand request, CancellationToken cancellationToken)
    {
        var pass = await passRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (pass is null)
            return new Response<ClaimRewardResultDto>(HttpStatusCode.NotFound, "TcellPass не найден. Откройте приложение для начала.");

        // Reward for level N unlocks only after reaching level N+1 (except level 20 which unlocks at level 20)
        var requiredLevel = request.Level < 20 ? request.Level + 1 : 20;
        if (pass.CurrentLevel < requiredLevel)
            return new Response<ClaimRewardResultDto>(HttpStatusCode.BadRequest, $"Для получения этой награды вам необходимо достичь уровня {requiredLevel}.");

        pass.CheckPremiumExpiry();
        var reward = await rewardRepository.GetByLevelAndTierAsync(request.Level, pass.Tier, cancellationToken);
        if (reward is null)
            return new Response<ClaimRewardResultDto>(HttpStatusCode.NotFound, "Награда не найдена.");

        var existing = await userRewardRepository.GetByUserAndLevelAsync(request.UserId, request.Level, pass.Tier, cancellationToken);
        if (existing is not null && existing.Status == RewardClaimStatus.Claimed)
            return new Response<ClaimRewardResultDto>(HttpStatusCode.BadRequest, "Эта награда уже получена.");

        var userReward = UserLevelReward.Create(request.UserId, reward.Id, request.Level);
        userReward.MarkClaimed();
        await userRewardRepository.CreateAsync(userReward, cancellationToken);

        return new Response<ClaimRewardResultDto>(new ClaimRewardResultDto(
            Level: request.Level,
            RewardDescription: reward.RewardDescription,
            Message: $"Поздравляем! Ваша награда: {reward.RewardDescription}"
        ));
    }
}
