using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;


var builder = WebApplication.CreateBuilder(args);

AddAppHealthChecks(builder.Services);

var app = builder.Build();

app.UseHttpsRedirection();

UseAppHealthChecks(app);

app.Run();

void AddAppHealthChecks(IServiceCollection services)
{
    _ = services.AddHealthChecks()
        .AddCheck<TestHealthCheck>(nameof(TestHealthCheck), HealthStatus.Unhealthy, new List<string>()
        {
                    nameof(TestHealthCheck)
        });
}

void UseAppHealthChecks(WebApplication webApplication)
{
    webApplication.MapHealthChecks("/healthz", new HealthCheckOptions
    {
        Predicate = healthCheck => healthCheck.Tags.Contains(nameof(TestHealthCheck)),
        ResponseWriter = ResponseWriter
    });
}

Task ResponseWriter(HttpContext context, HealthReport healthReport)
{
    var lengthyString = "";
    for (int i = 0; i < 100; i++)
    {
        lengthyString += Guid.NewGuid().ToString();
    }

    return context.Response.WriteAsync(lengthyString);
}

public class TestHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        Tracker.NumberOfRequests++;
        if (Tracker.NumberOfRequests % 200 == 0)
        {
            GC.Collect();
            var info = GC.GetGCMemoryInfo();
            Tracker.TotalPromotions += info.PromotedBytes;

            Console.WriteLine($"{Tracker.NumberOfRequests} requests. " + Environment.NewLine +
                $"Promoted bytes: {info.PromotedBytes}. Total: {Tracker.TotalPromotions}" + Environment.NewLine +
                $"FinalizationPendingCount: {info.FinalizationPendingCount})" + Environment.NewLine +
                $"FragmentedBytes: {info.FragmentedBytes}" + Environment.NewLine +
                $"TotalCommitedBytes: {info.TotalCommittedBytes}" + Environment.NewLine +
                $"HeapSize: {info.HeapSizeBytes}. Growth: {info.HeapSizeBytes - Tracker.PreviousHeapSizeBytes}" + Environment.NewLine +
                $"");

            Tracker.PreviousHeapSizeBytes = info.HeapSizeBytes;            
        }
        return HealthCheckResult.Healthy();
    }
}

static class Tracker
{
    public static long TotalPromotions { get; set; }
    public static long PreviousHeapSizeBytes { get; set; }
    public static int NumberOfRequests { get; set; }
}
