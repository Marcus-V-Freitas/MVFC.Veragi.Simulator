# ClearScenarioHandler

## Descrição
Remove a configuração de simulação ativa (falhas HTTP, status forçados, retenção de processamento) de um alvo especificado, de forma global ou restrita a um estabelecimento específico.

- **Rota:** `DELETE /_simulator/scenarios/{target}?merchantCnpj={cnpj}`
- **Retorno:** HTTP 200 com `{ "data": true }`.

## Payload
Esta requisição não possui corpo no request.

**Parâmetros:**
- `target` (URL, obrigatório): identificador do alvo (ex: `schedule-process`, `contract-process`, `acquirers-list`).
- `merchantCnpj` (Query string, opcional): CNPJ do estabelecimento. Se omitido, remove a regra global.

**Resposta (HTTP 200):**
```json
{
  "data": true
}
```

## Diagrama
```mermaid
sequenceDiagram
    participant C as Cliente
    participant A as Minimal API
    participant H as ClearScenarioHandler
    participant S as ScenarioService
    participant M as MongoDB
    C->>A: DELETE /_simulator/scenarios/{target}?merchantCnpj={cnpj}
    A->>H: ClearScenarioCommand
    H->>S: ClearAsync(target, cnpj)
    S->>M: Remove cenário de simulation_scenarios
    M-->>S: OK
    S-->>H: Result(true)
    H-->>A: Result(true)
    A-->>C: 200 OK
```
