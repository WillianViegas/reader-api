# FLUTTER-DATE: Detalhes do encontro

## Objetivo

Implementar a tela de detalhes de `DateEvent`, incluindo mídia, comentários e avaliação.

## Tasks

- [ ] **DATE-01** Consumir `GET /api/date-events/{dateEventId}`.
- [ ] **DATE-02** Exibir título, datas, local, status, descrição e participantes.
- [ ] **DATE-03** Exibir galeria de imagens e vídeos com preview.
- [ ] **DATE-04** Selecionar e enviar mídia conforme a estratégia de storage definida.
- [ ] **DATE-05** Mostrar progresso, sucesso e erro de upload.
- [ ] **DATE-06** Listar, criar, editar e remover comentários conforme permissão.
- [ ] **DATE-07** Exibir média e permitir criar/atualizar avaliação.
- [ ] **DATE-08** Atualizar detalhes após cada mutação.
- [ ] **DATE-09** Testar estados sem mídia/comentários, upload, comentário e rating.

## Endpoints

`GET /api/date-events/{dateEventId}`, endpoints `/media`, `/comments` e `/rating` descritos em `05-date-events.md`.

## Dependências e decisões bloqueadoras

- O contrato final de upload precisa definir multipart, URL pré-assinada ou upload direto ao Firebase Storage.
- A escala de `Rating.Score` deve ser definida pela API/produto.
- A API deve informar as permissões do usuário ou o app deve derivá-las com segurança do contexto.

## Critérios de aceite

- A tela funciona sem conteúdo opcional.
- Usuário só vê ações permitidas.
- Upload valida extensão/tamanho antes do envio e trata falha do servidor.
- Comentário próprio pode ser editado/removido quando autorizado.
- Uma avaliação é criada ou atualizada conforme o contrato, sem duplicidade visual.
- Layout não apresenta overflow com teclado ou em telas pequenas.
