# ListArrangementsHandler

## Descrição
Retorna o catálogo de arranjos de pagamento suportados pelo simulador (bandeira e modalidade de crédito ou débito, ex: Visa Crédito `VCC`, Mastercard Crédito `MCC`).

- **Rota:** `GET /module/card-receivable/bases-control/payment-arrangements`
- **Retorno:** HTTP 200 com a lista de arranjos de pagamento.

## Payload
Esta requisição não recebe corpo.

**Resposta (HTTP 200):**
```json
{
  "data": [
    {
      "code": "VCC",
      "description": "VISA CREDITO"
    },
    {
      "code": "MCC",
      "description": "MASTERCARD CREDITO"
    }
  ]
}
```

## Diagrama
```mermaid
sequenceDiagram
    participant C as Cliente
    participant A as Minimal API
    participant H as ListArrangementsHandler
    participant S as CatalogService
    C->>A: GET /bases-control/payment-arrangements
    A->>H: ListArrangementsCommand
    H->>S: GetPaymentArrangements()
    S-->>H: Lista de arranjos do catálogo
    H-->>A: Result(Response)
    A-->>C: 200 OK
```
