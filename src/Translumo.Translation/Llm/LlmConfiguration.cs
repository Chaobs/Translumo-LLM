using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Translumo.Utils;

namespace Translumo.Translation.Llm
{
    /// <summary>
    /// Configuration for the LLM AI translation backend. Stored (encrypted) alongside the other
    /// application settings and editable from the Languages settings panel.
    /// </summary>
    public class LlmConfiguration : BindableBase
    {
        /// <summary>
        /// Default system prompt. Optimized for electronic games and films/TV series: it asks for
        /// accurate terminology, medium-appropriate tone, consistent proper nouns and no extra prose.
        /// The {SourceLanguage} / {TargetLanguage} placeholders are substituted at request time.
        /// </summary>
        private const string DEFAULT_SYSTEM_PROMPT =
@"You are a professional game and film/TV localization translator.
Translate the user's text from {SourceLanguage} into {TargetLanguage}.

# Core principles
1. Terminology localization: Use the official, widely-accepted localized terms for game/film UI, menus, items, skills, equipment, races, classes, achievements, and character/place names. Apply them consistently across the whole project.
2. Proper nouns: Keep character names, place names and titles consistent and recognizable. Use the official localized name when one exists; otherwise transliterate/translate in a way that matches the work's established tone. Never invent or arbitrarily change a named entity.
3. Tone & register: Match the medium and the speaker. For games keep UI/menu/quest lines concise and imperative, system messages clear and neutral, and NPC dialogue lively and in-character. For films/TV keep dialogue natural, expressive and lip-sync friendly while preserving subtext and emotion.
4. Cultural adaptation: Localize idioms, jokes, puns and cultural references so they land naturally for a {TargetLanguage} audience instead of translating literally. Preserve the intended humor, sarcasm or formality.
5. Character & scene fit: Reflect each character's personality, social status, age and relationships through word choice, speech style, honorifics and dialect where the target language supports it.
6. Fidelity: Preserve the original meaning, pacing, emotion and information completely. Do not add, omit, explain or comment. Do not translate brand names, engine/technical terms or untranslatable on-screen tags unless an established localized equivalent exists.
7. Format: Keep line breaks, numbering and on-screen layout markers exactly. Output ONLY the translated text — no commentary, no quotes, no ""Translation:"" prefix.

If the text is already in {TargetLanguage}, or is nonsensical/garbled OCR output, return it unchanged.";

        /// <summary>Display name of this LLM profile (e.g. "DeepSeek", "My GPT-4").</summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public LlmProvider Provider
        {
            get => _provider;
            set
            {
                if (_provider == value)
                {
                    return;
                }

                SetProperty(ref _provider, value);
                if (!_deserializing)
                {
                    ApplyProviderDefaults();
                }
            }
        }

        public string ApiKey
        {
            get => _apiKey;
            set => SetProperty(ref _apiKey, value);
        }

        /// <summary>Optional override of the provider endpoint. Falls back to the preset when empty.</summary>
        public string Endpoint
        {
            get => _endpoint;
            set => SetProperty(ref _endpoint, value);
        }

        /// <summary>Optional override of the model name. Falls back to the preset when empty.</summary>
        public string ModelName
        {
            get => _modelName;
            set => SetProperty(ref _modelName, value);
        }

        public string SystemPrompt
        {
            get => _systemPrompt;
            set => SetProperty(ref _systemPrompt, value);
        }

        public double Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        public int MaxTokens
        {
            get => _maxTokens;
            set => SetProperty(ref _maxTokens, value);
        }

        [JsonIgnore]
        public bool Enabled => Provider != LlmProvider.Custom ||
                               (!string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ModelName));

        /// <summary>Endpoint to use, preferring the user override over the preset default.</summary>
        [JsonIgnore]
        public string ResolvedEndpoint =>
            string.IsNullOrWhiteSpace(Endpoint) ? LlmProviderPresets.Presets[Provider].DefaultEndpoint : Endpoint;

        /// <summary>Model to use, preferring the user override over the preset default.</summary>
        [JsonIgnore]
        public string ResolvedModel =>
            string.IsNullOrWhiteSpace(ModelName) ? LlmProviderPresets.Presets[Provider].DefaultModel : ModelName;

        [JsonIgnore]
        public LlmApiStyle ApiStyle => LlmProviderPresets.Presets[Provider].ApiStyle;

        public LlmConfiguration()
        {
            Name = "Default";
            SystemPrompt = DEFAULT_SYSTEM_PROMPT;
            Temperature = 0.3;
            MaxTokens = 4096;
            ApplyProviderDefaults();
        }

        /// <summary>
        /// Seed the endpoint / model fields from the provider preset so the user only has to
        /// supply the API key for well-known providers. Custom requires all three fields.
        /// Skipped while deserializing so saved overrides are preserved intact.
        /// </summary>
        private void ApplyProviderDefaults()
        {
            var preset = LlmProviderPresets.Presets[_provider];
            if (_provider == LlmProvider.Custom)
            {
                Endpoint = string.Empty;
                ModelName = string.Empty;
            }
            else
            {
                Endpoint = preset.DefaultEndpoint;
                ModelName = preset.DefaultModel;
            }
        }

        [OnDeserializing]
        private void OnDeserializing() => _deserializing = true;

        [OnDeserialized]
        private void OnDeserialized() => _deserializing = false;

        [JsonIgnore]
        private bool _deserializing;

        private string _name = "Default";
        private LlmProvider _provider = LlmProvider.DeepSeek;
        private string _apiKey;
        private string _endpoint;
        private string _modelName;
        private string _systemPrompt;
        private double _temperature = 0.3;
        private int _maxTokens = 4096;
    }
}
