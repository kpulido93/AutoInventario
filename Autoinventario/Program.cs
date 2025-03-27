using Autoinventario;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

class Program
{
    static async Task Main()
    {
        // Obtener datos del sistema
        var systemInfo = new
        {
            workstation = new
            {
                name = SystemInfo.GetHostname(),
                org_serial_number = SystemInfo.GetSerialNumber(),
                computer_system = new
                {
                    model = SystemInfo.GetModel()
                },
                product = new
                {
                    product_type = new
                    {
                        id = 1
                    }
                },
                workstation_udf_fields = new
                {
                    udf_sline_3003 = SystemInfo.GetHostname(),
                    udf_sline_6908 = SystemInfo.GetResponsibleUser(),
                    udf_sline_6909 = SystemInfo.GetDeviceType(),
                    udf_sline_6910 = SystemInfo.GetOS(),
                    udf_sline_6911 = SystemInfo.GetAntivirus(),
                    udf_sline_6924 = SystemInfo.GetEncryptionStatus(),
                    udf_sline_6912 = SystemInfo.GetSerialNumber(),
                    udf_sline_6913 = SystemInfo.GetBrand(),
                    udf_sline_6915 = SystemInfo.GetProcessor(),
                    udf_sline_6916 = SystemInfo.GetStorage(),
                    udf_sline_6917 = SystemInfo.GetRAM(),
                    udf_sline_6920 = SystemInfo.GetDomain(),
                    udf_sline_6918 = SystemInfo.GetWindowsLicense(),
                    udf_sline_6919 = SystemInfo.GetOfficeLicense(),
                    udf_sline_6923 = "",
                    udf_sline_7801 = SystemInfo.GetRecoveryPassword(),
                    udf_sline_7802 = SystemInfo.GetCurrentDateTime(),
                    udf_sline_7803 = SystemInfo.GetPurchaseDate()
                }
            }
        };

        // Convertir a JSON
        string jsonData = JsonSerializer.Serialize(systemInfo, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(jsonData);
        string publicKey = LoadPublicKey();
        // Encriptar JSON
        (string encryptedJson, string encryptedKey, string iv) = EncryptData(jsonData, publicKey);

        Console.WriteLine($"Datos encriptados:\n{encryptedJson}");

        // Enviar datos al webhook
        await SendDataToWebhook("idcliente", encryptedJson, encryptedKey, iv);

        Console.WriteLine("JSON encriptado generado y enviado correctamente.");
    }

    static string LoadPublicKey()
    {
        using (Stream stream = typeof(Program).Assembly.GetManifestResourceStream("AutoInventario.Resources.public.key"))
        using (StreamReader reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

    public static (string EncryptedData, string EncryptedKey, string IV) EncryptData(string plainText, string publicKey)
    {
        using (Aes aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();

            byte[] encryptedData;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (StreamWriter writer = new StreamWriter(cs))
                {
                    writer.Write(plainText);
                }
                encryptedData = ms.ToArray();
            }

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportFromPem(publicKey.ToCharArray());
                byte[] encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);

                return (
                    Convert.ToBase64String(encryptedData),
                    Convert.ToBase64String(encryptedKey),
                    Convert.ToBase64String(aes.IV)
                );
            }
        }
    }

    static async Task SendDataToWebhook(string clientID, string encryptedData, string encryptedKey, string iv)
    {
        using (HttpClient client = new HttpClient())
        {
            var payload = new
            {
                clientID = clientID,
                data = encryptedData,
                key = encryptedKey,
                iv = iv
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            Console.WriteLine($"Enviando JSON: {jsonPayload}");

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            content.Headers.ContentType.CharSet = "utf-8";

            HttpResponseMessage response = await client.PostAsync("http://localhost:5000/webhooks", content);

            string responseText = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Respuesta del servidor: {response.StatusCode} - {responseText}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Datos enviados correctamente al webhook.");
            }
            else
            {
                Console.WriteLine($"Error al enviar los datos: {response.StatusCode} - {responseText}");
                Console.WriteLine(jsonPayload);
            }
        }
    }
}
