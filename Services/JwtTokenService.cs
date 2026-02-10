using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace SRM.Services
{
    public class JwtTokenService
    {
        private readonly string _secretKey;
        private readonly int _expirationHours;

        public JwtTokenService(string secretKey, int expirationHours = 24)
        {
            _secretKey = secretKey;
            _expirationHours = expirationHours;
        }

        /// <summary>
        /// Generate JWT Token (Works with .NET 4.5+)
        /// </summary>
        public string GenerateToken(int agentId, string pno, string email, string privilege, bool isAdmin)
        {
            try
            {
                // Header
                var header = new { alg = "HS256", typ = "JWT" };

                // Payload (claims)
                var payload = new
                {
                    iss = "SRM",
                    aud = "SRMUsers",
                    sub = pno,
                    iat = ToUnixTimeSeconds(DateTime.UtcNow),
                    exp = ToUnixTimeSeconds(DateTime.UtcNow.AddHours(_expirationHours)),
                    agentId = agentId,
                    email = email ?? "",
                    privilege = privilege ?? "View",
                    role = isAdmin ? "Admin" : "User"
                };

                // Encode header and payload
                string headerJson = JsonConvert.SerializeObject(header);
                string payloadJson = JsonConvert.SerializeObject(payload);

                string headerEncoded = Base64UrlEncode(headerJson);
                string payloadEncoded = Base64UrlEncode(payloadJson);

                // Create signature
                string headerPayload = $"{headerEncoded}.{payloadEncoded}";
                byte[] key = Encoding.UTF8.GetBytes(_secretKey);

                using (var hmac = new HMACSHA256(key))
                {
                    byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(headerPayload));
                    string signature = Base64UrlEncode(hash);

                    return $"{headerPayload}.{signature}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Token generation error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validate JWT Token
        /// </summary>
        public bool IsTokenValid(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return false;

                var parts = token.Split('.');
                if (parts.Length != 3)
                    return false;

                // Verify signature
                string headerPayload = $"{parts[0]}.{parts[1]}";
                byte[] key = Encoding.UTF8.GetBytes(_secretKey);

                using (var hmac = new HMACSHA256(key))
                {
                    byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(headerPayload));
                    string signature = Base64UrlEncode(hash);

                    if (signature != parts[2])
                    {
                        System.Diagnostics.Debug.WriteLine("Token signature invalid");
                        return false;
                    }
                }

                // Decode and check expiration
                string payloadJson = Base64UrlDecode(parts[1]);
                dynamic payload = JsonConvert.DeserializeObject(payloadJson);

                long exp = payload.exp;
                long now = ToUnixTimeSeconds(DateTime.UtcNow);

                if (now > exp)
                {
                    System.Diagnostics.Debug.WriteLine("Token expired");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Token validation error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get claim from token
        /// </summary>
        public T GetClaim<T>(string token, string claimName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return default(T);

                var parts = token.Split('.');
                if (parts.Length < 2)
                    return default(T);

                string payloadJson = Base64UrlDecode(parts[1]);
                dynamic payload = JsonConvert.DeserializeObject(payloadJson);

                if (payload[claimName] == null)
                    return default(T);

                return (T)Convert.ChangeType(payload[claimName], typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        // Helper: Base64 URL encode
        private string Base64UrlEncode(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            return Base64UrlEncode(inputBytes);
        }

        private string Base64UrlEncode(byte[] input)
        {
            string output = Convert.ToBase64String(input);
            output = output.Split('=')[0];
            output = output.Replace('+', '-');
            output = output.Replace('/', '_');
            return output;
        }

        // Helper: Base64 URL decode
        private string Base64UrlDecode(string input)
        {
            string output = input;
            output = output.Replace('-', '+');
            output = output.Replace('_', '/');

            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }

            try
            {
                byte[] convert = Convert.FromBase64String(output);
                return Encoding.UTF8.GetString(convert);
            }
            catch
            {
                return null;
            }
        }

        // Helper: Convert DateTime to Unix timestamp (works with .NET 4.5)
        private long ToUnixTimeSeconds(DateTime dateTime)
        {
            TimeSpan timeSpan = dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)timeSpan.TotalSeconds;
        }
    }
}