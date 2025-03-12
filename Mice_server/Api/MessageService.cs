using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mice_server.Services;

namespace Mice_server.Api;

public class MessageService(ILogger<MessageService> logger, ChatState chatState) : Mice_server.MessageService.MessageServiceBase
{
    public override async Task ConnectRequest(Connect connection, IServerStreamWriter<Message> responseStream, ServerCallContext context)
    {
        logger.LogInformation($"Connection requested from: {connection.User.Name}");

        var response = new Message
        {
            UserId = Guid.NewGuid().ToString(),
            Content = $"Welcome to the Mice Server {connection.User.Name}",
            SentDate = Timestamp.FromDateTime(DateTime.UtcNow)
        };
        
        await responseStream.WriteAsync(response);
        chatState.AddUser(connection, responseStream);
    }

    public override async Task<Close> BroadcastMessage(Message message, ServerCallContext context)
    {
        logger.LogInformation($"Broadcasting message: {message}");
        await chatState.BroadcastMessage(message);
        return new Close();
    }
}