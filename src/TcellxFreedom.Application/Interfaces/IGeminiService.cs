using TcellxFreedom.Application.DTOs.Gemini;

namespace TcellxFreedom.Application.Interfaces;

public interface IGeminiService
{
    Task<GeminiScheduleResult> ScheduleTasksAsync(GeminiScheduleRequest request, CancellationToken ct = default);
    Task<GeminiChatScheduleResult> ParseAndScheduleFromChatAsync(GeminiChatScheduleRequest request, CancellationToken ct = default);
    Task<List<string>> GenerateImprovementSuggestionsAsync(GeminiStatsRequest request, CancellationToken ct = default);
}
