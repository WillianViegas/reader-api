# DATE: DateEvent, mídia, comentários e avaliações

## Objetivo

Completar a experiência de um encontro com detalhes, imagens, vídeos, comentários e avaliação única por usuário.

## Escopo

- **DATE-01** Retornar dados de `DateEvent` nos detalhes.
- **DATE-02** Adicionar imagem.
- **DATE-03** Adicionar vídeo.
- **DATE-04** Integrar Firebase Storage ou storage aprovado.
- **DATE-05** Validar tipo, tamanho e URL.
- **DATE-06** Listar e remover mídias autorizadas.
- **DATE-07** Adicionar comentário.
- **DATE-08** Editar/remover comentário conforme autoria.
- **DATE-09** Avaliar encontro.
- **DATE-10** Garantir uma avaliação por usuário.
- **DATE-11** Validar score e calcular média.
- **DATE-12** Gerar notificações.
- **DATE-13** Criar testes.

## Contrato sugerido

- `GET /api/date-events/{dateEventId}`
- `POST /api/date-events/{dateEventId}/media`
- `DELETE /api/date-events/{dateEventId}/media/{mediaId}`
- `POST /api/date-events/{dateEventId}/comments`
- `PUT /api/date-events/{dateEventId}/comments/{commentId}`
- `DELETE /api/date-events/{dateEventId}/comments/{commentId}`
- `PUT /api/date-events/{dateEventId}/rating`

## Regras de domínio e segurança

- O recurso deve estar associado a um `CalendarEvent` do tipo `Date`.
- Usuário só altera comentário próprio e mídia que possui permissão para remover.
- `Rating.Score` deve ter faixa definida pelo produto; não assuma uma escala sem registrá-la.
- Use índice/constraint único para impedir mais de uma avaliação por usuário e encontro.
- Não confie em URLs, MIME type ou tamanho informados pelo cliente sem validação do servidor.
- Upload deve evitar expor credenciais de storage e deve definir estratégia de remoção de arquivos órfãos.

## Dependências

- Eventos e autorização de casal.
- Entidades `DateEvent`, `Media`, `Comment` e `Rating`.
- Storage escolhido e configuração segura.
- Notificações.

## Critérios de aceite

- Detalhes exibem mídia, comentários e média de avaliação corretamente.
- Upload rejeita tipo/tamanho inválido.
- Comentário e mídia respeitam autorização.
- Uma segunda avaliação do mesmo usuário atualiza ou é rejeitada conforme decisão documentada.
- Média é consistente após criação, atualização e remoção permitidas.
- Testes cobrem `401`, `403`, validação, duplicidade e falhas de storage.

## Validação e relatório

Relate a escala de score, a política de upload, o storage escolhido e como arquivos órfãos são tratados. Execute testes de domínio e endpoints.
