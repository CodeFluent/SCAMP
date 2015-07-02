using System;
using Microsoft.Azure.KeyVault;
using System.Threading.Tasks;

namespace KeyVaultRepositories.Implementation
{
    public class KeyRepository : IKeyRepository
    {
        private readonly KeyVaultScampClient _keyVaultClient;

        public KeyRepository(KeyVaultScampClient keyVaultClient)
        {
            _keyVaultClient = keyVaultClient;
        }
        private static string SecretKey(string resourceId, string key)
        {
            var secretKey = string.Format("{0}-{1}", resourceId, key);
            return secretKey;
        }
        public async Task<string> GetSecret(string resourceId, string key)
        {
            var secretKey = SecretKey(resourceId, key);
            var client = _keyVaultClient.GetClient();
            if (client != null)
            {
                var secret = await client.GetSecretAsync(_keyVaultClient.GetKeyVaultUrl(), secretKey);
                return secret.Value;
            }
            else
            {
                return "";
            }
        }

        public async Task UpsertSecret(string resourceId, string key, string value)
        {
            var secretKey = SecretKey(resourceId, key);
            var client = _keyVaultClient.GetClient();
            if (client != null)
            {
                await client.SetSecretAsync(_keyVaultClient.GetKeyVaultUrl(), secretKey, value);
            }
        }



        public async Task DeleteSecret(string resourceId, string key)
        {
            var secretKey = SecretKey(resourceId, key);
            var client = _keyVaultClient.GetClient();
            if (client != null)
            {
                await client.DeleteSecretAsync(_keyVaultClient.GetKeyVaultUrl(), secretKey);
            }
        }
    }
}