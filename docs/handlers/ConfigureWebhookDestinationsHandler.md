# ConfigureWebhookDestinationsHandler

## Descrição
Configura dinamicamente as URLs de destino para as entregas de webhooks de atualização de agenda e de contrato antecipado.

- **Rota:** `PUT /_simulator/webhooks`
- **Retorno:** HTTP 200 com os destinos persistidos.

## Payload
**Requisição (JSON):**
```json
{
  "scheduleUrl": "http://localhost:5090/webhooks/card-receivables/schedules/updated",
  "contractUrl": "http://localhost:5090/webhooks/card-receivable/schedules"
}
```

**Resposta (HTTP 200):**
```json
{
  "data": {
    "scheduleUrl": "http://localhost:5090/webhooks/card-receivables/schedules/updated",
    "contractUrl": "http://localhost:5090/webhooks/card-receivable/schedules",
    "persisted": true
  }
}
```

## Diagrama
```mermaid
sequenceDiagram
    participant C as Cliente
    participant A as Minimal API
    participant H as ConfigureWebhookDestinationsHandler
    participant S as WebhookRoutingService
    participant M as MongoDB
    C->>A: PUT /_simulator/webhooks
    A->>H: ConfigureWebhookDestinationsCommand(request)
    H->>S: ConfigureAsync(request)
    S->>M: Persiste URLs em webhook_routing
    M-->>S: OK
    S-->>H: WebhookDestinationsResponse
    H-->>A: Result(Response)
    A-->>C: 200 OK
```
