# DeleteMerchantHandler

## Descrição
Executa a exclusão lógica de um estabelecimento comercial (merchant), inativando seu cadastro no simulador.

- **Rota:** `DELETE /module/card-receivable/merchants/{cnpj}`
- **Retorno:** HTTP 204 No Content.

## Payload
Esta requisição não recebe corpo e não retorna conteúdo.

**Parâmetros:**
- `cnpj` (URL, obrigatório): CNPJ de 14 dígitos do estabelecimento a ser removido.

**Resposta (HTTP 204):**
*Sem corpo.*

## Diagrama
```mermaid
sequenceDiagram
    participant C as Cliente
    participant A as Minimal API
    participant H as DeleteMerchantHandler
    participant S as MerchantService
    participant M as MongoDB
    C->>A: DELETE /merchants/{cnpj}
    A->>H: DeleteMerchantCommand(cnpj)
    H->>S: DeleteAsync(cnpj)
    S->>M: Atualiza status do estabelecimento para inativo em merchants
    M-->>S: OK
    S-->>H: Sucesso
    H-->>A: Result(Success)
    A-->>C: 204 No Content
```
