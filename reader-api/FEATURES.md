# Reader API

## Resumo do projeto

A Twogether API será o backend de uma aplicação para casais organizarem eventos, encontros e memórias compartilhadas.

A API deverá oferecer:

- autenticação de usuários usando Firebase Authentication;
- perfis individuais de usuários;
- cadastro e convite de casais;
- calendário de eventos compartilhados;
- eventos comuns e encontros (`DateEvent`);
- mídias, comentários e avaliações em encontros;
- notificações relacionadas às atividades do usuário e do casal.

O projeto utiliza .NET 10 e está organizado em camadas:

| Camada | Responsabilidade |
| --- | --- |
| `reader-api` | Host HTTP, controllers, configuração da aplicação e middleware |
| `Application` | Casos de uso, serviços de aplicação e orquestração das regras |
| `Domain` | Entidades, enums, DTOs, interfaces e regras de domínio |
| `Infrastructure` | Persistência, Entity Framework Core, migrations e repositórios |
| `reader-api.Tests` | Testes automatizados |

A autenticação não será emitida pela API. O frontend fará o login pelo Firebase, enviará o Firebase ID token no header `Authorization: Bearer <token>`, e a API deverá validar esse token usando a integração oficial do Firebase Admin SDK ou uma integração equivalente.

## Modelo de domínio

- **User**: usuário da aplicação, identificado pelo Firebase UID.
- **Profile**: dados públicos e pessoais complementares do usuário.
- **Couple**: vínculo entre dois usuários.
- **CoupleMember**: associação entre usuário e casal, com papel e data de entrada.
- **CalendarEvent**: evento do calendário, podendo representar um evento normal ou um encontro.
- **DateEvent**: dados específicos de um encontro associado a um `CalendarEvent`.
- **Media**: imagem ou vídeo anexado a um encontro.
- **Comment**: comentário de um usuário em um encontro.
- **Rating**: avaliação de um encontro por um usuário.
- **Notification**: aviso gerado por eventos relevantes, como convite, comentário ou avaliação.

Enums previstos:

- `CalendarEventType`: `Date`, `Personal`, `Work`, `Family`, `Travel`, `Other`.
- `DateStatus`: `Planned`, `Confirmed`, `Completed`, `Cancelled`.
- `MediaType`: `Image`, `Video`.
- `NotificationType`: `EventCreated`, `EventUpdated`, `CommentAdded`, `RatingAdded`, `Reminder`.

## Jornada do usuário

1. O usuário se registra no Firebase.
2. O usuário realiza login no Firebase.
3. O frontend acessa a API com o Firebase ID token.
4. Após o login, o usuário visualiza a home com os `CalendarEvents` disponíveis.
5. O usuário acessa os detalhes de um evento.
6. O usuário cria um `CalendarEvent` normal ou um `DateEvent`.
7. Em um `DateEvent`, o usuário adiciona mídia, comentários e avaliações.
8. O usuário consulta e atualiza o próprio perfil.
9. O usuário cria um casal e convida outro usuário.
10. O usuário acompanha as notificações geradas pelas ações relevantes.

## Backlog de tasks

Os briefings detalhados para execução futura por subagents estão em [`docs/tasks/`](docs/tasks/README.md). Cada arquivo é independente e contém escopo, dependências, critérios de aceite, testes e formato de relatório.

- [Fundação e autenticação](docs/tasks/01-authentication.md)
- [Usuário e perfil](docs/tasks/02-user-profile.md)
- [Casal e convites](docs/tasks/03-couple-invitations.md)
- [Calendário e eventos](docs/tasks/04-calendar-events.md)
- [DateEvent, mídia, comentários e avaliações](docs/tasks/05-date-events.md)
- [Notificações](docs/tasks/06-notifications.md)
- [API, persistência e qualidade](docs/tasks/07-api-persistence-quality.md)
- [Aplicativo Flutter](docs/tasks/08-flutter-app.md)

### 1. Fundação e autenticação

