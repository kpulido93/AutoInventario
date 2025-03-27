using Microsoft.AspNetCore.Mvc;
using Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Webhook;
using Services;

[ApiController]
[Route("webhooks")]
public class WebhookController : ControllerBase
{
    private const string VerifyToken = "YOUR_VERIFY_TOKEN";
    private readonly EventStore _eventStore;

    public WebhookController(EventStore eventStore)
    {
        _eventStore = eventStore;
    }

    [HttpGet]
    public IActionResult Verify([FromQuery(Name = "hub.mode")] string hub_mode,
                                [FromQuery(Name = "hub.challenge")] string hub_challenge,
                                [FromQuery(Name = "hub.verify_token")] string hub_verify_token)
    {
        if (hub_mode == "subscribe" && hub_verify_token == VerifyToken)
        {
            return Ok(hub_challenge);  // Devuelve el desafío si es válido
        }
        return Forbid();  // Devuelve 403 si el token no coincide
    }

    //[HttpPost]

    //public async Task<IActionResult> HandleEvent()
    //{
    //    try
    //    {
    //        // Leer el cuerpo de la solicitud como texto
    //        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
    //        {
    //            var requestBody = await reader.ReadToEndAsync();
    //            Console.WriteLine($"JSON recibido: {requestBody}");
    //            _eventStore.AddEvent(requestBody);

    //            // Aquí puedes deserializar el JSON si es necesario
    //            var webhookEvent = JsonConvert.DeserializeObject<InventoryEvent>(requestBody);
    //            if (webhookEvent == null)
    //            {
    //                Console.WriteLine("El JSON recibido no coincide con el modelo.");
    //                return BadRequest("Formato inválido.");
    //            }

    //            // Procesar el webhookEvent
    //            return Ok("Evento procesado correctamente.");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error procesando el JSON: {ex.Message}");
    //        return StatusCode(500, "Error interno del servidor.");
    //    }
    //}

    [HttpPost]
    public async Task<IActionResult> HandleEvent([FromBody] WebhookEvent webhookEvent)
    {
        if (webhookEvent == null || string.IsNullOrEmpty(webhookEvent.Data) ||
            string.IsNullOrEmpty(webhookEvent.Key) || string.IsNullOrEmpty(webhookEvent.IV))
        {
            return BadRequest("Formato inválido.");
        }

        try
        {
            string decryptedData = DecryptData(webhookEvent.Data, webhookEvent.Key, webhookEvent.IV);
            Console.WriteLine($"Datos desencriptados: {decryptedData}");

            // Guarda el evento localmente
            _eventStore.AddEvent(decryptedData);

            // Llama a la Lambda
            var lambdaInvoker = new LambdaInvoker();
            await lambdaInvoker.InvokeLambdaAsync(new
            {
                ClientId = webhookEvent.ClientID,
                Data = decryptedData
            });

            return Ok("Evento procesado correctamente y enviado a Lambda.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al procesar el evento: {ex.Message}");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    private string LoadPrivateKey()
    {
        using (Stream stream = typeof(Program).Assembly.GetManifestResourceStream("Webhook-Inventario.Resources.private.key"))
        using (StreamReader reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

    private string DecryptData(string encryptedData, string encryptedKey, string ivBase64)
    {
        string privateKey = LoadPrivateKey();

        byte[] encryptedAesKey = Convert.FromBase64String(encryptedKey);
        byte[] iv = Convert.FromBase64String(ivBase64);
        byte[] cipherText = Convert.FromBase64String(encryptedData);

        // Desencriptar clave AES con RSA
        byte[] aesKey;
        using (RSA rsa = RSA.Create())
        {
            rsa.ImportFromPem(privateKey.ToCharArray());
            aesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);
        }

        // Desencriptar datos con AES
        using (Aes aes = Aes.Create())
        {
            aes.Key = aesKey;
            aes.IV = iv;
            using (MemoryStream ms = new MemoryStream(cipherText))
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (StreamReader reader = new StreamReader(cs))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
