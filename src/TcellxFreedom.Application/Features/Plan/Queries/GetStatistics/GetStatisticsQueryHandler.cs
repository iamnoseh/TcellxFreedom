using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.Statistics;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.Plan.Queries.GetStatistics;

public sealed class GetStatisticsQueryHandler(
    IUserTaskStatisticRepository statisticsRepository,
    ILogger<GetStatisticsQueryHandler> logger)
    : IRequestHandler<GetStatisticsQuery, Response<StatisticsDto>>
{
    public async Task<Response<StatisticsDto>> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
    {
        var stats = await statisticsRepository.GetByUserIdAsync(request.UserId, request.WeekCount, cancellationToken);

        var weeklyDtos = stats.Select(s => new WeeklyStatDto(
            s.WeekStartDate, s.TotalTasks, s.CompletedTasks, s.SkippedTasks, s.CompletionRate)).ToList();

        var aiSuggestions = ParseAiSuggestions(stats.FirstOrDefault(s => s.AiImprovementSuggestions is not null)?.AiImprovementSuggestions);

        return new Response<StatisticsDto>(new StatisticsDto(weeklyDtos, aiSuggestions));
    }

    private List<string> ParseAiSuggestions(string? json)
    {
        if (json is null) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Хатогӣ ҳангоми хондани тавсияҳои ИИ аз JSON");
            return [];
        }
    }
}
