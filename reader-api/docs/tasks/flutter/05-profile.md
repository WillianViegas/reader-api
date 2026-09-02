# FLUTTER-PROFILE: Perfil do usuário

## Objetivo

Permitir que o usuário consulte e atualize os próprios dados de perfil.

## Tasks

- [ ] **PROFILE-01** Criar tela de perfil e estado de carregamento.
- [ ] **PROFILE-02** Consumir `GET /api/users/me` e `GET /api/profiles/me`.
- [ ] **PROFILE-03** Criar formulário para nome, bio, avatar e data de nascimento.
- [ ] **PROFILE-04** Validar campos localmente e exibir erros `422` da API.
- [ ] **PROFILE-05** Atualizar avatar conforme storage definido.
- [ ] **PROFILE-06** Consumir `PUT /api/profiles/me`.
- [ ] **PROFILE-07** Exibir logout e testar consulta, edição e falhas.

## Critérios de aceite

- O app nunca recebe `userId` para editar o próprio perfil.
- Campos Firebase-controlados não são sobrescritos sem decisão explícita.
- Alteração bem-sucedida atualiza a tela e mostra confirmação.
- Avatar mantém placeholder e fallback quando a URL falha.
- Formulário funciona com teclado e sem overflow.
