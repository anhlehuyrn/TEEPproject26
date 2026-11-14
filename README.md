# OLA Cultural Learning System

An AR (Augmented Reality) educational application that teaches the cultural heritage of **Vietnam**, **Taiwan**, and **Kerala (India)** by scanning physical paintings and interacting with an AI-powered cultural guide.

Point the device camera at a card/painting, the app tracks the image and spawns a 3D cultural avatar with floating interactive items. Students can then ask questions by voice and receive spoken answers in their own language.

Built with Unity + AR Foundation (ARCore), powered by a lightweight Node.js proxy that talks to the OpenAI API, and accompanied by a Streamlit web dashboard for gallery browsing and interaction analytics.

---

## Key Features

- **AR image tracking** — Detects the registered reference images and spawns the matching avatar and interactive items anchored to the painting.
- **AI cultural NPC (voice Q&A)** — Tap to record a question, the audio is sent to the backend proxy, and the answer comes back as both text (typewriter effect) and synthesized speech.
- **Vision-aware answers** — The scanned artwork image and a per-artwork RAG context (lesson context) are sent to the model so answers stay on-topic.
- **Multilingual by design (EN / ZH-TW / ML / VI)** — UI text, fonts, intro screens, and voice-over audio all switch with the selected language. Traditional Chinese and Malayalam use dedicated generated font fallbacks.
- **Passport & stamp system** — Each discovered artwork awards a stamp; progress, collected stamps, and explored regions are persisted via `PlayerPrefs`.
- **Floating item interaction** — Sprites, animations, and descriptions pop out of the painting in 3D and can be tapped/zoomed.
- **Cultural video library** — Short clips for costume, festival, and food of each region (Vietnam / Taiwan / Kerala).
- **Fungus dialogue integration** — Image-triggered dialogue flows with animator sync and localized voice lines.
- **Editor tooling** — Layout/font generators and a magazine UI generator under `Assets/Editor` for rebuilding screens quickly.

---

## Tech Stack

| Layer | Technology |
| --- | --- |
| App engine | Unity 6 (`6000.5.0f1`) |
| AR | AR Foundation / ARCore (`com.unity.xr.arcore` 6.5.0), XR Plug-in Management |
| UI | Unity uGUI + TextMesh Pro |
| Dialogue | Fungus (bundled in `Assets/fungus-master`) |
| Input | Input System (`com.unity.inputsystem`) |
| Backend proxy | Node.js >= 20 (ES modules, zero dependencies) |
| AI | OpenAI Chat / Transcribe / TTS models |
| Web dashboard | Python + Streamlit + pandas + Plotly |
| Effects | Hovl Studio Magic Effects Pack |

---

## Repository Structure

```text
TEEP_New/
├── Assets/
│   ├── AIChat/                        # AI NPC voice Q&A controller
│   ├── Scripts/                       # AR, spawner, dialogue triggers, scene loading
│   ├── Editor/                        # UI / font / layout generators (editor-only)
│   ├── Scenes/                        # HomePage, MainPage, ScanPage
│   ├── Resources/                     # localization.json, TextData.csv, voice-over, fonts
│   ├── Server/                        # Node.js OpenAI proxy + its own README
│   ├── Audio/  Pictures/  Video/      # Media assets
│   ├── fungus-master/                 # Fungus dialogue framework (third-party)
│   ├── *.cs                           # Localization, passport, stamp, nav managers
│   └── Knowledge.json                 # In-app multilingual knowledge strings
├── app/                               # Design references & UI assets (DESIGN.md, mockups)
├── paper/                             # ICTDMD2026 paper and figures
├── website/                           # Streamlit dashboard (app.py)
├── gallery_data.json                  # Dashboard data store
├── Packages/manifest.json             # Unity package dependencies
├── ProjectSettings/                   # Unity project settings
└── requirements.txt                   # Python deps for the web dashboard
```

> `Library/`, `Temp/`, `Logs/`, `UserSettings/`, and build outputs are generated and git-ignored.

---

## Getting Started

### 1. Open the Unity project

1. Install **Unity 6** (version `6000.5.0f1` via Unity Hub).
2. Add this folder as a project and open it — packages resolve from `Packages/manifest.json`.
3. Switch the build target to **Android** (ARCore) for on-device testing, or run in the editor with a connected webcam.
4. Main scenes:
   - `Assets/Scenes/HomePage.unity` — language/location entry.
   - `Assets/Scenes/MainPage 30th6.unity` — home, explore, and passport tabs.
   - `Assets/Scenes/ScanPage.unity` — AR scanning experience.

### 2. Run the AI backend proxy

The proxy keeps the OpenAI API key off the device. See the full guide in [Assets/Server/README_AI_NPC.md](Assets/Server/README_AI_NPC.md).

```powershell
cd Assets\Server
$env:OPENAI_API_KEY="YOUR_OPENAI_API_KEY"
node .\openai-proxy.mjs
```

The server listens on `http://127.0.0.1:8787` (health check at `/health`).

Environment variables (only `OPENAI_API_KEY` is required):

```text
OPENAI_API_KEY           = your OpenAI API key
OPENAI_CHAT_MODEL        = gpt-4.1-mini
OPENAI_TRANSCRIBE_MODEL  = gpt-4o-mini-transcribe
OPENAI_TTS_MODEL         = gpt-4o-mini-tts
OPENAI_TTS_VOICE         = alloy
```

In the scene, set `AiNpcQuestionController.Server Base Url` to the local URL for desktop testing or to a deployed URL (e.g. Render) for Android tablets. For local-network device testing, use the computer's LAN IP (e.g. `http://192.168.1.23:8787`) and keep both devices on the same Wi-Fi.

### 3. Run the web dashboard

```powershell
pip install -r requirements.txt
cd website
streamlit run app.py
```

The dashboard provides a gallery, AR-scan analytics (Plotly charts), and a guestbook, with the same four-language translation set. Data is persisted to `gallery_data.json`.

---

## Configuration Notes

- **Localization** — Key/value pairs live in `Assets/Resources/localization.json`; in-app knowledge strings are in `Assets/Knowledge.json`. Language selection is stored in `PlayerPrefs` under `AppLanguage` (`0` EN, `1` ZH-TW, `2` ML, `3` VI).
- **Reference images** — Scanned targets are registered in `Assets/ReferenceImageLibrary.asset`; target names there must match the names used by the spawners and controllers.
- **Voice-over** — Clips are resolved from `Assets/Resources/VoiceOver/` using a language prefix (`en`, `cn`, `kl`, `vn`) + base file name.
- **Security** — Never place `OPENAI_API_KEY` in C# scripts, `PlayerPrefs`, `Resources`, `StreamingAssets`, or any shipped APK. The key belongs only on the backend.

---

## Related Documentation

- [Assets/Server/README_AI_NPC.md](Assets/Server/README_AI_NPC.md) — AI NPC proxy setup, Render deployment, and Unity wiring.
- [app/UI/DESIGN.md](app/UI/DESIGN.md) — "Heritage Scholarly Minimalist" design system (colors, typography, spacing, components).

---

## Third-Party Components

- [Fungus](https://fungusgames.com/) — open-source Unity dialogue/visual-scripting framework (see `Assets/fungus-master/LICENSE`).
- Hovl Studio — Magic Effects Pack.
- Fonts: EB Garamond, Inter, Noto Sans Display, Noto Serif TC, Noto Serif Malayalam.
