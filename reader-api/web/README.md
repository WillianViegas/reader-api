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
