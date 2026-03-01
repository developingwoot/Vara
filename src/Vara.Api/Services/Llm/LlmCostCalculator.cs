namespace Vara.Api.Services.Llm;

public static class LlmCostCalculator
{
    public static decimal Calculate(IConfiguration config, string model, int inputTokens, int outputTokens)
    {
        var section = config.GetSection($"Llm:Pricing:{model}");
        var inputRate  = section.GetValue<decimal>("InputPerMToken");
        var outputRate = section.GetValue<decimal>("OutputPerMToken");
        return (inputTokens  / 1_000_000m * inputRate)
             + (outputTokens / 1_000_000m * outputRate);
    }
}
