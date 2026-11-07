# Contrato da Reader API para o app Flutter

Documento de referência para construir o app Flutter em um projeto separado. O contrato foi extraído dos controllers em `reader-api/Controllers`, dos DTOs em `Application/Dtos/ReaderDtos.cs` e do cliente web em `web/app.js`. Se algo divergir, o código da API é a fonte de verdade; o OpenAPI fica em `GET /openapi/v1.json` (Swagger UI em `/swagger`).

> Os documentos `docs/tasks/08-flutter-app.md` e `docs/tasks/flutter/*` descrevem outro produto (Firebase, casal, convites) e **não** se aplicam a esta API.

## 1. Convenções gerais

| Item | Valor |
| --- | --- |
| Base URL | Configurável (`API_BASE_URL`). Produção: URL do serviço no Render. Dev: `http://10.0.2.2:5094` (emulador Android) ou `http://localhost:5094`. |
| Formato | JSON (`camelCase`), exceto o upload (`multipart/form-data`) e os binários (PDF, JPEG, imagens). |
| Enums | Serializados como **texto**: `"MangaDex"` ou `"UploadedPdf"`. |
| Datas | ISO 8601 com offset (`DateTimeOffset`). |
| IDs | Biblioteca/progresso/vocabulário: GUID. Mangá/capítulo do MangaDex: string (UUID). PDF: GUID. |
| Autenticação | `Authorization: Bearer <accessToken>`. |
| Paginação | Resposta `{ items, page, pageSize, totalCount }`. Query: `page` (1-based) e `pageSize` (máximo efetivo 100). |
| Cold start | O plano free do Render hiberna o serviço. A primeira chamada pode levar dezenas de segundos: use timeout de conexão longo (≥ 60 s), indicador de carregamento e "tentar novamente". |

### Rotas públicas e protegidas

- **Públicas:** `/api/auth/*` e `/api/catalog/*`.
- **Protegidas (JWT):** `/api/users/*`, `/api/library/*`, `/api/vocabulary/*`.

## 2. Autenticação

O token é um JWT de curta duração (padrão: 60 minutos). **Não existe refresh token**: ao receber `401`, limpe a sessão e volte ao login.

| Método | Rota | Corpo | Resposta |
| --- | --- | --- | --- |
| `POST` | `/api/auth/login` | `{ "email", "password" }` | `200 { "accessToken" }`; `401` sem corpo se credenciais inválidas. |
| `POST` | `/api/auth/register` | `{ "email", "password", "displayName" }` | `201 { "accessToken" }`; `400 { "message" }` (senha < **8** caracteres ou dados inválidos); `409 { "message" }` (e-mail já cadastrado). |
| `GET` | `/api/users/me` | — | `200 { id, email, displayName, createdAt, updatedAt }`; `401`. |

Regras para o app:

- A senha exige **no mínimo 8 caracteres** (o guia de login anterior citava 6).
- O cadastro já devolve token: não é necessário chamar o login em seguida.
- Fluxo de abertura: ler o token salvo → `GET /api/users/me` → sucesso abre a área logada; falha limpa o token e abre o login.
- Armazene o token com `flutter_secure_storage` (mobile). Nunca grave a senha nem registre token em logs.
- **Não há** endpoint para editar perfil, avatar ou trocar senha. Por ora a tela de perfil é somente leitura.

## 3. Formato de erros

| Origem | Formato |
| --- | --- |
| Falhas de domínio/aplicação | `application/problem+json`: `{ status, title, detail, instance }`. |
| Validação de DTO | `ValidationProblemDetails`: `{ status: 400, title, errors: { campo: [mensagens] } }`. |
| `/api/auth/register` | `{ "message": "..." }` (não é ProblemDetails). |
| `/api/auth/login` | `401` sem corpo. |

| Status | Quando | Tratamento sugerido |
| --- | --- | --- |
| `400` | Validação, regra de domínio | Mostrar `detail` / `errors`. |
| `401` | Token ausente, inválido ou expirado | Limpar sessão e ir ao login. |
| `404` | Recurso inexistente; em `continue-reading`, "nada para continuar" | Não tratar como falha em `continue-reading`. |
| `409` | Item já está na biblioteca; e-mail já cadastrado | Mensagem amigável. |
| `503` | MangaDex indisponível, host de imagem com falha ou resposta de páginas vazia/incompleta | Permitir tentar novamente; a API aplica retries limitados a falhas transitórias e respeita `Retry-After`. |
| `500` | Erro inesperado (`detail` vem vazio) | Mensagem genérica. |

