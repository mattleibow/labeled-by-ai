namespace LabeledByAI.Services;

public record CalculateEngagementScoreRequestIssue(
    string Owner,
    string Repo,
    int? Number);
