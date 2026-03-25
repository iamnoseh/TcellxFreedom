using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;

namespace TcellxFreedom.Application.Features.TcellPass.Commands.ClaimReward;

public sealed record ClaimRewardCommand(string UserId, int Level) : IRequest<Response<ClaimRewardResultDto>>;
