using Refit;

namespace WebApi1Telemetry.HttpClients;

public interface IWebApi2TelClient
{
    [Get("/weatherforecast")]
    Task<WeatherForecast[]> GetResourceAsync();

    [Get("/getordermarker/{id}")]
    Task<string> GetOrderMarker(Guid id, CancellationToken cancellationToken);
}