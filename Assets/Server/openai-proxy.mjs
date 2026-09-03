import http from "node:http";
import { mkdir, writeFile, readFile, stat } from "node:fs/promises";
import { existsSync } from "node:fs";
import path from "node:path";
import crypto from "node:crypto";

const PORT = Number(process.env.PORT || 8787);
const API_KEY = process.env.OPENAI_API_KEY;
const CHAT_MODEL = process.env.OPENAI_CHAT_MODEL || "gpt-4o-mini"; 
const TRANSCRIBE_MODEL = process.env.OPENAI_TRANSCRIBE_MODEL || "whisper-1"; 
const TTS_MODEL = process.env.OPENAI_TTS_MODEL || "tts-1"; 
const TTS_VOICE = process.env.OPENAI_TTS_VOICE || "shimmer";
const AUDIO_DIR = path.resolve(".audio");
const MAX_BODY_BYTES = 24 * 1024 * 1024;

// 1. KHO TRI THỨC VĂN HÓA (Semantic Cultural Knowledge Base)
const culturalKnowledgeBase = {
  // --- VIỆT NAM ---
  "DongHo": `Artwork: Dong Ho Folk Painting - The Mice's Wedding. Semantic Interpretation: A sharp political satire. The Cat represents the greedy ruling class. The Bribe (bird and fish) critiques corruption. The Mice represent ordinary peasants' resilience and hope for peace.`,
  
  "VN_cloth": `Artwork: Vietnamese Ao Tu Than (Four-panel dress). Semantic Interpretation: The four panels symbolize the four parents (birth parents and in-laws). The tied belt represents a faithful bond in love. It highlights the virtue, sacrifice, and grace of ancient Vietnamese women.`,
  
  "VN_food": `Artwork: Banh Phu The (Husband and Wife Cake). Semantic Interpretation: The sticky texture symbolizes an unbreakable marriage bond. The square box and round cake inside represent the harmony of Yin and Yang, Heaven and Earth, ensuring a happy and balanced union.`,
  
  "VN_fest": `Artwork: Dong Ky Firecracker Festival. Semantic Interpretation: A vibrant celebration honoring the village guardian deity. The loud firecrackers symbolize dispelling evil spirits, awakening the spring, and a powerful collective wish for a prosperous new year.`,

  // --- ĐÀI LOAN ---
  "TW_cloth": `Artwork: Paiwan Indigenous Costume. Semantic Interpretation: The intricate glass beads and eagle feathers are not just decorations; they signify social status, nobility, and family heirlooms. The embroidery deeply connects the wearer to their ancestors and nature.`,
  
  "TW_food": `Artwork: Taiwanese Beef Noodle Soup. Semantic Interpretation: More than just a dish, it represents a historical fusion of cultures in Taiwan. It symbolizes resilience, agricultural transition, and the comforting warmth of Taiwanese hospitality.`,
  
  "TW_fest": `Artwork: Taiwan Lantern Festival. Semantic Interpretation: Releasing sky lanterns symbolizes letting go of the past year's burdens and illuminating the path forward with new hopes, dreams, and prayers for peace.`,

  // --- KERALA (ẤN ĐỘ) ---
  "KL_cloth": `Artwork: Kerala Kasavu Saree. Semantic Interpretation: The pristine white color symbolizes purity and elegance, while the golden border (Zari) represents prosperity. It reflects the deeply rooted simplicity, grace, and spiritual devotion of Malayali culture.`,
  
  "KL_food": `Artwork: Kerala Sadya. Semantic Interpretation: A grand feast served on a banana leaf featuring all six Ayurvedic tastes. Sitting on the floor to eat together symbolizes absolute equality, humility, and the sharing of nature's abundance.`,
  
  "KL_fest": `Artwork: Onam Festival. Semantic Interpretation: Celebrates the annual return of the mythical King Mahabali. It represents an ideal, utopian society where everyone is equal, prosperous, and happy, completely devoid of caste or class discrimination.`
};

// 2. PROMPT HỆ THỐNG CƠ BẢN (Cultural Mediator Persona)
const BASE_SYSTEM_PROMPT = [
  "You are an expert 'Cultural Guardian' AR museum avatar.",
  "Your role is cultural mediation. Always focus on explaining the metaphorical and satirical meanings (e.g., Cat = corrupt rulers, Mice = clever peasants), not just describing what is visible.",
  "Use a warm, engaging, and culturally deep tone.",
  "Auto-detect the user's language (English, Vietnamese, or Chinese) and reply naturally in that exact same language.",
  "Keep your answers concise and strictly under 80 words to suit an AR environment.",
  "If the user asks off-topic questions (e.g., math, coding, politics), gently guide them back to exploring the cultural secrets of the painting."
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

async function getRagContext(targetName) {
  if (!targetName) return "Focus on traditional cultural values.";

  const scriptDir = path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, "$1"));
  const possiblePaths = [
    path.join(scriptDir, "rag-docs", `${targetName}.md`),
    path.resolve("Assets", "Server", "rag-docs", `${targetName}.md`),
    path.resolve("rag-docs", `${targetName}.md`),
    path.resolve(".", "rag-docs", `${targetName}.md`)
  ];

  for (const p of possiblePaths) {
    if (existsSync(p)) {
      try {
        const content = await readFile(p, "utf8");
        if (content && content.trim()) {
          console.log(`[RAG] Loaded comprehensive document: ${path.basename(p)}`);
          return content.trim();
        }
      } catch (err) {
        console.warn(`[RAG] Warning reading ${p}:`, err.message);
      }
    }
  }

  return culturalKnowledgeBase[targetName] || "Focus on traditional cultural values.";
}

async function createNpcReply(transcript, body) {
  const npcName = body.npcName || "NPC Avatar";
  const targetName = body.targetName || "the scanned image";
  const context = body.context || "";
  
  // 3. TIÊM NGỮ NGHĨA VĂN HÓA VÀO PROMPT (Từ file Markdown RAG hoặc Knowledge Base)
  const deepCulturalContext = await getRagContext(targetName);
  const dynamicSystemPrompt = `${BASE_SYSTEM_PROMPT}\n\nComprehensive Cultural Knowledge & Deep Context:\n${deepCulturalContext}`;

  const userPrompt = [
    `NPC name: ${npcName}`,
    `Current AR target image: ${targetName}`,
    context ? `Lesson context: ${context}` : "",
    `Student question: ${transcript}`
  ].filter(Boolean).join("\n");

  const response = await fetch("https://api.openai.com/v1/chat/completions", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${API_KEY}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      model: CHAT_MODEL,
      temperature: 0.2, // Giữ nguyên mức 0.2 để giảm hallucination
      messages: [
        { role: "system", content: dynamicSystemPrompt },
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
  if (data.choices && data.choices.length > 0 && data.choices[0].message) {
    return data.choices[0].message.content;
  }
  return "Error: Could not parse AI response.";
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