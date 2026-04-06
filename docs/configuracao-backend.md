# Configuração do backend

Este backend não versiona segredos, credenciais nem caminhos sensíveis.

As configurações obrigatórias devem ser fornecidas fora do repositório, por mecanismo compatível com o ASP.NET Core.

## Variáveis obrigatórias

| Chave | Finalidade | Obrigatória |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | Define a conexão principal com o banco PostgreSQL. | Sim |
| `Jwt__Issuer` | Define o emissor lógico dos tokens. | Sim |
| `Jwt__Audience` | Define a audiência lógica dos tokens. | Sim |
| `Jwt__SigningKey` | Define o segredo usado na assinatura dos JWTs. | Sim |
| `AttachmentStorage__RootPath` | Define o diretório local de armazenamento dos anexos. | Sim |

## Variáveis opcionais

| Chave | Finalidade | Obrigatória |
| --- | --- | --- |
| `Jwt__AccessTokenLifetimeMinutes` | Define a duração do access token em minutos. | Não |
| `Jwt__RefreshTokenLifetimeDays` | Define a duração do refresh token em dias. | Não |
| `AttachmentStorage__MaxFileSizeInBytes` | Define o tamanho máximo permitido para cada anexo. | Não |

## Restrições

- O diretório de anexos não pode apontar para `wwwroot`.
- O armazenamento de anexos não é servido como conteúdo estático.
- O download de anexos ocorre apenas por endpoint autenticado e autorizado.
