using MediatR;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Statistics;

namespace TcellxFreedom.Application.Features.Plan.Queries.GetStatistics;

public sealed record GetStatisticsQuery(
    string UserId,
    int WeekCount = 4
) : IRequest<Response<StatisticsDto>>;
