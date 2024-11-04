using System.Net;
using System.Net.Sockets;

namespace LgTvControl.Websocket.Extensions;

public static class TelevisionPowerExtensions
{
    public static async Task PowerOff(this WebSocketTvClient client)
    {
        await client.Request("ssap://system/turnOff");
    }
    
    private static async Task SendWakeOnLan(string ip, string mac)
    {
        // Split MAC address into array of bytes
        byte[] macBytes = mac.Split(':')
            .Select(s => Convert.ToByte(s, 16))
            .ToArray();

        // Create magic packet
        byte[] magicPacket = new byte[102];

        // Add 6 bytes of 0xFF to the beginning of the packet
        for (int i = 0; i < 6; i++)
        {
            magicPacket[i] = 0xFF;
        }

        // Repeat target device's MAC address 16 times
        for (int i = 6; i < 102; i += 6)
        {
            Array.Copy(macBytes, 0, magicPacket, i, 6);
        }

        // Create UDP client and send magic packet to port 9 on the target device's subnet
        using UdpClient udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        await udpClient.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Parse(ip), 9));
    }
}