using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

public class WebSocketHandler
{
    private static ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _projectConnections = new ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>>();

    public async Task HandleWebSocketAsync(HttpContext context, string projectId, string userId)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var connections = _projectConnections.GetOrAdd(projectId, new ConcurrentDictionary<string, WebSocket>());
            connections[userId] = socket;

            // Gửi danh sách các userId đã kết nối trước đó
            var connectedUserIds = connections.Keys.Where(id => id != userId).ToList();
            if (connectedUserIds.Count > 0)
            {
                // var message = $"ConnectedUsers:{string.Join(",", connectedUserIds)}";
                var messageObj = new 
                {
                  type = "userList",
                  list = connectedUserIds.ToArray()
                };
                var message = JsonSerializer.Serialize(messageObj);
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }

            await ReceiveMessageAsync(socket, async (result, buffer) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await SendMessageToOthersAsync(projectId, userId, message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    connections.TryRemove(userId, out _); // Xoá userID khỏi danh sách kết nối
                    await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
            });
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }

    private async Task ReceiveMessageAsync(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
    {
        var buffer = new byte[1024 * 4];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            handleMessage(result, buffer);
        }
    }

    private async Task SendMessageToOthersAsync(string projectId, string senderId, string message)
    {
        if (_projectConnections.TryGetValue(projectId, out var connections))
        {
            foreach (var connection in connections)
            {
                if (connection.Key != senderId)
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    await connection.Value.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
