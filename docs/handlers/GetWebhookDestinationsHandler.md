# GetWebhookDestinationsHandler

## Descrição
Consulta as URLs de destino efetivas e persistidas para entregas de webhooks de agenda e contratos de antecipação.

- **Rota:** `GET /_simulator/webhooks`
- **Retorno:** HTTP 200 com as URLs atuais.

## Payload
Esta requisição não recebe corpo.

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
    participant H as GetWebhookDestinationsHandler
    participant S as WebhookRoutingService
    participant M as MongoDB
    C->>A: GET /_simulator/webhooks
    A->>H: GetWebhookDestinationsCommand
    H->>S: GetDestinationsAsync()
    S->>M: Consulta documento em webhook_routing
    M-->>S: URLs cadastradas (ou padrão)
    S-->>H: WebhookDestinationsResponse
    H-->>A: Result(Response)
    A-->>C: 200 OK
```
