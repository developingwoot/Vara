using Vara.Api.Models.DTOs;
using Vara.Api.Services.Llm;

namespace Vara.Api.Endpoints;

public static class LlmEndpoints
{
    public static RouteGroupBuilder MapLlmEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/generate", Generate)
            .WithTags("LLM")
            .WithSummary("Send a prompt to the configured LLM provider");

        return group;
    }

    // -------------------------------------------------------------------------
    // POST /api/llm/generate
    // -------------------------------------------------------------------------

    private static async Task<IResult> Generate(
        GenerateRequest req,
        ILlmOrchestrator orchestrator)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return Results.BadRequest(new { error = "Prompt is required." });

        if (string.IsNullOrWhiteSpace(req.TaskType))
            return Results.BadRequest(new { error = "TaskType is required." });

        var options = new LlmOptions(
            MaxTokens: req.MaxTokens,
            Temperature: req.Temperature);

        var result = await orchestrator.ExecuteAsync(req.TaskType, req.Prompt, options);

        return Results.Ok(new GenerateResponse(
            result.Content,
            result.PromptTokens,
            result.CompletionTokens,
            result.CostUsd,
            result.ProviderName,
            result.ModelUsed,
            result.GeneratedAt));
    }
}
