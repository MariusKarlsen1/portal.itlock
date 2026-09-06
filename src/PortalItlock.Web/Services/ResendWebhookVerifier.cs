using System.Security.Cryptography;
using System.Text;

namespace PortalItlock.Web.Services;

public static class ResendWebhookVerifier
{
    public static bool ErGyldig(IHeaderDictionary headers, string body, string secret)
    {
        var id = headers["svix-id"].ToString();
        var timestamp = headers["svix-timestamp"].ToString();
        var signaturHeader = headers["svix-signature"].ToString();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signaturHeader))
        {
            return false;
        }

        if (long.TryParse(timestamp, out var ts))
        {
            var alderSekunder = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts);
            if (alderSekunder > 300)
            {
                return false;
            }
        }

        var hemmeligUtenPrefix = secret.StartsWith("whsec_", StringComparison.Ordinal) ? secret["whsec_".Length..] : secret;

        byte[] secretBytes;
        byte[] forventet;
        try
        {
            secretBytes = Convert.FromBase64String(hemmeligUtenPrefix);
            var signertInnhold = $"{id}.{timestamp}.{body}";
            using var hmac = new HMACSHA256(secretBytes);
            forventet = hmac.ComputeHash(Encoding.UTF8.GetBytes(signertInnhold));
        }
        catch (FormatException)
        {
            return false;
        }

        foreach (var del in signaturHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var kommaIndeks = del.IndexOf(',');
            var base64Signatur = kommaIndeks >= 0 ? del[(kommaIndeks + 1)..] : del;

            byte[] mottatt;
            try
            {
                mottatt = Convert.FromBase64String(base64Signatur);
            }
            catch (FormatException)
            {
                continue;
            }

            if (CryptographicOperations.FixedTimeEquals(mottatt, forventet))
            {
                return true;
            }
        }

        return false;
    }
}
