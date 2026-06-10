using System.Security.Cryptography;
using System.Text;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.Auth;

public sealed class TotpService : ITotpService
{
    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return Base32Encode(bytes);
    }

    public string BuildProvisioningUri(string issuer, string accountName, string secret, int digits, int stepSeconds)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        // Most authenticator apps don't decode %40 back to @, so preserve it literally.
        var encodedAccount = Uri.EscapeDataString(accountName).Replace("%40", "@");
        return $"otpauth://totp/{encodedIssuer}:{encodedAccount}?secret={secret}&issuer={encodedIssuer}&digits={digits}&period={stepSeconds}";
    }

    public bool ValidateCode(string secret, string code, DateTime utcNow, int digits, int stepSeconds, int allowedDriftWindows)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            return false;

        var normalized = code.Trim();
        if (normalized.Length != digits || !normalized.All(char.IsDigit))
            return false;

        var key = Base32Decode(secret);
        var nowCounter = (long)Math.Floor((utcNow - DateTime.UnixEpoch).TotalSeconds / stepSeconds);

        for (var offset = -allowedDriftWindows; offset <= allowedDriftWindows; offset++)
        {
            var counter = nowCounter + offset;
            var expected = GenerateCode(key, counter, digits);
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(normalized)))
                return true;
        }

        return false;
    }

    private static string GenerateCode(byte[] key, long counter, int digits)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binaryCode = ((hash[offset] & 0x7f) << 24)
                         | (hash[offset + 1] << 16)
                         | (hash[offset + 2] << 8)
                         | hash[offset + 3];
        var mod = (int)Math.Pow(10, digits);
        var code = binaryCode % mod;
        return code.ToString(new string('0', digits));
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((data.Length * 8 + 4) / 5);

        var bitBuffer = 0;
        var bitsInBuffer = 0;
        foreach (var b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitsInBuffer += 8;
            while (bitsInBuffer >= 5)
            {
                var index = (bitBuffer >> (bitsInBuffer - 5)) & 0x1f;
                output.Append(alphabet[index]);
                bitsInBuffer -= 5;
            }
        }

        if (bitsInBuffer > 0)
        {
            var index = (bitBuffer << (5 - bitsInBuffer)) & 0x1f;
            output.Append(alphabet[index]);
        }

        return output.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        var clean = input.Trim().TrimEnd('=').ToUpperInvariant();
        var bytes = new List<byte>(clean.Length * 5 / 8);

        var bitBuffer = 0;
        var bitsInBuffer = 0;
        foreach (var ch in clean)
        {
            var val = ch switch
            {
                >= 'A' and <= 'Z' => ch - 'A',
                >= '2' and <= '7' => ch - '2' + 26,
                _ => -1
            };

            if (val < 0)
                continue;

            bitBuffer = (bitBuffer << 5) | val;
            bitsInBuffer += 5;

            if (bitsInBuffer >= 8)
            {
                bytes.Add((byte)((bitBuffer >> (bitsInBuffer - 8)) & 0xff));
                bitsInBuffer -= 8;
            }
        }

        return bytes.ToArray();
    }
}
