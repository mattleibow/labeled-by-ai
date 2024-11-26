using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Octokit.GraphQL;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LabeledByAI;

public class LabelFunction(IChatClient chatClient, ILogger<LabelFunction> logger)
{
    public record LabelRequest(
        LabelRequestLabels Labels,
        LabelRequestIssue Issue);

    public record LabelRequestLabels(
        LabelRequestLabelsRules[]? Rules);

    public record LabelRequestLabelsRules(
        string? Name,
        string[]? Labels,
        string? Pattern);

    public record LabelRequestIssue(
        string Title,
        string Body);

    private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
        };

    [Function("label")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request)
    {
        logger.LogInformation("Starting to process a new issue...");

        var newIssue = await GetNewIssueAsync(request);

        if (newIssue is null)
        {
            logger.LogError("No new issue was provided in the request body.");
            return new BadRequestObjectResult("The new issue is null.");
        }
        if (string.IsNullOrWhiteSpace(newIssue.Issue.Body))
        {
            logger.LogError("No new issue body was provided in the request body.");
            return new BadRequestObjectResult("The new issue body is null.");
        }
        if (newIssue.Labels?.Rules is null || newIssue.Labels.Rules.Length == 0)
        {
            logger.LogError("No label rules were provided in the request body.");
            return new BadRequestObjectResult("No labels provided.");
        }

        logger.LogInformation("The new issue is a valid object.");

        logger.LogInformation("Loading matching labels from GitHub...");

        var connection = new Connection(
            new("Octokit.GraphQL.Net.SampleApp", "1.0"),
            request.Headers["X-GitHub-Token"]);

        var query = new Query()
            .RepositoryOwner(new("owner"))
            .Repository(Variable.Var("name"))
            .Select(repo => new
            {
                repo.Id,
                repo.Name,
                repo.Owner.Login,
                repo.IsFork,
                repo.IsPrivate,
            }).Compile();

        var vars = new Dictionary<string, object>
{
    { "owner", "octokit" },
    { "name", "octokit.graphql.net" },
};

        var result = await connection.Run(query, vars);

        //logger.LogInformation("Loaded labels, found {matching}.", matching.Count);

        logger.LogInformation("Generating OpenAI request...");

        IList<ChatMessage> messages =
        [
            //new(ChatRole.System, GetSystemPrompt(newIssue.Labels)),
            //new(ChatRole.Assistant, GetIssuePrompt(newIssue.Title, newIssue.Body)),
        ];

        logger.LogInformation(
            $"""
            messages >>>
            {string.Join(Environment.NewLine, messages.Select(m => $"{m.Role} => {m.Text}"))}
            <<< messages
            """);

        logger.LogInformation("Sending a request to OpenAI...");

        var options = new ChatOptions
        {
            MaxOutputTokens = 400,
        };
        var response = await chatClient.CompleteAsync(messages, options);

        logger.LogInformation("OpenAI has replied.");

        logger.LogInformation(
            $"""
            response >>>
            {response}
            <<< response
            """);

        return new OkObjectResult(response.ToString());
    }

    private async Task<LabelRequest?> GetNewIssueAsync(HttpRequest request)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<LabelRequest>(request.Body, SerializerOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize the request body.");
            return null;
        }
    }

    private static string GetSystemPrompt(params IEnumerable<string> labels) =>
        $$"""
        You are an expert developer who is able to correctly and
        accurately assign labels to new issues that are opened.

        You are to pick from the following list of labels and
        assign just one of them. If none of the labels are
        correct, do not assign any labels. If no issue content 
        was provided or if there is not enough content to make
        a decision, do not assign any labels. If the label that
        you have selected is not in the list of labels, then
        do not assign any labels.

        You also provide a reason as to why that label was 
        selected to make sure that everyone knows why.

        If no labels match or can be assigned, then you are to
        reply with a null label and null reason. 
        The only labels that are valid for assignment are found
        between the "===== Available Labels =====" lines. Do not
        return a label if that label is not found in there.

        ===== Available Labels =====
        {{string.Join(Environment.NewLine, labels)}}
        ===== Available Labels =====
        
        Please reply in json with the format and only in this format:

        { 
            "label": "LABEL_NAME_HERE",
            "reason": "REASON_FOR_LABEL_HERE"
        }

        """;

    private static string GetIssuePrompt(string? title, string body) => $"""
        A new issue has arrived, please label it correctly and accurately.
        
        The issue title is:
        {title ?? "-"}
        
        The issue body is:
        {body}
        """;
}
