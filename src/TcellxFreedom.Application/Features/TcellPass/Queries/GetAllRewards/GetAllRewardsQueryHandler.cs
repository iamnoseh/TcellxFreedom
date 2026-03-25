using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.TcellPass.Queries.GetAllRewards;

public sealed class GetAllRewardsQueryHandler(
    ILevelRewardRepository rewardRepository,
    IUserLevelRewardRepository userRewardRepository)
    : IRequestHandler<GetAllRewardsQuery, Response<List<LevelRewardDto>>>
{
    public async Task<Response<List<LevelRewardDto>>> Handle(GetAllRewardsQuery request, CancellationToken cancellationToken)
    {
        var allRewards = await rewardRepository.GetAllAsync(cancellationToken);
        var userRewards = await userRewardRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var claimedRewardIds = userRewards.Select(r => r.LevelRewardId).ToHashSet();

        var dtos = allRewards.Select(r => new LevelRewardDto(
            r.Level,
            r.Tier.ToString(),
            r.RewardType.ToString(),
            r.RewardDescription,
            r.RewardQuantity,
            claimedRewardIds.Contains(r.Id)
        )).ToList();

        return new Response<List<LevelRewardDto>>(dtos);
    }
}
