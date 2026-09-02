# EVENT: Calendário e eventos

## Objetivo

Implementar a listagem, consulta e manutenção dos eventos pessoais e compartilhados do usuário e do casal.

## Escopo

- **EVENT-01** Listar eventos do usuário e do casal.
- **EVENT-02** Filtrar por período, tipo e status.
- **EVENT-03** Consultar detalhes.
- **EVENT-04** Criar evento normal.
- **EVENT-05** Criar evento do tipo `Date` com `DateEvent`.
- **EVENT-06** Atualizar dados permitidos.
- **EVENT-07** Cancelar ou excluir conforme regra de negócio.
- **EVENT-08** Restringir acesso por usuário/casal.
- **EVENT-09** Validar datas e transições de `DateStatus`.
- **EVENT-10** Gerar notificações de criação/atualização.
- **EVENT-11** Criar testes.

## Contrato sugerido

- `GET /api/calendar-events`
- `GET /api/calendar-events/{eventId}`
- `POST /api/calendar-events`
- `PUT /api/calendar-events/{eventId}`
- `DELETE /api/calendar-events/{eventId}`

## Regras de domínio

- Use `CalendarEventType` para distinguir `Date`, `Personal`, `Work`, `Family`, `Travel` e `Other`.
- Um evento `Date` deve possuir os dados obrigatórios de `DateEvent`.
- Datas devem ser armazenadas com uma política explícita de timezone.
- Apenas membros autorizados podem consultar ou alterar eventos compartilhados.
- Mudanças de status inválidas devem ser rejeitadas.

## Dependências

- Firebase Authentication.
- Usuário/perfil.
- Casal e autorização de membros.
- Persistência e enums do domínio.

## Critérios de aceite

- A home consegue listar eventos visíveis para o usuário.
- Detalhes retornam o evento correto sem vazamento entre usuários/casais.
- Criação diferencia evento normal e `DateEvent`.
- Filtros combinados funcionam sem alterar a autorização.
- Datas inválidas e transições proibidas retornam erro de validação.
- Testes cobrem CRUD, filtros, autorização e concorrência relevante.

## Validação e relatório

Execute build, testes e, quando disponível, teste de integração dos endpoints. Relate a política de timezone e as regras finais para exclusão versus cancelamento.
