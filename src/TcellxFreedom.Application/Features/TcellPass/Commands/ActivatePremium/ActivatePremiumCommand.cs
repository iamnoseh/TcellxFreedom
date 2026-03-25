using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;

namespace TcellxFreedom.Application.Features.TcellPass.Commands.ActivatePremium;

public sealed record ActivatePremiumCommand(string UserId) : IRequest<Response<ActivatePremiumResultDto>>;
