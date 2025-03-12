using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mice_server;

namespace Mice_server.Services;

public class GreeterService(ILogger<GreeterService> logger) : MessageService.MessageServiceBase
{
    public override async Task SendMessage(Message request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
    {
        logger.LogInformation($"Received message from: {request.UserId}");

        var response = new Message
        {
            UserId = Guid.NewGuid().ToString(),
            Content = $"Welcome to the Mice Server {request.UserId}",
            SentDate = Timestamp.FromDateTime(DateTime.UtcNow)
        };
        
        await responseStream.WriteAsync(response);
    }
}