Extraia a mensagem nesta ordem: `detail` → `title` → `message` → `"Erro {status}"`.

## 4. Modelos (DTOs)

```jsonc
// MangaReference (usado em biblioteca e continue-reading)
{ "provider": "MangaDex|UploadedPdf", "externalId": "string", "title": "string", "coverUrl": "string|null", "originalLanguage": "string|null" }

// MangaSummary (resultado de busca)
{ "provider": "MangaDex", "id": "string", "title": "string", "coverUrl": "string|null", "originalLanguage": "string|null" }

// MangaDetails
{ "provider": "MangaDex", "id": "string", "title": "string", "coverUrl": "string|null", "originalLanguage": "string|null", "description": "string|null" }

// CatalogTag
{ "id": "string", "name": "string", "group": "string" }

// ChapterSummary
{ "provider": "MangaDex", "id": "string", "language": "string", "title": "string|null", "volume": "string|null", "number": "string|null", "publishedAt": "datetime|null" }

// ChapterReference (corpo do progresso de mangá)
{ "provider": "MangaDex|UploadedPdf", "externalId": "string", "language": "string", "title": "string|null", "volume": "string|null", "number": "string|null" }

// ChapterPages
{ "provider": "MangaDex", "chapterId": "string", "pageUrls": ["https://..."] }

// ReadingProgress
{ "id": "guid", "chapter": ChapterReference, "currentPage": 1, "pageCount": 20, "lastReadAt": "datetime", "completedAt": "datetime|null" }

// LibraryItem
{ "id": "guid", "manga": MangaReference, "isFavorite": false, "isArchived": false, "createdAt": "datetime", "updatedAt": "datetime", "progresses": [ReadingProgress] }

// ContinueReading
{ "libraryItemId": "guid", "manga": MangaReference, "isArchived": false, "progress": ReadingProgress }

// SavedVocabularyTranslation
{ "id": "guid", "originalText": "", "translatedText": "", "sourceLanguage": "en|it|pt-BR", "targetLanguage": "en|it|pt-BR",
  "contextText": "string|null", "mangaExternalId": "string|null", "mangaTitle": "string|null",
  "chapterExternalId": "string|null", "chapterTitle": "string|null", "pageNumber": 1, "savedAt": "datetime" }
```

`pageCount` é nulo quando desconhecido (`int?`). `currentPage` é **1-based**.

## 5. Endpoints

### 5.1 Catálogo (público)

| Método | Rota | Descrição |
| --- | --- | --- |
| `GET` | `/api/catalog/manga?title&category&tagIds&page&pageSize` | Busca. `tagIds` pode repetir (`tagIds=a&tagIds=b`). Retorna `Paged<MangaSummary>`. |
| `GET` | `/api/catalog/tags` | Lista de tags para filtro. |
| `GET` | `/api/catalog/manga/{mangaId}` | Detalhes. `404` se inexistente. |
| `GET` | `/api/catalog/manga/{mangaId}/chapters?page&pageSize` | Capítulos (ordenados por volume e número) cujo feed declara páginas e que não têm `externalUrl`. Essa descoberta não consulta o host de cada capítulo; a disponibilidade real é validada ao carregar as páginas. |
| `GET` | `/api/catalog/chapters/{chapterId}/pages` | `ChapterPages` com URLs absolutas das imagens (MangaDex@Home). Retorna `404` se o capítulo não existir e `503 ProblemDetails` se a resposta não tiver páginas, estiver incompleta ou indicar host inválido. |
| `GET` | `/api/catalog/chapters/{chapterId}/pages/{pageIndex}` | Imagem da página (índice 0-based), servida pela API. Use esta rota para exibir e para OCR, pois evita depender do host de imagens externo. `Cache-Control: private, max-age=300`. Retorna `404 ProblemDetails` para capítulo/índice inexistente e `503 ProblemDetails` se o host não entregar a imagem após retries limitados. |
| `GET` | `/api/catalog/covers/{mangaId}/{fileName}` | Proxy de capas (cache de 1 dia). |

### 5.2 Biblioteca (JWT)

