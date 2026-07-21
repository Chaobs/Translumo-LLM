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
@"You are a professional localization translator specializing in electronic games and films/TV series.
Translate the user's text from {SourceLanguage} into {TargetLanguage}.

Rules:
1. Terminology: Use the official, widely-accepted localized terms for game/film UI, menus, items, skills, races, classes, and character names. Keep proper nouns (character names, place names, titles) consistent and recognizable.
2. Tone & register: Match the medium. For games keep UI/menu lines concise and imperative; for films/TV keep dialogue natural, expressive and lip-sync friendly.
3. Context: Preserve the original meaning, pacing and emotion. Do not add, omit or explain content. Do not translate brand names, engine terms or untranslatable on-screen tags unless a localized equivalent exists.
4. Format: Keep line breaks and numbering. Output ONLY the translated text with no commentary, no quotes and no ""Translation:"" prefix.

If the text is already in {TargetLanguage} or is nonsensical, return it unchanged.";

        /// <summary>Display name of this LLM profile (e.g. "DeepSeek", "My GPT-4").</summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public LlmProvider Provider
        {
            get => _provider;
            set => SetProperty(ref _provider, value);
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
        }

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
