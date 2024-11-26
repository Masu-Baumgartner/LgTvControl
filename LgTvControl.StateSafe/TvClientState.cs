namespace LgTvControl.StateSafe;

public enum TvClientState
{
    Offline,
    Reset,
    Connecting,
    Connected,
    RequestPairing,
    WaitForPairingResponse,
    Pairing,
    AcceptingPair,
    ConnectingTelnet,
    TelnetConnected,
    SendingAutoAccept,
    SentAutoAccept,
    WaitForPairingAccept,
    Paired,
    WaitingForChannelResponse,
    ChannelIntentSuccess,
    WaitingForVolumeIntent,
    VolumeIntentSuccess,
    Ready,
    WaitingForKeepAlive
}