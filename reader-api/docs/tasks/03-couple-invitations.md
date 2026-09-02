# COUPLE: Casal e convites

## Objetivo

Permitir que um usuário crie um casal, convide outro usuário e gerencie o ciclo de vida do convite com regras de autorização e limite de membros.

## Escopo

- **COUPLE-01** Criar casal.
- **COUPLE-02** Criar o `CoupleMember` do criador.
- **COUPLE-03** Convidar por e-mail ou Firebase UID.
- **COUPLE-04** Notificar o usuário convidado.
- **COUPLE-05** Aceitar ou recusar convite.
- **COUPLE-06** Limitar o casal a dois membros ativos.
- **COUPLE-07** Bloquear auto convite, duplicidade e usuário já vinculado.
- **COUPLE-08** Listar casal e membros autorizados.
- **COUPLE-09** Criar testes do fluxo completo.

## Contrato sugerido

- `POST /api/couples`
- `GET /api/couples/me`
- `POST /api/couples/{coupleId}/invitations`
- `GET /api/couples/invitations`
- `POST /api/couples/invitations/{invitationId}/accept`
- `POST /api/couples/invitations/{invitationId}/decline`

## Dependências

- Usuário autenticado e sincronizado.
- `User`, `Couple` e `CoupleMember`.
- Mecanismo de notificação, mesmo que inicialmente seja apenas persistência.

## Critérios de aceite

- Apenas usuários autenticados criam ou consultam casais.
- Um casal nunca possui mais de dois membros ativos.
- Convites têm estado, validade e destinatário definidos.
- O convidado só aceita ou recusa o próprio convite.
- Convite duplicado, auto convite e convite para membro existente são rejeitados.
- Acesso ao casal e aos membros é restrito aos membros autorizados.
- Operações concorrentes não ultrapassam o limite de dois membros.
- Testes cobrem sucesso, conflitos, `401`, `403` e `404`.

## Fora de escopo

- Casais com mais de duas pessoas.
- Chat entre membros.
- Notificações push, que pertencem ao contexto de notificações.

## Validação e relatório

Execute testes do fluxo completo e descreva a estratégia usada para garantir o limite de dois membros em concorrência.