| Método | Rota | Corpo | Resposta |
| --- | --- | --- | --- |
| `GET` | `/api/library?favoriteOnly&archivedOnly&page&pageSize` | — | `Paged<LibraryItem>`. Inclui mangás **e** PDFs (`provider`). Por padrão (`archivedOnly=false`) exclui arquivados; `archivedOnly=true` lista somente arquivados. Filtros são aplicados antes de `page` e `totalCount`. |
| `POST` | `/api/library` | `{ "manga": MangaReference, "isFavorite": false }` | `201 LibraryItem`; `409` se já existe. |
| `DELETE` | `/api/library/{mangaId}` | — | `204`; `404`. Remove mangá do MangaDex. |
| `PUT` | `/api/library/{mangaId}/favorite` | `{ "isFavorite": true }` | `200 LibraryItem`; `404`. |
| `PUT` | `/api/library/{mangaId}/archive` | `{ "isArchived": true }` ou `{ "isArchived": false }` | `200 LibraryItem` atualizado; `404` se não estiver na biblioteca do usuário. Arquivar preserva progresso e favoritos. |
| `GET` | `/api/library/continue-reading` | — | `200 ContinueReading` (sempre `isArchived=false`) ou `404` (nada em andamento; inclui o caso de todas as leituras estarem arquivadas). |
| `GET` | `/api/library/{mangaId}/progress` | — | `200 [ReadingProgress]`. |
| `PUT` | `/api/library/{mangaId}/progress` | `{ "chapter": ChapterReference, "currentPage": 1, "pageCount": 20 }` | `200 ReadingProgress`; `400`, `404`. |
| `POST` | `/api/library/{mangaId}/chapters/{chapterId}/complete` | — | `204`; `404`. |

### 5.3 PDFs importados (JWT)

| Método | Rota | Corpo | Resposta |
| --- | --- | --- | --- |
| `POST` | `/api/library/pdfs` | `multipart/form-data` (ver seção 6) | `201 LibraryItem` (`provider: "UploadedPdf"`, `manga.externalId` = id do PDF). |
| `GET` | `/api/library/pdfs/{pdfId}` | — | `200 application/pdf`; `404`. |
| `GET` | `/api/library/pdfs/{pdfId}/cover` | — | `200 image/jpeg` (cache privado de 1 dia); `404`. |
| `DELETE` | `/api/library/pdfs/{pdfId}` | — | `204`; `404`. Remove também o arquivo e a capa do S3. |
| `PUT` | `/api/library/pdfs/{pdfId}/archive` | `{ "isArchived": true }` ou `{ "isArchived": false }` | `200 LibraryItem` atualizado; `404` se o PDF não estiver na biblioteca do usuário. Arquivar não apaga o arquivo, a capa nem o progresso. |
| `GET` | `/api/library/pdfs/{pdfId}/progress` | — | `200 [ReadingProgress]`. |
| `PUT` | `/api/library/pdfs/{pdfId}/progress` | `{ "currentPage": 1, "pageCount": 120 }` (ambos ≥ 1) | `200 ReadingProgress`; `404`. |

Notas:

- Os PDFs são privados: só o dono acessa, e sempre com Bearer. Para exibir capas e PDFs, baixe com o cabeçalho de autorização (não use URL direta sem token).
- Para itens `UploadedPdf`, use as rotas `/pdfs/...`. As rotas de mangá do MangaDex (`/{mangaId}/...`) são só para `MangaDex`.
- O `chapter.externalId` do progresso de PDF é o próprio `pdfId`.

### 5.4 Vocabulário (JWT)

| Método | Rota | Corpo | Resposta |
| --- | --- | --- | --- |
| `GET` | `/api/vocabulary?page&pageSize` | — | `Paged<SavedVocabularyTranslation>`. `pageSize` de 1 a 100 (padrão 50). |
| `POST` | `/api/vocabulary` | Ver abaixo | `201 SavedVocabularyTranslation`; `400` (validação). |
| `GET` | `/api/vocabulary/{id}` | — | `200`; `404`. |
| `DELETE` | `/api/vocabulary/{id}` | — | `204`; `404`. |

Validação do `POST`:

| Campo | Regra |
| --- | --- |
| `originalText` | Obrigatório, até 500 caracteres. |
| `translatedText` | Obrigatório, até 2000 caracteres. |
| `sourceLanguage`, `targetLanguage` | Obrigatórios e somente `en`, `it` ou `pt-BR` (diferencia maiúsculas). |
| `contextText` | Opcional, até 500. |
| `mangaExternalId`, `chapterExternalId` | Opcionais, até 200. |
| `mangaTitle`, `chapterTitle` | Opcionais, até 500. |
| `pageNumber` | Opcional, ≥ 1. |

## 6. Upload de PDF

`POST /api/library/pdfs` como `multipart/form-data`:

