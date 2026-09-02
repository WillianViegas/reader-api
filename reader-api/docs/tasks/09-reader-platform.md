# Reader Platform: plano de implementacao

## Objetivo

Construir a Reader API como backend de uma aplicacao de leitura de manga. O produto permite descobrir obras, manter uma biblioteca pessoal, iniciar a leitura de capitulos e retomar exatamente da pagina mais recente.

A MangaDex e a fonte externa inicial de catalogo, capitulos e paginas. O Flutter e qualquer frontend web devem falar apenas com a Reader API. A integracao com AniList fica explicitamente fora deste plano inicial.

O OpenAPI da MangaDex fornecido como referencia informa as capacidades externas relevantes, especialmente busca de manga, detalhes de manga, feed de capitulos e resolucao de paginas por `GET /at-home/server/{chapterId}`. Ele nao define o modelo do dominio local.

## Decisoes de dominio

### O que e persistido

| Conceito | Decisao | Motivo |
| --- | --- | --- |
| `User` | Agregado persistido | Identifica o dono da biblioteca e do progresso. |
| `LibraryItem` | Agregado persistido | Representa a intencao do usuario de salvar e favoritar uma obra. |
| `ReadingProgress` | Entidade filha persistida de `LibraryItem` | O progresso pertence a um usuario e a uma obra da biblioteca. |
| `MangaReference` | Value Object persistido dentro de `LibraryItem` | Mantem o identificador externo e um snapshot minimo sem transformar o catalogo externo em agregado local. |
| `ChapterReference` | Value Object persistido dentro de `ReadingProgress` | Identifica o capitulo externo lido, sem duplicar a fonte. |
| Metadados e paginas do catalogo | Nao persistir inicialmente | Sao obtidos sob demanda do provedor e podem mudar sem migracoes locais. |

`Manga` e `Chapter` nao devem existir como entidades persistidas nesta primeira versao. Sao recursos de catalogo externos. A biblioteca guarda somente o identificador do provedor e um snapshot de exibicao: titulo principal, idioma/origem quando disponivel, URL da capa e ultima atualizacao do snapshot. Isso deixa a biblioteca utilizavel mesmo se a busca externa estiver temporariamente indisponivel, sem criar sincronizacao, cache e conflitos prematuros.

`LibraryItem` e o limite do agregado. Todas as mutacoes de favorito e progresso ocorrem por ele, garantindo no maximo um item por usuario e manga. O usuario nao precisa carregar toda a biblioteca para registrar progresso.

## Regras de negocio

1. Um usuario pode ter somente um `LibraryItem` por `MangaReference.Provider` e `MangaReference.ExternalId`.
2. Apenas o dono pode listar, alterar ou remover itens da propria biblioteca e seus progressos.
3. Favoritar exige que a obra ja esteja na biblioteca. Remover um item remove seus progressos associados.
4. Um progresso pertence a um unico capitulo de uma unica obra da biblioteca.
5. `CurrentPage` e baseado em um indice humano, iniciando em 1. Ele deve estar entre 1 e `PageCount` quando `PageCount` for conhecido.
6. Ao registrar uma pagina, `LastReadAt` deve ser atualizado em UTC. Um progresso concluido permanece concluido se o cliente enviar novamente a mesma pagina; uma nova leitura de outro capitulo cria ou atualiza o progresso desse capitulo.
7. Um capitulo e concluido quando o usuario marca explicitamente como concluido ou quando chega a ultima pagina conhecida.
8. O item de "continuar lendo" e o progresso nao concluido mais recentemente acessado. Como fallback, use o progresso mais recentemente atualizado.
9. Identificadores externos nao sao UUIDs locais: devem ser validados como strings nao vazias e limitadas; validacao especifica do provedor fica no adaptador de infraestrutura.

## Step 1: estabelecer a fundacao do dominio

**Objetivo:** criar o modelo puro no projeto `Domain`, sem controllers, EF Core, HTTP clients ou dependencias da MangaDex.

