# Diagramas C4

Tres niveles, del contexto al detalle. Se escriben en Mermaid para que se versionen
junto al código y se rendericen directamente en GitHub: un diagrama en imagen se
desactualiza sin que nadie lo note.

---

## Nivel 1 — Contexto

Quién usa el submódulo y con qué sistemas habla.

```mermaid
graph TB
    operario["<b>Operario de tienda</b><br/>Solicita la impresión<br/>de una ETQ/LPN"]
    supervisor["<b>Supervisor / Administrador</b><br/>Autoriza reimpresiones<br/>y consulta la operación"]

    subgraph submodulo["Submódulo de Impresión de ETQ"]
        sistema["<b>Impresión de Etiquetas</b><br/>Valida reglas, simula la impresión<br/>y deja trazabilidad"]
    end

    wms["<b>Sistema origen (WMS/OMS)</b><br/>Genera documentos y ETQ<br/><i>simulado con datos mock</i>"]
    inventario["<b>Inventario por zona</b><br/>Disponibilidad y abastecimiento<br/><i>simulado con datos mock</i>"]
    impresora["<b>Impresora Zebra</b><br/>Recibe el ZPL<br/><i>impresión simulada</i>"]

    operario -->|"Consulta ETQ e imprime"| sistema
    supervisor -->|"Reimprime con motivo<br/>y consulta historial"| sistema
    sistema -.->|"Lee documentos,<br/>ETQ y productos"| wms
    sistema -.->|"Consulta disponibilidad<br/>por zona"| inventario
    sistema -.->|"Entrega el ZPL"| impresora

    classDef persona fill:#0b4f6c,stroke:#062f40,color:#fff
    classDef interno fill:#1668b3,stroke:#0d4577,color:#fff
    classDef externo fill:#6b7280,stroke:#374151,color:#fff
    class operario,supervisor persona
    class sistema interno
    class wms,inventario,impresora externo
```

Los tres sistemas externos aparecen punteados porque **están simulados**: el enunciado
pide operar desacoplado de sistemas corporativos reales. Se dibujan igual porque definen
las fronteras que la solución tendría en producción.

---

## Nivel 2 — Contenedores

Cómo se reparte la solución en piezas desplegables.

```mermaid
graph TB
    usuario["<b>Usuario operativo</b><br/>Navegador"]

    subgraph cloudflare["Cloudflare Pages"]
        spa["<b>homecenter-labelprinting-site</b><br/>Angular 20 · standalone + signals<br/>Build estático"]
    end

    subgraph render["Render"]
        api["<b>Homecenter.Microservice.Api.LabelPrinting</b><br/>.NET 8 Web API · Docker<br/>Reglas, auditoría y simulación"]
        db[("<b>PostgreSQL</b><br/>Usuarios, documentos, ETQ,<br/>inventario y auditoría")]
    end

    usuario -->|HTTPS| spa
    spa -->|"HTTPS · JSON<br/>JWT en Authorization"| api
    api -->|"Npgsql · EF Core"| db

    classDef persona fill:#0b4f6c,stroke:#062f40,color:#fff
    classDef contenedor fill:#1668b3,stroke:#0d4577,color:#fff
    classDef datos fill:#2d6a4f,stroke:#1b4332,color:#fff
    class usuario persona
    class spa,api contenedor
    class db datos
```

**Viven en dominios distintos**, y eso tiene tres consecuencias que no son
accidentales:

- CORS es requisito de funcionamiento, no un ajuste: sin el origen registrado, la
  aplicación no opera.
- El JWT viaja en el header `Authorization`, no en cookie, para evitar por completo la
  fricción de cookies cross-site.
- El `apiUrl` se resuelve en tiempo de build, así que la URL de Render debe conocerse
  **antes** de publicar el frontend.

---

## Nivel 3 — Componentes del API

Cómo fluye una solicitud de impresión por dentro.

