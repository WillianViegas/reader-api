const MY_MEMORY_ENDPOINT = 'https://api.mymemory.translated.net/get';
const MAX_SEGMENT_BYTES = 500;

export async function translateText(text, sourceLanguage, targetLanguage) {
  const source = text.trim();
  if (!source) throw new Error('Selecione ou digite um texto para traduzir.');
  if (sourceLanguage === targetLanguage) throw new Error('Escolha idiomas diferentes.');
  if (new TextEncoder().encode(source).length > MAX_SEGMENT_BYTES) {
    throw new Error('O trecho excede o limite de 500 bytes do serviço gratuito. Selecione um balão menor.');
  }

  const url = new URL(MY_MEMORY_ENDPOINT);
  url.searchParams.set('q', source);
  url.searchParams.set('langpair', `${sourceLanguage}|${targetLanguage}`);

  const response = await fetch(url);
  const result = await response.json();
  if (!response.ok || result.responseStatus !== 200) {
    throw new Error(result.responseDetails || `O serviço respondeu com HTTP ${response.status}.`);
  }

  const translatedText = result.responseData?.translatedText?.trim();
  if (!translatedText) throw new Error('O serviço não retornou uma tradução para este trecho.');
  return translatedText;
}