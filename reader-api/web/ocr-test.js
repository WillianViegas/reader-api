import { isSelectableOcrWord, recognizePageText, resolveOcrLanguage } from './ocr-engine.js';
import { translateText } from './translator.js';

const $ = (id) => document.getElementById(id);
let selectedImageUrl = null;
let requestId = 0;
let selectionStart = null;
let translationRequestId = 0;

function showLines(lines, text = lines.map((line) => line.text).join('\n')) {
  $('selected-text').value = text;
  $('translation-result').value = '';
  $('translation-status').textContent = '';
  const words = lines.flatMap((line) => line.words || []);
  $('word-list').replaceChildren(...words
    .filter(isSelectableOcrWord)
    .map((word) => {
      const button = document.createElement('button');
      button.className = 'word';
      button.type = 'button';
      button.textContent = word.text.trim();
      button.addEventListener('click', () => {
        $('selected-text').value = button.textContent;
        $('translation-result').value = '';
        $('translation-status').textContent = '';
        $('word-list').querySelectorAll('.is-selected').forEach((item) => item.classList.remove('is-selected'));
        button.classList.add('is-selected');
      });
      return button;
    }));
}

function clearTranslation() {
  translationRequestId++;
  $('translate-button').disabled = false;
  $('translation-result').value = '';
  $('translation-status').textContent = '';
}

function syncTranslationSource() {
  const sourceForOcr = { eng: 'en', ita: 'it', por: 'pt-BR' }[$('ocr-language').value];
  if (!sourceForOcr) return;

  $('source-language').value = sourceForOcr;
  if ($('target-language').value === sourceForOcr) {
    $('target-language').value = ['en', 'it', 'pt-BR'].find((language) => language !== sourceForOcr);
  }
  clearTranslation();
}

async function translateSelection() {
  const button = $('translate-button');
  const currentRequest = ++translationRequestId;
  button.disabled = true;
  $('translation-result').value = '';
  $('translation-status').textContent = 'Traduzindo…';

  try {
    const translated = await translateText(
      $('selected-text').value,
      $('source-language').value,
      $('target-language').value,
    );
    if (currentRequest !== translationRequestId) return;
    $('translation-result').value = translated;
    $('translation-status').textContent = '';
  } catch (error) {
    if (currentRequest === translationRequestId) $('translation-status').textContent = error.message;
  } finally {
    if (currentRequest === translationRequestId) button.disabled = false;
  }
}

function drawLines(image, lines) {
  const regions = $('ocr-regions');
  const scaleX = image.clientWidth / image.naturalWidth;
  const scaleY = image.clientHeight / image.naturalHeight;
  regions.replaceChildren(...lines.map((line) => {
    const button = document.createElement('button');
    button.className = 'region';
    button.type = 'button';
    button.title = line.text;
    button.setAttribute('aria-label', `Texto reconhecido: ${line.text}`);
    button.style.left = `${line.bbox.x0 * scaleX}px`;
    button.style.top = `${line.bbox.y0 * scaleY}px`;
    button.style.width = `${Math.max(8, (line.bbox.x1 - line.bbox.x0) * scaleX)}px`;
    button.style.height = `${Math.max(8, (line.bbox.y1 - line.bbox.y0) * scaleY)}px`;
    button.addEventListener('click', () => {
      regions.querySelectorAll('.is-active').forEach((region) => region.classList.remove('is-active'));
      button.classList.add('is-active');
      showLine(line);
    });
    return button;
  }));
}

function showLine(line) {
  showLines([line]);
}

async function recognizeSelectedImage() {
  const image = $('test-image');
  if (!image.src) return;
  const currentRequest = ++requestId;
  const language = resolveOcrLanguage($('ocr-language').value);
  $('ocr-status').textContent = 'Preparando OCR…';
  $('result-count').textContent = '';
  $('selected-text').value = '';
  $('word-list').replaceChildren();
  $('ocr-regions').replaceChildren();

  try {
    const lines = await recognizePageText(image, language, (message) => {
      if (currentRequest !== requestId || !message.status) return;
      const progress = Number.isFinite(message.progress) ? ` ${Math.round(message.progress * 100)}%` : '';
      $('ocr-status').textContent = `${message.status}${progress}`;
    });
    if (currentRequest !== requestId) return;
    drawLines(image, lines);
    $('result-count').textContent = `${lines.length} linhas`;
    $('ocr-status').textContent = lines.length ? 'Toque sobre uma linha para ver o texto.' : 'Nenhum texto reconhecido.';
  } catch (error) {
    if (currentRequest === requestId) {
      $('ocr-status').textContent = `OCR indisponível: ${error.message}`;
    }
  }
}

function imagePoint(event) {
  const rect = $('test-image').getBoundingClientRect();
  return {
    x: Math.max(0, Math.min(rect.width, event.clientX - rect.left)),
    y: Math.max(0, Math.min(rect.height, event.clientY - rect.top)),
  };
}

function updateSelectionBox(point) {
  const box = $('selection-box');
  const left = Math.min(selectionStart.x, point.x);
  const top = Math.min(selectionStart.y, point.y);
  box.hidden = false;
  box.style.left = `${left}px`;
  box.style.top = `${top}px`;
  box.style.width = `${Math.abs(point.x - selectionStart.x)}px`;
  box.style.height = `${Math.abs(point.y - selectionStart.y)}px`;
}

