import http from "node:http";
import { mkdir, writeFile, readFile, stat } from "node:fs/promises";
import { existsSync } from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import { fileURLToPath } from "node:url";

const PORT = Number(process.env.PORT || 8787);
const API_KEY = process.env.OPENAI_API_KEY;
const CHAT_MODEL = process.env.OPENAI_CHAT_MODEL || "gpt-4o-mini";
const TRANSCRIBE_MODEL = process.env.OPENAI_TRANSCRIBE_MODEL || "whisper-1";
const TTS_MODEL = process.env.OPENAI_TTS_MODEL || "tts-1";
const TTS_VOICE = process.env.OPENAI_TTS_VOICE || "nova";
const SERVER_DIR = path.dirname(fileURLToPath(import.meta.url));
const AUDIO_DIR = path.join(SERVER_DIR, ".audio");
const RAG_DOCS_DIR = path.join(SERVER_DIR, "rag-docs");
const MAX_BODY_BYTES = 24 * 1024 * 1024;

// 1. KHO TRI THỨC VĂN HÓA DỰ PHÒNG
const culturalKnowledgeBase = {
  "DongHo": "Artwork: Dong Ho Folk Painting - The Mice's Wedding. Satirical meaning: Criticizes feudal corruption (the greedy cat receiving bribes of birds/fish) while celebrating the resilience, humor, and hope of common people.",
  "VN_cloth": "Artwork: Vietnamese Ao Tu Than (Four-panel dress). Semantic meaning: Four panels symbolize parents and in-laws; the tied belt represents marital fidelity, elegance, and sacrifice of Vietnamese women.",
  "VN_food": "Artwork: Banh Phu The (Husband and Wife Cake). Meaning: Sticky texture symbolizes marriage bond; square box and round cake harmonize Yin-Yang, Heaven-Earth.",
  "VN_fest": "Artwork: Dong Ky Firecracker Festival. Meaning: Honoring village deities, dispelling bad luck, ushering in spring, and wishing for community prosperity.",
  "TW_cloth": "Artwork: Paiwan Indigenous Costume. Meaning: Intricate glass beads and embroidery symbolize social rank, ancestry connection, and noble status.",
  "TW_food": "Artwork: Taiwanese Beef Noodle Soup. Meaning: Culinary fusion representing warmth, agricultural transition, and Taiwanese hospitality.",
  "TW_fest": "Artwork: Taiwan Lantern Festival. Meaning: Releasing glowing lanterns into the night sky to let go of old burdens and make wishes for peace and hope.",
  "KL_cloth": "Artwork: Kerala Kasavu Saree. Meaning: Pristine white and golden border (Zari) symbolizing purity, prosperity, simplicity, and grace in Malayali heritage.",
  "KL_food": "Artwork: Kerala Sadya. Meaning: Grand vegetarian feast on a banana leaf with six Ayurvedic tastes, symbolizing unity, equality, and natural abundance.",
  "KL_fest": "Artwork: Onam Festival. Meaning: Commemorating King Mahabali's return to an egalitarian, prosperous, and joyous golden era with flower carpets (Pookkalam)."
};

const systemPrompt = [
  "You are a friendly, enthusiastic AR museum culture avatar.",
  "Your persona is a lively young female guide who loves sharing cultural wisdom.",
  "Answer student questions clearly, concisely (under 75 words), and engagingly.",
  "Keep answers suitable for a classroom AR learning activity.",
  "Explain the metaphorical, symbolic, and satirical meanings of the cultural elements.",
  "You are fully multilingual in English, Vietnamese (Tiếng Việt), Traditional Chinese (繁體中文), and Malayalam (മലയാളം).",
  "CRITICAL LANGUAGE RULE: Automatically detect the language used in the student's question. If the student asks in Vietnamese, reply strictly in fluent natural Vietnamese. If the student asks in Malayalam, reply strictly in Malayalam script. If the student asks in Traditional Chinese, reply strictly in Traditional Chinese. If in English, reply in English. Match the student's language 100% naturally."
].join(" ");

