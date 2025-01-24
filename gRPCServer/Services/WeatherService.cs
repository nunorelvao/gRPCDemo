using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using gRPCServer.Weather;

namespace gRPCServer.Services;

/// <summary>
/// Weather Service inherits proto.
/// </summary>
public class WeatherService : gRPCServer.Weather.WeatherService.WeatherServiceBase
{
    private readonly ILogger<WeatherService> _logger;

    /// <summary>
    /// The Weather Service
    /// </summary>
    /// <param name="logger"></param>
    public WeatherService(ILogger<WeatherService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Unary Method for simple gRPC calls
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Task<WeatherResponse> CurrentWeatherUnary(WeatherForCityRequest request, ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPCServer contacted to return temperature for City: {city} in {unit} unit", request.City, request.Unit);

        var temperature = 20.0d;
        var feelsLike = 0.0d;
        switch (request.Unit)
        {
            case Units.Standard:
                feelsLike = temperature + 1.5d;
                break;
            case Units.Metric:
                feelsLike = temperature + 1.6d;
                break;
            case Units.Imperial:
                temperature =  DegreesToCelsius(20);
                feelsLike = DegreesToCelsius(20, 1.5d);
                break;
            default:
                feelsLike = temperature + 1;
                break;
        }

        var result = new WeatherResponse()
        {
            Temperature = temperature,
            FeelsLike = feelsLike,
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            City = request.City,
            Unit = request.Unit
        };
        return Task.FromResult(result);
    }

    private static double DegreesToCelsius(double tempCelsius, double increment = 0)
    {
        return (tempCelsius + increment) * (9 / 5) + 32;
    }

    /// <summary>
    /// Server streaming for multiple server responses periodically
    /// </summary>
    /// <param name="request"></param>
    /// <param name="responseStream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task<WeatherResponse> CurrentWeatherStream(WeatherForCityRequest request,
        IServerStreamWriter<WeatherResponse> responseStream, ServerCallContext context)
    {
        var weatherResponse = new WeatherResponse();
        
        //just to limit the max requests per second streaming 
        for (var i = 0; i < 60; i++)
        {
            weatherResponse = await CurrentWeatherUnary(request, context);
            
            if (context.CancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("CurrentWeatherStream cancelled at {i} seconds!", i);
                 break;
            }
            
            await responseStream.WriteAsync(weatherResponse);
            
            _logger.LogInformation("CurrentWeatherStream at {i} seconds reported at {date}", i, weatherResponse.Timestamp.ToDateTime());
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return weatherResponse;
    }

    /// <summary>
    /// Client streaming for multiple requests form client and one server side process each
    /// </summary>
    /// <param name="requestStream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task<MultiWeatherResponse> CurrentMultiWeatherStream(
        IAsyncStreamReader<WeatherForCityRequest> requestStream, ServerCallContext context)
    {
        var response = new MultiWeatherResponse();
        
        await foreach (var request in requestStream.ReadAllAsync())
        {
            var weatherResponse = await CurrentWeatherUnary(request, context);
            response.Weather.Add(weatherResponse);
        }
        
        return response;
    }

    public override async Task CurrentMultiWeatherBidirectionalStream(
        IAsyncStreamReader<WeatherForCityRequest> requestStream, IServerStreamWriter<WeatherResponse> responseStream,
        ServerCallContext context)
    {
        while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
        {
            var request = requestStream.Current;
            await Task.Delay(1000);
            await responseStream.WriteAsync(  await CurrentWeatherUnary(request, context));
        }
    }
}