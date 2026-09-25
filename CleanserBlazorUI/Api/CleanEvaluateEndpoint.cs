using CleanserBlazorUI.Engine;

namespace CleanserBlazorUI.Api;

/// <summary>HTTP entry point used by CKMS to evaluate exception rows.</summary>
public static class CleanEvaluateEndpoint
{
    public static void MapCleanEvaluate(this WebApplication app)
    {
        app.MapPost("/api/v1/clean/evaluate", EvaluateAsync)
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithName("CleanEvaluate");
    }

    private static async Task<IResult> EvaluateAsync(
        HttpRequest request,
        CleanserEngine engine,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var expectedApiKey = configuration["CleanserApi:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedApiKey))
            return Results.Problem("CleanserApi:ApiKey is not configured.", statusCode: 503);

        if (!request.Headers.TryGetValue("X-Api-Key", out var suppliedApiKey) ||
            !string.Equals(suppliedApiKey.ToString(), expectedApiKey, StringComparison.Ordinal))
            return Results.Unauthorized();

        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "Expected multipart form fields 'recordType' and 'file'." });

        var form = await request.ReadFormAsync(cancellationToken);
        var recordType = form["recordType"].ToString();
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "Upload a non-empty CSV in form field 'file'." });
        if (string.IsNullOrWhiteSpace(recordType))
            return Results.BadRequest(new { error = "Form field 'recordType' must be 'ind' or 'bus'." });
        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "Only CSV uploads are supported." });

        try
        {
            await using var stream = file.OpenReadStream();
            var response = await engine.EvaluateCsvAsync(recordType, stream, cancellationToken);
            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
