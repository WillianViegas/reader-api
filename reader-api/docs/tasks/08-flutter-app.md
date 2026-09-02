# FLUTTER: Aplicativo Twogether

## Objetivo

Criar o aplicativo Flutter do Twogether para Android, iOS e, quando necessário, Web. O app deve implementar a jornada de usuário descrita em [FEATURES.md](../../FEATURES.md), consumir a Twogether API e usar o Firebase Authentication como provedor de identidade.

Este arquivo é um briefing para um subagent de frontend. O subagent deve construir o app em um projeto Flutter separado, preservando a API .NET deste repositório. Se o app for criado em outro repositório, mantenha este documento como referência do contrato.

Para execução incremental, use os briefings específicos em [`flutter/`](flutter/README.md): fundação, autenticação, home/eventos, `DateEvent`, perfil, casal/convites e notificações.

## Referências do produto

A interface deve seguir a direção visual observada nas imagens anexadas:

- identidade romântica, leve e contemporânea;
- fundo claro e superfícies com tons suaves;
- vermelho/carmim como cor de ação e marca;
- navegação inferior para Home, criação de evento e Perfil;
- calendários e listas de eventos com leitura rápida;
- telas de detalhe com destaque para título, data, local, mídia, comentários e avaliação;
- formulários simples, com campos claros e validação próxima ao campo;
- responsividade para telefones e Flutter Web.

Não reproduza imagens, textos ou elementos de marca de terceiros. Use a identidade Twogether definida pelo projeto e componentes nativos/permitidos.

## Stack mínima

- Flutter stable com Dart 3 compatível com a versão adotada pelo projeto.
- Firebase Core.
- Firebase Authentication.
- Firebase Storage, caso o upload seja realizado diretamente pelo app.
- Cliente HTTP escolhido pelo projeto, como `dio` ou `http`.
- Gerenciamento de estado escolhido e documentado pelo subagent, preferindo uma solução consistente em todo o app.
- Testes unitários, de widgets e de integração para os fluxos principais.

O subagent deve confirmar as versões atuais antes de instalar dependências e registrar as escolhas no `pubspec.yaml` e na documentação do app.

## Arquitetura sugerida

Organize o app por feature, mantendo separação entre apresentação, aplicação e infraestrutura:

```text
lib/
  app/
    app.dart
    router.dart
    theme/
  core/
    config/
    errors/
    networking/
    auth/
    widgets/
  features/
    auth/
    home/
    calendar_events/
    date_events/
    profile/
    couple/
    notifications/
```

Responsabilidades:

- `core/auth`: sessão Firebase, estado de autenticação e obtenção/atualização do ID token.
- `core/networking`: cliente HTTP, base URL, interceptors e tratamento de `401`, `403`, `404`, `409` e `422`.
- `features/*/data`: models, datasources e repositories concretos.
- `features/*/domain`: entidades, contratos e casos de uso quando a complexidade justificar.
- `features/*/presentation`: telas, controllers/notifiers, estados e widgets.
- `app/router.dart`: rotas públicas e protegidas, incluindo redirecionamento por sessão.

Não coloque chamadas HTTP diretamente nos widgets. Não use modelos de resposta da API como estado visual sem conversão quando isso acoplar a UI ao backend.

## Jornada e telas

### 1. Sessão e autenticação

Tasks relacionadas: `AUTH-01` a `AUTH-07` do backend.

- [ ] **FLUTTER-AUTH-01** Configurar Firebase no Android, iOS e Web se aplicável.
- [ ] **FLUTTER-AUTH-02** Criar tela de cadastro.
- [ ] **FLUTTER-AUTH-03** Criar tela de login.
- [ ] **FLUTTER-AUTH-04** Observar o estado de sessão e redirecionar para login ou home.
- [ ] **FLUTTER-AUTH-05** Obter o Firebase ID token e enviá-lo como `Authorization: Bearer <token>`.
- [ ] **FLUTTER-AUTH-06** Tratar sessão expirada, logout e erro de credenciais.
- [ ] **FLUTTER-AUTH-07** Não armazenar senha nem credenciais privadas no app.

Critérios de aceite:

- Usuário não autenticado não acessa telas protegidas.
- Cadastro e login exibem estados de carregamento, sucesso e erro.
- O cliente renova ou reobtém o ID token pelo Firebase quando necessário.
- Logout remove a sessão local e retorna para a tela de login.

### 2. Home e calendário

Tasks relacionadas: `EVENT-01` a `EVENT-03`.

- [ ] **FLUTTER-EVENT-01** Criar home com calendário/listagem de `CalendarEvents`.
- [ ] **FLUTTER-EVENT-02** Exibir estado inicial, carregamento, vazio, erro e retry.
- [ ] **FLUTTER-EVENT-03** Permitir filtro por período, tipo e status.
- [ ] **FLUTTER-EVENT-04** Diferenciar visualmente evento normal e `DateEvent`.
- [ ] **FLUTTER-EVENT-05** Abrir detalhes ao tocar em um evento.
- [ ] **FLUTTER-EVENT-06** Atualizar a listagem após criar, editar, cancelar ou excluir um evento.

