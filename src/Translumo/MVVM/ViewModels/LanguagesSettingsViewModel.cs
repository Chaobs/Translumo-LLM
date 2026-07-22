using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Input;
using OpenCvSharp;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Translumo.Dialog;
using Translumo.Dialog.Stages;
using Translumo.Infrastructure.Language;
using Translumo.MVVM.Common;
using Translumo.MVVM.Models;
using Translumo.OCR.Configuration;
using Translumo.OCR.WindowsOCR;
using Translumo.Translation;
using Translumo.Translation.Configuration;
using Translumo.Translation.Llm;
using Translumo.TTS;
using Translumo.Utils;
using Translumo.Utils.Extensions;
using RelayCommand = Microsoft.Toolkit.Mvvm.Input.RelayCommand;

namespace Translumo.MVVM.ViewModels
{
    public sealed class LanguagesSettingsViewModel : BindableBase, IAdditionalPanelController, IDisposable
    {
        public event EventHandler<bool> PanelStateIsChanged;


        public IList<DisplayLanguage> AvailableLanguages { get; set; }
        public IList<DisplayLanguage> AvailableTranslationLanguages { get; set; }

        /// <summary>
        /// Enum-based ComboBox ItemsSource collections.
        /// These are refreshed when <see cref="LocalizationManager.CultureChanged"/> fires,
        /// forcing WPF to re-run value converters so localized display names update immediately.
        /// </summary>
        public ObservableCollection<DisplayEnumItem> AvailableTranslators { get; } = new();
        public ObservableCollection<DisplayEnumItem> AvailableTtsEngines { get; } = new();
        public ObservableCollection<DisplayEnumItem> AvailableLlmProviders { get; } = new();

        public TranslationConfiguration Model { get; set; }

        public TtsConfiguration TtsSettings { get; set; }

        public LlmConfiguration LlmSettings => _llmProfiles.Active;

