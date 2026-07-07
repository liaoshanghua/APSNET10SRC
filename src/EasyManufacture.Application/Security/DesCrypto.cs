using System.Security.Cryptography;
using System.Text;

namespace EasyManufacture.Application.Security;

/// <summary>与旧版 StringHelper DES 加解密兼容（密钥 )( *&amp;!@#$）。</summary>
public static class DesCrypto
{
    private const string Password = ")(*&!@#$";

    public static string Encrypt(string str)
    {
        using var des = DES.Create();
        var input = Encoding.UTF8.GetBytes(str);
        des.Key = Encoding.ASCII.GetBytes(Password);
        des.IV = Encoding.ASCII.GetBytes(Password);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(input, 0, input.Length);
            cs.FlushFinalBlock();
        }

        var ret = new StringBuilder();
        foreach (var b in ms.ToArray())
        {
            ret.AppendFormat("{0:X2}", b);
        }

        return ret.ToString();
    }

    public static string Decrypt(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        try
        {
            using var des = DES.Create();
            var input = new byte[str.Length / 2];
            for (var x = 0; x < str.Length / 2; x++)
            {
                input[x] = Convert.ToByte(str.Substring(x * 2, 2), 16);
            }

            des.Key = Encoding.ASCII.GetBytes(Password);
            des.IV = Encoding.ASCII.GetBytes(Password);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(input, 0, input.Length);
                cs.FlushFinalBlock();
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch
        {
            return str;
        }
    }
}
