namespace LgTvControl.IpControl;

public class IpControlClient
{
    public bool IsConnected => ControlConnection.IsConnected;
    public event Func<string, Task>? OnError;
    public event Func<int, Task>? OnVolume;
    public event Func<string, Task>? OnApp;
    public event Func<string, Task>? OnChannel;
    public event Func<bool, Task>? OnMute;

    private readonly IpControlConnection ControlConnection;

    public IpControlClient(string ip, int port, string key)
    {
        ControlConnection = new(ip, port, key);

        ControlConnection.OnMessageReceived += HandleMessage;
    }

    public async Task Connect()
        => await ControlConnection.Connect();

    public async Task LaunchApp(string id)
    {
        if (!IsConnected)
            await Connect();
        
        await ControlConnection.SendMessage($"APP_LAUNCH {id}");
    }

    public async Task GetMute()
    {
        if (!IsConnected)
            await Connect();
        
        await ControlConnection.SendMessage("MUTE_STATE");
    }

    public async Task GetChannel()
    {
        if (!IsConnected)
            await Connect();
        
        await ControlConnection.SendMessage("CURRENT_CH");
    }

    public async Task GetApp()
    {
        if (!IsConnected)
            await Connect();
        
        await ControlConnection.SendMessage("CURRENT_APP");
    }

    public async Task GetVolume()
    {
        if (!IsConnected)
            await Connect();
        
        await ControlConnection.SendMessage("CURRENT_VOL");
    }

    public async Task GetMacAddress(string device)
    {
        if (!IsConnected)
            await Connect();
        
        await ControlConnection.SendMessage($"GET_MACADDRESS {device}");
    }

    public async Task SendKey(IpControlKey key)
    {
        var id = key switch
        {
            IpControlKey.Exit => "exit",
            IpControlKey.ChannelUp => "channelup",
            IpControlKey.ChannelDown => "channeldown",
            IpControlKey.VolumeUp => "volumeup",
            IpControlKey.VolumeDown => "volumedown",
            IpControlKey.ArrowRight => "arrowright",
            IpControlKey.ArrowLeft => "arrowleft",
            IpControlKey.VolumeMute => "volumemute",
            IpControlKey.DeviceInput => "deviceinput",
            IpControlKey.SleepReserve => "sleepreserve",
            IpControlKey.LiveTv => "livetv",
            IpControlKey.PreviousChannel => "previouschannel",
            IpControlKey.FavoriteChannel => "favoritechannel",
            IpControlKey.Teletext => "teletext",
            IpControlKey.TeletextOption => "teletextoption",
            IpControlKey.ReturnBack => "returnback",
            IpControlKey.AvMode => "avmode",
            IpControlKey.CaptionSubtitle => "captionsubtitle",
            IpControlKey.ArrowUp => "arrowup",
            IpControlKey.ArrowDown => "arrowdown",
            IpControlKey.MyApp => "myapp",
            IpControlKey.SettingMenu => "settingmenu",
            IpControlKey.Ok => "ok",
            IpControlKey.QuickMenu => "quickmenu",
            IpControlKey.VideoMode => "videomode",
            IpControlKey.AudioMode => "audiomode",
            IpControlKey.ChannelList => "channellist",
            IpControlKey.BlueButton => "bluebutton",
            IpControlKey.YellowButton => "yellowbutton",
            IpControlKey.GreenButton => "greenbutton",
            IpControlKey.RedButton => "redbutton",
            IpControlKey.AspectRatio => "aspectratio",
            IpControlKey.AudioDescription => "audiodescription",
            IpControlKey.ProgrammOrder => "programmorder",
            IpControlKey.UserGuide => "userguide",
            IpControlKey.SmartHome => "smarthome",
            IpControlKey.SimpleLink => "simplelink",
            IpControlKey.FastForward => "fastforward",
            IpControlKey.Rewind => "rewind",
            IpControlKey.ProgrammingInfo => "programminfo",
            IpControlKey.ProgramGuide => "programguide",
            IpControlKey.Play => "play",
            IpControlKey.SlowPlay => "slowplay",
            IpControlKey.SoccerScreen => "soccerscreen",
            IpControlKey.Reord => "reord",
            IpControlKey.Autoconfig => "autoconfig",
            IpControlKey.App => "app",
            IpControlKey.ScreenBright => "screenbright",
            IpControlKey.Number0 => "number0",
            IpControlKey.Number1 => "number1",
            IpControlKey.Number2 => "number2",
            IpControlKey.Number3 => "number3",
            IpControlKey.Number4 => "number4",
            IpControlKey.Number5 => "number5",
            IpControlKey.Number6 => "number6",
            IpControlKey.Number7 => "number7",
            IpControlKey.Number8 => "number8",
            IpControlKey.Number9 => "number9",
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };

        if (!IsConnected)
            await Connect();

        await ControlConnection.SendMessage($"KEY_ACTION {id}");
    }

    public async Task SwitchInput(IpControlInput controlInput)
    {
        var id = controlInput switch
        {
            IpControlInput.Atv => "atv",
            IpControlInput.Avav1 => "avav1",
            IpControlInput.Catv => "catv",
            IpControlInput.Component1 => "component1",
            IpControlInput.Dtv => "dtv",
            IpControlInput.Hdmi1 => "hdmi1",
            IpControlInput.Hdmi2 => "hdmi2",
            IpControlInput.Hdmi3 => "hdmi3"
        };

        if (!IsConnected)
            await Connect();

        await ControlConnection.SendMessage($"INPUT_SELECT {id}");
    }

    public async Task Disconnect()
        => await ControlConnection.Disconnect();

    private async Task HandleMessage(string message)
    {
        if (message.StartsWith("VOL:"))
        {
            var volume = int.Parse(message.Replace("VOL:", "").Trim());

            if (OnVolume != null)
                await OnVolume.Invoke(volume);
        }
        else if (message.StartsWith("APP:"))
        {
            var appId = message.Replace("APP:", "").Trim();

            if (OnApp != null)
                await OnApp.Invoke(appId);
        }
        else if (message.StartsWith("CH:"))
        {
            var channel = message.Replace("CH:", "").Trim();

            if (OnChannel != null)
                await OnChannel.Invoke(channel);
        }
        else if (message.StartsWith("MUTE:"))
        {
            var mute = message.Replace("MUTE:", "").Trim();

            if (OnMute != null)
                await OnMute.Invoke(mute.Equals("on", StringComparison.InvariantCultureIgnoreCase));
        }
        else
        {
            if (OnError != null)
                await OnError.Invoke(message);
        }
    }
}