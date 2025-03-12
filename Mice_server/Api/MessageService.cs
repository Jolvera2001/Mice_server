using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mice_server.Services;

namespace Mice_server.Api;

public class MessageService(ILogger<MessageService> logger, ChatState chatState) : Mice_server.MessageService.MessageServiceBase
{
    public override async Task ConnectRequest(Connect connection, IServerStreamWriter<Message> responseStream, ServerCallContext context)
    {
        logger.LogInformation($"Connection requested from: {connection.User.Name}");
        var content = $"Welcome to the Mice Server {connection.User.Name}";

        var response = chatState.CreateMessage(content);
        
        await responseStream.WriteAsync(response);
        var errorTcs = chatState.AddUser(connection, responseStream);

        context.CancellationToken.Register(() =>
        {
            logger.LogInformation($"{connection.User.Name} has disconnected (detected via cancellation token)");
            chatState.SignalClientDisconnect(connection);
        });

        try
        {
            var error = await errorTcs.Task;
            logger.LogError($"Connection error for user: {connection.User.Name}: {error.Message}");
        }
        catch (Exception e)
        {
            logger.LogInformation($"Client disconnected: {connection.User.Name}");
        }
        finally
        {
            chatState.RemoveUser(connection);
        }
    }

    public override async Task<Close> BroadcastMessage(Message message, ServerCallContext context)
    {
        await chatState.BroadcastMessage(message);
        return new Close();
    }
}