namespace TcellxFreedom.Application.DTOs.Plan;

public sealed record AiSuggestionReviewDto(
    List<TaskAcceptanceDto> Decisions
);

public sealed record TaskAcceptanceDto(
    Guid TaskId,
    bool Accept
);
