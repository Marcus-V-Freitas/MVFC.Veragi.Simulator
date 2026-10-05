# CreateReconciliationHandler

## Descrição
Registra um lançamento bancário e realiza o rateio automático do valor pago entre os contratos de antecipação ativos vinculados à conta corrente do estabelecimento, atualizando o ledger de conciliação.

- **Rota:** `POST /module/card-receivable/reconciliation/entry`
- **Retorno:** HTTP 201 sem corpo.

## Payload
**Requisição (JSON):**
```json
{
  "entryId": "d018583c-5e19-44f0-a9c4-9fe935da7d9b",
  "referenceDate": "2026-10-04",
  "merchant": "22185894000174",
  "acquirer": "01027058000191",
  "bankAccount": "12345-6",
  "value": 2500.00
}
```

**Resposta (HTTP 201):**
*Sem corpo (No Content/Created).*

## Diagrama
```mermaid
sequenceDiagram
    participant C as Cliente
    participant A as Minimal API
    participant H as CreateReconciliationHandler
    participant S as ReconciliationService
    participant L as AllocationService
    participant M as MongoDB
    C->>A: POST /reconciliation/entry
    A->>H: CreateReconciliationCommand(request)
    H->>S: ProcessEntryAsync(request)
    S->>M: Consulta contratos ativos e URs elegíveis
    M-->>S: Contratos
    S->>L: Calcula alocações por UR
    L-->>S: Alocações e valor não alocado
    S->>M: Persiste entrada em reconciliation_entries
    M-->>S: OK
    S-->>H: Sucesso
    H-->>A: Result(Success)
    A-->>C: 201 Created
```
