# WorkFlow — Pipeline de Desarrollo

## Flujo del Proyecto

```
USUARIO
   │
   ▼
@build (orquestador primario)
   │
   ├──→ @plan ──── crea/actualiza task.md
   │
   ├──→ @code ──── implementa UNA tarea
   │
   ├──→ @review ──── audita y aprueba/rechaza
   │
   └──→ @explore ──── busca info en codebase
```

## Delegación entre Agentes

Todos los agentes usan `mode: all` y tienen `task: allow` para delegar entre sí.

### Flujo de una tarea:
1. `@build` recibe requerimiento del usuario
2. `@build` delega a `@plan` → `@plan` escribe `task.md`
3. `@build` delega a `@code` → `@code` implementa UNA tarea + `dotnet build`
4. `@build` delega a `@review` → `@review` audita + `dotnet build`
5. Si `@review` aprueba → siguiente tarea
6. Si `@review` rechaza → `@build` devuelve a `@code` con correcciones

### Flujo de bug:
1. `@build` recibe reporte de bug
2. `@build` delega a `@review` para diagnóstico (root cause)
3. `@review` identifica causa raíz
4. `@build` delega a `@code` con el diagnóstico
5. `@code` corrige + build
6. `@build` delega a `@review` para validación

## Comandos Disponibles:
- `/develop <requirement>` — Pipeline completo: plan → code → review
- `/reindex` — Regenera PROJECT_INDEX.json si existe

## Skills Disponibles:
- `incremental-implementation` — Entrega en slices verticales delgados
- `debugging-and-error-recovery` — Debug sistemático root-cause
- `code-review-and-quality` — Review multi-eje con quality gates
- `code-simplification` — Simplificar sin romper comportamiento
- `git-workflow-and-versioning` — Convenciones de git y versionado
