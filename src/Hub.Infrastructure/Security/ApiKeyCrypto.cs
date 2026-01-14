using System.Security.Cryptography;
using System.Text;

namespace Hub.Infrastructure.Security;

public sealed class ApiKeyCrypto
{
    private readonly SecurityOptions _opt;

    public ApiKeyCrypto(SecurityOptions opt) => _opt = opt;

    public (string Plaintext, string Prefix, string HashB64, string SaltB64, int Iterations) Create()
    {
        var TokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Base64UrlEncode(TokenBytes);
        var plaintext = $"hub_{token}";
        var prefix = token[..Math.Min(_opt.PrefixLength, token.Length)];

        var salt = RandomNumberGenerator.GetBytes(_opt.SaltBytes);
        var saltB64 = Convert.ToBase64String(salt);

        var iter = _opt.DefaultIterations;
        var hash = DeriveHash(plaintext, salt, iter);
        var hashB64 = Convert.ToBase64String(hash);

        return (plaintext,prefix,hashB64,saltB64,iter);
    }

    public bool Verify(string plaintext, string storedHashB64, string storedSaltB64, int iterations)
    {
        var salt = Convert.FromBase64String(storedSaltB64);
        var expected = Convert.FromBase64String(storedHashB64);

        var actual = DeriveHash(plaintext, salt, iterations);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

     private byte[] DeriveHash(string plaintext, byte[] salt, int iterations)
    {
        var input = Encoding.UTF8.GetBytes($"{plaintext}:{_opt.ApiKeyPepper}");
        return Rfc2898DeriveBytes.Pbkdf2(input, salt, iterations, HashAlgorithmName.SHA256, _opt.HashBytes);
    }

        private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
