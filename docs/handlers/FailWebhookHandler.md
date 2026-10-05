# FailWebhookHandler

## Descrição
Endpoint de diagnóstico utilizado em testes para simular indisponibilidade no recebimento de webhooks. Sempre responde com erro de servidor HTTP 503 Service Unavailable.

- **Rota:** `POST /_simulator/webhooks/fail`
- **Retorno:** HTTP 503 Service Unavailable.

## Payload
**Requisição:**
Sem corpo.

**Resposta (HTTP 503):**
```json
{
  "error": "Simulated failure"
}
```

## Diagrama
```mermaid
sequenceDiagram
    participant C as Dispatcher / Cliente
    participant A as Minimal API
    participant H as FailWebhookHandler
    C->>A: POST /_simulator/webhooks/fail
    A->>H: FailWebhookCommand
    H-->>A: Result(ServiceUnavailable)
    A-->>C: 503 Service Unavailable
```
