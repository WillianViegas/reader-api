const OCR_SCRIPT_URL = 'https://cdn.jsdelivr.net/npm/tesseract.js@5.1.1/dist/tesseract.min.js';
const OCR_LANGUAGE_CODES = {
  ar: 'ara', de: 'deu', en: 'eng', es: 'spa', fr: 'fra', it: 'ita', ja: 'jpn',
  ko: 'kor', pl: 'pol', 'pt-br': 'por', pt: 'por', ru: 'rus', zh: 'chi_sim',
};

const workers = new Map();
let scriptPromise = null;
const MIN_WORD_CONFIDENCE = 50;

export function resolveOcrLanguage(language) {
  const normalized = (language || 'en').toLowerCase();
  return OCR_LANGUAGE_CODES[normalized] || OCR_LANGUAGE_CODES[normalized.split('-')[0]] || 'eng';
}

function loadTesseract() {
  if (window.Tesseract) return Promise.resolve(window.Tesseract);
  if (!scriptPromise) {
    scriptPromise = new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = OCR_SCRIPT_URL;
      script.async = true;
      script.onload = () => window.Tesseract
        ? resolve(window.Tesseract)
        : reject(new Error('A biblioteca de OCR não foi carregada.'));
      script.onerror = () => {
        scriptPromise = null;
        reject(new Error('Não foi possível baixar a biblioteca de OCR.'));
      };
      document.head.append(script);
    });
  }
  return scriptPromise;
}

async function getWorker(language, logger) {
  if (!workers.has(language)) {
    const workerPromise = loadTesseract().then((tesseract) => tesseract.createWorker(language, 1, {
      langPath: 'https://tessdata.projectnaptha.com/4.0.0',
      logger,
    }));
    workers.set(language, workerPromise);
    workerPromise.catch(() => workers.delete(language));
  }
  return workers.get(language);
}

export async function recognizePageText(image, language, logger = () => {}) {
  const worker = await getWorker(language, logger);
  const { data } = await worker.recognize(image);
  return (data.lines || [])
    .filter((line) => line.text?.trim() && line.bbox)
    .map((line) => ({ text: line.text.trim(), bbox: line.bbox, words: line.words || [] }));
}

export function isSelectableOcrWord(word) {
  const text = word?.text?.trim() || '';
  if (!/[\p{L}\p{N}]/u.test(text)) return false;
  return typeof word.confidence !== 'number' || word.confidence >= MIN_WORD_CONFIDENCE;
}