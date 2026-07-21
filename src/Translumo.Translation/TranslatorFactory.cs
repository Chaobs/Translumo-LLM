using System;
using Microsoft.Extensions.Logging;
using Translumo.Infrastructure.Dispatching;
using Translumo.Infrastructure.Language;
using Translumo.Translation.Configuration;
using Translumo.Translation.Deepl;
using Translumo.Translation.Google;
using Translumo.Translation.Llm;
using Translumo.Translation.Papago;
using Translumo.Translation.Yandex;

namespace Translumo.Translation
{
    public class TranslatorFactory
    {
        private readonly LanguageService _languageService;
        private readonly IActionDispatcher _actionDispatcher;
        private readonly ILogger _logger;
        private readonly LlmProfiles _llmProfiles;

        public TranslatorFactory(LanguageService languageService, IActionDispatcher actionDispatcher,
            LlmProfiles llmProfiles, ILogger<TranslatorFactory> logger)
        {
            this._languageService = languageService;
            this._actionDispatcher = actionDispatcher;
            this._llmProfiles = llmProfiles;
            this._logger = logger;
        }

        public ITranslator CreateTranslator(TranslationConfiguration translatorConfiguration)
        {
            switch (translatorConfiguration.Translator)
            {
                case Translators.Deepl:
                    return new DeepLTranslator(translatorConfiguration, _languageService, _logger);
                case Translators.Yandex:
                    return new YandexTranslator(translatorConfiguration, _languageService, _actionDispatcher, _logger);
                case Translators.Papago:
                    return new PapagoTranslator(translatorConfiguration, _languageService, _logger);
                case Translators.Google:
                    return new GoogleTranslator(translatorConfiguration, _languageService, _logger);
                case Translators.Llm:
                    var llmProfile = _llmProfiles.Active;
                    if (llmProfile == null)
                    {
                        throw new InvalidOperationException("No LLM profile is configured.");
                    }
                    return new LlmTranslator(translatorConfiguration, llmProfile, _languageService, _logger);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
