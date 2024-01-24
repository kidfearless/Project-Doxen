using System.Security.Cryptography;

namespace ProjectDoxen.Manager;

public class CryptoService
{
    private readonly string _key;
    private readonly byte[] _salt;
    private const int Iterations = 10000; // Or whatever you're comfortable with

    public CryptoService()
    {
        var type = GetType();
        _key = type.GUID.ToString()!;
        // get the .dll file path
        var path = type!.Assembly!.Location!;
        _salt = File.ReadAllBytes(path);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentNullException(plainText);


        using var aesAlg = Aes.Create();
        var keyGenerator = new Rfc2898DeriveBytes(_key, _salt, Iterations, HashAlgorithmName.SHA256);

        aesAlg.Key = keyGenerator.GetBytes(aesAlg.KeySize / 8);
        aesAlg.IV = keyGenerator.GetBytes(aesAlg.BlockSize / 8);

        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            using var swEncrypt = new StreamWriter(csEncrypt);
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentNullException(cipherText);


        using var aesAlg = Aes.Create();
        var keyGenerator = new Rfc2898DeriveBytes(_key, _salt, Iterations, HashAlgorithmName.SHA256);

        byte[] bytes = Convert.FromBase64String(cipherText);

        aesAlg.Key = keyGenerator.GetBytes(aesAlg.KeySize / 8);
        aesAlg.IV = keyGenerator.GetBytes(aesAlg.BlockSize / 8);

        var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
        using var msDecrypt = new MemoryStream(bytes);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }
}
