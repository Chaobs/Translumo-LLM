[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![GitHub All Releases](https://img.shields.io/github/downloads/Chaobs/Translumo-LLM/total.svg)](https://github.com/Chaobs/Translumo-LLM/releases)

<p align="center">
  <img src="docs/banner_new.png" alt="Translumo LLM Banner" width="830">
</p>

<h2 align="center" style="border: 0">Translumo LLM — Advanced Real-Time Screen Translator (LLM Edition)</h2>

<p align="center"><strong>English</strong> | <a href="docs/README-ZH.md"><strong>简体中文</strong></a></p>

> **LLM Edition** — a fork of [Translumo](https://github.com/ramjke/Translumo) by
> [Chaobs](https://github.com/Chaobs). It adds **LLM AI translation** (DeepSeek, Qwen, Kimi, GLM, MiniMax,
> ChatGPT, Claude, Gemini, Grok, Ollama for local models, and custom OpenAI-compatible endpoints) on top of
> the original OCR engines, plus Simplified/Traditional Chinese and Japanese localization. See
> [NOTICE](NOTICE) for the full changelog.
> Project page: [github.com/Chaobs/Translumo-LLM](https://github.com/Chaobs/Translumo-LLM) ·
> Report issues: [Chaobs/Translumo-LLM/issues](https://github.com/Chaobs/Translumo-LLM/issues)

## Original Project

Translumo-LLM is built upon **[Translumo](https://github.com/ramjke/Translumo)** — the original
real-time screen translator by [ramjke](https://github.com/ramjke). Visit the upstream project for its
background, history, and the original feature set that this LLM edition extends.

> **Note:** Because Translumo-LLM has added a large number of features that are incompatible with the
> original project (LLM translation, Ollama local models, new UI localization, theme switching, instant
> image translation, etc.), it has been **detached from the upstream fork** and now operates as an
> **independent project**. Future development continues here and is no longer synced with ramjke/Translumo.

## Download Translumo-LLM

**Direct download link to the latest version (v1.2.2):**

[Translumo-LLM-v1.2.2.zip](https://github.com/Chaobs/Translumo-LLM/releases/download/v1.2.2/Translumo-LLM-v1.2.2.zip)

After downloading, unzip the archive and run `Translumo-LLM.exe`. All required dependencies (Python
runtime, OCR models, and WebView2) are bundled — no separate installation is needed.

Full release history: [Chaobs/Translumo-LLM/releases](https://github.com/Chaobs/Translumo-LLM/releases)

## Main Features

- **LLM AI translation**
  Use large language models for higher-quality, context-aware, and more natural translations. Supported
  providers: DeepSeek, Qwen, Kimi, GLM (Zhipu), MiniMax, ChatGPT, Claude, Gemini, Grok, **Ollama (run
  open-source models locally with no API key)**, and any OpenAI-compatible custom endpoint. Configure them
  from **Settings → Manage API**.

- **High text recognition precision**
  Translumo combines multiple OCR engines and uses a machine-learning model to score each result, then
  selects the best one.

  <p align="center">
    <img width="740" src="docs/FrameProcessingDiagram.png">
  </p>

- **Game oriented**
  Designed for real-time translation in PC games, but works anywhere on screen with any application.

- **Low latency**
  Several optimizations reduce system impact and minimize latency between text appearance and translation.

- **Integrated modern OCR engines**: Windows OCR (recommended), Tesseract 5.2 (legacy), EasyOCR (legacy).

- **Available classic translators**: DeepL (recommended), Google Translate, Yandex Translate, Naver Papago.

- **Supported recognition languages**: English, Russian, Japanese, Chinese (Simplified), Korean.

- **Supported translation languages**: English, Russian, Japanese, Chinese (Simplified), Korean, French,
  Spanish, German, Portuguese, Italian, Vietnamese, Thai, Turkish, Arabic, Greek, Brazilian Portuguese,
  Polish, Belarusian, Persian, Indonesian, Bulgarian, Czech, Danish, Estonian, Finnish, Hungarian,
  Lithuanian, Latvian, Dutch, Romanian, Slovak, Slovenian, Swedish, Ukrainian.

## System Requirements

### Minimal requirements to use Tesseract and Windows OCR
- Windows 10 version 2004 (build 19041) or later, or Windows 11
- DirectX 11 compatible GPU
- 2 GB RAM

### Minimal requirements to use EasyOCR
- NVIDIA GPU with CUDA SDK 11.8 support (GTX 750, 8xxM, 9xx series or newer)
- 8 GB RAM
- At least 5 GB of free storage space

## How to Use

**English demo:**

[![Watch Demo](docs/demo-cover/setting-tutorial-en_cover.jpg)](https://github.com/user-attachments/assets/7149c277-d5ec-489c-b37e-8ad769a09a43)

1. Open the Settings (**Alt+G**) or double-click the icon in the system tray
2. Select languages: source language for OCR and translation language
3. Select text recognition engines (see Usage Tips for recommended modes)
4. Define the capture area: press **Alt+Q** and select an area on the screen
5. Run translation (press **~**)

### Recommended OCR Engines

- It is recommended to use **WindowsOCR** only.

Tesseract is old, slow, and produces many errors.  
EasyOCR is even slower, requires significant resources (including a specific GPU), and often leads to bugs.

It is generally better to keep only WindowsOCR, but the other engines are still included for historical reasons.

### Select Minimum Capture Area
Reducing the capture area decreases the chance of picking up random letters from the background. Larger frames take longer to process.

### Use Proxy List to Avoid Blocking by Translation Services
Some translators may block clients sending many requests. Configure personal or shared IPv4 proxies (1–2 is usually enough) under **Languages → Proxy tab**. The app alternates proxies to reduce requests from a single IP.

### Use Borderless or Windowed Modes in Games (Not Fullscreen)
These modes are required for correct translation overlay display. If your game does not support them, use tools like [Borderless Gaming](https://github.com/Codeusa/Borderless-Gaming).

## AI Config Tutorial

LLM translation requires an API key from your chosen provider. The short flow:

**English demo:**

[![Watch Demo](docs/demo-cover/ai-config-en_cover.jpg)](https://github.com/user-attachments/assets/3dadc829-9001-491b-9e42-eb2ffd87688f)

1. Open the Settings (**Alt+G**) or double-click the icon in the system tray
2. Click **Manage API**
3. Select your AI provider and models, then enter your API key
4. Define the capture area: press **Alt+Q** and select an area on the screen
5. Run translation (press **~**)

> API keys are stored locally and protected with OS-level encryption (DPAPI) on first launch. They are
> never sent anywhere except to the provider you configure.

## FAQ

**Q: How do I configure LLM translation?**
A: Open **Settings → Manage API**, choose a provider (DeepSeek, Qwen, Kimi, GLM, MiniMax, ChatGPT, Claude, Gemini, Grok, Ollama, or a custom OpenAI-compatible endpoint), select a model, and enter your API key. For **Ollama**, no API key is required — just point it at your local Ollama server (default `http://localhost:11434`). See the AI Config Tutorial above.

**Q: What is the "Google Lens Style" instant image translation?**
A: Press **Alt+D** and select any region on your screen. Translumo-LLM captures that region once, runs OCR to detect the text (the source language is auto-detected), and shows a translation overlay in your configured target language. It is ideal for static content that does not change continuously — game menus, item descriptions, dialogue boxes, signs, etc. Unlike the continuous translation mode (Alt+Q + `~`), this mode translates a single captured frame on demand. The translation uses your LLM provider when available and falls back to Google Translate if the LLM fails. To change the target language, set it under **Settings → Languages**.

**Q: LLM translation returns errors or no result**
A: Verify your API key is correct and has remaining quota, the selected model is available for your account, and your network can reach the provider. If a translator blocks frequent requests, configure a proxy under **Languages → Proxy tab**.

**Q: I get "Failed to capture screen" or nothing happens after translation starts**
A: Ensure the target window is active. Restart Translumo-LLM or reopen the target window if needed.

**Q: Borderless/windowed mode is set, but the translation window is under the game**
A: With the game running and focused, press the hotkey (**Alt+T** by default) to hide and show the translation window.

**Q: Hotkeys don't work**
A: Other applications may be intercepting hotkeys.

**Q: Text detection failed (TesseractOCREngine)**
A: Ensure the application path contains only Latin letters.

## Build

**Prerequisites**
- Windows 10 (build 19041) or later / Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 with the **".NET desktop development"** workload (recommended), or any editor that can run `dotnet` commands

**Steps**

1. Clone the repository (the `master` branch always corresponds to the latest release):
   ```bash
   git clone https://github.com/Chaobs/Translumo-LLM.git
   cd Translumo-LLM
   ```

2. Restore dependencies and build the solution in Release mode:
   ```bash
   dotnet build Translumo-LLM.sln -c Release
   ```
   > The first build runs **binaries_extract.bat**, which downloads and extracts the OCR models and the embedded Python runtime (~400 MB) into the output directory. This only happens once and requires an internet connection.

3. Run it directly from the build output (for development / testing):
   ```bash
   dotnet run --project src/Translumo -c Release
   ```

4. Produce a distributable single-file build:
   ```bash
   dotnet publish src/Translumo/Translumo.csproj -c Release -r win-x64 -p:SolutionDir="$(pwd)/"
   ```
   The published files land in `src/Translumo/bin/Release/net8.0-windows/win-x64/publish/`. On first launch the .NET host extracts the single-file bundle to a local `temp\` folder next to the executable, then runs normally.

## Changelog

This section summarizes the key features Translumo-LLM has added and the main bugs it has fixed since the original Translumo project.

**New features**
- **LLM AI translation** — context-aware, higher-quality translations via LLM providers: DeepSeek, Qwen, Kimi, GLM (Zhipu), MiniMax, ChatGPT, Claude, Gemini, Grok, and any custom OpenAI-compatible endpoint.
- **Ollama local models** — run open-source AI models entirely on your own machine through Ollama; no API key or internet connection required for translation.
- **Google Lens Style instant image translation** — press **Alt+D** to capture and translate any static region on screen on demand (see FAQ).
- **UI localization** — the interface is now available in Simplified Chinese, Traditional Chinese, Japanese, Russian, and English.
- **Switchable light/dark theme** — choose the appearance that suits you.
- **TTS voice switcher** — select among more system voices (including OneCore voices) for the speech feature.
- **Secure API key storage** — keys are encrypted with OS-level DPAPI on first launch.
- **Auto source-language detection** for LLM translation (no need to set the OCR source language manually).

**Bug fixes**
- Fixed crashes when the application was installed under a path containing non-ASCII characters (e.g. Chinese/Japanese/Russian paths) — the app now works from any directory, including non-English ones.
- Reduced excessive `C:` drive space usage caused by repeated single-file extraction / temp caching.
- Fixed the target language not following the **Settings** selection in image translation.
- Fixed a slow overlay popup in image translation (now appears instantly with results filled in the background).
- Various stability and compatibility improvements.

## Todo-List

Planned future work (not yet implemented):
1. **Improve translation stability and speed** — make LLM and classic translation more reliable and faster under load.
2. **Fix potential bugs** — address issues reported by users and found during testing.
3. **Boost performance and reduce resource usage** — lower the CPU/GPU/RAM footprint and improve efficiency.
4. **Develop a more modern UI** — refresh the interface with a cleaner, more intuitive design.

## Credits

- [Translumo](https://github.com/ramjke/Translumo) — the original real-time screen translator by ramjke, which this LLM edition is based on. Thank you for the outstanding work.
- [Material Design In XAML Toolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- [Tesseract .NET wrapper](https://github.com/charlesw/tesseract)
- [OpenCvSharp](https://github.com/shimat/opencvsharp)
- [Python.NET](https://github.com/pythonnet/pythonnet)
- [EasyOCR](https://github.com/JaidedAI/EasyOCR)
- [Silero TTS](https://github.com/snakers4/silero-models)
