
using System.Security.Cryptography;
using System.Text;

namespace TradingSystem.Dashboard.Api.Authentication;

public static class DashboardPasswordVerifier
{
    private const string Algorithm = "PBKDF2-SHA256";
    private const int MinimumIterations = 100_000;

    public static bool Verify(string password, string encodedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(encodedHash))
            return false;

        var parts = encodedHash.Split('$', StringSplitOptions.None);

        if (parts.Length != 4 || !parts[0].Equals(Algorithm, StringComparison.Ordinal))
            return false;

        if (!int.TryParse(parts[1], out var iterations) || iterations < MinimumIterations)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);

            if (salt.Length < 16 || expected.Length < 32)
                return false;

            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool FixedTimeEquals(string left, string right)
    {
        var leftHash = SHA256.HashData(Encoding.UTF8.GetBytes(left));
        var rightHash = SHA256.HashData(Encoding.UTF8.GetBytes(right));

        return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
    }
}
