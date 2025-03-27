using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Security;

namespace Autoinventario
{
    public static class EncryptionHelper
    {
        public static string EncryptData(string jsonData, SecureString privateKey)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = DeriveKey(privateKey);
                    aes.GenerateIV();

                    using (MemoryStream memoryStream = new MemoryStream())
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);
                        cryptoStream.Write(jsonBytes, 0, jsonBytes.Length);
                        cryptoStream.FlushFinalBlock();

                        byte[] encryptedData = memoryStream.ToArray();
                        return Convert.ToBase64String(aes.IV) + ":" + Convert.ToBase64String(encryptedData);
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error en cifrado: {ex.Message}";
            }
        }

        private static byte[] DeriveKey(SecureString secureString)
        {
            string unsecureKey = SecureStringHelper.ConvertToUnsecureString(secureString);
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(unsecureKey));
            }
        }
    }
}
