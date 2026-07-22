using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Translumo.Infrastructure.Encryption;
using Xunit;

namespace Translumo.Tests.Infrastructure
{
    public class AesEncryptionServiceTests
    {
        [Fact]
        public void Encrypt_then_decrypt_round_trips()
        {
            var svc = new AesEncryptionService();
            var original = "my-secret-api-key-123";
            byte[] encrypted;

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(original)))
            {
                encrypted = svc.Encrypt(ms, "password");
            }

            Assert.NotEqual(Encoding.UTF8.GetBytes(original), encrypted);

            string decrypted;
            using (var ms = new MemoryStream(encrypted))
            {
                decrypted = svc.Decrypt(ms, "password");
            }

            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void Decrypt_with_wrong_password_throws()
        {
            var svc = new AesEncryptionService();
            byte[] encrypted;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("secret")))
            {
                encrypted = svc.Encrypt(ms, "right");
            }

            using var msDecrypt = new MemoryStream(encrypted);
            Assert.Throws<CryptographicException>(() => svc.Decrypt(msDecrypt, "wrong"));
        }

        [Fact]
        public void Empty_payload_round_trips()
        {
            var svc = new AesEncryptionService();
            byte[] encrypted;
            using (var ms = new MemoryStream(Array.Empty<byte>()))
            {
                encrypted = svc.Encrypt(ms, "p");
            }

            using var msDecrypt = new MemoryStream(encrypted);
            Assert.Equal(string.Empty, svc.Decrypt(msDecrypt, "p"));
        }
    }
}