| Campo | Tipo | Regra |
| --- | --- | --- |
| `title` | texto | Obrigatório, até 500 caracteres. Sugestão: nome do arquivo sem extensão. |
| `file` | arquivo | Obrigatório. Maior que 0 e até **100 MB**. Os 5 primeiros bytes devem ser `%PDF-`. |
| `cover` | arquivo | Opcional. JPEG (`FF D8 FF`), maior que 0 e até **2 MB**. |

Comportamento esperado no app:

1. Selecionar o PDF com `file_picker`. Validar localmente extensão `.pdf` e tamanho ≤ 100 MB **antes** de enviar.
2. Gerar a capa a partir da primeira página (largura ~400 px, JPEG qualidade ~0,8, limite 2 MB). Se falhar, importar sem capa.
3. Enviar com `dio` e `FormData`, exibindo progresso (`onSendProgress`), com opção de cancelar e timeout de envio generoso.
4. Tratar `400` (tamanho/assinatura/capa inválidos) mostrando `detail`.
5. Após `201`, adicionar o item à biblioteca local e abrir o leitor se desejado.

O servidor limita a requisição a ~103 MB. O servidor grava o PDF em `users/{userId}/pdfs/{pdfId}.pdf` e a capa em `.jpg` no mesmo prefixo (bucket S3 privado). O app **não** acessa o S3 diretamente.

## 7. Leitor

### Mangá (MangaDex)

1. Listar capítulos (`pageSize` até 100) e filtrar/ordenar por idioma.
2. Ao abrir o capítulo, obter o número de páginas com `GET /catalog/chapters/{id}/pages` (`pageUrls.length`).
3. Exibir cada página por `GET /catalog/chapters/{id}/pages/{i}` ou pelas `pageUrls`.
4. Pré-carregar a página seguinte e liberar páginas distantes da memória.
5. Retomar do progresso salvo (`currentPage - 1`).

### PDF

1. Baixar `GET /library/pdfs/{id}` com Bearer (cachear em disco; invalidar ao excluir).
2. Renderizar com `pdfrx` ou `pdfx`. Manter cache de páginas num raio pequeno (o web usa um raio curto).
3. Retomar do progresso salvo.

### Progresso (ambos)

- Salvar com **debounce de 800 ms** ao trocar de página e também ao pausar, ocultar ou sair do leitor.
- Mangá: `PUT /library/{mangaId}/progress` com o `chapter` completo, `currentPage` (1-based) e `pageCount`. Ao chegar na última página, chame também `POST .../chapters/{chapterId}/complete`.
- PDF: `PUT /library/pdfs/{id}/progress` com `currentPage` e `pageCount`.
- Falha ao salvar progresso não deve interromper a leitura: registrar e tentar de novo.
- A tela inicial usa `GET /library/continue-reading` (trate `404` como "nenhum item").

Atalhos de teclado do web (←/→, Esc, Z) só se aplicam a Flutter Web/desktop; no mobile use gestos de toque/deslize e zoom por pinça.

## 8. OCR e tradução

Comportamento do web (`ocr-engine.js`, `translator.js`) que o app deve reproduzir:

1. **OCR por região ou página:** o usuário ativa o modo de seleção, arrasta uma região da página e o app reconhece o texto; cada resultado traz linhas com caixa delimitadora e palavras com confiança.
2. **Palavras selecionáveis:** descarte palavras sem letras/números e com confiança < 50. Mostre o texto reconhecido em campo editável para correção manual.
3. **Idioma do OCR:** derivado do idioma do capítulo (`language`). Tabela do web: `ar→ara, de→deu, en→eng, es→spa, fr→fra, it→ita, ja→jpn, ko→kor, pl→pol, pt/pt-br→por, ru→rus, zh→chi_sim`; padrão `eng`.
4. **Tradução:** o web usa a API MyMemory (`https://api.mymemory.translated.net/get?q=...&langpair=src|dst`), com limite de **500 bytes UTF-8** por trecho e falha se origem = destino. Em Flutter, alternativa offline: Google ML Kit Translation.
5. **Salvar no vocabulário:** `POST /api/vocabulary` com texto original, tradução, idiomas, contexto (linha reconhecida) e, quando houver, mangá, capítulo e página.

Atenção às restrições:

- O vocabulário só aceita `en`, `it` e `pt-BR`. Mapeie `pt`/`pt-br` do capítulo para `pt-BR`; para outros idiomas, a tradução pode funcionar, mas **não poderá ser salva** até a API aceitar mais idiomas (ver seção 10).
- Defina o texto de contexto com até 500 caracteres e o original com até 500 caracteres.

### Bibliotecas sugeridas (confirmar versões atuais)

