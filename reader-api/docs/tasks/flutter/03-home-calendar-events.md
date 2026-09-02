# FLUTTER-EVENTS: Home e calendário

## Objetivo

Implementar a home com calendário/listagem e o fluxo de criação, consulta, edição e cancelamento de `CalendarEvents`.

## Tasks

- [ ] **EVENT-01** Criar home com navegação inferior e acesso à lista de eventos.
- [ ] **EVENT-02** Consumir `GET /api/calendar-events`.
- [ ] **EVENT-03** Exibir calendário, cards/lista e diferenciação entre evento normal e `DateEvent`.
- [ ] **EVENT-04** Implementar filtros por período, tipo e status.
- [ ] **EVENT-05** Abrir `GET /api/calendar-events/{eventId}` ao selecionar um evento.
- [ ] **EVENT-06** Criar formulário para evento normal e evento `Date`.
- [ ] **EVENT-07** Consumir criação, atualização, cancelamento/exclusão conforme API.
- [ ] **EVENT-08** Tratar estados vazio, loading, erro, retry e conflito.
- [ ] **EVENT-09** Atualizar a home após mutações.
- [ ] **EVENT-10** Testar models, filtros, formulário, navegação e repository.

## Endpoints

`GET /api/calendar-events`, `GET /api/calendar-events/{eventId}`, `POST /api/calendar-events`, `PUT /api/calendar-events/{eventId}`, `DELETE /api/calendar-events/{eventId}`.

## Critérios de aceite

- A home mostra somente eventos retornados pela API para o usuário.
- Datas, status e tipos são exibidos conforme o contrato do backend.
- A criação de `Date` revela os campos específicos do encontro.
- Datas inválidas não são enviadas.
- Falhas de rede oferecem retry e não deixam a tela em branco.
- Ações indisponíveis por autorização não são apresentadas ou retornam feedback adequado.
