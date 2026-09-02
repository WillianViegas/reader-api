# USER: Usuário e perfil

## Objetivo

Permitir que o usuário autenticado consulte sua identidade local e gerencie o próprio perfil.

## Escopo

- **USER-01** Consultar o usuário autenticado.
- **USER-02** Consultar o próprio perfil.
- **USER-03** Atualizar nome, bio, avatar e data de nascimento.
- **USER-04** Validar unicidade e formato dos dados identificadores.
- **USER-05** Impedir alteração do perfil de outro usuário.
- **USER-06** Criar testes dos fluxos de consulta e atualização.

## Contrato sugerido

- `GET /api/users/me`
- `GET /api/profiles/me`
- `PUT /api/profiles/me`

O DTO de atualização deve aceitar apenas campos editáveis. O response não deve expor segredos, credenciais ou detalhes internos de persistência.

## Dependências

- Firebase Authentication e acesso ao usuário autenticado.
- Entidades `User` e `Profile`.
- Repositórios e migrations necessários.

## Critérios de aceite

- A consulta usa a identidade do token, nunca um `userId` enviado pelo cliente.
- Usuário inexistente é criado/sincronizado conforme o fluxo de autenticação.
- Atualização valida campos obrigatórios, limites e formato.
- Não é possível acessar ou alterar o perfil de outro usuário.
- Atualizações persistem e são retornadas em seguida.
- Testes cobrem `200`, `401`, validação e isolamento de identidade.

## Fora de escopo

- Login/cadastro no frontend.
- Perfil público de terceiros, salvo decisão posterior.
- Vínculo de casal.

## Validação e relatório

Execute testes unitários e de integração dos endpoints. Relate o contrato final dos DTOs e qualquer decisão sobre avatar, e-mail ou campos controlados pelo Firebase.