| Necessidade | Opção |
| --- | --- |
| OCR (latim, japonês, chinês, coreano) | `google_mlkit_text_recognition` (offline, rápido; entrega blocos, linhas e palavras com caixas). |
| OCR para os demais idiomas (ru, ar, pl...) | `flutter_tesseract_ocr` (mesmos dados `tessdata` do web). |
| Tradução | `google_mlkit_translation` (offline) ou MyMemory via `dio`. |
| PDF | `pdfrx` ou `pdfx`. |
| Seleção de arquivo | `file_picker`. |
| HTTP | `dio` com interceptors. |
| Token | `flutter_secure_storage`. |

## 9. Arquitetura e fases sugeridas

```text
lib/
  core/        config (API_BASE_URL), errors (ApiException), networking (dio + interceptors), storage (token)
  data/        models (DTOs tipados), services (auth, catalog, library, vocabulary)
  features/    auth, catalog, library, reader (manga + pdf), upload, ocr, vocabulary, profile
```

- **HTTP:** interceptor que injeta o Bearer, converte erros para uma exceção única (seção 3) e trata `401`.
- **Estado:** escolha uma solução única (Riverpod ou Bloc) e documente no `pubspec.yaml`.
- **Navegação:** `go_router` com redirecionamento conforme a sessão.
- **Modelos:** gerar a partir de `/openapi/v1.json` ou escrever à mão com `json_serializable`; os enums vêm como string.

| Fase | Entrega | Critério de aceite |
| --- | --- | --- |
| 1. Fundação | Projeto, tema, `dio`, config por ambiente, tratamento de erro | Chamada a `/api/catalog/tags` funciona nos ambientes. |
| 2. Autenticação | Login, cadastro, gate de sessão, logout, perfil somente leitura | Sessão persiste após reiniciar; `401` volta ao login. |
| 3. Catálogo e biblioteca | Busca com filtros, detalhes, capítulos, favoritos, "continuar lendo" | Paginação e `409`/`404` tratados. |
| 4. Leitor | Mangá e PDF, zoom, progresso com debounce | Progresso retomado corretamente. |
| 5. Upload de PDF | Seleção, validações, capa, progresso, cancelamento | Regras de tamanho/assinatura respeitadas; erros claros. |
| 6. OCR, tradução e vocabulário | Região, palavras, tradução, salvar, listar, excluir | Texto editável; limites de 500/2000 respeitados. |
| 7. Qualidade | Testes de unidade, widget e integração; revisão de acessibilidade | Fluxos principais cobertos. |

## 10. Pendências e decisões na API

Itens que impactam o app e precisam de decisão (nenhum bloqueia as fases 1 a 5):

1. **Perfil editável:** não existe `PUT /api/users/me`. Definir se haverá edição de nome, troca de senha e avatar.
2. **Refresh de sessão:** o JWT expira em 60 minutos e não há refresh. Decidir entre login frequente, aumentar a duração ou criar refresh token.
3. **Idiomas do vocabulário:** a API restringe a `en`, `it` e `pt-BR`, enquanto o OCR do web cobre mais idiomas. Decidir se amplia a validação.
4. **Paginação de capítulos:** a API limita `pageSize` a 100. Mangás com mais capítulos exigem paginação no app.
5. **CORS (somente Flutter Web):** adicionar a origem do app em `Cors__AllowedOrigins__N`. Apps Android/iOS nativos não usam CORS.
6. **Cold start do Render free:** considerar plano pago ou um endpoint de saúde para "acordar" o serviço na abertura do app.
7. **HTTP em dev (Android):** emulador acessa o host em `10.0.2.2`; para `http` em release, é necessário configurar `usesCleartextTraffic` e, no iOS, ATS. Em produção use HTTPS.

## 11. Checklist de contrato para testes

- [ ] Login/cadastro: `401` sem corpo, `400` e `409` com `{ message }`, senha ≥ 8.
- [ ] `GET /api/users/me` com token inválido retorna `401` e o app limpa a sessão.
- [ ] Enum `provider` aceito e enviado como texto.
- [ ] Busca de catálogo com `tagIds` repetido.
- [ ] `continue-reading` `404` tratado como estado vazio.
- [ ] Upload: rejeita > 100 MB, arquivo vazio, sem `%PDF-`, capa não JPEG ou > 2 MB.
- [ ] Progresso de PDF com `currentPage` e `pageCount` ≥ 1.
- [ ] Vocabulário: validação dos limites e dos idiomas permitidos.
- [ ] Capa e PDF só são acessados com Bearer.
