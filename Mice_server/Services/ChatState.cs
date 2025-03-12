using System;
using System.Collections.Concurrent;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Mice_server.Services;

public class ChatState(ILogger<ChatState> logger)
{
    public ConcurrentDictionary<Connect, IServerStreamWriter<Message>> Users =
        new ConcurrentDictionary<Connect, IServerStreamWriter<Message>>();

    public bool AddUser(Connect connection, IServerStreamWriter<Message> stream)
    {
        bool result = Users.TryAdd(connection, stream);
        if (result)
        {
            logger.LogInformation($"User {connection.User.Name} added");
        }
        else
        {
            logger.LogError($"User {connection.User.Name} failed to be added");
        }
        return result;
    }

    public bool RemoveUser(Connect connection)
    {
        bool result = Users.TryRemove(connection, out _);
        if (result)
        {
            logger.LogInformation($"User {connection.User.Name} removed");
        }
        else
        {
            logger.LogError($"User {connection.User.Name} failed to be removed");
        }
        return result;
    }

    public async Task BroadcastMessage(Message message)
    {
        logger.LogInformation($"Broadcasting message: {message}");
        int successCount = 0;
        var deadConnections = new List<Connect>();

        foreach (var (connection, stream) in Users)
        {
            try
            {
                await stream.WriteAsync(message);
                successCount++;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error while broadcasting message: {ex.Message}");
                deadConnections.Add(connection);
            }
        }

        foreach (var connection in deadConnections)
        {
            RemoveUser(connection);
        }
    }

    public Message CreateMessage(string content)
    {
        return new Message
        {
            UserId = "system",
            Content = content,
            SentDate = Timestamp.FromDateTime(DateTime.Now)
        };
    }
}
