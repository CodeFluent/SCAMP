using Microsoft.Framework.ConfigurationModel;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.KeyVault;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace KeyVaultRepositories
{
    public class KeyVaultScampClient
    {
        private static IConfiguration _configuration;
        private readonly KeyVaultClient _keyVaultClient;
        private readonly string _keyVaultUrl;

#if !DEBUG
        private static X509Certificate2 clientAssertionCertPfx;
#endif

        public KeyVaultScampClient(IConfiguration configuration)
        {
            _configuration = configuration;

            _keyVaultUrl = _configuration["KeyVault:Url"];

            if (_keyVaultUrl == null)
            {
                _keyVaultClient = null;
            }
            else
            {
#if DEBUG
                //This should be used only during development
                _keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessToken));
#else
                //This use certificate service in Azure platform and doesn't need shared keys
                _keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessToken));
                clientAssertionCertPfx = FindCertificateByThumbprint(_configuration["KeyVault:AuthCertThumbprintSetting"]);
#endif
            }
        }

        public KeyVaultClient GetClient()
        {
            return _keyVaultClient;
        }
        public string GetKeyVaultUrl()
        {
            return _keyVaultUrl;
        }

#if !DEBUG
        public  string GetAccessTokenCert(string authority, string resource, string scope)
        {
            var clientId = _configuration["KeyVault:AuthClientId"];

            var context = new AuthenticationContext(authority, null);

            var assertionCert = new ClientAssertionCertificate(clientId, clientAssertionCertPfx);

            var result = context.AcquireToken(resource, assertionCert);

            return result.AccessToken;
        }
#endif

        public static async Task<string> GetAccessToken(string authority, string resource, string scope)
        {
            var clientId = _configuration["KeyVault:AuthClientId"];
            var clientSecret = _configuration["KeyVault:AuthClientSecret"];

            var clientCredential = new ClientCredential(clientId, clientSecret);
            var context = new AuthenticationContext(authority, null);
            var result = await context.AcquireTokenAsync(resource, clientCredential);

            return result.AccessToken;
        }

        public static X509Certificate2 FindCertificateByThumbprint(string findValue)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection col = store.Certificates.Find(X509FindType.FindByThumbprint, findValue, false); // Don't validate certs, since the test root isn't installed.
                if (col == null || col.Count == 0)
                    return null;
                return col[0];
            }
            finally
            {
                store.Close();
            }
        }
    }
}
