using Grants.ApplicantPortal.API.Core.Plugins.External;

namespace Grants.ApplicantPortal.API.FunctionalTests.ApiEndpoints.Contributors.Mocks;

/// <summary>
/// Mock implementation of IExternalServiceClient for functional tests
/// </summary>
public class MockExternalServiceClient : IExternalServiceClient
{
    public Task<ExternalServiceResponse<T>> CallAsync<T>(string pluginId, ExternalServiceRequest request, CancellationToken cancellationToken = default)
    {
        // Return a simple mock response for tests
        var response = new ExternalServiceResponse<T>
        {
            IsSuccess = true,
            Data = default,
            StatusCode = 200
        };

        return Task.FromResult(response);
    }

    public Task<ExternalServiceResponse<string>> CallAsync(string pluginId, ExternalServiceRequest request, CancellationToken cancellationToken = default)
    {
        // Return a simple mock response for tests
        var response = new ExternalServiceResponse<string>
        {
            IsSuccess = true,
            Data = $"{{\"mockData\": \"test data for {pluginId}\"}}",
            StatusCode = 200
        };

        return Task.FromResult(response);
    }
}
