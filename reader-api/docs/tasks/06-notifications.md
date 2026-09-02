# NOTIFICATION: Notificações

## Objetivo

Criar e consultar notificações geradas por convites, eventos, comentários, avaliações e lembretes.

## Escopo

- **NOTIFICATION-01** Notificar convite de casal.
- **NOTIFICATION-02** Notificar criação/atualização de eventos.
- **NOTIFICATION-03** Notificar comentários e avaliações.
- **NOTIFICATION-04** Listar notificações do usuário.
- **NOTIFICATION-05** Marcar uma notificação como lida.
- **NOTIFICATION-06** Marcar todas como lidas.
- **NOTIFICATION-07** Ordenar por criação.
- **NOTIFICATION-08** Criar testes.

## Contrato sugerido

- `GET /api/notifications`
- `PATCH /api/notifications/{notificationId}/read`
- `PATCH /api/notifications/read-all`

## Regras

- Usuário só pode consultar e atualizar suas próprias notificações.
- `NotificationType` deve cobrir `EventCreated`, `EventUpdated`, `CommentAdded`, `RatingAdded` e `Reminder`.
- A operação deve ser idempotente: marcar como lida mais de uma vez não gera erro indevido.
- Ordenação padrão deve ser por `CreatedAt` decrescente, com paginação quando necessário.
- Dados de referência devem permitir navegar ao recurso sem confiar em texto livre.

## Dependências

- Usuário autenticado.
- Casal, eventos, comentários e avaliações.
- Entidade `Notification` e persistência.

## Critérios de aceite

- Eventos relevantes geram notificações para os destinatários corretos.
- Notificações não são duplicadas em retries quando a operação for repetida.
- Listagem é isolada por usuário, ordenada e paginável.
- Marcar uma ou todas como lidas atualiza apenas registros autorizados.
- Testes cobrem geração, listagem, isolamento e idempotência.

## Fora de escopo

- Push notification ou e-mail, salvo decisão específica.
- Tela de notificações do frontend.

## Validação e relatório

Descreva como a geração será acoplada aos casos de uso e se haverá processamento síncrono ou assíncrono. Execute os testes relevantes.
