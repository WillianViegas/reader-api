# AUTH: Firebase Authentication

## Objetivo

Integrar a API ao Firebase Authentication para que o frontend autentique usuários no Firebase e a API valide o Firebase ID token recebido em `Authorization: Bearer <token>`.

## Escopo

- **AUTH-01** Integrar o Firebase Admin SDK.
- **AUTH-02** Validar o Firebase ID token em rotas protegidas.
- **AUTH-03** Mapear o Firebase UID para `User.Id`.
- **AUTH-04** Criar ou sincronizar o usuário no primeiro acesso autenticado.
- **AUTH-05** Retornar `401 Unauthorized` para token ausente, expirado ou inválido.
- **AUTH-06** Criar testes de autenticação.
- **AUTH-07** Configurar credenciais sem colocar secrets no repositório.

## Orientação técnica

- Use o Firebase UID como identificador externo estável.
- Prefira a configuração oficial do Firebase Admin SDK por variável de ambiente, User Secrets ou secret manager.
- Diferencie autenticação (`401`) de autorização (`403`).
- Mantenha o Swagger documentado com esquema Bearer.
- Não implemente emissão de tokens na API.

## Dependências

- Configuração base da API.
- Entidade `User` e persistência, caso já existam.
- Definição do mecanismo de credenciais Firebase no ambiente de execução.

## Critérios de aceite

- Uma rota protegida aceita um token Firebase válido.
- Token ausente, malformado, inválido ou expirado resulta em `401`.
- O UID autenticado está disponível para os casos de uso por uma abstração clara.
- O primeiro acesso cria ou sincroniza o registro local sem duplicidade.
- Nenhuma credencial privada aparece em código, `appsettings` versionado ou logs.
- Testes cobrem sucesso, falha e sincronização.

## Fora de escopo

- Tela de cadastro/login do frontend.
- Recuperação de senha.
- Emissão ou renovação manual de JWT pela API.
- Regras de casal, eventos e notificações.

## Validação e relatório

Execute build e testes do projeto. Relate como o ambiente de teste fornece tokens Firebase, ou documente claramente qualquer limitação para testes de integração.
