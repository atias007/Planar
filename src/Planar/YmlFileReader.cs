using Planar.Common.Exceptions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Planar
{
    public static class YmlFileReader
    {
        public const string EncryptPrefix = "encrypted:";
        private static readonly string CryptographyKey = Environment.GetEnvironmentVariable(Consts.CryptographyKeyVariableKey) ?? string.Empty;

        public static async Task<string> ReadTextAsync(string filename)
        {
            var text = await File.ReadAllTextAsync(filename);
            if (!text.StartsWith(EncryptPrefix)) { return text; }

            if (string.IsNullOrWhiteSpace(CryptographyKey))
            {
                throw new PlanarException(
                    $"""
                        Environment variable {Consts.CryptographyKeyVariableKey} not found.
                        You can create new key with cli command: service create-cryptography-key
                        Then set the key in environment variable (i.e. setx {Consts.CryptographyKeyVariableKey} <generated_key>)
                        """);
            }

            text = text[EncryptPrefix.Length..];
            var aes = new Aes256Cipher(CryptographyKey);
            var decrypted = aes.Decrypt(text);
            return decrypted;
        }

        public static async Task<Stream> ReadStreamAsync(string filename)
        {
            var text = await ReadTextAsync(filename);
            return GenerateStreamFromString(text);
        }

        private static MemoryStream GenerateStreamFromString(string value)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(value);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}