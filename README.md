<div align="center">

# NIO Ticket

### Sistema interno de gestão de reembolsos corporativos

<p>
  <img alt="status" src="https://img.shields.io/badge/status-em%20andamento-1f6f4a?style=flat-square">
  <img alt="backend" src="https://img.shields.io/badge/backend-ASP.NET%20Core%208-111111?style=flat-square">
  <img alt="frontend" src="https://img.shields.io/badge/frontend-React%20%2B%20Vite-111111?style=flat-square">
  <img alt="database" src="https://img.shields.io/badge/database-PostgreSQL-111111?style=flat-square">
</p>

<p>
Projeto em construção com foco em fluxo real de reembolso, rastreabilidade, autorização por papel e consistência operacional.
</p>

</div>

---

## Visão geral

O **NIO Ticket** é um sistema web interno para gerir o ciclo de **solicitações de reembolso corporativo**.

O produto continua exatamente neste domínio:

- colaborador cria e envia solicitação
- gestor aprova, recusa ou solicita complementação
- financeiro registra pagamento
- administrador mantém estruturas e acessos
- o sistema respeita escopo por centro de custo
- há workflow, auditoria, anexos protegidos, autenticação e autorização

Este repositório **não representa um produto concluído**. A base técnica já está sólida, mas o frontend e alguns refinamentos operacionais ainda seguem em evolução.

---

## Status atual

> **Importante**
>
> O projeto **ainda está em andamento**.  
> Já existe backend consistente, fluxo principal funcional e partes relevantes do frontend prontas, mas ainda há etapas pendentes antes de considerar a aplicação finalizada.

### Já funciona

- autenticação com sessão e renovação de token
- autorização por papel
- escopo por centro de custo
- criação e edição de rascunho
- envio de solicitação
- aprovação
- recusa
- solicitação de complementação
- registro de pagamento
- upload e download protegido de anexos
- histórico de workflow
- trilha de auditoria
- listagem e detalhe de solicitações
- geração de protocolo em PDF no frontend

### Ainda precisa evoluir

- acabamento visual final do dashboard
- alinhamento fino da interface com a referência visual aprovada
- fechamento completo das regras complementares no frontend
- revisão final de experiência ponta a ponta
- preparação de entrega/publicação

---

## Fluxo principal

```text
Colaborador
  -> cria rascunho
  -> anexa comprovantes
  -> envia solicitação

Gestor
  -> aprova
  -> recusa
  -> ou solicita complementação

Financeiro
  -> registra pagamento

Sistema
  -> audita ações
  -> mantém histórico
  -> protege anexos
  -> restringe acesso por papel e escopo
```

---

## Perfis do sistema

| Perfil | Responsabilidade |
|---|---|
| `Colaborador` | criar, editar, enviar e acompanhar suas solicitações |
| `Gestor` | aprovar, recusar e solicitar complementação dentro do escopo permitido |
| `Financeiro` | registrar pagamento e consultar fila financeira |
| `Administrador` | manter estruturas administrativas e acessos |

---

## Regras já cobertas

### Operacionais

- somente o colaborador dono pode editar rascunhos
- somente solicitações enviadas podem ser aprovadas ou recusadas
- solicitação com complementação pendente não pode ser aprovada
- pagamento só pode ser registrado em solicitação aprovada
- pagamento não pode ser duplicado
- valor pago deve corresponder ao valor aprovado

### Complementares

- categoria pode exigir comprovante obrigatório
- categoria pode impor prazo de envio
- sistema registra motivo padronizado de recusa
- sistema registra motivo padronizado de complementação
- aprovação exige justificativa registrada
- histórico carrega motivo e observação da decisão

---

## Arquitetura

### Backend

- `ASP.NET Core 8`
- `Entity Framework Core`
- `PostgreSQL`
- `JWT` com refresh token
- arquitetura em camadas

Estrutura principal:

- `backend/src/Reembolso.Api`
- `backend/src/Reembolso.Application`
- `backend/src/Reembolso.Domain`
- `backend/src/Reembolso.Infrastructure`

### Frontend

- `React`
- `Vite`
- `TypeScript`
- rotas públicas e protegidas
- geração de PDF no cliente

Estrutura principal:

- `frontend/src/pages`
- `frontend/src/features`
- `frontend/src/components`
- `frontend/src/layouts`
- `frontend/src/services`
- `frontend/src/types`
- `frontend/src/tests`

---

## Segurança

Este projeto foi organizado para **não expor segredos sensíveis no repositório**.

### Diretrizes adotadas

- sem credenciais reais no Git
- sem chave JWT no Git
- sem senha de banco no Git
- sem anexos sensíveis versionados
- sem storage privado exposto como conteúdo público
- configuração local privada fora do versionamento

### Importante

Os arquivos e scripts locais usados para ambiente de desenvolvimento **não devem ser publicados**.  
Eles existem apenas para execução segura na máquina local.

---

## Execução local

> A configuração local de banco, JWT e storage é privada e não deve ser commitada.

### Backend

O backend precisa de:

- conexão válida com PostgreSQL
- chave JWT válida
- diretório de armazenamento de anexos

### Frontend

O frontend roda com Vite e utiliza proxy para a API local.

---

## Qualidade

### Frontend

- build validado
- testes automatizados
- fluxo de PDF integrado

### Backend

- testes unitários
- testes de integração
- testes de segurança

---

## Roadmap resumido

### Curto prazo

- consolidar a rodada visual final do dashboard
- concluir os refinamentos restantes da fase atual
- fechar a experiência ponta a ponta com mais consistência

### Depois

- documentação final de entrega
- acabamento de apresentação
- publicação do projeto com narrativa mais fechada

---

## Situação honesta do projeto

Este repositório **já demonstra um sistema real**, com backend consistente e frontend funcional em áreas centrais.  
Ao mesmo tempo, ele **ainda não está finalizado** e não deve ser apresentado como produto totalmente pronto.

O foco atual é concluir a aplicação com qualidade, sem sacrificar:

- segurança
- rastreabilidade
- coerência de domínio
- consistência visual

---

## Observação final

Se você abrir o projeto e perceber partes ainda em construção, isso é esperado.

O compromisso aqui é evoluir de forma disciplinada:

- domínio preservado
- segurança preservada
- histórico de mudanças coerente
- nada sensível no GitHub
