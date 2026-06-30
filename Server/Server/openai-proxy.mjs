import http from "node:http";
import { mkdir, writeFile, readFile, stat } from "node:fs/promises";
import { existsSync } from "node:fs";
import path from "node:path";
import crypto from "node:crypto";

const PORT = Number(process.env.PORT || 8787);
const API_KEY = process.env.OPENAI_API_KEY;
const CHAT_MODEL = process.env.OPENAI_CHAT_MODEL || "gpt-4.1-mini";
const TRANSCRIBE_MODEL = process.env.OPENAI_TRANSCRIBE_MODEL || "gpt-4o-mini-transcribe";
const TTS_MODEL = process.env.OPENAI_TTS_MODEL || "gpt-4o-mini-tts";
const TTS_VOICE = process.env.OPENAI_TTS_VOICE || "alloy";
const AUDIO_DIR = path.resolve(".audio");
const MAX_BODY_BYTES = 24 * 1024 * 1024;

const systemPrompt = [
  "You are a friendly AR museum NPC avatar.",
  "Answer student questions clearly and briefly.",
  "Always answer in the same language as the student's question.",
  "If the student asks in Chinese, answer in Traditional Chinese.",
  "If the student asks in English, answer in English.",
  "Keep answers suitable for a classroom AR learning activity."
].join(" ");

await mkdir(AUDIO_DIR, { recursive: true });

const server = http.createServer(async (req, res) => {
  try {
    setCorsHeaders(res);

    if (req.method === "OPTIONS") {
      res.writeHead(204);
      res.end();
      return;
    }

    const url = new URL(req.url || "/", `http://${req.headers.host}`);

    if (req.method === "GET" && url.pathname === "/health") {
      sendJson(res, 200, { ok: true });
      return;
    }

    if (req.method === "GET" && url.pathname.startsWith("/audio/")) {
      await serveAudio(url.pathname, res);
      return;
    }

    if (req.method === "POST" && url.pathname === "/ask") {
      await handleAsk(req, res);
      return;
    }

    sendJson(res, 404, { error: "Not found" });
  } catch (error) {
    console.error(error);
    sendJson(res, 500, { error: error.message || "Server error" });
  }
});

server.listen(PORT, "0.0.0.0", () => {
  console.log(`AI NPC proxy running at http://0.0.0.0:${PORT}`);
  if (!API_KEY) {
    console.warn("OPENAI_API_KEY is not set. /ask will fail until you set it.");
  }
});

async function handleAsk(req, res) {
  requireApiKey();

  const body = await readJsonBody(req);
  const audioBase64 = body.audioBase64;

  if (!audioBase64) {
    sendJson(res, 400, { error: "audioBase64 is required" });
    return;
  }

  const mimeType = body.mimeType || "audio/wav";
  const audioBuffer = Buffer.from(audioBase64, "base64");
  const transcript = await transcribeAudio(audioBuffer, mimeType);
  console.log("Student transcript:", transcript || "(empty)");

  const reply = transcript.trim()
    ? await createNpcReply(transcript, body)
    : "I could not hear the question clearly. Please tap Ask AI again and speak closer to the tablet.";

  console.log("NPC reply:", reply);
  const audioUrl = await synthesizeSpeech(reply);

  sendJson(res, 200, {
    transcript,
    reply,
    audioUrl
  });
}

async function transcribeAudio(audioBuffer, mimeType) {
  const form = new FormData();
  form.append("model", TRANSCRIBE_MODEL);
  form.append("file", new Blob([audioBuffer], { type: mimeType }), "student-question.wav");

  const response = await fetch("https://api.openai.com/v1/audio/transcriptions", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${API_KEY}`
    },
    body: form
  });

  const data = await readOpenAiResponse(response);
  return data.text || "";
}

async function createNpcReply(transcript, body) {
  const npcName = body.npcName || "NPC Avatar";
  const targetName = body.targetName || "the scanned image";
  const context = body.context || "";
  const userPrompt = [
    `NPC name: ${npcName}`,
    `Current AR target image: ${targetName}`,
    context ? `Lesson context: ${context}` : "",
    `Student question: ${transcript}`
  ].filter(Boolean).join("\n");

  const response = await fetch("https://api.openai.com/v1/responses", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${API_KEY}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      model: CHAT_MODEL,
      input: [
        { role: "system", content: systemPrompt },
        { role: "user", content: userPrompt }
      ]
    })
  });

  const data = await readOpenAiResponse(response);
  return extractResponseText(data).trim();
}

async function synthesizeSpeech(text) {
  const response = await fetch("https://api.openai.com/v1/audio/speech", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${API_KEY}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      model: TTS_MODEL,
      voice: TTS_VOICE,
      input: text,
      format: "mp3"
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`OpenAI TTS error ${response.status}: ${errorText}`);
  }

  const id = crypto.randomUUID();
  const fileName = `${id}.mp3`;
  const filePath = path.join(AUDIO_DIR, fileName);
  const audioBuffer = Buffer.from(await response.arrayBuffer());
  await writeFile(filePath, audioBuffer);
  return `/audio/${fileName}`;
}

function extractResponseText(data) {
  if (typeof data.output_text === "string") {
    return data.output_text;
  }

  const chunks = [];
  for (const item of data.output || []) {
    for (const content of item.content || []) {
      if (typeof content.text === "string") {
        chunks.push(content.text);
      }
    }
  }

  return chunks.join("\n");
}

async function serveAudio(pathname, res) {
  const fileName = path.basename(pathname);
  const filePath = path.join(AUDIO_DIR, fileName);

  if (!existsSync(filePath)) {
    sendJson(res, 404, { error: "Audio file not found" });
    return;
  }

  const fileStat = await stat(filePath);
  const bytes = await readFile(filePath);
  res.writeHead(200, {
    "Content-Type": "audio/mpeg",
    "Content-Length": fileStat.size,
    "Cache-Control": "no-store"
  });
  res.end(bytes);
}

async function readOpenAiResponse(response) {
  const text = await response.text();
  let data;

  try {
    data = JSON.parse(text);
  } catch {
    data = { raw: text };
  }

  if (!response.ok) {
    throw new Error(`OpenAI API error ${response.status}: ${text}`);
  }

  return data;
}

async function readJsonBody(req) {
  const chunks = [];
  let total = 0;

  for await (const chunk of req) {
    total += chunk.length;
    if (total > MAX_BODY_BYTES) {
      throw new Error("Request body is too large");
    }
    chunks.push(chunk);
  }

  const rawBody = Buffer.concat(chunks).toString("utf8");
  return JSON.parse(rawBody || "{}");
}

function sendJson(res, status, data) {
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store"
  });
  res.end(JSON.stringify(data));
}

function setCorsHeaders(res) {
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET,POST,OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type");
}

function requireApiKey() {
  if (!API_KEY) {
    throw new Error("OPENAI_API_KEY is not set on the server.");
  }
}
