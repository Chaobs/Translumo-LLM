using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Translumo.Infrastructure.Language;
using Translumo.Translation.Configuration;
using Translumo.Translation.Exceptions;
using Translumo.Utils.Http;

namespace Translumo.Translation.Llm
{
    /// <summary>
    /// LLM-backed translator. Talks to any OpenAI-compatible endpoint plus Anthropic and Google
    /// Gemini using the chat/completions family of APIs. The system prompt (configurable) is tuned
    /// for electronic games and films/TV series localization.
    /// </summary>
    public sealed class LlmTranslator : BaseTranslator<LlmContainer>
    {
        private readonly LlmConfiguration _llmSettings;

        public LlmTranslator(TranslationConfiguration translationConfiguration, LlmConfiguration llmConfiguration,
            LanguageService languageService, ILogger logger)
            : base(translationConfiguration, languageService, logger)
        {
            this._llmSettings = llmConfiguration;
        }

        protected override IList<LlmContainer> CreateContainers(TranslationConfiguration configuration)
        {
            var result = configuration.ProxySettings.Select(proxy => new LlmContainer(proxy)).ToList();
            result.Add(new LlmContainer(isPrimary: true));

            return result;
        }

        protected override async Task<string> TranslateTextInternal(LlmContainer container, string sourceText)
        {
            var cfg = _llmSettings;

            if (string.IsNullOrWhiteSpace(cfg.ApiKey))
            {
                throw new TranslationException("LLM translator: API Key is not configured.");
            }

            if (!cfg.Enabled)
            {
                throw new TranslationException("LLM translator: endpoint and model name are required for a custom provider.");
            }

            var systemPrompt = (cfg.SystemPrompt ?? string.Empty)
                .Replace("{SourceLanguage}", SourceLangDescriptor.Language.ToString(), StringComparison.Ordinal)
                .Replace("{TargetLanguage}", TargetLangDescriptor.Language.ToString(), StringComparison.Ordinal);

            var apiStyle = cfg.ApiStyle;
            string url;
            string json;

            container.Reader.OptionalHeaders.Clear();

            switch (apiStyle)
            {
                case LlmApiStyle.OpenAi:
                    container.Reader.OptionalHeaders["Authorization"] = "Bearer " + cfg.ApiKey;
                    url = cfg.ResolvedEndpoint;
                    json = BuildOpenAiRequest(cfg, systemPrompt, sourceText);
                    break;

                case LlmApiStyle.Anthropic:
                    container.Reader.OptionalHeaders["x-api-key"] = cfg.ApiKey;
                    container.Reader.OptionalHeaders["anthropic-version"] = "2023-06-01";
                    url = cfg.ResolvedEndpoint;
                    json = BuildAnthropicRequest(cfg, systemPrompt, sourceText);
                    break;

                case LlmApiStyle.Gemini:
                    url = string.Format(cfg.ResolvedEndpoint, cfg.ResolvedModel) + "?key=" + Uri.EscapeDataString(cfg.ApiKey);
                    json = BuildGeminiRequest(cfg, systemPrompt, sourceText);
                    break;

                default:
                    throw new TranslationException($"Unsupported LLM API style: {apiStyle}");
            }

            HttpResponse response = await container.Reader.RequestWebDataAsync(url, HttpMethods.POST, json, false)
                .ConfigureAwait(false);

            if (!response.IsSuccessful)
            {
                throw new TranslationException($"LLM request failed ({url}): {response.Body}");
            }

            return ParseResponse(apiStyle, response.Body);
        }

        private static string BuildOpenAiRequest(LlmConfiguration cfg, string systemPrompt, string userText)
        {
            var request = new
            {
                model = cfg.ResolvedModel,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userText }
                },
                temperature = cfg.Temperature,
                max_tokens = cfg.MaxTokens
            };

            return JsonSerializer.Serialize(request);
        }

        private static string BuildAnthropicRequest(LlmConfiguration cfg, string systemPrompt, string userText)
        {
            var request = new
            {
                model = cfg.ResolvedModel,
                max_tokens = cfg.MaxTokens,
                system = systemPrompt,
                messages = new object[]
                {
                    new { role = "user", content = userText }
                }
            };

            return JsonSerializer.Serialize(request);
        }

        private static string BuildGeminiRequest(LlmConfiguration cfg, string systemPrompt, string userText)
        {
            var request = new
            {
                contents = new object[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[] { new { text = userText } }
                    }
                },
                systemInstruction = new
                {
                    parts = new object[] { new { text = systemPrompt } }
                },
                generationConfig = new
                {
                    temperature = cfg.Temperature,
                    maxOutputTokens = cfg.MaxTokens
                }
            };

            return JsonSerializer.Serialize(request);
        }

        private static string ParseResponse(LlmApiStyle apiStyle, string body)
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                var message = errorElement.TryGetProperty("message", out var msg) ? msg.GetString() : body;
                throw new TranslationException($"LLM API error: {message}");
            }

            string result = apiStyle switch
            {
                LlmApiStyle.OpenAi => ExtractOpenAi(document.RootElement),
                LlmApiStyle.Anthropic => ExtractAnthropic(document.RootElement),
                LlmApiStyle.Gemini => ExtractGemini(document.RootElement),
                _ => throw new TranslationException($"Unsupported LLM API style: {apiStyle}")
            };

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new TranslationException($"Unexpected LLM response: '{body}'");
            }

            return result.Trim();
        }

        private static string ExtractOpenAi(JsonElement root)
        {
            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
            {
                var message = choices[0].GetProperty("message");
                return message.GetProperty("content").GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static string ExtractAnthropic(JsonElement root)
        {
            if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array && content.GetArrayLength() > 0)
            {
                var first = content[0];
                if (first.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private static string ExtractGemini(JsonElement root)
        {
            if (root.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0)
            {
                var cand = candidates[0];
                if (cand.TryGetProperty("content", out var content))
                {
                    if (content.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array && parts.GetArrayLength() > 0)
                    {
                        return parts[0].GetProperty("text").GetString() ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }
    }
}
