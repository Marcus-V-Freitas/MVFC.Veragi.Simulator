# ListContractsHandler

## Descrição
Lista os contratos de antecipação registrados para um determinado estabelecimento credenciado, com filtro opcional por situação (`ACTIVE` ou `INACTIVE`).

- **Rota:** `GET /module/card-receivable/contracts?contractorCnpj={cnpj}&situation={situation}`
- **Retorno:** HTTP 200 com a lista de contratos.

## Payload
Esta requisição não recebe corpo.

**Parâmetros de Query:**
- `contractorCnpj` (obrigatório): CNPJ do estabelecimento contratante.
- `situation` (opcional): situação do contrato (`ACTIVE` ou `INACTIVE`).

**Resposta (HTTP 200):**
```json
{
  "data": [
    {
      "externalReference": "REF-CONTRATO-001",
      "status": 1,
      "statusDescription": "Ativo",
      "contractType": 2,
      "contractorCnpj": "22185894000174",
      "requestedAmount": 2500.00,
      "reachedAmount": 2500.00
    }
  ]
}
```

## Diagrama
```mermaid
sequenceDiagram
    participant C as Cliente
    participant A as Minimal API
    participant H as ListContractsHandler
    participant S as ContractService
    participant M as MongoDB
    C->>A: GET /contracts?contractorCnpj={cnpj}&situation={situation}
    A->>H: ListContractsCommand(cnpj, situation)
    H->>S: ListAsync(cnpj, situation)
    S->>M: Consulta contratos em operations filtrando por merchant e situação
    M-->>S: Contratos encontrados
    S-->>H: ContractListResponse
    H-->>A: Result(Response)
    A-->>C: 200 OK
```
