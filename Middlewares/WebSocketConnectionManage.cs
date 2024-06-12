// using System.Collections.Concurrent;
// using System.Net.WebSockets;
// using System.Text;

// public class WebSocketConnectionManager
// {
//     private ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, WebSocket>> _projectSockets = new ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, WebSocket>>();

//     public void AddSocket(Guid projectId, Guid socketId, WebSocket socket)
//     {
//         var sockets = _projectSockets.GetOrAdd(projectId, new ConcurrentDictionary<Guid, WebSocket>());
//         sockets.TryAdd(socketId, socket);
//     }

//     public async Task ReceiveAsync(Guid projectId, Guid socketId, WebSocket webSocket, Func<WebSocket, string, Task> handleMessage)
//     {
//         var buffer = new byte[1024 * 4];
//         WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
//         while (!result.CloseStatus.HasValue)
//         {
//             var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
//             await handleMessage(webSocket, receivedMessage);
//             result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
//         }
//         if (_projectSockets.TryGetValue(projectId, out var sockets))
//         {
//             sockets.TryRemove(socketId, out _);
//         }
//         await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
//     }

//     public async Task BroadcastMessageAsync(Guid projectId, string message, Guid? excludeSocketId = null)
//     {
//         var serverMessage = Encoding.UTF8.GetBytes(message);
//         if (_projectSockets.TryGetValue(projectId, out var sockets))
//         {
//             foreach (var kvp in sockets)
//             {
//                 if (kvp.Key != excludeSocketId && kvp.Value.State == WebSocketState.Open)
//                 {
//                     await kvp.Value.SendAsync(new ArraySegment<byte>(serverMessage, 0, serverMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
//                 }
//             }
//         }
//     }
// }