Criar as pastas e tipos abaixo, seguindo o namespace `Reader.Api.Domain` ou o namespace adotado pelo repositorio:

- `Entities/User.cs`: `Id`, `ExternalSubject`, `DisplayName`, `CreatedAt` e `UpdatedAt`.
- `Entities/LibraryItem.cs`: identificador local, `UserId`, `MangaReference`, `IsFavorite`, datas de criacao/atualizacao e colecao privada de progressos.
- `Entities/ReadingProgress.cs`: identificador local, `ChapterReference`, `CurrentPage`, `PageCount`, `LastReadAt` e `CompletedAt` opcional.
- `ValueObjects/ExternalResourceId.cs`: combina `Provider` e `Value`, normaliza provider, impede valores vazios e compara por valor.
- `ValueObjects/MangaReference.cs`: `ExternalResourceId`, `Title`, `CoverUrl`, `OriginalLanguage` opcional e `SnapshotUpdatedAt`.
- `ValueObjects/ChapterReference.cs`: `ExternalResourceId`, `Title` opcional, `Volume` opcional, `Number` opcional e `Language`.
- `Enums/ExternalCatalogProvider.cs`: iniciar somente com `MangaDex`.
- `Exceptions/DomainException.cs` e erros especificos, como `InvalidReadingProgressException` e `LibraryItemAlreadyExistsException`.

Metodos que devem viver em `LibraryItem`:

- `SetFavorite(bool isFavorite)`.
- `RegisterProgress(ChapterReference chapter, int currentPage, int? pageCount, DateTimeOffset readAt)`.
- `MarkChapterCompleted(ExternalResourceId chapterId, DateTimeOffset completedAt)`.
- `GetResumeProgress()`.

**Criterios de aceite:** testes unitarios do Domain cobrem duplicidade, limites de pagina, conclusao automatica na ultima pagina, atualizacao de `LastReadAt`, selecao de retomada e remocao logica/fisica definida para o item.

## Step 2: definir portas e casos de uso da Application

**Objetivo:** tornar as regras acessiveis sem conhecer banco, HTTP ou UI.

Criar no projeto `Application` os comandos, queries, DTOs e interfaces.

### Commands

- `RegisterUserCommand` e `AuthenticateUserCommand` para o fluxo inicial de identidade.
- `AddMangaToLibraryCommand` com `UserId`, `MangaReferenceDto` e favorito inicial opcional.
- `RemoveMangaFromLibraryCommand` com `UserId` e `MangaId` externo.
- `SetMangaFavoriteCommand` com `UserId`, `MangaId` externo e valor booleano.
- `RegisterReadingProgressCommand` com usuario, manga, capitulo, pagina atual e total opcional de paginas.
- `CompleteChapterCommand` com usuario, manga e capitulo.

### Queries

- `GetUserProfileQuery`.
- `GetLibraryQuery` com filtros `FavoriteOnly`, pagina e tamanho de pagina.
- `GetContinueReadingQuery`.
- `GetReadingProgressQuery` para uma obra especifica.
- `SearchCatalogQuery`, `GetMangaDetailsQuery`, `GetMangaChaptersQuery` e `GetChapterPagesQuery`.

### DTOs de Application

- `UserProfileDto`.
- `MangaSummaryDto`, `MangaDetailsDto` e `MangaReferenceDto`.
- `ChapterSummaryDto`, `ChapterReferenceDto` e `ChapterPagesDto`.
- `LibraryItemDto`, `ReadingProgressDto` e `ContinueReadingDto`.
- `PagedResultDto<T>` e `ApplicationErrorDto`.

Os DTOs de catalogo representam o contrato estavel da Reader API. Nao devem expor objetos, nomes de campos ou codigos de erro da MangaDex.

### Interfaces de portas

