# FLUTTER-COUPLE: Casal e convites

## Objetivo

Implementar a configuração do casal, visualização dos membros e fluxo de convites.

## Tasks

- [ ] **COUPLE-01** Criar tela de casal para estado sem vínculo, convite pendente e casal completo.
- [ ] **COUPLE-02** Consumir `POST /api/couples` e `GET /api/couples/me`.
- [ ] **COUPLE-03** Criar fluxo para convidar por e-mail ou Firebase UID, conforme contrato.
- [ ] **COUPLE-04** Listar convites recebidos.
- [ ] **COUPLE-05** Aceitar e recusar convites.
- [ ] **COUPLE-06** Tratar auto convite, duplicidade, casal completo e conflito `409`.
- [ ] **COUPLE-07** Atualizar home e notificações após aceite.
- [ ] **COUPLE-08** Testar estados de vínculo e autorização.

## Endpoints

Endpoints de casal e convites descritos em `03-couple-invitations.md`.

## Critérios de aceite

- O usuário entende em qual estado de relacionamento está.
- Não existe ação para adicionar um terceiro membro.
- Convite possui feedback de envio, aceite e recusa.
- O app não expõe dados de casal de usuários não autorizados.
- Erros de rede e convite expirado possuem retry ou orientação adequada.
