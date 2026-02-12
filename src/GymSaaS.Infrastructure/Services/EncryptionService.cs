using GymSaaS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace GymSaaS.Infrastructure.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;

        public EncryptionService(IConfiguration configuration)
        {
            var secretKey = configuration["Security:EncryptionKey"] ?? throw new Exception("Encryption Key not found");

            // AES requiere llaves de 16, 24 o 32 bytes. 
            // Normalizamos la llave a 32 bytes (AES-256) usando SHA256 para garantizar el tamaño correcto.
            using (var sha256 = SHA256.Create())
            {
                _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretKey));
            }
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.GenerateIV();
                var iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                using (var ms = new MemoryStream())
                {
                    // Guardamos el IV al principio del stream para que el Decrypt sepa cuál es
                    ms.Write(iv, 0, iv.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);

                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    var iv = new byte[16];
                    var cipher = new byte[fullCipher.Length - iv.Length];

                    // Extraemos el IV y el contenido cifrado del array combinado
                    Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                    Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                    using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
                    using (var ms = new MemoryStream(cipher))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                // En caso de error de desencriptación (llave cambiada), devolvemos el original o vacío
                return string.Empty;
            }
        }
    }
}