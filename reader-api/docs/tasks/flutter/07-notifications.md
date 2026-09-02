# FLUTTER-NOTIFICATIONS: Central de notificações

## Objetivo

Exibir notificações relacionadas a convites, eventos, comentários, avaliações e lembretes.

## Tasks

- [ ] **NOTIFICATION-01** Criar tela/lista de notificações.
- [ ] **NOTIFICATION-02** Consumir `GET /api/notifications`.
- [ ] **NOTIFICATION-03** Diferenciar lidas e não lidas sem depender apenas de cor.
- [ ] **NOTIFICATION-04** Abrir o recurso relacionado à notificação.
- [ ] **NOTIFICATION-05** Consumir marcação individual e em massa como lida.
- [ ] **NOTIFICATION-06** Exibir badge de não lidas.
- [ ] **NOTIFICATION-07** Implementar refresh, paginação e retry.
- [ ] **NOTIFICATION-08** Testar ordenação, navegação, isolamento e estados vazios.

## Endpoints

`GET /api/notifications`, `PATCH /api/notifications/{notificationId}/read` e `PATCH /api/notifications/read-all`.

## Critérios de aceite

- A lista respeita ordenação e paginação da API.
- Marcar como lida é idempotente no comportamento visual.
- Referência inválida não quebra a tela.
- O badge é atualizado após leitura e refresh.
- Convites podem abrir diretamente o fluxo de casal.
