# ConfigureContractAvailabilityHandler

## Descrição
Configura a política de validação de saldo para contratações de antecipação. Permite alternar entre recusa síncrona imediata (`IMMEDIATE`) e recusa diferida durante o ciclo de processamento (`DEFERRED`).

- **Rota:** `PUT /_simulator/contracts/availability`
- **Retorno:** HTTP 200 com a política persistida.

## Payload
**Requisição (JSON):**
```json
{
  "mode": "IMMEDIATE"
}
```
*Valores aceitos para `mode`: `"IMMEDIATE"` ou `"DEFERRED"`.*

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
    participant H as ConfigureContractAvailabilityHandler
    participant S as ContractAvailabilityService
    participant M as MongoDB
    C->>A: PUT /_simulator/contracts/availability
    A->>H: ConfigureContractAvailabilityCommand
    H->>S: ConfigureAsync(mode)
    S->>M: Salva em contract_availability_configuration
    M-->>S: OK
    S-->>H: ContractAvailabilityResponse
    H-->>A: Result(Response)
    A-->>C: 200 OK
```
