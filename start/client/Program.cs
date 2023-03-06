using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // This tutorial uses WebSockets for communication.
            // It should be noted that the MTE can be used with any type of communication. (WEBSOCKETS are not required!)

            // Here is where you would want to gather settings for the MTE
            // Check Mte license and run drbg self test

            // Set default IP - but also prompt for IP in case user cannot use our default
            string ip = "localhost";

            Console.Write($"Please enter IP to use, default IP is {ip}: ");
            string newIp = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newIp))
            {
                ip = newIp;
            }

            // Set default port - but also prompt for port in case user cannot use our default
            int port = 27015;

            Console.Write($"Please enter port to use, default port is {port}: ");
            string newPort = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newPort))
            {
                while (!int.TryParse(newPort, out port))
                {
                    Console.WriteLine($"{newPort} is not a valid integer, please try again.");
                    newPort = Console.ReadLine();
                }
            }

            string uri = $"ws://{ip}:{port}/";

            // Creation of WebSocket
            ClientWebSocket webSocket = new ClientWebSocket();
            CancellationTokenSource tokenSource = new CancellationTokenSource();

            try
            {
                // Connect the WebSocket to the remote endpoint
                await webSocket.ConnectAsync(new Uri(uri), tokenSource.Token);
                Console.WriteLine($"WebSocket connected to {uri}");

                byte[] encodedMessage;
                string decodedMessage;
                var buffer = new byte[1024 * 4];

                Console.WriteLine("Connected to WebSocket server");

                // Loop through sending messages until quit is sent
                while (true)
                {
                    // Enter text to send to server
                    Console.Write("Please enter text to send (to end please type 'quit'): ");
                    string message = Console.ReadLine();

                    // MTE Encoding the text would go here instead of using the C# stdlib to encode to bytes
                    encodedMessage = Encoding.UTF8.GetBytes(message);

                    // Send the encoded message over WebSocket
                    await webSocket.SendAsync(new ArraySegment<byte>(encodedMessage), WebSocketMessageType.Binary, true, tokenSource.Token);

                    // Receieve message from the server into the buffer
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), tokenSource.Token);

                    byte[] newBuffer = new byte[result.Count];
                    Array.Copy(buffer, 0, newBuffer, 0, result.Count);

                    // MTE Decoding the bytes would go here instead of using the C# stdlib to decode into a string
                    decodedMessage = Encoding.UTF8.GetString(buffer);

                    Console.WriteLine($"Decoded data: {decodedMessage}\n\n");

                    if (message.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                    {
                        break;
                    }
                }

                // Close client socket and prompt to end
                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, webSocket.CloseStatusDescription, tokenSource.Token);
                Console.WriteLine("Client closed, please hit ENTER to end this...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, webSocket.CloseStatusDescription, tokenSource.Token);
                throw ex;
            }
        }
    }
}