function selectRectangle(event) {
  const start = imagePoint(event);
  selectionStart = { ...start, pointerId: event.pointerId };
  $('image-wrap').setPointerCapture(event.pointerId);
  updateSelectionBox(start);
}

async function recognizeSelection(rectangle) {
  const image = $('test-image');
  const scaleX = image.naturalWidth / image.clientWidth;
  const scaleY = image.naturalHeight / image.clientHeight;
  const left = Math.round(rectangle.left * scaleX);
  const top = Math.round(rectangle.top * scaleY);
  const width = Math.round(rectangle.width * scaleX);
  const height = Math.round(rectangle.height * scaleY);
  if (width < 12 || height < 12) {
    $('ocr-status').textContent = 'Selecione uma região um pouco maior.';
    return;
  }

  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  canvas.getContext('2d').drawImage(image, left, top, width, height, 0, 0, width, height);
  const crop = new Image();
  crop.src = canvas.toDataURL('image/png');
  await crop.decode();

  const currentRequest = ++requestId;
  const language = resolveOcrLanguage($('ocr-language').value);
  $('ocr-status').textContent = 'Reconhecendo região selecionada…';
  $('result-count').textContent = '';
  $('selected-text').value = '';
  $('word-list').replaceChildren();
  $('ocr-regions').replaceChildren();

  try {
    const croppedLines = await recognizePageText(crop, language, (message) => {
      if (currentRequest !== requestId || !message.status) return;
      const progress = Number.isFinite(message.progress) ? ` ${Math.round(message.progress * 100)}%` : '';
      $('ocr-status').textContent = `${message.status}${progress}`;
    });
    if (currentRequest !== requestId) return;

    const lines = croppedLines.map((line) => ({
      ...line,
      bbox: {
        x0: line.bbox.x0 + left,
        y0: line.bbox.y0 + top,
        x1: line.bbox.x1 + left,
        y1: line.bbox.y1 + top,
      },
    }));
    drawLines(image, lines);
    showLines(lines);
    $('result-count').textContent = `${lines.length} linhas no recorte`;
    $('ocr-status').textContent = lines.length ? 'Revise ou corrija o texto reconhecido.' : 'Nenhum texto reconhecido nessa região.';
  } catch (error) {
    if (currentRequest === requestId) $('ocr-status').textContent = `OCR indisponível: ${error.message}`;
  }
}

function finishSelection(event) {
  if (!selectionStart || event.pointerId !== selectionStart.pointerId) return;
  const end = imagePoint(event);
  const rectangle = {
    left: Math.min(selectionStart.x, end.x),
    top: Math.min(selectionStart.y, end.y),
    width: Math.abs(end.x - selectionStart.x),
    height: Math.abs(end.y - selectionStart.y),
  };
  selectionStart = null;
  recognizeSelection(rectangle);
}

$('image-file').addEventListener('change', async (event) => {
  const file = event.target.files?.[0];
  if (!file) return;
  if (!file.type.startsWith('image/')) {
    $('ocr-status').textContent = 'Selecione um arquivo de imagem.';
    return;
  }

  if (selectedImageUrl) URL.revokeObjectURL(selectedImageUrl);
  selectedImageUrl = URL.createObjectURL(file);
  $('empty-state').hidden = true;
  $('image-wrap').hidden = false;
  $('test-image').src = selectedImageUrl;
  $('test-image').onload = recognizeSelectedImage;
  $('ocr-status').textContent = 'Carregando imagem…';
});

$('ocr-language').addEventListener('change', () => {
  syncTranslationSource();
  recognizeSelectedImage();
});
$('selected-text').addEventListener('input', clearTranslation);
$('source-language').addEventListener('change', clearTranslation);
$('target-language').addEventListener('change', clearTranslation);
$('translate-button').addEventListener('click', translateSelection);
$('swap-languages').addEventListener('click', () => {
  const source = $('source-language');
  const target = $('target-language');
  const currentSource = source.value;
  source.value = target.value;
  target.value = currentSource;
  clearTranslation();
});

$('selection-toggle').addEventListener('click', (event) => {
  const enabled = event.currentTarget.getAttribute('aria-pressed') !== 'true';
  event.currentTarget.setAttribute('aria-pressed', String(enabled));
  $('image-wrap').classList.toggle('is-selecting', enabled);
  $('ocr-status').textContent = enabled
    ? 'Arraste sobre um balão ou caixa de texto.'
    : 'Toque sobre uma linha para ver o texto.';
});

$('image-wrap').addEventListener('pointerdown', (event) => {
  if (!$('image-wrap').classList.contains('is-selecting') || event.button > 0) return;
  event.preventDefault();
  selectRectangle(event);
});
$('image-wrap').addEventListener('pointermove', (event) => {
  if (!selectionStart || event.pointerId !== selectionStart.pointerId) return;
  updateSelectionBox(imagePoint(event));
});
$('image-wrap').addEventListener('pointerup', finishSelection);
$('image-wrap').addEventListener('pointercancel', () => {
  selectionStart = null;
  $('selection-box').hidden = true;
});

window.addEventListener('beforeunload', () => {
  if (selectedImageUrl) URL.revokeObjectURL(selectedImageUrl);
});