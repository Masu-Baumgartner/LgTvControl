using System.Text.Json.Serialization;

namespace LgTvControl.Websocket.Payloads.ServerBound;

public partial class PairingRequest
{
    [JsonPropertyName("forcePairing")] public bool ForcePairing { get; set; }

    [JsonPropertyName("pairingType")] public string PairingType { get; set; }

    [JsonPropertyName("manifest")] public Manifest Manifest { get; set; }

    [JsonPropertyName("client-key")] public string ClientKey { get; set; }
}

public partial class Manifest
{
    [JsonPropertyName("manifestVersion")] public long ManifestVersion { get; set; }

    [JsonPropertyName("appVersion")] public string AppVersion { get; set; }

    [JsonPropertyName("signed")] public Signed Signed { get; set; }

    [JsonPropertyName("permissions")] public string[] Permissions { get; set; }

    [JsonPropertyName("signatures")] public Signature[] Signatures { get; set; }
}

public partial class Signature
{
    [JsonPropertyName("signatureVersion")] public long SignatureVersion { get; set; }

    [JsonPropertyName("signature")] public string SignatureSignature { get; set; }
}

public partial class Signed
{
    [JsonPropertyName("created")]
    public string Created { get; set; }

    [JsonPropertyName("appId")] public string AppId { get; set; }

    [JsonPropertyName("vendorId")] public string VendorId { get; set; }

    [JsonPropertyName("localizedAppNames")] public LocalizedAppNames LocalizedAppNames { get; set; }

    [JsonPropertyName("localizedVendorNames")] public LocalizedVendorNames LocalizedVendorNames { get; set; }

    [JsonPropertyName("permissions")] public string[] Permissions { get; set; }

    [JsonPropertyName("serial")] public string Serial { get; set; }
}

public partial class LocalizedAppNames
{
    [JsonPropertyName("")]
    public string Empty { get; set; }

    [JsonPropertyName("ko-KR")]
    public string KoKr { get; set; }

    [JsonPropertyName("zxx-XX")]
    public string ZxxXx { get; set; }
}

public partial class LocalizedVendorNames
{
    [JsonPropertyName("")]
    public string Empty { get; set; }
}