
var builder = DistributedApplication.CreateBuilder(args);

var insights = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureApplicationInsights("app-insights")
    : builder.AddConnectionString("app-insights");

//var openai = builder.ExecutionContext.IsPublishMode
//    ? builder.AddAzureOpenAI("openai") // deploy with app
//        .AddDeployment(new("openai-model", "gpt-4o-mini", "2024-07-18"))
//    : builder.AddConnectionString("openai"); // use external

var openai = builder.AddConnectionString("openai");

//var openai = builder.AddAzureOpenAI("openai");

var funcStorage = builder.AddAzureStorage("func-storage")
    .RunAsEmulator();

var func = builder
    .AddAzureFunctionsProject<Projects.LabeledByAI>("labeled-by-ai")
    .WithExternalHttpEndpoints()
    .WithHostStorage(funcStorage)
    .WithReference(openai)
    .WithReference(insights);

builder.Build().Run();