- [ ] **AUTH-01** Integrar a API ao Firebase Admin SDK.
- [ ] **AUTH-02** Validar o Firebase ID token em todas as rotas protegidas.
- [ ] **AUTH-03** Mapear o Firebase UID para `User.Id`.
- [ ] **AUTH-04** Criar ou sincronizar o usuário no primeiro acesso autenticado.
- [ ] **AUTH-05** Retornar `401 Unauthorized` quando o token estiver ausente, expirado ou inválido.
- [ ] **AUTH-06** Criar testes para token válido, inválido e usuário não sincronizado.
- [ ] **AUTH-07** Configurar credenciais do Firebase por secret manager ou variável de ambiente.

### 2. Usuário e perfil

- [ ] **USER-01** Criar endpoint para consultar o usuário autenticado.
- [ ] **USER-02** Criar endpoint para consultar o próprio perfil.
- [ ] **USER-03** Criar endpoint para atualizar nome, bio, avatar e data de nascimento.
- [ ] **USER-04** Validar unicidade e formato dos dados identificadores do usuário.
- [ ] **USER-05** Impedir que um usuário altere o perfil de outro sem autorização.
- [ ] **USER-06** Criar testes dos fluxos de consulta e atualização do perfil.

Endpoints sugeridos:

- `GET /api/users/me`
- `GET /api/profiles/me`
- `PUT /api/profiles/me`

### 3. Casal e convite

- [ ] **COUPLE-01** Criar um casal para o usuário autenticado.
- [ ] **COUPLE-02** Criar `CoupleMember` para o criador do casal.
- [ ] **COUPLE-03** Convidar outro usuário por e-mail ou Firebase UID.
- [ ] **COUPLE-04** Criar notificação para o usuário convidado.
- [ ] **COUPLE-05** Aceitar ou recusar um convite.
- [ ] **COUPLE-06** Impedir que um casal tenha mais de dois membros ativos.
- [ ] **COUPLE-07** Impedir convite para o próprio usuário ou para alguém já vinculado ao casal.
- [ ] **COUPLE-08** Listar o casal e seus membros para usuários autorizados.
- [ ] **COUPLE-09** Criar testes de criação, convite, aceite, recusa e regras de duplicidade.

Endpoints sugeridos:

- `POST /api/couples`
- `GET /api/couples/me`
- `POST /api/couples/{coupleId}/invitations`
- `GET /api/couples/invitations`
- `POST /api/couples/invitations/{invitationId}/accept`
- `POST /api/couples/invitations/{invitationId}/decline`

### 4. Calendário e eventos

- [ ] **EVENT-01** Listar `CalendarEvents` do usuário e do casal.
- [ ] **EVENT-02** Permitir filtros por período, tipo e status.
- [ ] **EVENT-03** Consultar os detalhes de um `CalendarEvent`.
- [ ] **EVENT-04** Criar um evento normal.
- [ ] **EVENT-05** Criar um evento do tipo `Date` com os dados de `DateEvent`.
- [ ] **EVENT-06** Atualizar título, descrição, datas, localização, visibilidade e status.
- [ ] **EVENT-07** Cancelar ou excluir um evento conforme as regras de negócio.
- [ ] **EVENT-08** Restringir acesso aos eventos do próprio usuário ou do casal.
- [ ] **EVENT-09** Validar intervalo de datas e transições de `DateStatus`.
- [ ] **EVENT-10** Criar notificações para criação e atualização de eventos relevantes.
- [ ] **EVENT-11** Criar testes de listagem, detalhes, criação, atualização, cancelamento e autorização.

Endpoints sugeridos:

- `GET /api/calendar-events`
- `GET /api/calendar-events/{eventId}`
- `POST /api/calendar-events`
- `PUT /api/calendar-events/{eventId}`
- `DELETE /api/calendar-events/{eventId}`

### 5. DateEvent, mídia, comentários e avaliações

