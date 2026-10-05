# RefreshScheduleHandler

## Descrição
Recalcula uma agenda existente que já havia sido apurada, incorporando novas vendas de cartão ou compromissos externos registrados posteriormente, e gera nova notificação de atualização de agenda.

- **Rota:** `POST /_simulator/schedules/{id}/refresh`
- **Retorno:** HTTP 200 com confirmação booleana.

## Payload
Esta requisição não recebe corpo.

**Parâmetros de URL:**
- `id`: identificador UUID da solicitação de agenda (`requestId`).

**Resposta (HTTP 200):**
```json
{
  "data": true
}
```

## Diagrama
```mermaid
sequenceDiagram
    participant C as Cliente
    participant A as Minimal API
    participant H as RefreshScheduleHandler
    participant P as OperationProcessor
    participant M as MongoDB
    C->>A: POST /_simulator/schedules/{id}/refresh
    A->>H: RefreshScheduleCommand(id)
    H->>P: RefreshScheduleAsync(id)
    P->>M: Recalcula saldos da agenda e enfileira entrega em webhook_deliveries
    M-->>P: OK
    P-->>H: Sucesso
    H-->>A: Result(true)
    A-->>C: 200 OK
```
