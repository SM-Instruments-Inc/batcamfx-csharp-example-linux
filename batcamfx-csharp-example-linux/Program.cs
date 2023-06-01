// See https://aka.ms/new-console-template for more information

using batcamfx_csharp_example_linux.Interpolator;
using batcamfx_csharp_example_linux.WebSocket;
using Newtonsoft.Json;
using OpenCvSharp;
using System.Diagnostics;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace batcamfx_csharp_example_linux;

public class Program {
    
    // MARK: - WebSocket Variables -------------------------------------------------------
    private const string CameraIp = "10.1.5.33";
    private const string Username = "admin";
    private const string Password = "admin";

    private static WebSocketSharp.WebSocket _webSocket;

    private static bool _shouldCloseWebsocket = false;
    // MARK: -----------------------------------------------------------------------------
    
    // MARK: - Interpolation Variables ---------------------------------------------------
    private static BeamformingInterpolator _interpolator;
    private readonly static OpenCvSharp.Size _destinationSize = new(1600, 1200);
    // MARK: -----------------------------------------------------------------------------
    
    public static void Main() {
        var options = new FxWebSocketOptions(CameraIp, Username, Password);
        _webSocket = new WebSocketSharp.WebSocket($"ws://{options.CameraIp}:80/ws", "subscribe");
        _interpolator = new BeamformingInterpolator(_destinationSize);

        PrepareWebSocket(options);
        Console.ReadLine();
    }
    
    // MARK: - WebSocket Functions -------------------------------------------------------
        /// <summary>
        /// Preparing websocket for receiving BF Data from BATCAM FX.
        ///
        /// This function sets credential for WebSocket, and connect delegate functions to MainWindow.
        /// </summary>
        /// <param name="options">
        /// Used for setting credential for WebSocket.
        /// Refer to credential information from provided document.
        /// </param>
        static void PrepareWebSocket(FxWebSocketOptions options) {
            _webSocket.SetCredentials(options.Username, options.Password, true);
            try {
                _webSocket.OnOpen += OnWebSocketOpen;
                _webSocket.OnMessage += OnWebSocketMessage;
                _webSocket.OnError += OnWebSocketError;
                _webSocket.OnClose += OnWebSocketClose;
                _webSocket.EnableRedirection = true;
                _webSocket.Connect();
                if (_webSocket.IsAlive) {
                    Debug.WriteLine("[WebSocket/Initialization] Socket alive");
                    SendSubscribeMessage();
                }
            } catch (Exception exception) {
                Debug.WriteLine($"[WebSocket/Initialization] Error: {exception.Message}: {exception.StackTrace ?? string.Empty}");
            }
        }

        /// <summary>
        /// Sends a subscribe message to BATCAM FX for subscribing BF Data events.
        ///
        /// In this example, example only uses id 0 event.
        /// It can be subscribed more sent by EventTrigger, if you needed.  
        /// </summary>
        static void SendSubscribeMessage() {
            var message = new FxWebSocketMessage("subscribe", 0);
            var json = JsonConvert.SerializeObject(message);
            
            Debug.WriteLine($"[WebSocket] Sending Message... {message}");
            _webSocket.Send(json);
            Debug.WriteLine("[WebSocket] Message Sent");
        }

        static void OnWebSocketOpen(object? sender, EventArgs e) => Debug.WriteLine("[Websocket] Opened");

        /// <summary>
        /// Handle data from WebSocket for creating overlay image.
        ///
        /// In this function, creates color matrix using OpenCV in BeamformingInterpolator class.
        /// See BeamformingInterpolator.cs for more information.
        /// Due to ToWritableBitmap function costs bit high,
        /// if you want to handle multiple sockets with program,
        /// you have to optimize some functions.
        /// </summary>
        /// <param name="sender">The caller who called this delegate function.</param>
        /// <param name="e">The response from WebSocket</param>
        async static void OnWebSocketMessage(object? sender, MessageEventArgs e) {
            if (e.IsPing) {
                Debug.WriteLine("[WebSocket/Message] Ping");
                return;
            }
            try {
                // Converts json response into Object<FxWebSocketResponse>
                var message = JsonConvert.DeserializeObject<FxWebSocketResponse>(e.Data);
                Mat? matrix = null;
                await Task.Run(() => {
                        // Creates matrix using BeamformingInterpolator. 
                        matrix = _interpolator.GenerateMatrix(message.BeamformingData, message.Gain);
                    }
                );
                
            } catch (Exception exception) {
                Debug.WriteLine($"[WebSocket/Message] Error: {exception.Message}: {exception.StackTrace ?? string.Empty}");
            }
        }

        static void OnWebSocketError(object? sender, ErrorEventArgs e) {
            Debug.WriteLine($"[WebSocket] Error: ({e.Message ?? string.Empty}) {e.Exception?.Message ?? string.Empty}: {e.Exception?.StackTrace ?? string.Empty}");
            throw new InvalidOperationException($"WebSocket got error {e.Message ?? "Unknown error"}");
        }

        /// <summary>
        /// Handle event on websocket close.
        ///
        /// Websocket can disconnect anytime for various reason.
        /// For preventing unhandled disconnection from BATCAM FX,
        /// the function calls connect websocket again from here,
        /// except when requested to close gracefully.
        /// 
        /// </summary>
        /// <param name="sender">The caller who called this delegate function.</param>
        /// <param name="closeEventArgs">The created data when WebSocket closing.</param>
        static void OnWebSocketClose(object? sender, CloseEventArgs closeEventArgs) {
            if (_shouldCloseWebsocket) {
                Debug.WriteLine("[Websocket] ShouldShutdownWebSocket is currently ON. App won't reconnect websocket");
                return;
            }

            Debug.WriteLine($"[WebSocket] Closed");

            _webSocket.Connect();
            if (_webSocket.IsAlive) {
                Debug.WriteLine("[WebSocket/Reconnect] Socket alive");
                SendSubscribeMessage();
            }
        }

}
