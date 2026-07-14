# AI NPC first-phase setup with Render

This first phase uses a backend proxy so the OpenAI API key is not stored in the Unity app.

## Project structure

```text
Test0624
+-- Assets
|   +-- AIChat
|       +-- AiNpcQuestionController.cs
+-- Server
    +-- openai-proxy.mjs
    +-- package.json
    +-- README_AI_NPC.md
```

## 1. Test the local proxy first

Open PowerShell in the Unity project folder:

```powershell
cd C:\Users\User\Desktop\TEEP\2025_Extend\Test0624
$env:OPENAI_API_KEY="YOUR_OPENAI_API_KEY"
node .\Server\openai-proxy.mjs
```

The server listens on:

```text
http://127.0.0.1:8787
```

Health check:

```text
http://127.0.0.1:8787/health
```

## 2. Deploy the proxy to Render

1. Push this Unity project to GitHub.
2. Go to Render.
3. Create a new Web Service.
4. Connect the GitHub repository.
5. Set the service options:

```text
Root Directory: Server
Runtime: Node
Build Command: npm install
Start Command: npm start
```

6. Add Environment Variables:

```text
OPENAI_API_KEY = your OpenAI API key
OPENAI_CHAT_MODEL = gpt-4.1-mini
OPENAI_TRANSCRIBE_MODEL = gpt-4o-mini-transcribe
OPENAI_TTS_MODEL = gpt-4o-mini-tts
OPENAI_TTS_VOICE = alloy
```

Only `OPENAI_API_KEY` is required. The others are optional.

7. Deploy the service.
8. After deployment, copy the Render URL, for example:

```text
https://your-ai-npc-server.onrender.com
```

9. Test:

```text
https://your-ai-npc-server.onrender.com/health
```

Expected response:

```json
{"ok":true}
```

## 3. Add the Unity controller

In Unity:

1. Create an empty GameObject named `AI_NPC_QuestionController`.
2. Add `AiNpcQuestionController`.
3. Add an `AudioSource` component on the same GameObject.
4. Set:

```text
Server Base Url = https://your-ai-npc-server.onrender.com
NPC Name = Dragon Boat NPC
Target Name = DragonBoat
Lesson Context = the short classroom context for this NPC
```

5. Drag your Ask button into `Ask Button`.
6. Drag a small status Text into `Status Text`.
7. Drag the dialogue / answer Text into `Answer Text`.
8. Drag the AudioSource into `Answer Audio Source`.

For local computer testing, use:

```text
Server Base Url = http://127.0.0.1:8787
```

For Android tablet testing, use the Render URL:

```text
Server Base Url = https://your-ai-npc-server.onrender.com
```

## 4. Connect the Ask button

On the Button `On Click()` event:

```text
AI_NPC_QuestionController -> AiNpcQuestionController.ToggleRecording()
```

Tap once to start recording, tap again to stop and ask.

## 5. Android note

For official tablet testing, prefer the Render URL.

If testing with a local computer server instead of Render, replace `127.0.0.1` with the computer LAN IP address, for example:

```text
http://192.168.1.23:8787
```

The phone and computer must be on the same Wi-Fi network.

## 6. Security note

Do not put `OPENAI_API_KEY` in Unity C# scripts, PlayerPrefs, Resources, StreamingAssets, or any APK file.

The API key belongs only on Render or another backend server.
