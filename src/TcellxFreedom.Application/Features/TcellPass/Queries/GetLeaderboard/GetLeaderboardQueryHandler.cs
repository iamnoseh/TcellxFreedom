using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.TcellPass.Queries.GetLeaderboard;

public sealed class GetLeaderboardQueryHandler(
    IUserTcellPassRepository passRepository,
    IUserRepository userRepository)
    : IRequestHandler<GetLeaderboardQuery, Response<List<LeaderboardEntryDto>>>
{
    public async Task<Response<List<LeaderboardEntryDto>>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var topPasses = await passRepository.GetTopByXpAsync(request.TopN, cancellationToken);
        var userIds = topPasses.Select(p => p.UserId).ToList();
        var displayNames = await userRepository.GetDisplayNamesByIdsAsync(userIds, cancellationToken);

        var dtos = topPasses.Select((p, i) => new LeaderboardEntryDto(
            Rank: i + 1,
            UserId: p.UserId,
            DisplayName: displayNames.TryGetValue(p.UserId, out var name) ? name : p.UserId,
            TotalXp: p.TotalXp,
            CurrentLevel: p.CurrentLevel,
            Tier: p.Tier.ToString()
        )).ToList();

        return new Response<List<LeaderboardEntryDto>>(dtos);
    }
}
