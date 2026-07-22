using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Translumo.Infrastructure.Encryption;
using Translumo.Utils;
using Translumo.Utils.Extensions;

namespace Translumo.Configuration
{
    public class  ConfigurationStorage
    {
        // The encryption password is no longer hardcoded. On first launch a cryptographically random
        // password is generated and protected with the Windows DPAPI (CurrentUser scope), then persisted
        // inside the config folder as "encryption.key". Subsequent launches read and unprotect it.
        // This is a deliberate, breaking change from the previous weak constant (test phase).
        private static readonly string EncryptionPassword = LoadOrCreateEncryptionPassword();

        // Compile-time app-specific entropy. Not a secret by itself, but adds a layer on top of DPAPI
        // so the protected blob is bound to this application.
        private static readonly byte[] KeyEntropy =
        {
            0x54, 0x72, 0x61, 0x6E, 0x73, 0x6C, 0x75, 0x6D,
            0x6F, 0x2D, 0x6C, 0x4C, 0x4D, 0x2D, 0x6B, 0x65,
            0x79, 0x2D, 0x76, 0x31, 0x00, 0x00, 0x00, 0x00
        };

        private static string LoadOrCreateEncryptionPassword()
        {
            const string keyFileName = "encryption.key";
            try
            {
                var keyPath = Path.Combine(AppPaths.GetConfigDirectory(), keyFileName);
                if (File.Exists(keyPath))
                {
                    var protectedBytes = File.ReadAllBytes(keyPath);
                    var passwordBytes = ProtectedData.Unprotect(protectedBytes, KeyEntropy, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(passwordBytes);
                }

                // First launch: generate a random password and persist it protected by DPAPI.
                var password = GenerateRandomPassword();
                var plainBytes = Encoding.UTF8.GetBytes(password);
                var protectedKeyBytes = ProtectedData.Protect(plainBytes, KeyEntropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(keyPath, protectedKeyBytes);
                return password;
            }
            catch (Exception)
            {
                // DPAPI unavailable (e.g. non-Windows host): fall back to a deterministic,
                // per-machine/per-user, per-app derivation so the app can still run.
                return DeriveFallbackPassword();
            }
        }

        private static string GenerateRandomPassword()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return Convert.ToBase64String(bytes);
        }

        private static string DeriveFallbackPassword()
        {
            var seed = $"{AppPaths.GetConfigDirectory()}|{Environment.MachineName}|{Environment.UserName}|Translumo-LLM";
            using (var sha = SHA256.Create())
            {
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(seed)));
            }
        }

        private const string CONFIGURATION_FILE = "settings";

        private readonly IServiceProvider _serviceProvider;
        private readonly IEncryptionService _encryptionService;
        private readonly IList<Type> _configurationTypes;
        private readonly ILogger _logger;

        public ConfigurationStorage(IServiceProvider serviceProvider, IEncryptionService encryptionService, ILogger<ConfigurationStorage> logger)
        {
            this._logger = logger;
            _serviceProvider = serviceProvider;
            _encryptionService = encryptionService;
            _configurationTypes = new List<Type>();
        }

        public void RegisterConfiguration<TConfiguration>()
            where TConfiguration: class
        {
            if (!_configurationTypes.Contains(typeof(TConfiguration)))
            {
                _configurationTypes.Add(typeof(TConfiguration));
            }
        }

        public void LoadConfiguration()
        {
            List<object> configurations = _configurationTypes.Select(type => _serviceProvider.GetService(type)).ToList();
            var serializer = new XmlSerializer(typeof(List<object>), _configurationTypes.ToArray());
            List<object> savedConfigs;
            try
            {
                var confPath = GetConfigurationPath();
                if (!File.Exists(confPath))
                {
                    // One-time migration: import from the legacy AppData location if present.
                    var legacyPath = Path.Combine(AppPaths.GetLegacyConfigDirectory(), CONFIGURATION_FILE);
                    if (File.Exists(legacyPath))
                    {
                        confPath = legacyPath;
                    }
                }

                _logger.LogTrace($"Loading configuration from '{confPath}'");
                using (FileStream fs = new FileStream(confPath, FileMode.Open))
                {
                    var decryptedConfig = _encryptionService.Decrypt(fs, EncryptionPassword);
                    using (var textReader = new StringReader(decryptedConfig))
                    {
                        savedConfigs = serializer.Deserialize(textReader) as List<object>;
                    }
                }

                foreach (var configuration in configurations)
                {
                    var savedConfig = savedConfigs.FirstOrDefault(dc => dc.GetType() == configuration.GetType());
                    if (savedConfig == null)
                    {
                        continue;
                    }

                    savedConfig.MapTo(configuration);
                }
                _logger.LogTrace("Configuration loaded");
            }
            catch (FileNotFoundException)
            {
                //IGNORE
            }
            catch (Exception)
            {
                _logger.LogError($"Unexpected error loading configuration");
            }
        }


        public void SaveConfiguration()
        {
            List<object> configurations = _configurationTypes.Select(type => _serviceProvider.GetService(type)).ToList();

            var serializer = new XmlSerializer(typeof(List<object>), _configurationTypes.ToArray());
            var savePath = GetConfigurationPath();
            _logger.LogTrace($"Saving configuration to '{savePath}'");
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    serializer.Serialize(ms, configurations);
                    ms.Position = 0;

                    byte[] encryptedConfig = _encryptionService.Encrypt(ms, EncryptionPassword);
                    File.WriteAllBytes(savePath, encryptedConfig);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save configuration to '{savePath}'");
            }
        }


        private string GetConfigurationPath()
        {
            return Path.Combine(AppPaths.GetConfigDirectory(), CONFIGURATION_FILE);
        }
    }
}
