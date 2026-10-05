# RunSimulationHandler

## Descrição
Força a execução de um ciclo imediato e determinístico de processamento das operações pendentes (agendas e contratos) e de envio dos webhooks na fila outbox, permitindo testes rápidos sem aguardar os timers dos schedulers em background.

- **Rota:** `POST /_simulator/run`
- **Retorno:** HTTP 200 com a contagem de agendas, contratos e entregas processadas.

## Payload
Esta requisição não recebe corpo.

**Resposta (HTTP 200):**
```json
{
  "data": {
    "schedulesProcessed": 1,
    "contractsProcessed": 1,
    "deliveriesDispatched": 2
  }
}
```

## Diagrama
```mermaid
sequenceDiagram
    participant C as Cliente / Script
    participant A as Minimal API
    participant H as RunSimulationHandler
    participant S as SimulationProcessingService
    participant O as OperationProcessor
    participant D as WebhookDispatcher
    C->>A: POST /_simulator/run
    A->>H: RunSimulationCommand
    H->>S: RunCycleAsync()
    S->>O: Processa pendências de agenda e contrato
    O-->>S: Contagem de operações
    S->>D: Despacha entregas de webhook pendentes
    D-->>S: Contagem de entregas
    S-->>H: SimulationRunResponse
    H-->>A: Result(Response)
    A-->>C: 200 OK
```
