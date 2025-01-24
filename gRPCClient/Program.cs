using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using gRPCServer.Greet;
using gRPCServer.Weather;

// The port number must match the port of the gRPC server.
using var channel = GrpcChannel.ForAddress("https://localhost:7042");

//unary Greet
var client = new Greeter.GreeterClient(channel);
var reply = await client.SayHelloUnaryAsync(
    new HelloRequest { Name = "GreeterClient" });
Console.WriteLine("Greeting: " + reply.Message);


//unary call Weather
//use case: as in any API call Request/response
var client2 = new WeatherService.WeatherServiceClient(channel);
var reply2 = await client2.CurrentWeatherUnaryAsync(
    new WeatherForCityRequest() { City = "London", Unit = Units.Imperial });
Console.WriteLine($"Weather temperature: {reply2.Temperature}, feels like: {reply2.FeelsLike}");


//Client Streaming, multiple inputs immediately sent for server to process, but only returns when commit all happened
//use case: maybe some kind of app that sends values from a form and needs to log each step done and then returns all steps 
//example audit?
try
{
    var client3 = new WeatherService.WeatherServiceClient(channel);
    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    using AsyncClientStreamingCall<WeatherForCityRequest, MultiWeatherResponse> clientStreamingCall = 
         client3.CurrentMultiWeatherStream(cancellationToken: cancellationToken.Token);
    var i = 0;
    while (true)
    {
        if (i >= 10)
        {
            await clientStreamingCall.RequestStream.CompleteAsync();
            Console.WriteLine("Client Streaming completed.");
            break;
        }
        else
        {
            //write to stream
            await clientStreamingCall.RequestStream.WriteAsync(
                new WeatherForCityRequest
                {
                    City = "City" +i,
                    Unit = i %2 == 0 ? Units.Imperial : Units.Metric
                });
            i++;
        }
    }
    var response = await clientStreamingCall;
    Console.WriteLine(response.Weather);
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
{
    Console.WriteLine("Stream cancelled.");
}


//Server Streaming
//use case: there are some external services needed to be called by server to accomplish different results for same input
//then in result will list various possibilities, example a price comparer on several providers
//not sure what would happen or how to treat a response if in middle there was a very slow response from one of this providers?
try
{
    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    
    using var channelstream = GrpcChannel.ForAddress("https://localhost:7042");
    var client4 = new WeatherService.WeatherServiceClient(channelstream);
    var streamingCall = client4.CurrentWeatherStream(new WeatherForCityRequest() { City = "London", Unit = Units.Imperial });
    await foreach (var weatherData in streamingCall.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken.Token))
    {
        Console.WriteLine($"Weather temperature: {weatherData.Temperature}, feels like: {weatherData.FeelsLike}, at timestamp: {weatherData}");
    }

    Console.WriteLine("Stream completed.");
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
{
    Console.WriteLine("Stream cancelled.");
}


//Bidirectional Streaming
//Multiple inputs immediately sent for server to process, and server can commit back and stream to client the response
//for each one without waiting on all final commit, after commit will end channel opened
//use case: maybe some kind of app that sends values from a form and needs each step sent to calculate something and return immediately that value so it can be captured client side.
//example messages logging the steps as they are being processed on client side with confirmation?
try
{
    using var channelbistream = GrpcChannel.ForAddress("https://localhost:7042");
    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var client4 = new WeatherService.WeatherServiceClient(channelbistream);
    using AsyncDuplexStreamingCall<WeatherForCityRequest, WeatherResponse> duplexStreamingCall =
        client4.CurrentMultiWeatherBidirectionalStream(cancellationToken: cancellationToken.Token);
    
    var i = 0;
    Task task = Task.WhenAll(new[]
    {
        Task.Run(async () =>
        {
            while (true)
            {
                if (i >= 10)
                {
                    await duplexStreamingCall.RequestStream.CompleteAsync();
                    Console.WriteLine("Client Streaming completed.");
                    break;
                }
                else
                {
                    //write to stream
                    await duplexStreamingCall.RequestStream.WriteAsync(
                        new WeatherForCityRequest
                        {
                            City = "City" + i,
                            Unit = i %2 == 0 ? Units.Imperial : Units.Metric
                        });
                    i++;
                }
            }
        }),
        Task.Run(async () =>
        {
            //read from stream
            while (!cancellationToken.IsCancellationRequested &&
                   await duplexStreamingCall.ResponseStream.MoveNext())
            {
                Console.WriteLine(duplexStreamingCall.ResponseStream.Current);
            }
        })
    });

    try
    {
        task.Wait(cancellationToken.Token);
    }
    catch (OperationCanceledException e)
    {
        await duplexStreamingCall.RequestStream.CompleteAsync();
        Thread.Sleep(6000);
    }
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
{
    Console.WriteLine("Stream cancelled.");
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();