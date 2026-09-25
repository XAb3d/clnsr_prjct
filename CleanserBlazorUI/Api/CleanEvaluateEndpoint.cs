using CleanserBlazorUI.Engine;

namespace CleanserBlazorUI.Api;

public static class CleanEvaluateEndpoint
{
    public static void MapCleanEvaluate(this WebApplication app)
    {
        app.MapPost("/api/v1/clean/evaluate", Evaluate)
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithName("CleanEvaluate");
    }

    static async Task<IResult> Evaluate(
        HttpContext http,
        CleanserEngine engine,
        IConfiguration config)
    {
        var expected = config["CleanserApi:ApiKey"];
        if (!string.IsNullOrEmpty(expected))
        {
            if (!http.Request.Headers.TryGetValue("X-Api-Key", out var provided) ||
                !string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            }
        }

        if (!http.Request.HasFormContentType)
            return Results.BadRequest(new { error = "Expected multipart form with 'file' and 'recordType'." });

        var form = await http.Request.ReadFormAsync();
        var recordType = form["recordType"].ToString();
        var file = form.Files.GetFile("file");
        if (file == null || file.Length == 0)
            return Results.BadRequest(new { error = "Upload a CSV as form field 'file'." });
        if (string.IsNullOrWhiteSpace(recordType))
            return Results.BadRequest(new { error = "Form field 'recordType' must be ind or bus." });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await engine.EvaluateCsvAsync(recordType, stream, http.RequestAborted);
            return Results.Json(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
