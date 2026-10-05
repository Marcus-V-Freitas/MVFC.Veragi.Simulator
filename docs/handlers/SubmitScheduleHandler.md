# SubmitScheduleHandler

## Descrição
Registra a solicitação assíncrona de consulta de agenda de recebíveis futuros de um estabelecimento credenciado. Cria a operação com status inicial `PROCESSING` e devolve um `requestId` para acompanhamento e correlação com a entrega de webhook.

- **Rota:** `POST /module/card-receivable/schedules/query-requests`
- **Headers:** `Idempotency-Key` (UUID)
- **Retorno:** HTTP 202 Accepted com o identificador da solicitação (`requestId`).

## Payload
**Requisição (JSON):**
```json
{
  "merchantCnpj": "22185894000174",
  "queryType": "STANDARD"
}
```

**Resposta (HTTP 202):**
```json
{
  "data": {
    "requestId": "fc3ec865-f088-4e22-af68-5581c280d3dc",
    "status": "PROCESSING",
    "detail": "Solicitacao de agenda aceita e em processamento"
  }
}
```

## Diagrama
```mermaid
sequenceDiagram
    participant C as Cliente
    participant A as Minimal API
    participant H as SubmitScheduleHandler
    participant S as ScheduleService
    participant M as MongoDB
    C->>A: POST /schedules/query-requests (Idempotency-Key)
    A->>H: SubmitScheduleCommand(request)
    H->>S: SubmitAsync(request, idempotencyKey)
    S->>M: Valida merchant e cria operação PROCESSING em operations
    M-->>S: OK
    S-->>H: ScheduleQueryResponse
    H-->>A: Result(Response)
    A-->>C: 202 Accepted
```