```mermaid
graph TB
    subgraph host["Capa Api · Host"]
        mw["<b>Middleware</b><br/>CorrelationId · Errores<br/>Rate limiting · CORS"]
        ctrl["<b>Controllers</b><br/>Auth · Labels · Zones<br/>PrintRequests · Health"]
    end

    subgraph logic["Capa Logic"]
        uc["<b>ProcessPrintRequestUseCase</b><br/>Resuelve contexto, evalúa,<br/>imprime y audita"]
        engine["<b>PrintRuleEngine</b><br/>Ordena y corta en<br/>la primera violación"]
        rules["<b>Reglas R0–R4</b><br/>RequiredData · LabelExists<br/>DocumentStatus · ZoneAvailability<br/>ReprintPolicy"]
        sim["<b>PrintSimulator</b><br/>Evento lógico + ZPL"]
        enc["<b>AesEncryptionService</b><br/>AES-256-CBC"]
    end

    subgraph abs["Capa Abstractions"]
        repos["<b>Interfaces</b><br/>ILabelRepository · IZoneRepository<br/>IInventoryRepository · IPrintRequestRepository"]
    end

    subgraph ef["Capa EntityFramework"]
        impl["<b>Repositorios EF Core</b><br/>+ MockDataSeeder"]
    end

    db[("PostgreSQL")]

    ctrl --> uc
    mw -.->|"envuelve"| ctrl
    ctrl --> enc
    uc --> engine
    engine --> rules
    uc --> sim
    uc --> repos
    repos <-.->|"implementa"| impl
    impl --> db

    classDef capaApi fill:#1668b3,stroke:#0d4577,color:#fff
    classDef capaLogic fill:#2d6a4f,stroke:#1b4332,color:#fff
    classDef capaAbs fill:#7c5295,stroke:#4c3059,color:#fff
    classDef capaEf fill:#b45309,stroke:#78350f,color:#fff
    classDef datos fill:#374151,stroke:#111827,color:#fff
    class mw,ctrl capaApi
    class uc,engine,rules,sim,enc capaLogic
    class repos capaAbs
    class impl capaEf
    class db datos
```

La flecha entre `Abstractions` y `EntityFramework` apunta **hacia el dominio**: la capa
de datos implementa interfaces que declara el dominio, no al revés. Por eso
`PrintRuleEngine` y las reglas pueden probarse sin base de datos.

---

## Flujo de una solicitud de impresión

```mermaid
sequenceDiagram
    participant U as Operario
    participant F as Angular
    participant C as PrintRequestsController
    participant UC as ProcessPrintRequestUseCase
    participant E as PrintRuleEngine
    participant R as Repositorios
    participant P as PrintSimulator

    U->>F: LPN + zona
    F->>C: POST /api/print-requests<br/>(JWT en Authorization)
    C->>UC: ExecuteAsync(dto)

    UC->>R: Resolver etiqueta, zona,<br/>inventario e impresión previa
    R-->>UC: Contexto completo

    UC->>E: Evaluate(context)
    Note over E: R0 → R1 → R2 → R3 → R4<br/>corta en la primera violación
    E-->>UC: Traza + regla que falló (o ninguna)

    alt Todas las reglas se cumplen
        UC->>P: PrintAsync(label)
        P-->>UC: ZPL
    end

    UC->>R: Persistir PrintRequest + PrintAuditLogs
    Note over UC,R: SIEMPRE, apruebe o rechace

    alt Aprobada
        UC-->>C: 200 · success true · ZPL + legacy
    else Rechazo de negocio
        UC-->>C: 200 · success false · código y motivo
    end
    C-->>F: ApiResponse
    F-->>U: Banner Éxito / Rechazo + badge
```

Los dos puntos que este diagrama existe para dejar explícitos: **la auditoría se
persiste antes de responder y no depende del resultado**, y **el rechazo de negocio
viaja como 200**, porque la solicitud se procesó bien aunque la impresión no proceda.
