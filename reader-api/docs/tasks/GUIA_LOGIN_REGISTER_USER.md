# Guia de Registro, Login e Entidade User

Este documento descreve o fluxo de autenticação implementado na API do Projeto Habits. O código de autenticação está concentrado no `AuthController`, nos handlers da camada `Application` e na entidade `Domain.Entities.User`.

## Visão geral

```text
Cliente
  |
  | POST /api/Auth/register
  v
RegisterUserHandler -> valida dados -> BCrypt -> AppDbContext.Users
  |
  | 201 UserDto
  v

Cliente
  |
  | POST /api/Auth/login
  v
LoginHandler -> busca email -> verifica BCrypt -> JwtTokenService
  |
  | 200 AuthResponseDto com JWT
  v
Requisições protegidas: Authorization: Bearer <token>
```

Arquivos principais:

| Responsabilidade | Arquivo |
| --- | --- |
| Rotas HTTP | `src/Api/Controllers/AuthController.cs` |
| Registro | `src/Application/Handlers/Auth/RegisterUserHandler.cs` |
| Login | `src/Application/Handlers/Auth/LoginHandler.cs` |
| Geração do token | `src/Application/Services/JwtTokenService.cs` |
| Hash da senha | `src/Application/Services/PasswordService.cs` |
| Entidade persistida | `src/Domain/Entities/User.cs` |
| Mapeamento EF Core | `src/Infrastructure/AppDbContext.cs` |

## Pré-requisitos

1. Inicie a API e o banco de dados conforme as instruções do projeto.
2. Confira a seção `JwtSettings` no `appsettings.json` ou nas variáveis de ambiente.
3. Use uma `SecretKey` com pelo menos 32 caracteres em ambientes reais. A chave de desenvolvimento não deve ser usada em produção.

Configuração usada por padrão no ambiente de desenvolvimento:

```json
{
  "JwtSettings": {
    "SecretKey": "dev-only-super-secret-key-with-at-least-32-characters",
    "Issuer": "HabitTrackingApi",
    "Audience": "HabitTrackingApiClients",
    "ExpirationMinutes": 60
  }
}
```

## Entidade `User`

A entidade está em `src/Domain/Entities/User.cs` e representa o usuário armazenado no banco.

| Propriedade | Tipo | Regra |
| --- | --- | --- |
| `Id` | `Guid` | Gerado no construtor. É a chave primária. |
| `Name` | `string` | Obrigatório; máximo de 100 caracteres no banco. |
| `Email` | `string?` | Pode ser nulo na entidade, mas é obrigatório no registro e tem índice único. |
| `PasswordHash` | `string` | Hash BCrypt; nunca deve ser retornado para o cliente. |
| `AvatarUrl` | `string?` | Opcional; máximo de 500 caracteres no banco. |
| `CreatedAt` | `DateTime` | Definido com `DateTime.UtcNow`. |

### Encapsulamento

As propriedades usam `private set`, portanto outras camadas não alteram o usuário diretamente. As alterações devem usar os métodos da entidade:

```csharp
user.UpdateName("Novo nome");
user.UpdateEmail("novo@email.com");
user.UpdateAvatarUrl("https://exemplo/avatar.png");
user.SetPasswordHash(novoHash);
```

O construtor privado sem argumentos existe para o Entity Framework Core. Para criar um usuário na aplicação, use o construtor público, passando o hash da senha, e não a senha em texto puro:

```csharp
var passwordHash = passwordService.HashPassword(password);
var user = new User(name, email, passwordHash: passwordHash);
```

O banco também aplica índice único em `Email`. O handler verifica a duplicidade antes da inserção e retorna conflito quando o email já existe.

## Registro

### Endpoint

```http
POST /api/Auth/register
Content-Type: application/json
```

### Corpo da requisição

```json
{
  "name": "Maria Silva",
  "email": "maria@example.com",
  "password": "Strong@123"
}
```

O corpo é representado por `RegisterUserCommand`.

### Validações

O `RegisterUserHandler` exige:

- `name`, `email` e `password` preenchidos;
- senha com pelo menos 8 caracteres;
- pelo menos uma letra maiúscula;
- pelo menos uma letra minúscula;
- pelo menos um número;
- pelo menos um símbolo;
- email ainda não cadastrado.

A senha nunca é salva diretamente. O `PasswordService` gera um hash BCrypt com salt automático, e somente esse hash é colocado em `User.PasswordHash`.

### Resposta de sucesso

Status: `201 Created`

```json
{
  "id": "6f9a2d8b-1b48-4b3d-9b37-3c3e9a3d1f10",
  "name": "Maria Silva",
  "email": "maria@example.com",
  "avatarUrl": null,
  "createdAt": "2026-09-14T12:00:00Z"
}
```