Critérios de aceite:

- A home lista somente eventos autorizados pela API.
- O usuário consegue localizar eventos por calendário e lista.
- Uma lista vazia possui ação clara para criar o primeiro evento.
- Falhas de rede não causam tela em branco e podem ser repetidas.

### 3. Criação e edição de eventos

Tasks relacionadas: `EVENT-04` a `EVENT-10`.

- [ ] **FLUTTER-EVENT-07** Criar fluxo de novo evento.
- [ ] **FLUTTER-EVENT-08** Permitir escolher evento normal ou tipo `Date`.
- [ ] **FLUTTER-EVENT-09** Criar formulário de título, descrição, data inicial, data final, local, visibilidade e status.
- [ ] **FLUTTER-EVENT-10** Exibir campos específicos de encontro quando o tipo for `Date`.
- [ ] **FLUTTER-EVENT-11** Validar campos antes do envio e mostrar erros da API.
- [ ] **FLUTTER-EVENT-12** Criar edição e cancelamento conforme permissões retornadas pela API.

Critérios de aceite:

- O formulário não envia campos inválidos ou datas invertidas.
- A seleção de `Date` revela os campos de `DateEvent` necessários.
- O usuário recebe confirmação visual após uma operação bem-sucedida.
- O app trata `403`, `404`, `409` e `422` com mensagens compreensíveis.

### 4. Detalhes do DateEvent

Tasks relacionadas: `DATE-01` a `DATE-13`.

- [ ] **FLUTTER-DATE-01** Criar tela de detalhes do encontro.
- [ ] **FLUTTER-DATE-02** Exibir informações do evento, status, local e participantes.
- [ ] **FLUTTER-DATE-03** Exibir galeria de imagens e vídeos.
- [ ] **FLUTTER-DATE-04** Permitir selecionar e enviar mídia.
- [ ] **FLUTTER-DATE-05** Mostrar progresso e falha de upload.
- [ ] **FLUTTER-DATE-06** Permitir remover mídia quando autorizado.
- [ ] **FLUTTER-DATE-07** Listar comentários e permitir adicionar comentário.
- [ ] **FLUTTER-DATE-08** Permitir editar/remover comentário próprio.
- [ ] **FLUTTER-DATE-09** Exibir média e permitir registrar/atualizar avaliação.
- [ ] **FLUTTER-DATE-10** Atualizar a tela após mídia, comentário ou avaliação.

Critérios de aceite:

- Imagens e vídeos têm preview, estado de carregamento e tratamento de falha.
- O app não exibe ações de edição para recursos sem permissão.
- Comentários longos, teclado e rolagem funcionam em telas pequenas.
- A escala de avaliação é exibida conforme o contrato definido pela API.
- A tela permanece utilizável quando não há mídia, comentário ou avaliação.

### 5. Perfil

Tasks relacionadas: `USER-01` a `USER-06`.

- [ ] **FLUTTER-PROFILE-01** Criar tela de perfil do usuário.
- [ ] **FLUTTER-PROFILE-02** Consultar dados em `GET /api/users/me` e `GET /api/profiles/me`.
- [ ] **FLUTTER-PROFILE-03** Editar nome, bio, avatar e data de nascimento.
- [ ] **FLUTTER-PROFILE-04** Fazer upload/atualização de avatar conforme estratégia definida.
- [ ] **FLUTTER-PROFILE-05** Exibir logout e estado de sessão.

Critérios de aceite:

- O app preenche o formulário com dados atuais e preserva alterações válidas.
- Erros de validação aparecem junto aos campos correspondentes.
- O usuário só visualiza e edita o próprio perfil.

### 6. Casal e convites

Tasks relacionadas: `COUPLE-01` a `COUPLE-09`.

- [ ] **FLUTTER-COUPLE-01** Criar tela de configuração do casal.
- [ ] **FLUTTER-COUPLE-02** Exibir membros e estado do vínculo.
- [ ] **FLUTTER-COUPLE-03** Criar fluxo para convidar outro usuário.
- [ ] **FLUTTER-COUPLE-04** Listar convites recebidos.
- [ ] **FLUTTER-COUPLE-05** Aceitar ou recusar convite.
- [ ] **FLUTTER-COUPLE-06** Tratar casal completo, convite duplicado e conflito.

Critérios de aceite:

- O app mostra claramente se o usuário está sem casal, com convite pendente ou em casal completo.
- Ações de convite possuem confirmação e feedback de erro/sucesso.
- O app não permite tentar convidar a si mesmo ou criar um terceiro membro.

### 7. Notificações

Tasks relacionadas: `NOTIFICATION-01` a `NOTIFICATION-08`.

- [ ] **FLUTTER-NOTIFICATION-01** Criar lista de notificações.
- [ ] **FLUTTER-NOTIFICATION-02** Exibir indicador de notificações não lidas.
- [ ] **FLUTTER-NOTIFICATION-03** Abrir o recurso relacionado ao tocar na notificação.
- [ ] **FLUTTER-NOTIFICATION-04** Marcar notificação individual como lida.
- [ ] **FLUTTER-NOTIFICATION-05** Marcar todas como lidas.
- [ ] **FLUTTER-NOTIFICATION-06** Atualizar por pull-to-refresh e ao retornar para a tela.

