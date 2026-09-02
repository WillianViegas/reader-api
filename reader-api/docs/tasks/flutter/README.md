# Tasks Flutter

Briefings independentes para implementar o app Flutter do Twogether. Leia primeiro [08-flutter-app.md](../08-flutter-app.md) e [FEATURES.md](../../../FEATURES.md).

## Ordem recomendada

1. `01-foundation.md`
2. `02-authentication.md`
3. `03-home-calendar-events.md`
4. `04-date-events.md`
5. `05-profile.md`
6. `06-couple-invitations.md`
7. `07-notifications.md`

## Regras para subagents Flutter

- Verifique se o projeto Flutter existe antes de editar; se não existir, informe o caminho esperado e não crie código Flutter dentro da solução .NET.
- Consulte o Swagger/NSwag da API antes de fixar models e campos.
- Mantenha chamadas HTTP fora dos widgets.
- Use Firebase Authentication para login, cadastro e sessão; a API não emite tokens.
- Trate estados de carregamento, vazio, erro, retry, `401`, `403`, `404`, `409` e `422`.
- Adicione testes para cada fluxo implementado.
- Não versionar secrets ou arquivos privados do Firebase.
- Ao finalizar, informe arquivos, tasks concluídas/bloqueadas, testes executados e pendências.
