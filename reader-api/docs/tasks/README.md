# Briefings para subagents

Esta pasta contém briefings independentes para implementar o backlog da Reader API. Cada subagent deve ler este índice, o briefing correspondente e [FEATURES.md](../../FEATURES.md) antes de começar.

## Ordem recomendada

1. `01-authentication.md`
2. `02-user-profile.md`
3. `03-couple-invitations.md`
4. `04-calendar-events.md`
5. `05-date-events.md`
6. `06-notifications.md`
7. `07-api-persistence-quality.md`
8. `08-flutter-app.md`
9. `09-reader-platform.md`

As tasks granulares do frontend estão em [`flutter/`](flutter/README.md).

O plano do produto de leitura, incluindo dominio, Application, integracao futura com MangaDex e consumidores Flutter/web, esta em [`09-reader-platform.md`](09-reader-platform.md).

A ordem reduz retrabalho: autenticação fornece a identidade, usuário e casal definem autorização, eventos dependem dessas relações e o app Flutter consome os contratos dos contextos anteriores.

## Regras para qualquer subagent

- Inspecione o estado atual do repositório antes de editar.
- Preserve a separação entre `Domain`, `Application`, `Infrastructure` e `reader-api`.
- Não substitua alterações existentes do usuário.
- Não exponha entidades diretamente nos contratos HTTP.
- Proteja dados por usuário e casal, validando autorização no servidor.
- Adicione ou atualize testes para o comportamento implementado.
- Execute build e os testes relevantes antes de concluir.
- Não marque uma task como concluída se ela apenas foi parcialmente implementada.
- Atualize [FEATURES.md](../../FEATURES.md) somente quando a implementação estiver realmente validada.

## Relatório esperado

Ao finalizar, o subagent deve informar:

- tasks concluídas e tasks bloqueadas;
- arquivos criados ou alterados;
- decisões de domínio e segurança;
- comandos de build/test executados e resultado;
- pendências ou decisões que exigem validação do time.