A resposta usa `UserDto`; ela não contém `PasswordHash`.

### Erros possíveis

| Status | Situação |
| --- | --- |
| `400 Bad Request` | Campo obrigatório ausente ou senha fora da política. |
| `409 Conflict` | Email já cadastrado. |

Exemplo de erro de validação:

```json
{
  "message": "Password must contain at least 8 characters.",
  "errors": []
}
```

## Login

### Endpoint

```http
POST /api/Auth/login
Content-Type: application/json
```

### Corpo da requisição

```json
{
  "email": "maria@example.com",
  "password": "Strong@123"
}
```

O corpo é representado por `LoginCommand`.

O `LoginHandler` busca o usuário por email usando `AsNoTracking()` e compara a senha enviada com o hash armazenado usando `PasswordService.VerifyPassword`. Se o email não existir ou a senha estiver incorreta, a API retorna a mesma mensagem genérica para não revelar qual dado falhou.

### Resposta de sucesso

Status: `200 OK`

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "user": {
    "id": "6f9a2d8b-1b48-4b3d-9b37-3c3e9a3d1f10",
    "name": "Maria Silva",
    "email": "maria@example.com",
    "avatarUrl": null,
    "createdAt": "2026-09-14T12:00:00Z"
  }
}
```

`expiresIn` é informado em segundos. Com `ExpirationMinutes: 60`, o valor retornado é `3600`.

### Erros possíveis

| Status | Situação |
| --- | --- |
| `400 Bad Request` | Email ou senha vazios. |
| `401 Unauthorized` | Credenciais inválidas. |

Resposta de credenciais inválidas:

```json
{
  "message": "Invalid credentials."
}
```

## JWT e requisições autenticadas

O `JwtTokenService` cria um token assinado com HMAC-SHA256. O payload contém as claims:

| Claim | Conteúdo |
| --- | --- |
| `sub` | `User.Id` |
| `email` | Email do usuário |
| `name` | Nome do usuário |
| `jti` | Identificador único do token |
| `iss` | `JwtSettings:Issuer` |
| `aud` | `JwtSettings:Audience` |
| `exp` | Data de expiração |

Para acessar uma rota protegida, envie o token no header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

O middleware JWT valida assinatura, issuer, audience e validade. Depois, endpoints marcados com `[Authorize]` podem obter o ID pelo claim `sub`.

### Perfil do usuário autenticado

```http
GET /api/Auth/me
Authorization: Bearer <token>
```

Respostas:

- `200 OK`: retorna `UserDto`;
- `401 Unauthorized`: token ausente, inválido ou sem um ID de usuário válido;
- `404 Not Found`: usuário do token não foi encontrado.

## Testando com cURL

```bash
# Registrar
curl -X POST "https://localhost:7001/api/Auth/register" \
  -H "Content-Type: application/json" \
  -d '{"name":"Maria Silva","email":"maria@example.com","password":"Strong@123"}'

# Fazer login
curl -X POST "https://localhost:7001/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"maria@example.com","password":"Strong@123"}'

# Consultar o perfil usando o token retornado pelo login
curl -X GET "https://localhost:7001/api/Auth/me" \
  -H "Authorization: Bearer <token-retornado-pelo-login>"
```

Se o certificado HTTPS local não for confiável, configure o ambiente de desenvolvimento adequadamente. Evite desabilitar a validação TLS em clientes usados fora do desenvolvimento.

## Testando pelo Swagger

1. Abra a URL do Swagger da API.
2. Execute `POST /api/Auth/register` com uma senha que atenda à política.
3. Execute `POST /api/Auth/login` com as mesmas credenciais.
4. Copie o valor de `token` da resposta.
5. Clique em **Authorize** e informe `Bearer <token>`.
6. Execute `GET /api/Auth/me`.

## Testes automatizados

O arquivo `tests/IntegrationTests/AuthenticationIntegrationTests.cs` cobre:

- registro com dados válidos;
- rejeição de senha fraca;
- login com credenciais válidas e geração de token;
- rejeição de senha incorreta.

Execute os testes de integração com:

```bash
dotnet test tests/IntegrationTests/IntegrationTests.csproj
```

## Regras de segurança

- Nunca retorne ou registre `PasswordHash`.
- Nunca registre o JWT completo nos logs.
- Não use a `SecretKey` de desenvolvimento em produção.
- Armazene a chave JWT em Secret Manager, variável de ambiente ou serviço de segredos.
- Mantenha HTTPS habilitado para impedir exposição de credenciais e tokens.
- Preserve a mensagem genérica de login inválido.
- Mantenha o rate limiting aplicado aos endpoints de registro e login.