        public ObservableCollection<string> LlmProfileNames { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> AvailableModels { get; } = new ObservableCollection<string>();

        public string SelectedLlmProfileName
        {
            get => _llmProfiles.ActiveProfileName;
            set
            {
                if (_llmProfiles.ActiveProfileName == value)
                {
                    return;
                }

                _llmProfiles.ActiveProfileName = value;
                SubscribeActiveProfile();
                RefreshAvailableModels();
                OnPropertyChanged(nameof(LlmSettings));
                _llmProfiles.Save();
            }
        }

        public ICommand AddLlmProfileCommand => new RelayCommand(OnAddLlmProfile);

        public ICommand DeleteLlmProfileCommand => new AsyncRelayCommand(OnDeleteLlmProfileAsync);

        public bool IsLlmSelected
        {
            get => _isLlmSelected;
            set => SetProperty(ref _isLlmSelected, value);
        }

        public string LlmTestStatus
        {
            get => _llmTestStatus;
            set => SetProperty(ref _llmTestStatus, value);
        }

        public bool IsLlmTesting
        {
            get => _isLlmTesting;
            set => SetProperty(ref _isLlmTesting, value);
        }

        public ICommand TestLlmConnectionCommand => new AsyncRelayCommand(TestLlmConnectionAsync);

        private ObservableCollection<VoiceInfo> _availableVoices;
        public ObservableCollection<VoiceInfo> AvailableVoices
        {
            get => _availableVoices;
            set => SetProperty(ref _availableVoices, value);
        }

        private VoiceInfo _selectedVoice;
        public VoiceInfo SelectedVoice
        {
            get => _selectedVoice;
            set
            {
                SetProperty(ref _selectedVoice, value);
                if (value != null)
                {
                    Action updateVoiceAction = () =>
                    {
                        TtsSettings.SelectedVoiceName = value.Name;
                    };
                    
                    _ = ReconfigureTts(TtsSettings.TtsLanguage, TtsSettings.TtsSystem, updateVoiceAction);
                }
            }
        }

        public bool IsTtsWindowsSelected => TtsSettings.TtsSystem == TTSEngines.WindowsTTS;

        public bool IsTtsEnabled => TtsSettings.TtsSystem != TTSEngines.None;


        public ObservableCollection<ProxyCardItem> ProxyCollection
        {
            get => _proxyCollection;
            set
            {
                SetProperty(ref _proxyCollection, value);
            }
        }
        public bool ProxySettingsIsOpened
        {
            get => _proxySettingsIsOpened;
            set
            {
                SetProperty(ref _proxySettingsIsOpened, value);
                PanelStateIsChanged?.Invoke(this, value);
            }
        }

        public bool LlmSettingsIsOpened
        {
            get => _llmSettingsIsOpened;
            set
            {
                SetProperty(ref _llmSettingsIsOpened, value);
                PanelStateIsChanged?.Invoke(this, value);
            }
        }

        public Languages TranslateFromLang
        {
            get => Model.TranslateFromLang;
            set
            {
                _ = ChangeSourceLanguage(value);
            }
        }

        public Languages TranslateToLang
        {
            get => Model.TranslateToLang;
            set
            {
                _ = ChangeTargetLanguage(value);
            }
        }

        public TTSEngines TtsSystem
        {
            get => TtsSettings.TtsSystem;
            set
            {
                _ = ChangeTtsSystem(value);
            }
        }

        public ICommand ProxySettingsClickedCommand => new RelayCommand(OnProxySettingsClicked);
        public ICommand ProxyItemDeletedCommand => new RelayCommand<ProxyCardItem>(OnProxyItemDeletedCommand);
        public ICommand ProxyItemAddCommand => new RelayCommand(OnProxyItemAddCommand);
        public ICommand ProxySettingsSubmitCommand => new RelayCommand<bool>(OnProxySettingsSubmit);
        public ICommand ManageApiClickedCommand => new RelayCommand(OnManageApiClicked);
        public ICommand CloseLlmPanelCommand => new RelayCommand(OnCloseLlmPanel);

        private ObservableCollection<ProxyCardItem> _proxyCollection;
        private bool _proxySettingsIsOpened;
        private bool _llmSettingsIsOpened;

        private readonly DialogService _dialogService;
        private readonly OcrGeneralConfiguration _ocrConfiguration;
        private readonly LanguageService _languageService;
        private readonly ILogger _logger;

        private bool _isLlmSelected;
        private string _llmTestStatus;
        private bool _isLlmTesting;
        private LlmProfiles _llmProfiles;
        private LlmConfiguration _subscribedProfile;

        public LanguagesSettingsViewModel(LanguageService languageService, TranslationConfiguration translationConfiguration,
            OcrGeneralConfiguration ocrConfiguration, TtsConfiguration ttsConfiguration, DialogService dialogService,
            LlmProfiles llmProfiles, ILogger<LanguagesSettingsViewModel> logger)
        {
            var languages = languageService.GetAll(true)
                .Select(lang => (lang.TranslationOnly, new DisplayLanguage(lang, GetLanguageDisplayName(lang))))
                .ToArray();
            this.AvailableLanguages = languages
                .Where(lang => !lang.TranslationOnly)
                .OrderBy(lang => lang.Item2.DisplayName)
                .Select(lang => lang.Item2)
                .ToList();

            this.AvailableTranslationLanguages = languages
                .OrderBy(lang => lang.Item2.DisplayName)
                .Select(lang => lang.Item2)
                .ToList();

            this.Model = translationConfiguration;
            this.TtsSettings = ttsConfiguration;
            this.TtsSettings.TtsLanguage = this.Model.TranslateToLang;
            this._llmProfiles = llmProfiles;
            this.Model.PropertyChanged += OnModelPropertyChanged;
            this.IsLlmSelected = this.Model.Translator == Translators.Llm;
            RefreshProfileNames();
            SubscribeActiveProfile();
            RefreshAvailableModels();

            this.AvailableVoices = new ObservableCollection<VoiceInfo>();
            
            if (this.TtsSettings.TtsSystem == TTSEngines.WindowsTTS)
            {
                var languageCode = languageService.GetLanguageDescriptor(this.TtsSettings.TtsLanguage).Code;
                LoadAvailableVoices(languageCode);
            }

            this._languageService = languageService;
            this._dialogService = dialogService;
            this._ocrConfiguration = ocrConfiguration;
            this._logger = logger;

            // Initialize enum ComboBox collections and subscribe to culture changes
            RefreshEnumCollections();
            LocalizationManager.CultureChanged += OnCultureChanged;
        }

        private void LoadAvailableVoices(string languageCode)
        {
            try
            {
                var voices = GetAvailableVoicesForLanguage(languageCode);
                AvailableVoices = new ObservableCollection<VoiceInfo>(voices);
                
                if (!string.IsNullOrEmpty(TtsSettings.SelectedVoiceName))
                {
                    _selectedVoice = AvailableVoices.FirstOrDefault(v => 
                        v.Name.Equals(TtsSettings.SelectedVoiceName, StringComparison.OrdinalIgnoreCase));
                }
                
                if (_selectedVoice == null && AvailableVoices.Count > 0)
                {
                    _selectedVoice = AvailableVoices[0];
                    TtsSettings.SelectedVoiceName = _selectedVoice.Name;
                }
                
                OnPropertyChanged(nameof(SelectedVoice));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Load available voices error");
                AvailableVoices = new ObservableCollection<VoiceInfo>();
            }
        }

        private List<VoiceInfo> GetAvailableVoicesForLanguage(string languageTag)
        {
            using var synth = new SpeechSynthesizer();
            var result = new List<VoiceInfo>();
            
            try
            {
                var voices = synth.GetInstalledVoices(new CultureInfo(languageTag));
                if (voices.Count > 0)
                {
                    result.AddRange(voices.Select(v => v.VoiceInfo));
                    return result;
                }
            }
            catch
            {
            }

            try
            {
                var shortTag = languageTag.Split('-')[0];
                var voices = synth.GetInstalledVoices(new CultureInfo(shortTag));
                if (voices.Count > 0)
                {
                    result.AddRange(voices.Select(v => v.VoiceInfo));
                }
            }
            catch
            {
            }

            return result;
        }

        private void OnProxySettingsClicked()
        {
            LlmSettingsIsOpened = false;
            InitializeProxyCollection();
            ProxySettingsIsOpened = true;
        }

        private void OnManageApiClicked()
        {
            ProxySettingsIsOpened = false;
            LlmSettingsIsOpened = true;
        }

        private void OnCloseLlmPanel()
        {
            LlmSettingsIsOpened = false;
        }

        private void OnProxyItemDeletedCommand(ProxyCardItem itemToDelete)
        {
            _proxyCollection.Remove(itemToDelete);
        }

        private void OnProxyItemAddCommand()
        {
            _proxyCollection.Add(new ProxyCardItem());
        }

        private void OnProxySettingsSubmit(bool applyProxy)
        {
            if (applyProxy)
            {
                Model.ProxySettings = ProxyCollection.Where(pr => pr.IsValid())
                    .Select(pr => pr.MapTo<ProxyCardItem, Proxy>())
                    .ToList();
            }

            ProxySettingsIsOpened = false;
        }

        private void OnModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TranslationConfiguration.Translator))
            {
                IsLlmSelected = Model.Translator == Translators.Llm;
            }
        }

        private void RefreshProfileNames()
        {
            LlmProfileNames.Clear();
            foreach (var profile in _llmProfiles.Profiles)
            {
                LlmProfileNames.Add(profile.Name);
            }
        }

        private void SubscribeActiveProfile()
        {
            if (_subscribedProfile != null)
            {
                _subscribedProfile.PropertyChanged -= OnActiveProfilePropertyChanged;
            }

            _subscribedProfile = _llmProfiles.Active;
            if (_subscribedProfile != null)
            {
                _subscribedProfile.PropertyChanged += OnActiveProfilePropertyChanged;
            }
        }

        private void OnActiveProfilePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LlmConfiguration.Provider))
            {
                RefreshAvailableModels();
                OnPropertyChanged(nameof(LlmSettings));
            }

            _llmProfiles.Save();
        }

        private void RefreshAvailableModels()
        {
            var provider = LlmSettings?.Provider ?? LlmProvider.DeepSeek;
            AvailableModels.Clear();
            if (LlmModelCatalog.Models.TryGetValue(provider, out var models))
            {
                foreach (var model in models)
                {
                    AvailableModels.Add(model);
                }
            }

            OnPropertyChanged(nameof(AvailableModels));
        }

        private void OnAddLlmProfile()
        {
            int index = 1;
            string name;
            do
            {
                name = $"Profile {index++}";
            } while (_llmProfiles.Profiles.Any(p => p.Name == name));

            _llmProfiles.Profiles.Add(new LlmConfiguration { Name = name });
            _llmProfiles.ActiveProfileName = name;
            RefreshProfileNames();
            SubscribeActiveProfile();
            RefreshAvailableModels();
            OnPropertyChanged(nameof(LlmSettings));
            _llmProfiles.Save();
        }

        private async Task OnDeleteLlmProfileAsync()
        {
            if (_llmProfiles.Profiles.Count <= 1)
            {
                return;
            }

            var confirm = await _dialogService.ShowDialogAsync(
                SimpleDialogViewModel.Create(
                    LocalizationManager.GetValue("Str.LlmSettings.DeleteConfirm"),
                    SimpleDialogTypes.Question));
            if (confirm != MessageBoxResult.OK)
            {
                return;
            }

            var active = _llmProfiles.Active;
            if (active != null)
            {
                _llmProfiles.Profiles.Remove(active);
            }

            _llmProfiles.ActiveProfileName = _llmProfiles.Profiles.FirstOrDefault()?.Name;
            RefreshProfileNames();
            SubscribeActiveProfile();
            RefreshAvailableModels();
            OnPropertyChanged(nameof(LlmSettings));
            _llmProfiles.Save();
        }

        private async Task TestLlmConnectionAsync()
        {
            if (IsLlmTesting)
            {
                return;
            }

            IsLlmTesting = true;
            LlmTestStatus = LocalizationManager.GetValue("Str.LlmSettings.Testing");
            try
            {
                var translator = new LlmTranslator(Model, LlmSettings, _languageService, _logger);
                await translator.TranslateTextAsync("Hello");
                LlmTestStatus = LocalizationManager.GetValue("Str.LlmSettings.TestSuccess");
            }
            catch (Exception ex)
            {
                LlmTestStatus = string.Format(LocalizationManager.GetValue("Str.LlmSettings.TestFail"), ex.Message);
            }
            finally
            {
                IsLlmTesting = false;
            }
        }

        private async Task ChangeSourceLanguage(Languages language)
        {
            try
            {
                var changeLangStage = StagesFactory.CreateLanguageChangeStages(_dialogService, () => Model.TranslateFromLang = language,
                    _logger);

                if (_ocrConfiguration.GetConfiguration<WindowsOCRConfiguration>().Enabled)
                {
                    var langCode = _languageService.GetLanguageDescriptor(language).Code;
                    changeLangStage = StagesFactory.CreateWindowsOcrCheckingStages(_dialogService, langCode, changeLangStage, _logger);
                }

                await changeLangStage.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during source language change");
            }

            OnPropertyChanged(nameof(TranslateFromLang));
        }

        private async Task ChangeTargetLanguage(Languages language)
        {
            var changeLanguageAction = () =>
            {
                this.TtsSettings.TtsLanguage = language;
                this.Model.TranslateToLang = language;
                
                if (TtsSettings.TtsSystem == TTSEngines.WindowsTTS)
                {
                    var langCode = _languageService.GetLanguageDescriptor(language).Code;
                    LoadAvailableVoices(langCode);
                }
            };

            await this.ReconfigureTts(language, TtsSettings.TtsSystem, changeLanguageAction);
            OnPropertyChanged(nameof(TranslateToLang));
            OnPropertyChanged(nameof(IsTtsWindowsSelected));
            OnPropertyChanged(nameof(IsTtsEnabled));
        }

        private async Task ChangeTtsSystem(TTSEngines engine)
        {
            Action changeTtsEngineAction = () => 
            {
                this.TtsSettings.TtsSystem = engine;
                
                if (engine == TTSEngines.WindowsTTS)
                {
                    var langCode = _languageService.GetLanguageDescriptor(TtsSettings.TtsLanguage).Code;
                    LoadAvailableVoices(langCode);
                }
                else
                {
                    AvailableVoices = new ObservableCollection<VoiceInfo>();
                    _selectedVoice = null;
                    OnPropertyChanged(nameof(SelectedVoice));
                }
            };
            
            await this.ReconfigureTts(TtsSettings.TtsLanguage, engine, changeTtsEngineAction);
            OnPropertyChanged(nameof(TtsSystem));
            OnPropertyChanged(nameof(IsTtsWindowsSelected));
            OnPropertyChanged(nameof(IsTtsEnabled));
        }

        private async Task ReconfigureTts(Languages language, TTSEngines engine, Action changeParameter)
        {
            try
            {
                var changeLangStage = StagesFactory.CreateLanguageChangeStages(
                    _dialogService,
                    changeParameter,
                    _logger);

                if (engine == TTSEngines.WindowsTTS
                    && !TtsSettings.InstalledWinTtsLanguages.Contains(language))
                {
                    var langCode = _languageService.GetLanguageDescriptor(language).Code;
                    changeLangStage.AddNextStage(new ActionInteractionStage(_dialogService, () =>
                    {
                        this.TtsSettings.InstalledWinTtsLanguages.Add(language);
                        return Task.CompletedTask;
                    }));
                    changeLangStage = StagesFactory.CreateWindowsTtsCheckingStages(_dialogService, langCode, changeLangStage, _logger);
                }
                //else if (engine == TTSEngines.SileroTTS)
                //{
                //    var languageDescriptor = _languageService.GetLanguageDescriptor(language);
                //    changeLangStage = StagesFactory.CreateSileroTtsCheckingStages(languageDescriptor, _dialogService, changeLangStage, _logger);
                //}

                await changeLangStage.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during source language change");
            }
        }

        private string GetLanguageDisplayName(LanguageDescriptor languageDescriptor)
        {
            return LocalizationManager.GetValue($"Str.Languages.{languageDescriptor.Language}", false,
               OnLocalizedValueChanged, this);
        }

        private void OnLocalizedValueChanged(string key, string oldValue)
        {
            var availableLang = AvailableTranslationLanguages.First(lang => lang.DisplayName == oldValue);
            availableLang.DisplayName = LocalizationManager.GetValue(key, false, OnLocalizedValueChanged, this);
        }

        private void InitializeProxyCollection()
        {
            ProxyCollection = new ObservableCollection<ProxyCardItem>(Model.ProxySettings.Select(st => st.MapTo<Proxy, ProxyCardItem>()));
        }

        public void ClosePanel()
        {
            ProxySettingsIsOpened = false;
            LlmSettingsIsOpened = false;
        }

        /// <summary>
        /// Populates the enum ComboBox collections once at construction time.
        /// Language-change refreshes are handled by <see cref="OnCultureChanged"/> via
        /// CollectionView.Refresh() (which preserves the selected item).
        /// </summary>
        private void RefreshEnumCollections()
        {
            AvailableTranslators.Clear();
            foreach (Translators v in Enum.GetValues(typeof(Translators)))
                AvailableTranslators.Add(new DisplayEnumItem(v, GetTranslatorDisplayName(v)));

            AvailableTtsEngines.Clear();
            foreach (TTSEngines v in Enum.GetValues(typeof(TTSEngines)))
                AvailableTtsEngines.Add(new DisplayEnumItem(v, GetTtsDisplayName(v)));

            AvailableLlmProviders.Clear();
            foreach (LlmProvider v in Enum.GetValues(typeof(LlmProvider)))
                AvailableLlmProviders.Add(new DisplayEnumItem(v, GetLlmProviderDisplayName(v)));
        }

        private void OnCultureChanged()
        {
            // Update localized display names in place. The ComboBox binds
            // DisplayMemberPath="DisplayName", so mutating DisplayName raises
            // PropertyChanged and the UI refreshes immediately. The collection is never
            // cleared or replaced, therefore the TwoWay SelectedValue binding (which maps
            // back to the enum) keeps the user's selection (Translator/TTS/LLM provider)
            // intact across language switches.
            foreach (var item in AvailableTranslators)
                item.DisplayName = GetTranslatorDisplayName((Translators)item.Value);

            foreach (var item in AvailableTtsEngines)
                item.DisplayName = GetTtsDisplayName((TTSEngines)item.Value);

            foreach (var item in AvailableLlmProviders)
                item.DisplayName = GetLlmProviderDisplayName((LlmProvider)item.Value);
        }

        private static string GetTtsDisplayName(TTSEngines engine)
            => LocalizationManager.GetValue($"Str.Tts.{engine}") ?? engine.ToString();

        private static string GetTranslatorDisplayName(Translators translator)
            => LocalizationManager.GetValue($"Str.Translator.{translator}") ?? translator.ToString();

        private static string GetLlmProviderDisplayName(LlmProvider provider)
            => provider == LlmProvider.Custom
                ? LocalizationManager.GetValue("Str.LlmSettings.Provider_Custom") ?? provider.ToString()
                : provider.ToString();

        public void Dispose()
        {
            LocalizationManager.CultureChanged -= OnCultureChanged;

            if (_subscribedProfile != null)
            {
                _subscribedProfile.PropertyChanged -= OnActiveProfilePropertyChanged;
            }

            LocalizationManager.ReleaseChangedValuesCallbacks(this);
        }
    }
}