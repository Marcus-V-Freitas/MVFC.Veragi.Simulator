# RefreshScheduleHandler

## Descrição
Recalcula uma agenda existente que já havia sido apurada, incorporando novas vendas de cartão ou compromissos externos registrados posteriormente, e gera nova notificação de atualização de agenda.

Inclui reservas solicitadas por contratos pendentes e valores alcançados de contratos Active/Settled. A atualização automática após registro usa a mesma política. Cancelled/ContractSimulation liberam reservas e atualizam agendas afetadas somente quando os recebíveis mudam. O refresh explícito permanece disponível para regenerar o snapshot e conserva sua política existente de evento/deduplicação.

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
    H->>P: RepublishScheduleAsync(id)
    P->>M: Recalcula saldos da agenda e enfileira entrega em webhook_deliveries
    M-->>P: OK
    P-->>H: Sucesso
    H-->>A: Result(true)
    A-->>C: 200 OK
```
