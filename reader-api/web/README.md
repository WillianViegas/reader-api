# Reader — Frontend Web (Step 8)

Frontend estático leve (HTML/CSS/JS puro, sem build) que consome apenas a Reader API.

## Estrutura

| Arquivo | Responsabilidade |
| --- | --- |
| `index.html` | Shell da SPA e views (auth, biblioteca, detalhe, leitor) |
| `styles.css` | Tema "Dark Immersive" (baseado no exemplo de referência) |
| `app.js` | Cliente `ReaderApiClient`, estado e navegação |

## Rodando localmente

**Opção A — servido pela própria API (recomendado):**

A Reader API serve esta pasta `web/` como arquivos estáticos na raiz do site (configurado em `Program.cs`).

1. Suba o PostgreSQL: `docker compose up -d postgres` (na pasta da solução).
2. Suba a API: `dotnet run --project reader-api` (a partir da pasta da solução).
3. Abra `http://localhost:5094` — o frontend é servido pela própria API e já usa a mesma origem.

**Opção B — servidor estático separado:**

1. Suba a API em Development (`dotnet run --project reader-api`).
2. Sirva esta pasta: `python -m http.server 8000 --directory web` (ou `npx serve web`).
3. Abra `http://localhost:8000`.

## Configuração da URL da API

A URL da Reader API é definida por ambiente, nesta ordem de prioridade:

1. `window.READER_API_URL = '...'` (definido antes de carregar `app.js`)
2. `<meta name="reader-api-url" content="...">` no `index.html`
3. Padrão: `window.location.origin` (mesma origem — quando a API serve o frontend)

Quando servido em origem separada, ajuste `Cors:AllowedOrigins` em `reader-api/appsettings.Development.json` para a origem do frontend.

Ajuste `Cors:AllowedOrigins` em `reader-api/appsettings.Development.json` para a origem onde o frontend é servido.

## Segurança

- O token JWT é armazenado em `localStorage` (`reader.token`). Esta é uma decisão de segurança documentada: em produção, prefira cookie seguro (`HttpOnly; Secure; SameSite`) de sessão quando a estratégia de autenticação permitir, para reduzir exposição a XSS.
- O token nunca é colocado em URLs.

## Comportamento do leitor

- Renderiza apenas as URLs de página devolvidas pela Reader API (MangaDex@Home via backend).
- Registra progresso com debounce (800 ms) ao trocar de página, e novamente ao pausar/ocultar/sair da tela.
- Atalhos: `←`/`↑` anterior, `→`/`↓` próxima, `Esc` voltar, `Z` zoom.
- PDFs importados usam o mesmo leitor dos mangás: páginas renderizadas com PDF.js (carregado do CDN jsDelivr), navegação, zoom, barra de progresso, OCR/tradução, progresso salvo e "Continuar lendo".

## Importação de PDFs

- As opções de importação só aparecem para usuários autenticados (botão "+ Importar PDF" na Biblioteca; também aceita arrastar e soltar).
- A biblioteca aceita PDFs de livros, mangás e HQs de até 100 MB.
- O navegador gera a capa (primeira página, JPEG de até 2 MB) e a envia junto com o PDF; se a geração falhar, o PDF é importado sem capa.
- O título é informado no formulário de importação; o nome do arquivo é usado como sugestão inicial.
- A API armazena os arquivos em um bucket Amazon S3 privado e os entrega apenas após autenticação e validação de propriedade.
- No Docker Compose de desenvolvimento, o LocalStack 4.4 inicia junto com a API e cria automaticamente o bucket `reader-pdfs`. Execute `docker compose up --build`; a porta S3 local é `4566`. Configure outro nome com `PDF_STORAGE_BUCKET` e, se necessário, `AWS_REGION`.
- Para conectar a API fora do Docker ao LocalStack, configure `PdfStorage__ServiceUrl=http://localhost:4566`, `PdfStorage__BucketName=reader-pdfs`, `AWS_ACCESS_KEY_ID=test` e `AWS_SECRET_ACCESS_KEY=test`.
- Em produção, omita `PdfStorage__ServiceUrl` e use a cadeia padrão de credenciais do AWS SDK, preferencialmente uma IAM role. O principal AWS da aplicação precisa de `s3:PutObject`, `s3:GetObject` e `s3:DeleteObject` no bucket/prefixo usado pela aplicação; não grave chaves AWS no repositório.