- `IUserRepository`.
- `ILibraryRepository`, com busca por usuario e `ExternalResourceId` e salvamento atomico do agregado.
- `IUnitOfWork`.
- `ICurrentUser` para fornecer a identidade autenticada aos casos de uso.
- `ICatalogProvider` para busca e detalhes de manga.
- `IChapterProvider` para listar capitulos e resolver paginas de um capitulo.
- `IClock` para tornar datas testaveis.

Nao criar `IMangaDexRepository`: MangaDex e um provedor externo, nao um repositorio de dominio. A implementacao futura sera `MangaDexCatalogProvider` e `MangaDexChapterProvider` na Infrastructure.

**Criterios de aceite:** handlers de command/query testados com fakes em memoria. A Application referencia Domain, mas Domain nao referencia Application nem Infrastructure.

## Step 3: autenticacao e perfil minimo

**Objetivo:** dar uma identidade local ao usuario, antes de expor operacoes pessoais.

1. Escolher o provedor de identidade. Firebase ja aparece no backlog existente e pode ser mantido, desde que fique atras de uma interface de autenticacao.
2. No primeiro token valido, localizar ou criar `User` usando `ExternalSubject` como identificador estavel e unico.
3. Implementar `ICurrentUser` no host HTTP somente nesta etapa.
4. Expor apenas `GET /api/users/me` como primeira rota autenticada.
5. Retornar `401` sem token/invalido e `403` apenas quando houver identidade valida sem permissao.

**Criterios de aceite:** nenhum command de biblioteca recebe `UserId` confiado pelo corpo HTTP; o usuario e sempre derivado do token no adaptador de entrada.

## Step 4: persistencia e infraestrutura local

**Objetivo:** implementar as portas locais depois que o dominio e a Application estiverem estaveis.

1. Corrigir `Infrastructure.csproj` para biblioteca de classes, removendo `OutputType` igual a `Exe`.
2. Adicionar referencias de projeto `Infrastructure -> Application` e `Infrastructure -> Domain`; `reader-api -> Application` e `reader-api -> Infrastructure`.
3. Criar `ReaderDbContext` e configuracoes EF Core para `User`, `LibraryItem` e `ReadingProgress`.
4. Configurar `MangaReference` e `ChapterReference` como owned types, com `Provider` e `ExternalId` indexados.
5. Criar indice unico em `(UserId, MangaProvider, MangaExternalId)`.
6. Usar `DateTimeOffset` e UTC. Definir comportamento de cascade delete de `LibraryItem` para `ReadingProgress`.
7. Criar migration inicial e testes de integracao para o indice unico e cascade delete.

**Criterios de aceite:** persistencia nao armazena respostas JSON integrais da MangaDex nem URLs temporarias de paginas.

## Step 5: integrar MangaDex atras das portas

**Objetivo:** conectar a fonte externa sem vazar seu contrato para o restante da aplicacao.

1. Criar um `HttpClient` tipado para `https://api.mangadex.org` com timeout, politicas de retry apenas para falhas transitorias e logs sem dados pessoais.
2. Implementar `MangaDexCatalogProvider` para busca, detalhes e capa. Usar somente os campos mapeados para os DTOs internos.
3. Implementar `MangaDexChapterProvider` para o feed de capitulos e paginas. A resolucao do servidor de leitura deve usar a rota MangaDex@Home e devolver URLs de pagina apenas no DTO de resposta.
4. Nunca persistir URLs de paginas retornadas pelo MangaDex@Home, pois sao temporarias.
5. Mapear indisponibilidade do provedor para um erro de Application previsivel, por exemplo `ExternalCatalogUnavailable`, que o host converte em `503`.
6. Tratar `404` externo como recurso externo nao encontrado, sem expor a resposta bruta.
7. Criar testes de contrato com respostas gravadas/simuladas e testes de mapeamento para impedir acoplamento acidental ao JSON externo.

**Fora de escopo:** AniList, sincronizacao em lote, cache distribuido, downloads offline, upload de capitulos e qualquer mecanismo de ads ou monetizacao vinculado ao conteudo.

