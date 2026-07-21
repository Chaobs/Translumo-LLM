using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Translumo.Utils;

namespace Translumo.Translation.Llm
{
    /// <summary>
    /// Container holding all configured LLM translator profiles and the name of the
    /// currently active one. Persisted as plain JSON to
    /// %AppData%/Translumo-LLM/llm_profiles.json so the user can configure several LLM
    /// backends (DeepSeek, GPT, Claude, ...) and switch between them freely.
    /// </summary>
    public class LlmProfiles : BindableBase
    {
        private const string FILE_NAME = "llm_profiles.json";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private ObservableCollection<LlmConfiguration> _profiles = new ObservableCollection<LlmConfiguration>();
        private string _activeProfileName;

        public ObservableCollection<LlmConfiguration> Profiles
        {
            get => _profiles;
            set => SetProperty(ref _profiles, value ?? new ObservableCollection<LlmConfiguration>());
        }

        public string ActiveProfileName
        {
            get => _activeProfileName;
            set
            {
                var changed = !Equals(_activeProfileName, value);
                SetProperty(ref _activeProfileName, value);
                if (changed)
                {
                    OnPropertyChanged(nameof(Active));
                }
            }
        }

        /// <summary>The profile currently used for translation. Null only if no profiles exist.</summary>
        [JsonIgnore]
        public LlmConfiguration Active
        {
            get
            {
                var byName = _profiles.FirstOrDefault(p => p.Name == _activeProfileName);
                if (byName != null)
                {
                    return byName;
                }

                return _profiles.FirstOrDefault();
            }
        }

        public LlmProfiles()
        {
            Load();

            if (_profiles.Count == 0)
            {
                _profiles.Add(new LlmConfiguration { Name = "Default" });
            }

            if (string.IsNullOrEmpty(_activeProfileName) || !_profiles.Any(p => p.Name == _activeProfileName))
            {
                _activeProfileName = _profiles.FirstOrDefault()?.Name;
            }
        }

        /// <summary>Persist the profiles to the application data directory.</summary>
        public void Save()
        {
            try
            {
                var path = Path.Combine(GetDirectory(), FILE_NAME);
                var json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(path, json);
            }
            catch
            {
                // Persisting profiles must never crash the app; ignore I/O errors.
            }
        }

        private void Load()
        {
            try
            {
                var path = Path.Combine(GetDirectory(), FILE_NAME);
                if (!File.Exists(path))
                {
                    return;
                }

                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                // Deserialize into a plain DTO (NOT this type) to avoid re-entering the
                // parameterless constructor, which calls Load() — that previously caused
                // infinite recursion and a StackOverflow on every startup after the first save.
                var loaded = JsonSerializer.Deserialize<LlmProfilesData>(json, JsonOptions);
                if (loaded == null)
                {
                    return;
                }

                _profiles.Clear();
                if (loaded.Profiles != null)
                {
                    foreach (var profile in loaded.Profiles)
                    {
                        _profiles.Add(profile);
                    }
                }

                ActiveProfileName = loaded.ActiveProfileName;
            }
            catch
            {
                // Corrupt file: fall back to defaults.
            }
        }

        /// <summary>
        /// Serialization-only DTO. Kept separate from <see cref="LlmProfiles"/> so that
        /// <see cref="JsonSerializer"/> does not invoke this class's constructor (which calls
        /// <see cref="Load"/>) during deserialization.
        /// </summary>
        private sealed class LlmProfilesData
        {
            public List<LlmConfiguration> Profiles { get; set; } = new List<LlmConfiguration>();

            public string ActiveProfileName { get; set; }
        }

        private static string GetDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appName = AppDomain.CurrentDomain.FriendlyName.Split('.').First();
            var dir = Path.Combine(appData, appName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return dir;
        }
    }
}
