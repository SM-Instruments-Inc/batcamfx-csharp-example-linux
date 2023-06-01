using Newtonsoft.Json;

namespace batcamfx_csharp_example_linux.WebSocket;

/// <summary>
/// This is a container that contains Camera Information of BATCAM FX
/// </summary>
public struct FxWebSocketOptions {
    internal readonly string CameraIp;
    internal readonly string Username;
    internal readonly string Password;

    public FxWebSocketOptions(string cameraIp, string username, string password) {
        CameraIp = cameraIp;
        Username = username;
        Password = password;
    }
}

public struct FxWebSocketResponse {
    [JsonProperty("event_id")]
    public int EventId { get; set; }

    [JsonProperty("gain")]
    public int Gain { get; set; }

    [JsonProperty("bf")]
    public float[] BeamformingData { get; set; }
}

public struct FxWebSocketMessage {
    [JsonProperty("type")]
    public string Type { get; }
    
    [JsonProperty("id")]
    public int Id { get; }

    public FxWebSocketMessage(string type, int id) {
        Type = type;
        Id = id;
    }
}
