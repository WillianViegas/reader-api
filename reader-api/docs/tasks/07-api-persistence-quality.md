# API: Persistência e qualidade

## Objetivo

Consolidar a arquitetura da API, persistência, contratos HTTP, documentação, CORS e cobertura de testes para sustentar os demais contextos.

## Escopo

- **API-01** Separar controllers por contexto funcional.
- **API-02** Usar DTOs de entrada e saída.
- **API-03** Implementar casos de uso em `Application`.
- **API-04** Manter interfaces no `Domain` e implementações na `Infrastructure`.
- **API-05** Criar relacionamentos e índices.
- **API-06** Criar migrations e atualizar banco local.
- **API-07** Documentar endpoints no NSwag.
- **API-08** Padronizar erros.
- **API-09** Configurar CORS web/mobile.
- **API-10** Adicionar testes unitários e de integração.
- **API-11** Remover `WeatherForecast` quando houver controllers reais.

## Regras arquiteturais

- Controllers devem cuidar de transporte HTTP, não de regras de negócio.
- Casos de uso devem orquestrar operações e autorização de aplicação.
- Entidades e invariantes ficam no domínio.
- A infraestrutura não deve vazar para `Domain`.
- DTOs devem evitar overposting e exposição de campos internos.
- Repositórios devem suportar consultas autorizadas e índices necessários.

## Dependências

Este briefing é transversal e deve ser executado em conjunto com os contextos funcionais. Não substitua a validação específica deles.

## Critérios de aceite

- Solução compila sem warnings introduzidos.
- Migrations representam as relações, unicidades e índices essenciais.
- Endpoints possuem responses documentadas no NSwag.
- Erros de validação, autenticação, autorização, não encontrado e conflito têm formato consistente.
- CORS permite origens configuradas e bloqueia origens não autorizadas.
- Testes cobrem casos de uso e pelo menos os fluxos HTTP principais.
- Controller de exemplo não permanece como parte da API de produção após a criação dos controllers reais.

## Validação e relatório

Execute build da solução, testes unitários, testes de integração disponíveis e verificação da documentação. Relate migrations criadas, convenção de erros e qualquer débito técnico que permaneça.
