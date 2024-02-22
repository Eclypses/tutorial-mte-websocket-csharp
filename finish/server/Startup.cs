/*
THIS SOFTWARE MAY NOT BE USED FOR PRODUCTION. Otherwise,
The MIT License (MIT)

Copyright (c) Eclypses, Inc.

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

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
/* Step 2 */
using Eclypses.MTE;

namespace server
{
    public class Startup
    {
        /* Step 3 */
        private static MteBase _mteBase = new MteBase();

        //---------------------------------------------------
        // Comment out to use MKE or MTE FLEN instead of MTE Core
        //---------------------------------------------------
        private static MteDec _decoder = new MteDec();
        private static MteEnc _encoder = new MteEnc();

        //---------------------------------------------------
        // Uncomment to use MKE instead of MTE Core
        //---------------------------------------------------
        // private static MteMkeDec _decoder = new MteMkeDec();
        // private static MteMkeEnc _encoder = new MteMkeEnc();

        //---------------------------------------------------
        // Uncomment to use MTE FLEN
        //---------------------------------------------------
        // private const int _fixedLength = 8;
        // private static MteFlenEnc _encoder = new MteFlenEnc(_fixedLength);
        // private static MteDec _decoder = new MteDec();

        private static MteStatus _encoderStatus = MteStatus.mte_status_success;
        private static MteStatus _decoderStatus = MteStatus.mte_status_success;

        /* Step 4 */
        // Set default entropy, nonce and identifier
        // Providing Entropy in this fashion is insecure. This is for demonstration purposes only and should never be done in practice.
        // If this is a trial version of the MTE, entropy must be blank
        private static string _encoderEntropy = "";
        private static string _decoderEntropy = "";
        // OPTIONAL!!! adding 1 to Decoder nonce so return value changes -- same nonce can be used for Encoder and Decoder
        // Client side values will be switched so they match up Encoder to Decoder and vice versa
        private static ulong _encoderNonce = 0;
        private static ulong _decoderNonce = 1;
        private static string _identifier = "demo";
        private static string _licenseCompany = "Eclypses Inc.";
        private static string _licenseKey = "Eclypses123";
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public void ConfigureServices(IServiceCollection services) { }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime hostApplicationLifetime)
        {
            // This tutorial uses WebSockets for communication.
            // It should be noted that the MTE can be used with any type of communication. (WEBSOCKETS are not required!)

            app.UseWebSockets();

            app.Use(async(context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    // Display what version of MTE we are using
                    string mteVersion = _mteBase.GetVersion();
                    Console.WriteLine($"Using MTE Version {mteVersion}");

                    /* Step 5 */
                    // Check mte license
                    // Initialize MTE license. If a license code is not required (e.g., trial mode), this can be skipped.
                    if (!_mteBase.InitLicense(_licenseCompany, _licenseKey))
                    {
                        _encoderStatus = MteStatus.mte_status_license_error;
                        throw new ApplicationException($"License error ({_mteBase.GetStatusName(_encoderStatus)}): {_mteBase.GetStatusDescription(_encoderStatus)}.  Press any key to end.");
                    }

                    /* Step 6 */
                    // Instantiate the Encoder
                    InstantiateEncoder();

                    /* Step 6 CONTINUED... */
                    // Instantiate the Decoder
                    InstantiateDecoder();

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
            MteStatus mteStatus = MteStatus.mte_status_success;
            byte[] encodedMessage;
            string decodedMessage;
            byte[] buffer = new byte[1024 * 4];

            Console.WriteLine("WebSocket client connected");

            // Infinite loop until done sending messages
            while (true)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                // Copy buffer into a new buffer with correct length
                byte[] newBuffer = new byte[result.Count];
                Array.Copy(buffer, 0, newBuffer, 0, result.Count);

                /* Step 7 */
                // Decode received bytes and check to ensure successful result
                decodedMessage = _decoder.DecodeStr(newBuffer, out mteStatus);

                if (mteStatus != MteStatus.mte_status_success)
                {
                    Console.WriteLine($"Error decoding: Status: {_mteBase.GetStatusName(mteStatus)} / {_mteBase.GetStatusDescription(mteStatus)}");
                    _tokenSource.Cancel();
                    break;
                }

                // For demonstration purposes only to show packets
                Console.WriteLine($"\nReceived MTE packet: {Convert.ToBase64String(newBuffer)}");
                Console.WriteLine($"\nDecoded data: {decodedMessage}");

                /* Step 7 CONTINUED... */
                // Encode returning text and ensuring successful
                encodedMessage = _encoder.Encode(decodedMessage, out mteStatus);

                if (mteStatus != MteStatus.mte_status_success)
                {
                    Console.WriteLine($"Error encoding: Status: {_mteBase.GetStatusName(mteStatus)} / {_mteBase.GetStatusDescription(mteStatus)}");
                    _tokenSource.Cancel();
                    break;
                }

                // For demonstration purposes only to show packets
                Console.WriteLine($"\nMTE packet Sent: {Convert.ToBase64String(encodedMessage)}");

                // Send the encoded message over WebSocket
                await webSocket.SendAsync(new ArraySegment<byte>(encodedMessage), WebSocketMessageType.Binary, true, token);

                if (decodedMessage.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                {
                    _tokenSource.Cancel();
                    break;
                }
            }

            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }

            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing WebSocket", token);

            Console.WriteLine("Client disconnected");
        }

        public static void InstantiateEncoder()
        {
            try
            {
                // Check how long entropy we need and set default
                int entropyMinBytes = _mteBase.GetDrbgsEntropyMinBytes(_encoder.GetDrbg());
                _encoderEntropy = (entropyMinBytes > 0) ? new String('0', entropyMinBytes) : _encoderEntropy;

                _encoder.SetEntropy(Encoding.UTF8.GetBytes(_encoderEntropy));
                _encoder.SetNonce(_encoderNonce);

                _encoderStatus = _encoder.Instantiate(_identifier);

                if (_encoderStatus != MteStatus.mte_status_success)
                {
                    throw new ApplicationException($"Failed to initialize the MTE Encoder engine. Status: {_mteBase.GetStatusName(_encoderStatus)} / {_mteBase.GetStatusDescription(_encoderStatus)}");
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to initialize the MTE Encoder engine. Status: {_mteBase.GetStatusName(_encoderStatus)} / {_mteBase.GetStatusDescription(_encoderStatus)}", ex);
            }
        }

        public static void InstantiateDecoder()
        {
            try
            {
                // Check how long entropy we need and set default
                int entropyMinBytes = _mteBase.GetDrbgsEntropyMinBytes(_decoder.GetDrbg());
                _decoderEntropy = (entropyMinBytes > 0) ? new String('0', entropyMinBytes) : _decoderEntropy;

                _decoder.SetEntropy(Encoding.UTF8.GetBytes(_decoderEntropy));
                _decoder.SetNonce(_decoderNonce);

                _decoderStatus = _decoder.Instantiate(_identifier);

                if (_decoderStatus != MteStatus.mte_status_success)
                {
                    throw new ApplicationException($"Failed to initialize the MTE Decoder engine.  Status: {_mteBase.GetStatusName(_decoderStatus)} / {_mteBase.GetStatusDescription(_decoderStatus)}");
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to initialize the MTE Decoder engine.  Status: {_mteBase.GetStatusName(_decoderStatus)} / {_mteBase.GetStatusDescription(_decoderStatus)}", ex);
            }
        }
    }
}