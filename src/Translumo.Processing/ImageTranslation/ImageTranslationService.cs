using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Translumo.Infrastructure.Language;
using Translumo.OCR.WindowsOCR;
using Translumo.Translation;
using Translumo.Translation.Configuration;
using Translumo.Translation.Google;
using Translumo.Translation.Llm;

namespace Translumo.Processing.ImageTranslation
{
    /// <summary>
    /// Orchestrates the instant image-translation ("Google Lens") flow: positional OCR of a captured
    /// region (with source-language auto-detect) + per-line translation with bounded concurrency.
    ///
    /// Translator selection follows the global Translation settings:
    /// - When the active translator is <see cref="Translators.Llm"/>, the LLM is used with a
    ///   source-auto-detect prompt (the AI judges the source language per line).
    /// - Otherwise (Google, or a non-LLM/non-Google backend such as Yandex/DeepL/Papago) the feature
    ///   transparently falls back to Google with <c>sl=auto</c>, so image translation always works
    ///   without reconfiguring the user's chosen translator.
    ///
    /// The WPF layer captures the region bytes and renders the returned lines over their boxes.
    /// </summary>
    public sealed class ImageTranslationService
    {
        private const int MAX_CONCURRENT_TRANSLATIONS = 3;

        private readonly LanguageService _languageService;
        private readonly TranslationConfiguration _translationConfiguration;
        private readonly LlmProfiles _llmProfiles;
        private readonly ILogger _logger;
        private readonly AutoSourceGoogleTranslator _googleTranslator = new AutoSourceGoogleTranslator();

        public ImageTranslationService(
            LanguageService languageService,
            TranslationConfiguration translationConfiguration,
            LlmProfiles llmProfiles,
            ILogger<ImageTranslationService> logger)
        {
            _languageService = languageService;
            _translationConfiguration = translationConfiguration;
            _llmProfiles = llmProfiles;
            _logger = logger;
        }

        /// <summary>Installed Windows OCR source languages as (BCP-47 tag, display name), for the override dropdown.</summary>
        public IReadOnlyList<(string Tag, string DisplayName)> GetAvailableSourceLanguages()
        {
            return WindowsOcrPositional.GetInstalledRecognizers();
        }

        /// <summary>
        /// The target language used when the user has not overridden it in the overlay — mirrors the
        /// global "Target Language" setting rather than a hardcoded default.
        /// </summary>
        public Languages DefaultTargetLanguage => _translationConfiguration.TranslateToLang;

        /// <param name="regionImage">Encoded screenshot bytes of the selected region.</param>
        /// <param name="forcedSourceTag">BCP-47 recognizer tag to force, or null to auto-detect.</param>
        /// <param name="target">Target translation language.</param>
        public async Task<ImageTranslationResult> TranslateRegionAsync(byte[] regionImage, string forcedSourceTag, Languages target)
        {
            var ocr = await WindowsOcrPositional.DetectAndRecognizeAsync(regionImage, forcedSourceTag).ConfigureAwait(false);
            if (ocr == null || ocr.Lines.Count == 0)
            {
                return new ImageTranslationResult
                {
                    DetectedLanguageTag = ocr?.LanguageTag,
                    ImageWidth = ocr?.ImageWidth ?? 0,
                    ImageHeight = ocr?.ImageHeight ?? 0
                };
            }

            var translateLine = CreateLineTranslator(target);
            var translations = await TranslateLinesAsync(ocr.Lines.Select(l => l.Text), translateLine).ConfigureAwait(false);

            var lines = ocr.Lines
                .Select(l => new TranslatedLine
                {
                    Source = l.Text,
                    Translation = translations.TryGetValue(l.Text, out var tr) ? tr : l.Text,
                    Box = l.Box
                })
                .ToList();

            return new ImageTranslationResult
            {
                Lines = lines,
                DetectedLanguageTag = ocr.LanguageTag,
                ImageWidth = ocr.ImageWidth,
                ImageHeight = ocr.ImageHeight
            };
        }

        private Func<string, Task<string>> CreateLineTranslator(Languages target)
        {
            var activeLlm = _translationConfiguration.Translator == Translators.Llm ? _llmProfiles.Active : null;
            if (activeLlm != null && activeLlm.Enabled)
            {
                var llm = new LlmTranslator(_translationConfiguration, activeLlm, _languageService, _logger);
                var targetName = _languageService.GetLanguageDescriptor(target).Language.ToString();
                return src => llm.TranslateAutoDetectAsync(src, targetName);
            }

            // Google path (sl=auto). Also the automatic fallback for Yandex/DeepL/Papago, etc.
            var targetIso = _languageService.GetLanguageDescriptor(target).IsoCode;
            return src => _googleTranslator.TranslateAsync(src, targetIso);
        }

        private async Task<IDictionary<string, string>> TranslateLinesAsync(IEnumerable<string> sources, Func<string, Task<string>> translateLine)
        {
            var distinct = sources.Distinct().ToList();
            var map = new ConcurrentDictionary<string, string>();
            using var throttle = new SemaphoreSlim(MAX_CONCURRENT_TRANSLATIONS);

            await Task.WhenAll(distinct.Select(async src =>
            {
                await throttle.WaitAsync().ConfigureAwait(false);
                try
                {
                    map[src] = await translateLine(src).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Image line translation failed; keeping source text");
                    map[src] = src;
                }
                finally
                {
                    throttle.Release();
                }
            })).ConfigureAwait(false);

            return map;
        }
    }
}
