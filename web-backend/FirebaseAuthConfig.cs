using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography.X509Certificates;

namespace web_backend
{
    public class FirebaseAuthConfig
    {
        public static List<X509SecurityKey> GetIssuerSigningKeys()
        {
            HttpClient client = new HttpClient();
            string jsonResult = client.GetStringAsync("https://www.googleapis.com/robot/v1/metadata/x509/securetoken@system.gserviceaccount.com").Result;

            //Extract X509SecurityKeys from JSON result
            List<X509SecurityKey> x509IssuerSigningKeys = JObject.Parse(jsonResult)
                .Children()
                .Cast<JProperty>()
                .Select(i => BuildSecurityKey(i.Value.ToString())).ToList();

            return x509IssuerSigningKeys;
        }

        public static X509SecurityKey BuildSecurityKey(string certificate)
        {
            //Removing "-----BEGIN CERTIFICATE-----" and "-----END CERTIFICATE-----" lines
            var lines = certificate.Split('\n');
            var selectedLines = lines.Skip(1).Take(lines.Length - 3);
            var key = string.Join(Environment.NewLine, selectedLines);

            return new X509SecurityKey(new X509Certificate2(Convert.FromBase64String(key)));
        }
    }
}
