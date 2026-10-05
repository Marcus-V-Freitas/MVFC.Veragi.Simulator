using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Common;

namespace MVFC.Veragi.Simulator.Api.Tests.Infrastructure;

public static class SimulatorHttpExtensions
{
    public static async Task<T?> ReadJsonAsync<T>(this HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>(JsonExtensions.Options);
    }

    public static async Task<T> ReadDataAsync<T>(this HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonExtensions.Options);
        return result!.Data;
    }

    public static async Task<T> GetDataAsync<T>(this HttpClient client, string requestUri)
    {
        var result = await client.GetFromJsonAsync<ApiResponse<T>>(requestUri, JsonExtensions.Options);
        return result!.Data;
    }

    public static async Task<HttpResponseMessage> PostIdempotentAsync<T>(
        this HttpClient client,
        string requestUri,
        T content,
        string? key = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(content, options: JsonExtensions.Options)
        };
        request.Headers.Add("Idempotency-Key", key ?? Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString());

        return await client.SendAsync(request);
    }

    public static async Task ProcessAsync(this SimulatorFixture fixture, string kind)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<OperationProcessor>().ProcessAsync(kind, true, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    public static async Task ConfigureScenarioAsync(this HttpClient client, string target, SimulationScenarioRequest request)
    {
        var response = await client.PutAsJsonAsync("/_simulator/scenarios/" + target, request, JsonExtensions.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
