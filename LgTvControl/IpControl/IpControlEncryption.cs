using System.Security.Cryptography;
using System.Text;

namespace LgTvControl.IpControl;

public class IpControlEncryption
{
    private readonly int KeyIterations = 16384;
    private readonly int OutputLenght = 16;
    private readonly int IvLenght = 16;
    private readonly int MessageBlockSize = 16;
    
    private readonly byte[] Salt =
    [
        0x63, 0x61, 0xb8, 0x0e, 0x9b, 0xdc, 0xa6, 0x63, 0x8d, 0x07, 0x20, 0xf2,
        0xcc, 0x56, 0x8f, 0xb9
    ];
    
    private byte[] KeyBytes;
    
    public IpControlEncryption(string key)
    {
        KeyBytes = GenerateEncryptionKey(key);
    }
    
    private byte[] GenerateEncryptionKey(string keyStr)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(keyStr),
            Salt,
            KeyIterations,
            HashAlgorithmName.SHA256,
            OutputLenght
        );
    }

    private byte[] GenerateRandomIv()
    {
        var iv = new byte[IvLenght];
        var random = new Random();
        random.NextBytes(iv);

        return iv;
    }

    private string PadMessage(string message)
    {
        var sb = new StringBuilder(message);

        if (message.Length % MessageBlockSize == 0)
            sb.Append(' ');

        var remainder = sb.Length % MessageBlockSize;

        if (remainder == 0)
            return sb.ToString();

        var padding = MessageBlockSize - remainder;
        var paddingChar = (char)padding;
        
        for (int i = 0; i < padding; i++)
            sb.Append(paddingChar);

        return sb.ToString();
    }

    public byte[] Encode(string message)
    {
        var iv = GenerateRandomIv();
        var paddedMessage = PadMessage(message + "\r");
        var paddedMessageBytes = Encoding.UTF8.GetBytes(paddedMessage);

        using var ecbCipher = Aes.Create();
        ecbCipher.Key = KeyBytes;
        ecbCipher.Mode = CipherMode.ECB;
        ecbCipher.Padding = PaddingMode.None;

        using var encryptor = ecbCipher.CreateEncryptor();
        var ivEnc = encryptor.TransformFinalBlock(iv, 0, iv.Length);

        using var cbcCipher = Aes.Create();
        cbcCipher.Key = KeyBytes;
        cbcCipher.IV = iv;
        cbcCipher.Mode = CipherMode.CBC;
        cbcCipher.Padding = PaddingMode.None;

        using var cbcEncryptor = cbcCipher.CreateEncryptor();
        var dataEnc = cbcEncryptor.TransformFinalBlock(paddedMessageBytes, 0, paddedMessageBytes.Length);
        return ConcatenateArrays(ivEnc, dataEnc);
    }

    public string Decode(byte[] buffer)
    {
        using var ecbCipher = Aes.Create();
        ecbCipher.Key = KeyBytes;
        ecbCipher.Mode = CipherMode.ECB;
        ecbCipher.Padding = PaddingMode.None;

        using var decryptor = ecbCipher.CreateDecryptor();
        var iv = decryptor.TransformFinalBlock(buffer, 0, OutputLenght);

        using var cbcCipher = Aes.Create();
        cbcCipher.Key = KeyBytes;
        cbcCipher.IV = iv;
        cbcCipher.Mode = CipherMode.CBC;
        cbcCipher.Padding = PaddingMode.None;

        using var cbcDecryptor = cbcCipher.CreateDecryptor();
        var decrypted = cbcDecryptor.TransformFinalBlock(buffer, OutputLenght, buffer.Length - OutputLenght);
        
        return StripEnd(Encoding.UTF8.GetString(decrypted));
    }

    private string StripEnd(string message)
    {
        return message.Substring(0, message.IndexOf("\n", StringComparison.InvariantCulture));
    }
    
    private byte[] ConcatenateArrays(byte[] first, byte[] second)
    {
        var result = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
        return result;
    }
}