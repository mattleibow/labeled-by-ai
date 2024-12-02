namespace LabeledByAI.Services;

public record CalculateEngagementScoreResponse(
    CalculateEngagementScoreResponseIssue Issue,
    CalculateEngagementScoreResponseEngagment Engagement);
