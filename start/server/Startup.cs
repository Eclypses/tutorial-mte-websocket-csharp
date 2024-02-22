using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace server
{
    public class Startup
    {
        // This tutorial uses WebSockets for communication.
        // It should be noted that the MTE can be used with any type of communication. (WEBSOCKETS are not required!)

        // Here is where you would want to define your default settings for the MTE
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public void ConfigureServices(IServiceCollection services) { }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime hostApplicationLifetime)
        {
            app.UseWebSockets();

            app.Use(async(context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    // Here is where you would want to gather settings for the MTE
                    // Check Mte license and run drbg self test

                    // Creation of WebSocket
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                    try
                    {
                        await OnWebSocketMessage(context, webSocket, _tokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Close server socket
                        await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing WebSocket", _tokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing WebSocket", _tokenSource.Token);
                        Console.WriteLine($"Something went wrong: {ex}");
                    }
                    finally
                    {
                        // Prompt to end
                        _tokenSource.Dispose();
                        Console.WriteLine("Server closed, please hit ENTER to end this...");
                        Console.ReadLine();

                        hostApplicationLifetime.StopApplication();
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });
        }

        private async Task OnWebSocketMessage(HttpContext context, WebSocket webSocket, CancellationToken token)
        {
            byte[] encodedMessage;
            string decodedMessage;
            byte[] buffer = new byte[1024 * 4];

            Console.WriteLine("WebSocket client connected");

            // Infinite loop until done sending messages
            while (true)
            {
                // Receieve message from the client into the buffer
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                // Copy buffer into a new buffer with correct length
                byte[] newBuffer = new byte[result.Count];
                Array.Copy(buffer, 0, newBuffer, 0, result.Count);

                // MTE Decoding the bytes would go here instead of using the C# stdlib to decode the text
                decodedMessage = Encoding.UTF8.GetString(buffer);

                Console.WriteLine($"Decoded data: {decodedMessage}\n\n");

                // MTE Encoding the text would go here instead of using the C# stdlib to encode to bytes
                encodedMessage = Encoding.UTF8.GetBytes(decodedMessage);

                // Send the encoded message over WebSocket
                await webSocket.SendAsync(new ArraySegment<byte>(encodedMessage), WebSocketMessageType.Binary, endOfMessage : true, token);

                if (decodedMessage.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                {
                    _tokenSource.Cancel();
                }
            }

            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }

            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing WebSocket", token);

            Console.WriteLine("Client disconnected");
        }
    }
}