# FLUTTER-AUTH: Cadastro, login e sessão

## Objetivo

Implementar cadastro, login, logout e controle de sessão usando Firebase Authentication.

## Tasks

- [ ] **AUTH-01** Configurar Firebase Core e Firebase Authentication por plataforma.
- [ ] **AUTH-02** Criar tela de cadastro.
- [ ] **AUTH-03** Criar tela de login.
- [ ] **AUTH-04** Observar `authStateChanges` e proteger rotas autenticadas.
- [ ] **AUTH-05** Enviar o Firebase ID token no header `Authorization: Bearer <token>`.
- [ ] **AUTH-06** Tratar credenciais inválidas, conta já existente, sessão expirada e logout.
- [ ] **AUTH-07** Sincronizar o usuário com a API no primeiro acesso autenticado.
- [ ] **AUTH-08** Testar formulário, sessão, interceptor e redirecionamento.

## Dependências

`flutter/01-foundation.md` e integração backend definida em `01-authentication.md`.

## Critérios de aceite

- Usuário não autenticado não acessa áreas protegidas.
- Cadastro e login exibem loading, sucesso e erros compreensíveis.
- O token é obtido pelo Firebase SDK e nunca solicitado ao usuário.
- Logout limpa o estado local e retorna à tela pública.
- Token ausente ou expirado redireciona para login sem loop de navegação.
- Nenhuma senha ou credencial privada é armazenada pelo app.
