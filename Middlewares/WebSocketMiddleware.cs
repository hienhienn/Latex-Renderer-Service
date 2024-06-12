// using System;
// using System.Net.WebSockets;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Http;

// public class WebSocketMiddleware
// {
//     private readonly RequestDelegate _next;

//     public WebSocketMiddleware(RequestDelegate next)
//     {
//         _next = next;
//     }

//     public async Task InvokeAsync(
//         HttpContext context,
//         WebSocketConnectionManager webSocketConnectionManager
//     )
//     {
//         if (context.Request.Path.StartsWithSegments("/ws", out var remainingPath))
//         {
//             if (remainingPath.HasValue)
//             {
//                 var segments = remainingPath.Value.Trim('/').Split('/');
//                 if (segments.Length == 1 && Guid.TryParse(segments[0], out Guid projectId))
//                 {
//                     if (context.WebSockets.IsWebSocketRequest)
//                     {
//                         WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
//                         var socketId = Guid.NewGuid();
//                         webSocketConnectionManager.AddSocket(projectId, socketId, webSocket);

//                         await webSocketConnectionManager.ReceiveAsync(
//                             projectId,
//                             socketId,
//                             webSocket,
//                             async (socket, message) =>
//                             {
//                                 await webSocketConnectionManager.BroadcastMessageAsync(
//                                     projectId,
//                                     message,
//                                     socketId
//                                 );
//                             }
//                         );
//                     }
//                     else
//                     {
//                         context.Response.StatusCode = 400;
//                     }
//                 }
//                 else
//                 {
//                     context.Response.StatusCode = 400;
//                 }
//             }
//             else
//             {
//                 context.Response.StatusCode = 400;
//             }
//         }
//         else
//         {
//             await _next(context);
//         }
//     }
// }
