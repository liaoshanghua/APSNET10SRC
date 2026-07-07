using System.Security.Cryptography;
using System.Text;

namespace EasyManufacture.Application.Security;

public static class Md5Helper
{
    public static string Encrypt(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }

    public static bool IsMd5Hash(string? input) =>
        !string.IsNullOrEmpty(input) && System.Text.RegularExpressions.Regex.IsMatch(input, @"^[a-fA-F0-9]{32}$");
}
