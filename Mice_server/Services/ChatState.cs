using System;
using System.Collections.Concurrent;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Mice_server.Services;

public class ChatState(ILogger<ChatState> logger)
{
    public ConcurrentDictionary<Connect, IServerStreamWriter<Message>> Users =
        new ConcurrentDictionary<Connect, IServerStreamWriter<Message>>();

    private ConcurrentDictionary<Connect, TaskCompletionSource<Exception>> _errors =
        new ConcurrentDictionary<Connect, TaskCompletionSource<Exception>>();

    public TaskCompletionSource<Exception> AddUser(Connect connection, IServerStreamWriter<Message> stream)
    {
        Users.TryAdd(connection, stream);
        
        var tcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        _errors.TryAdd(connection, tcs);
        
        logger.LogInformation($"User {connection} connected. Total users: {Users.Count}");
        return tcs;
    }

    public void RemoveUser(Connect connection)
    {
        Users.TryRemove(connection, out _);
        _errors.TryRemove(connection, out _);
        logger.LogInformation($"User {connection} disconnected. Total users: {Users.Count}");
    }

    public void SignalClientDisconnect(Connect connection)
    {
        if (_errors.TryRemove(connection, out var tcs))
        {
            logger.LogInformation($"User {connection} disconnected. Total users: {Users.Count}");
            tcs.TrySetResult(new Exception("Client disconnected"));
        }
    }

    public async Task BroadcastMessage(Message message)
    {
        logger.LogInformation($"Broadcasting message: {message}");

        foreach (var (conn, stream) in Users)
        {
            try
            {
                await stream.WriteAsync(message);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error while broadcasting message: {e.Message}");

                if (_errors.TryGetValue(conn, out var tcs))
                {
                    tcs.TrySetException(e);
                }
            }
        }
    }

    public Message CreateMessage(string content)
    {
        return new Message
        {
            UserId = "system",
            Content = content,
            SentDate = Timestamp.FromDateTime(DateTime.UtcNow)
        };
    }
}