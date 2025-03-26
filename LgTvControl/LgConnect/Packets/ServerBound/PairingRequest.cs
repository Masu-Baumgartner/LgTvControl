using System.Text.Json.Serialization;

namespace LgTvControl.LgConnect.Packets.ServerBound;

public class PairingRequest
{
    [JsonPropertyName("forcePairing")] public bool ForcePairing { get; set; }

    [JsonPropertyName("pairingType")] public string PairingType { get; set; }

    [JsonPropertyName("manifest")] public ManifestData Manifest { get; set; }

    [JsonPropertyName("client-key")] public string ClientKey { get; set; }
    
    public class ManifestData
    {
        [JsonPropertyName("manifestVersion")] public long ManifestVersion { get; set; }

        [JsonPropertyName("appVersion")] public string AppVersion { get; set; }

        [JsonPropertyName("signed")] public SignedData Signed { get; set; }

        [JsonPropertyName("permissions")] public string[] Permissions { get; set; }

        [JsonPropertyName("signatures")] public SignatureData[] Signatures { get; set; }
    }

    public class SignatureData
    {
        [JsonPropertyName("signatureVersion")] public long SignatureVersion { get; set; }

        [JsonPropertyName("signature")] public string SignatureSignature { get; set; }
    }

    public class SignedData
    {
        [JsonPropertyName("created")]
        public string Created { get; set; }

        [JsonPropertyName("appId")] public string AppId { get; set; }

        [JsonPropertyName("vendorId")] public string VendorId { get; set; }

        [JsonPropertyName("localizedAppNames")] public LocalizedAppNamesData LocalizedAppNames { get; set; }

        [JsonPropertyName("localizedVendorNames")] public LocalizedVendorNamesData LocalizedVendorNames { get; set; }

        [JsonPropertyName("permissions")] public string[] Permissions { get; set; }

        [JsonPropertyName("serial")] public string Serial { get; set; }
    }

    public class LocalizedAppNamesData
    {
        [JsonPropertyName("")]
        public string Empty { get; set; }

        [JsonPropertyName("ko-KR")]
        public string KoKr { get; set; }

        [JsonPropertyName("zxx-XX")]
        public string ZxxXx { get; set; }
    }

    public class LocalizedVendorNamesData
    {
        [JsonPropertyName("")]
        public string Empty { get; set; }
    }
}