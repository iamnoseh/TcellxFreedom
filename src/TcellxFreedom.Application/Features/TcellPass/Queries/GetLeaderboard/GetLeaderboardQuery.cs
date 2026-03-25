using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;

namespace TcellxFreedom.Application.Features.TcellPass.Queries.GetLeaderboard;

public sealed record GetLeaderboardQuery(int TopN = 50) : IRequest<Response<List<LeaderboardEntryDto>>>;
