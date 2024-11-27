using System.Text.Json.Serialization;

namespace LabeledByAI.Services;

public record GitHubComment(
    string Id,
    string Author,
    string AuthorType,
    string Body,
    DateTimeOffset CreatedOn,
    int TotalReactions)
{
    public bool IsUser =>
        !AuthorType.StartsWith("/apps/");

    [JsonIgnore]
    public TimeSpan Age =>
        DateTimeOffset.UtcNow - CreatedOn;
}
