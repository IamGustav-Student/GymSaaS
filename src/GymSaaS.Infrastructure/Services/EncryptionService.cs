using System.Security.Cryptography;
using System.Text;
using GymSaaS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GymSaaS.Infrastructure.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly string _key;

        public EncryptionService(IConfiguration configuration)
        {
            // La clave debe ser de 32 caracteres para AES-256. 
            // Se lee de la configuración (appsettings o Variable de Entorno)
            _key = configuration["Security:EncryptionKey"]
                   ?? throw new ArgumentNullException("Security:EncryptionKey no está configurada.");
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            var iv = new byte[16];
            byte[] array;

            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_key);
                aes.GenerateIV();
                iv = aes.IV;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        array = ms.ToArray();
                    }
                }
            }

            // Concatenamos el IV al principio del texto cifrado para poder desencriptar luego
            var combinedIvCt = new byte[iv.Length + array.Length];
            Array.Copy(iv, 0, combinedIvCt, 0, iv.Length);
            Array.Copy(array, 0, combinedIvCt, iv.Length, array.Length);

            return Convert.ToBase64String(combinedIvCt);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);

                var iv = new byte[16];
                var cipher = new byte[fullCipher.Length - iv.Length];

                Array.Copy(fullCipher, iv, iv.Length);
                Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(_key);
                    aes.IV = iv;

                    var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (var ms = new MemoryStream(cipher))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Si falla (ej: dato viejo no encriptado), devolvemos vacío o el original según convenga
                return string.Empty;
            }
        }
    }
}