Critérios de aceite:

- A listagem respeita a ordenação e paginação da API.
- Notificações lidas e não lidas são distinguíveis sem depender apenas de cor.
- Referências inválidas não quebram a navegação.

## Contrato de integração com a API

Baseie os repositories nos endpoints atuais do backend:

| Recurso | Endpoints |
| --- | --- |
| Usuário/perfil | `GET /api/users/me`, `GET /api/profiles/me`, `PUT /api/profiles/me` |
| Casal | `POST /api/couples`, `GET /api/couples/me` |
| Convites | `POST /api/couples/{coupleId}/invitations`, `GET /api/couples/invitations`, `POST /api/couples/invitations/{invitationId}/accept`, `POST /api/couples/invitations/{invitationId}/decline` |
| Eventos | `GET /api/calendar-events`, `GET /api/calendar-events/{eventId}`, `POST /api/calendar-events`, `PUT /api/calendar-events/{eventId}`, `DELETE /api/calendar-events/{eventId}` |
| DateEvent | `GET /api/date-events/{dateEventId}` |
| Mídia | `POST /api/date-events/{dateEventId}/media`, `DELETE /api/date-events/{dateEventId}/media/{mediaId}` |
| Comentários | `POST /api/date-events/{dateEventId}/comments`, `PUT /api/date-events/{dateEventId}/comments/{commentId}`, `DELETE /api/date-events/{dateEventId}/comments/{commentId}` |
| Avaliação | `PUT /api/date-events/{dateEventId}/rating` |
| Notificações | `GET /api/notifications`, `PATCH /api/notifications/{notificationId}/read`, `PATCH /api/notifications/read-all` |

O contrato real da API prevalece sobre estes endpoints sugeridos. O subagent deve consultar o Swagger/NSwag e não inventar campos obrigatórios. Quando o backend ainda não estiver pronto, use mocks isolados atrás de interfaces e deixe explícita a pendência.

## Estados obrigatórios da UI

Toda tela que acessa a API deve prever:

- carregando;
- sucesso com dados;
- sucesso sem dados;
- erro com retry;
- sessão expirada;
- sem permissão;
- envio em andamento;
- validação local e erro de validação da API;
- confirmação para ações destrutivas.

Acessibilidade mínima:

- textos legíveis e contraste adequado;
- alvos de toque confortáveis;
- labels sem depender apenas de placeholder;
- suporte a teclado quando executado na Web;
- sem informação transmitida apenas por cor;
- sem overflow horizontal em telas pequenas.

## Testes

- [ ] Testar parsing de models e mapeamento de erros HTTP.
- [ ] Testar interceptor/header do Firebase ID token.
- [ ] Testar redirecionamento de sessão.
- [ ] Testar validação de formulários.
- [ ] Testar home vazia, carregada e com erro.
- [ ] Testar criação de evento normal e `Date`.
- [ ] Testar comentários, avaliações e upload de mídia.
- [ ] Testar casal, convite e notificações.
- [ ] Criar pelo menos um teste de integração do fluxo login -> home -> detalhes.

## Configuração por ambiente

- [ ] **FLUTTER-CONFIG-01** Separar base URL de desenvolvimento, homologação e produção.
- [ ] **FLUTTER-CONFIG-02** Não versionar secrets ou arquivos privados do Firebase.
- [ ] **FLUTTER-CONFIG-03** Documentar configuração Android/iOS/Web.
- [ ] **FLUTTER-CONFIG-04** Configurar Firebase Storage apenas quando o backend/storage estiver definido.
- [ ] **FLUTTER-CONFIG-05** Confirmar CORS quando Flutter Web for utilizado.

## Fora de escopo

- Implementar controllers ou migrations da API.
- Emitir tokens de autenticação pela API.
- Definir sozinho a escala de avaliação, política de timezone ou política de upload.
- Implementar push notifications sem decisão de produto e infraestrutura.
- Colocar credenciais Firebase no repositório.

## Critérios gerais de aceite

- O app inicia em uma tela pública ou na home conforme o estado do Firebase.
- Toda rota protegida exige sessão Firebase.
- O fluxo principal funciona: login -> home -> detalhes -> criação/interação -> perfil.
- A UI é responsiva e não apresenta overflow nos tamanhos de telefone usados nos designs.
- Nenhuma chamada de rede fica espalhada nos widgets.
- Falhas de rede, sessão e autorização têm tratamento visível.
- Testes e análise estática passam sem erros introduzidos.
- O app informa claramente quais endpoints dependem de implementação futura no backend.

## Entrega esperada do subagent

Ao finalizar, informe:

- versão do Flutter/Dart usada;
- tasks `FLUTTER-*` concluídas e bloqueadas;
- telas e rotas implementadas;
- packages adicionados e motivo;
- endpoints realmente consumidos e endpoints mockados;
- configuração Firebase necessária por plataforma;
- testes executados e resultado;
- decisões pendentes sobre design, rating, upload, timezone e push notifications.
