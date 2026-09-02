# FLUTTER-FOUNDATION: Fundação do app

## Objetivo

Preparar a base do aplicativo Flutter para permitir evolução por features, múltiplas plataformas e integração consistente com a API.

## Tasks

- [ ] **FOUNDATION-01** Criar ou localizar o projeto Flutter em diretório separado da API .NET.
- [ ] **FOUNDATION-02** Configurar tema, tipografia, cores e componentes compartilhados conforme a identidade Twogether.
- [ ] **FOUNDATION-03** Configurar roteamento para áreas públicas e protegidas.
- [ ] **FOUNDATION-04** Criar configuração de ambiente para base URL de desenvolvimento, homologação e produção.
- [ ] **FOUNDATION-05** Criar cliente HTTP com interceptors, serialização e tratamento de erros.
- [ ] **FOUNDATION-06** Definir gerenciamento de estado e convenção de features.
- [ ] **FOUNDATION-07** Criar widgets compartilhados para loading, vazio, erro, retry, confirmação e mensagens.
- [ ] **FOUNDATION-08** Configurar análise estática, testes e CI básico.

## Critérios de aceite

- O app inicia sem sessão ou na home conforme o estado de autenticação.
- Nenhuma chamada HTTP é feita diretamente por widget.
- Base URL e configuração Firebase variam por ambiente.
- Tema não depende de valores espalhados pelas telas.
- Testes e análise estática executam sem erros introduzidos.