await mkdir(AUDIO_DIR, { recursive: true });

const server = http.createServer(async (req, res) => {
  try {
    setCorsHeaders(res);
    if (req.method === "OPTIONS") return res.writeHead(204).end();

    const url = new URL(req.url || "/", `http://${req.headers.host}`);
    if (req.method === "GET" && url.pathname === "/health") return sendJson(res, 200, { ok: true });
    if (req.method === "GET" && url.pathname.startsWith("/audio/")) return await serveAudio(url.pathname, res);
    if (req.method === "POST" && url.pathname === "/ask") return await handleAsk(req, res);
    sendJson(res, 404, { error: "Not found" });
  } catch (error) {
    console.error(error);
    sendJson(res, 500, { error: error.message || "Server error" });
  }
});

server.listen(PORT, "0.0.0.0", () => {
  console.log(`AI NPC proxy running at http://0.0.0.0:${PORT}`);
  if (!API_KEY) console.warn("OPENAI_API_KEY is not set. /ask will fail until you set it.");
});

async function handleAsk(req, res) {
  requireApiKey();
  const body = await readJsonBody(req);
  if (!body.audioBase64) return sendJson(res, 400, { error: "audioBase64 is required" });

  // 1. Nhận diện giọng nói tự nhiên (Tự động nhận diện tiếng Việt / Malayalam / Trung / Anh)
  const transcript = await transcribeAudio(
    Buffer.from(body.audioBase64, "base64"),
    body.mimeType || "audio/wav",
    body.language // Truyền gợi ý nếu có, hoặc để trống để tự nhận diện
  );
  console.log(`Student transcript:`, transcript || "(empty)");

  // 2. Tạo câu trả lời tự động khớp ngôn ngữ
  const reply = transcript.trim()
    ? await createNpcReply(transcript, body)
    : "I could not hear the question clearly. Please tap Ask AI again and speak closer to the tablet / Mình chưa nghe rõ, bạn bấm nói lại nhé / എനിക്ക് കേൾക്കാനായില്ല.";

  console.log(`NPC reply:`, reply);
  const audioUrl = await synthesizeSpeech(reply, body.language);
  sendJson(res, 200, { transcript, reply, audioUrl });
}

async function transcribeAudio(audioBuffer, mimeType, preferredLanguage) {
  const form = new FormData();
  form.append("model", TRANSCRIBE_MODEL);
  form.append("file", new Blob([audioBuffer], { type: mimeType }), "student-question.wav");
  
  // Nếu là tiếng Malayalam đặc thù thì ưu tiên mã "ml", còn lại để Whisper tự động phát hiện linh hoạt
  if (preferredLanguage === "ml" || preferredLanguage === "Malayalam") {
    form.append("language", "ml");
  }

  const response = await fetch("https://api.openai.com/v1/audio/transcriptions", {
    method: "POST",
    headers: { Authorization: `Bearer ${API_KEY}` },
    body: form
  });
  return (await readOpenAiResponse(response)).text || "";
}

async function readArtworkInformation(targetName) {
  if (!targetName) return "Focus on traditional cultural values.";
  const cleanTarget = targetName.trim();
  const docPath = path.join(RAG_DOCS_DIR, `${cleanTarget}.md`);

  if (existsSync(docPath)) {
    try {
      const content = await readFile(docPath, "utf8");
      if (content && content.trim()) return content.trim();
    } catch (e) {
      console.warn(`Could not read ${docPath}:`, e.message);
    }
  }

  return culturalKnowledgeBase[cleanTarget] || "Focus on traditional cultural values and metaphors.";
}

