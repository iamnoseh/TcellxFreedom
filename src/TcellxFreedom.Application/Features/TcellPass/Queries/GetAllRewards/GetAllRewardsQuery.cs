using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;

namespace TcellxFreedom.Application.Features.TcellPass.Queries.GetAllRewards;

public sealed record GetAllRewardsQuery(string UserId) : IRequest<Response<List<LevelRewardDto>>>;
