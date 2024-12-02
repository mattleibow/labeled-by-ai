using LabeledByAI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace LabeledByAI;

public class EngagementScoreFunction(CalculateEngagementScoreService service, ILogger<EngagementScoreFunction> logger)
    : BaseFunction<CalculateEngagementScoreRequest>(logger)
{
    [Function("engagement-score")]
    public override Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request) =>
        base.Run(request);

    protected override async Task<IActionResult> OnRun(HttpRequest request, CalculateEngagementScoreRequest parsedBody)
    {
        var response = await service.ExecuteAsync(parsedBody, request.GetGithubToken());
        return new OkObjectResult(response);
    }
}
