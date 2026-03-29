using System.Security.Cryptography;
using System.Text;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class HashHelper
{
    public static string GetSha256HexString(string text)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash);
    }
}