async function createNpcReply(transcript, body) {
  const npcName = body.npcName || "Cultural Guide NPC";
  const targetName = body.targetName || "Culture";
  const context = body.context || "";
  const artworkInformation = await readArtworkInformation(targetName);

  const userPrompt = [
    `NPC name: ${npcName}`,
    `Current AR target image: ${targetName}`,
    context ? `Lesson context: ${context}` : "",
    "Artwork background knowledge:", artworkInformation,
    `Student question: ${transcript}`,
    "INSTRUCTION: Reply in the EXACT SAME LANGUAGE as the student's question (Vietnamese, Malayalam, Traditional Chinese, or English). Make sure the cultural explanation is engaging, accurate, and under 75 words."
  ].filter(Boolean).join("\n");

  const userContent = [{ type: "text", text: userPrompt }];

  if (body.imageBase64) {
    const mime = body.imageMimeType || "image/jpeg";
    userContent.push({
      type: "image_url",
      image_url: { url: `data:${mime};base64,${body.imageBase64}` }
    });
  }

  const response = await fetch("https://api.openai.com/v1/chat/completions", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${API_KEY}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      model: CHAT_MODEL,
      temperature: 0.3,
      messages: [
        { role: "system", content: systemPrompt },
        { role: "user", content: userContent }
      ]
    })
  });

  const data = await readOpenAiResponse(response);
  return extractResponseText(data).trim();
}

function isMalayalamText(text, preferredLang) {
  if (preferredLang === "ml" || preferredLang === "Malayalam") return true;
  return /[\u0D00-\u0D7F]/.test(text || "");
}

async function synthesizeSpeech(text, preferredLang) {
  const isMl = isMalayalamText(text, preferredLang);

  // 1. Dành riêng cho tiếng Malayalam: Dùng Google TTS Malayalam giọng bản địa chuẩn 100%
  if (isMl) {
    try {
      const url = `https://translate.google.com/translate_tts?ie=UTF-8&q=${encodeURIComponent(text)}&tl=ml&client=tw-ob`;
      const response = await fetch(url, {
        headers: { "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)" }
      });

      if (response.ok) {
        const fileName = `${crypto.randomUUID()}.mp3`;
        await writeFile(path.join(AUDIO_DIR, fileName), Buffer.from(await response.arrayBuffer()));
        return `/audio/${fileName}`;
      }
    } catch (err) {
      console.warn("Google TTS fallback error, switching to OpenAI TTS:", err.message);
    }
  }

  // 2. Dành cho Tiếng Việt, Tiếng Trung, Tiếng Anh: Dùng OpenAI TTS
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
  if (!response.ok) throw new Error(`OpenAI TTS error ${response.status}: ${await response.text()}`);

  const fileName = `${crypto.randomUUID()}.mp3`;
  await writeFile(path.join(AUDIO_DIR, fileName), Buffer.from(await response.arrayBuffer()));
  return `/audio/${fileName}`;
}

function extractResponseText(data) {
  if (data.choices && data.choices.length > 0 && data.choices[0].message) {
    return data.choices[0].message.content;
  }
  return "Error: Could not parse AI response.";
}

async function serveAudio(pathname, res) {
  const filePath = path.join(AUDIO_DIR, path.basename(pathname));
  if (!existsSync(filePath)) return sendJson(res, 404, { error: "Audio file not found" });
  const [fileStat, bytes] = await Promise.all([stat(filePath), readFile(filePath)]);
  res.writeHead(200, { "Content-Type": "audio/mpeg", "Content-Length": fileStat.size, "Cache-Control": "no-store" });
  res.end(bytes);
}

async function readOpenAiResponse(response) {
  const text = await response.text();
  let data;
  try { data = JSON.parse(text); } catch { data = { raw: text }; }
  if (!response.ok) throw new Error(`OpenAI API error ${response.status}: ${text}`);
  return data;
}

async function readJsonBody(req) {
  const chunks = [];
  let total = 0;
  for await (const chunk of req) {
    total += chunk.length;
    if (total > MAX_BODY_BYTES) throw new Error("Request body is too large");
    chunks.push(chunk);
  }
  return JSON.parse(Buffer.concat(chunks).toString("utf8") || "{}");
}

function sendJson(res, status, data) {
  res.writeHead(status, { "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" });
  res.end(JSON.stringify(data));
}

function setCorsHeaders(res) {
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET,POST,OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type");
}

function requireApiKey() {
  if (!API_KEY) throw new Error("OPENAI_API_KEY is not set on the server.");
}