# ListScenariosHandler

## Descrição
Retorna todos os cenários de simulação ativos configurados na base de dados, exibindo alvos, CNPJs, contadores de chamadas e sequências restantes.

- **Rota:** `GET /_simulator/scenarios`
- **Retorno:** HTTP 200 com a lista de cenários configurados.

## Payload
Esta requisição não recebe corpo.

**Resposta (HTTP 200):**
```json
[
  {
    "target": "schedule-process",
    "merchantCnpj": "22185894000174",
    "failuresRemaining": 1,
    "failureStatusCode": 503,
    "holdProcessing": false,
    "calls": 1,
    "processingOutcomes": ["PROCESSED"],
    "contractStatuses": []
  }
]
```

## Diagrama
```mermaid
sequenceDiagram
    participant C as Cliente
    participant A as Minimal API
    participant H as ListScenariosHandler
    participant S as ScenarioService
    participant M as MongoDB
    C->>A: GET /_simulator/scenarios
    A->>H: ListScenariosCommand
    H->>S: ListAsync()
    S->>M: Consulta registros em simulation_scenarios
    M-->>S: Lista de cenários
    S-->>H: Array de SimulationScenarioResponse
    H-->>A: Result(Response)
    A-->>C: 200 OK
```
