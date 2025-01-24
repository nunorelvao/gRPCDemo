using Grpc.Core;
using gRPCServer.Greet;

namespace gRPCServer.Services;

#region snippet
/// <summary>
/// 
/// </summary>
public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;
    /// <summary>
    /// s
    /// </summary>
    /// <param name="logger"></param>
    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Task<HelloReply> SayHelloUnary(HelloRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPCServer contacted to return - Hello request Name {name}", request.Name);
        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }
}

#endregion