## Step 6: expor a Reader API HTTP

**Objetivo:** adicionar controllers finos somente apos os passos anteriores.

Rotas sugeridas:

| Metodo e rota | Caso de uso |
| --- | --- |
| `GET /api/users/me` | Perfil do usuario autenticado. |
| `GET /api/catalog/manga` | Buscar catalogo externo por titulo e filtros suportados. |
| `GET /api/catalog/manga/{mangaId}` | Detalhes normalizados da obra. |
| `GET /api/catalog/manga/{mangaId}/chapters` | Capitulos disponiveis. |
| `GET /api/catalog/chapters/{chapterId}/pages` | Paginas temporarias para leitura. |
| `GET /api/library` | Biblioteca do usuario, com filtros e paginação. |
| `POST /api/library` | Adicionar obra, usando snapshot vindo do catalogo. |
| `DELETE /api/library/{mangaId}` | Remover item e progressos. |
| `PUT /api/library/{mangaId}/favorite` | Favoritar/desfavoritar. |
| `GET /api/library/continue-reading` | Retomar a leitura mais recente. |
| `GET /api/library/{mangaId}/progress` | Progressos da obra. |
| `PUT /api/library/{mangaId}/progress` | Registrar pagina atual. |
| `POST /api/library/{mangaId}/chapters/{chapterId}/complete` | Marcar capitulo concluido. |

O contrato HTTP deve usar DTOs e Problem Details. Controllers nao devem conter regra de negocio, chamadas diretas para MangaDex ou acesso ao `DbContext`.

## Step 7: consumidor Flutter

**Objetivo:** criar o aplicativo apenas contra a Reader API.

1. Criar um cliente `ReaderApiClient` com modelos de transporte separados dos modelos de tela.
2. Implementar sessao autenticada e armazenamento seguro do token conforme o provedor escolhido.
3. Criar telas, nesta ordem: login, busca/catalogo, detalhes da obra, biblioteca, continuar lendo e leitor de paginas.
4. Ao abrir um capitulo, chamar a Reader API para paginas. Ao trocar de pagina, enviar progresso com debounce; enviar novamente no ciclo de vida de pausa/encerramento.
5. Mostrar estados de carregamento, vazio, erro recuperavel e indisponibilidade temporaria do catalogo.
6. Nao colocar URL, token ou regra especifica da MangaDex no Flutter.

**Criterios de aceite:** trocar o `ICatalogProvider` no backend nao requer alteracao de contratos nem codigo de UI do Flutter.

## Step 8: frontend HTML, CSS e JavaScript simples

**Objetivo:** oferecer uma alternativa leve de validacao do produto antes ou em paralelo ao Flutter.

1. Criar uma aplicacao estatica separada do host .NET, com configuracao de URL da Reader API por ambiente.
2. Criar as mesmas telas essenciais: busca, detalhes, biblioteca, continuar lendo e leitor.
3. Manter o token fora de URLs e preferir cookie seguro de sessao quando a estrategia de autenticacao permitir; caso use bearer token, tratar o armazenamento como decisao de seguranca documentada.
4. Para o leitor, renderizar somente as URLs devolvidas pela Reader API e registrar o progresso ao mudar de pagina ou sair da tela.
5. Configurar CORS no host apenas para as origens locais/deploy autorizadas.

## Ordem de entrega e qualidade

1. Step 1 e Step 2 com testes unitarios, sem Infrastructure nem controllers.
2. Step 3 e Step 4 com testes de integracao locais.
3. Step 5 com testes de contrato do provedor MangaDex.
4. Step 6 com testes de API e autorizacao.
5. Step 7 ou Step 8 como primeiro consumidor; manter o outro como entrega posterior.

Em cada etapa, executar build da solucao e os testes pertinentes. Atualizar `FEATURES.md` somente apos os criterios de aceite da etapa correspondente estarem validados.
