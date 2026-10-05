# ResetEnvironmentHandler

## Descrição
Executa a limpeza completa dos dados do simulador nas coleções do MongoDB (estabelecimentos, vendas, agendas, contratos, conciliações, cenários e webhooks), retornando a quantidade total de documentos removidos e restaurando o ambiente para o estado inicial.

- **Rota:** `POST /_simulator/reset`
- **Retorno:** HTTP 200 com a contagem de documentos removidos (`removedDocuments`).

## Payload
Esta requisição não recebe corpo.

**Resposta (HTTP 200):**
```json
{
  "data": {
    "removedDocuments": 42
  }
}
```

## Diagrama
```mermaid
sequenceDiagram
    participant C as Cliente
    participant A as Minimal API
    participant H as ResetEnvironmentHandler
    participant S as EnvironmentService
    participant M as MongoDB
    C->>A: POST /_simulator/reset
    A->>H: ResetEnvironmentCommand
    H->>S: ResetEnvironmentAsync()
    S->>M: DeleteMany em todas as coleções próprias
    M-->>S: Contagem de documentos excluídos
    S-->>H: EnvironmentResetResponse
    H-->>A: Result(Response)
    A-->>C: 200 OK
```
