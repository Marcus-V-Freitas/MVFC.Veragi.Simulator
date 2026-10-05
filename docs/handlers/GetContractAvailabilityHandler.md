# GetContractAvailabilityHandler

## Descrição
Consulta a política ativa de disponibilidade de saldo para contratações de antecipação (`IMMEDIATE` ou `DEFERRED`).

- **Rota:** `GET /_simulator/contracts/availability`
- **Retorno:** HTTP 200 com a política vigente.

## Payload
Esta requisição não recebe corpo.

**Resposta (HTTP 200):**
```json
{
  "data": {
    "mode": "IMMEDIATE"
  }
}
```

## Diagrama
```mermaid
sequenceDiagram
    participant C as Cliente
    participant A as Minimal API
    participant H as GetContractAvailabilityHandler
    participant S as ContractAvailabilityService
    participant M as MongoDB
    C->>A: GET /_simulator/contracts/availability
    A->>H: GetContractAvailabilityCommand
    H->>S: GetCurrentAsync()
    S->>M: Consulta em contract_availability_configuration
    M-->>S: Configuração salva (ou padrão do appsettings)
    S-->>H: ContractAvailabilityResponse
    H-->>A: Result(Response)
    A-->>C: 200 OK
```