- [ ] **DATE-01** Retornar os dados específicos de `DateEvent` nos detalhes do evento.
- [ ] **DATE-02** Permitir adicionar imagem a um `DateEvent`.
- [ ] **DATE-03** Permitir adicionar vídeo a um `DateEvent`.
- [ ] **DATE-04** Integrar o upload com Firebase Storage ou outro storage definido pelo projeto.
- [ ] **DATE-05** Validar tipo, tamanho e URL de mídia.
- [ ] **DATE-06** Permitir listar e remover mídias do próprio encontro.
- [ ] **DATE-07** Permitir adicionar comentário a um `DateEvent`.
- [ ] **DATE-08** Permitir editar ou remover comentário conforme a permissão do autor.
- [ ] **DATE-09** Permitir avaliar um `DateEvent`.
- [ ] **DATE-10** Garantir uma avaliação por usuário em cada encontro.
- [ ] **DATE-11** Validar a faixa de `Rating.Score` e calcular a média do encontro.
- [ ] **DATE-12** Criar notificações para comentário e avaliação adicionados.
- [ ] **DATE-13** Criar testes de mídia, comentários, avaliações, duplicidade e autorização.

Endpoints sugeridos:

- `GET /api/date-events/{dateEventId}`
- `POST /api/date-events/{dateEventId}/media`
- `DELETE /api/date-events/{dateEventId}/media/{mediaId}`
- `POST /api/date-events/{dateEventId}/comments`
- `PUT /api/date-events/{dateEventId}/comments/{commentId}`
- `DELETE /api/date-events/{dateEventId}/comments/{commentId}`
- `PUT /api/date-events/{dateEventId}/rating`

### 6. Notificações

- [ ] **NOTIFICATION-01** Criar notificações para convite de casal.
- [ ] **NOTIFICATION-02** Criar notificações para criação ou atualização de eventos.
- [ ] **NOTIFICATION-03** Criar notificações para comentários e avaliações.
- [ ] **NOTIFICATION-04** Listar notificações do usuário autenticado.
- [ ] **NOTIFICATION-05** Marcar uma notificação como lida.
- [ ] **NOTIFICATION-06** Marcar todas as notificações como lidas.
- [ ] **NOTIFICATION-07** Ordenar notificações por data de criação.
- [ ] **NOTIFICATION-08** Criar testes de geração, leitura e atualização de notificações.

Endpoints sugeridos:

- `GET /api/notifications`
- `PATCH /api/notifications/{notificationId}/read`
- `PATCH /api/notifications/read-all`

### 7. API, persistência e qualidade

- [ ] **API-01** Criar controllers separados por contexto funcional.
- [ ] **API-02** Criar DTOs de entrada e saída sem expor entidades diretamente.
- [ ] **API-03** Implementar casos de uso na camada `Application`.
- [ ] **API-04** Implementar interfaces de repositório na camada `Domain` e suas implementações na `Infrastructure`.
- [ ] **API-05** Criar relacionamentos e índices do banco para as entidades do modelo.
- [ ] **API-06** Criar migrations iniciais e atualizar o banco local.
- [ ] **API-07** Documentar endpoints e respostas no NSwag.
- [ ] **API-08** Padronizar respostas de erro de validação, autenticação e autorização.
- [ ] **API-09** Configurar CORS para os ambientes web e mobile.
- [ ] **API-10** Adicionar testes unitários e de integração para os fluxos principais.
- [ ] **API-11** Remover o controller de exemplo `WeatherForecast` quando os primeiros controllers reais forem criados.

## Critérios gerais de aceite

- Rotas protegidas exigem um Firebase ID token válido.
- Um usuário só acessa dados próprios ou dados do casal ao qual pertence.
- Um casal possui no máximo dois membros ativos.
- Um `DateEvent` é sempre associado a um `CalendarEvent` do tipo `Date`.
- Um usuário não pode avaliar o mesmo `DateEvent` mais de uma vez.
- Comentários e mídias respeitam as permissões do usuário e do casal.
- Datas, status, tipos e campos obrigatórios são validados pela API.
- Alterações relevantes geram as notificações correspondentes.
- Os endpoints aparecem na documentação NSwag e possuem respostas de erro previsíveis.
- Os fluxos principais possuem testes automatizados.

## Ordem sugerida de implementação

1. **AUTH**: Firebase, usuário autenticado e autorização básica.
2. **USER**: usuário e perfil.
3. **COUPLE**: casal e convites.
4. **EVENT**: calendário e eventos.
5. **DATE**: detalhes, mídia, comentários e avaliações.
6. **NOTIFICATION**: central de notificações.
7. **API**: refinamento, testes, documentação e regras transversais.
