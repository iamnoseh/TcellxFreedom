using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.TcellPass;

namespace TcellxFreedom.Application.Features.TcellPass.Queries.GetMyPass;

public sealed record GetMyPassQuery(string UserId) : IRequest<Response<UserTcellPassDto>>;
