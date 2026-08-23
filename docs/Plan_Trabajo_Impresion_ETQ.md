# Plan de Trabajo — Submódulo de Impresión de ETQ (Prueba Técnica Homecenter)

## INFORMACIÓN GENERAL

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

- **Tipo:** Plan de trabajo por historias de usuario
- **Proyecto:** **Submódulo de Impresión de ETQ** — Prueba Técnica Dev Experto GTL Tienda
- **Duración:** 1 sprint de 2 días calendario (6–8 horas efectivas de desarrollo)
- **Fecha de inicio:** Sábado 22 de agosto de 2026
- **Fecha de fin:** Domingo 23 de agosto de 2026
- **Backend:** C# / .NET 8 Microservice Web API
- **Frontend:** Angular 20 (standalone + signals, con capa de facades)
- **Base de datos:** PostgreSQL (relacional)
- **Hosting backend:** Render — Web Service (Docker) + Render PostgreSQL
- **Hosting frontend:** Cloudflare Pages — build estático de Angular apuntando al API de Render
- **Responsable del desarrollo:** Andres Felipe Galeano Velasco
- **Asignación:** Backend y Frontend
- **Estado:** En planificación

## OBJETIVO DEL DESARROLLO

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Construir una solución **fullstack desacoplada** que reciba una solicitud de impresión sobre una **ETQ/LPN pre-generada**, resuelva el documento origen y los productos asociados, consulte disponibilidad e indicador de abastecimiento **por zona**, valide las reglas de negocio al momento de imprimir, simule la impresión y deje **trazabilidad completa** de impresiones y reimpresiones.

La generación de la etiqueta **NO** hace parte del alcance: la ETQ ya existe. Lo que se evalúa es criterio técnico: separación de capas, reglas de negocio explícitas y probadas, trazabilidad, seguridad, manejo de errores y documentación.

El desarrollo prioriza arquitectura clara y mantenible, separación frontend/backend, autenticación **JWT**, control de **roles operativos** y hardening de seguridad configurable desde `appsettings.json`.

## ALCANCE FUNCIONAL

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. **Autenticación y usuarios**
   - Login de usuarios operativos con JWT.
   - Roles `Operario`, `Supervisor` y `Admin`.
   - Usuarios semilla documentados para el evaluador.

2. **Resolución de ETQ/LPN**
   - Entrada funcional por **LPN o ETQ**, nunca por SKU.
   - Resolución de ETQ pre-generada, documento origen, productos asociados y ZPL.

3. **Validación de reglas al imprimir**
   - Existencia de la ETQ/LPN.
   - Estado del documento origen (rechazo si `ANULADA` o `DEVUELTA`).
   - Disponibilidad e indicador de abastecimiento por zona para todos los productos.

4. **Impresión simulada**
   - Confirmación lógica + evento de impresión + entrega del ZPL.

5. **Reimpresiones**
   - Detección automática de impresión previa por LPN.
   - Marca de reimpresión, motivo obligatorio y autorización por rol `Supervisor`/`Admin`.

6. **Trazabilidad y auditoría**
   - Registro de toda solicitud (aprobada o rechazada) con identificador, usuario, fecha/hora, ETQ/LPN, zona, resultado, motivo de rechazo y tipo de evento.
   - Consulta de historial con filtros y paginación.

7. **Seguridad**
   - JWT para autenticación y autorización por roles.
   - Hash seguro de contraseñas.
   - Cifrado/encriptación de datos sensibles frontend/backend cuando aplique.
   - Configuración de claves, JWT, CORS, cifrado y límites de solicitudes desde `appsettings.json`.
   - Rate limiting configurable por usuario autenticado, por IP y por política de endpoint.
   - CORS restringido y manejo de errores sin exponer información interna.

8. **Entregables**
   - **Dos repositorios Git públicos e independientes:** microservicio y sitio web.
   - `README.md` detallado con instalación, ejecución y credenciales de prueba.
   - Diagrama **C4 simplificado** y modelo de dominio.
   - Esquema de base de datos y mapeo BD → Backend → Frontend.
   - Swagger/OpenAPI y casos de prueba con evidencia.
   - Archivos mock de entrada/salida y datos semilla.
   - Documento de **soporte productivo** (escenario sección 7 del enunciado).
   - **Dos URLs hosteadas:** API en Render y aplicación web en Cloudflare Pages.

## HALLAZGOS DE LOS ANEXOS Y SUPUESTOS ASUMIDOS

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

| # | Hallazgo | Supuesto / decisión |
|---|---|---|
| H1 | `tableOrders.json` tiene un **error de sintaxis JSON**: falta la coma después de `"templateCode": "TPL-ETQ-STD-4X6"` (línea 16). | Se corrige en la semilla y se documenta el hallazgo en el README como evidencia de lectura del anexo. |
| H2 | `requetEtq.json` solo trae `lpn`, pero el frontend obligatorio (sección 9) exige **LPN + Zona + Usuario**. | El contrato acepta `lpn` obligatorio y `zone` como override del operador; el usuario **no** se recibe por body, se toma del JWT. Si no se envía zona, se usa la del documento origen. |
| H3 | `responseEtq.json` usa vocabulario distinto (`idEtiqueta`, `purchaseOrder`, `tcOrderId`, `sku`, `unidades`, `zpl`) al de `tableOrders.json`. | El response expone un bloque enriquecido propio **más** un bloque `legacy` compatible con `responseEtq.json`, para no romper al consumidor actual. |
| H4 | **No existe archivo de inventario** en los anexos, pero la Regla 3 depende de él. | Se crea `inventoryAvailability.json` (producto × zona → `availableQty`, `isStocked`) como fuente mock de la regla de disponibilidad. |
| H5 | El enunciado no define quién puede reimprimir. | Se asume control operativo real: la reimpresión exige rol `Supervisor`/`Admin` y **motivo obligatorio** (el enunciado lo valora positivamente). |
| H6 | El enunciado no define el código HTTP del rechazo de negocio. | Rechazo de negocio = HTTP `200` con envelope `success:false` + código y motivo (es una decisión válida del dominio, no un error técnico). Errores de forma = `400`; auth = `401/403`; límite = `429`. |

## ANEXOS DE ENTRADA — TRAZABILIDAD DE LOS JSON

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Los tres archivos entregados con el enunciado son la **fuente de verdad del contrato**. Ningún campo se inventa ni se descarta: cada uno tiene destino explícito en el modelo, en un DTO o en la semilla.

### 1. `requetEtq.json` → entrada del servicio

```json
{ "request": { "lpn": "olpn12345" } }
```

| Campo del anexo | Destino en la solución | Observación |
|---|---|---|
| `request.lpn` | `PrintRequestCreateDto.lpn` → `Labels.LpnId` | Única llave de entrada. Se conserva como campo obligatorio del request. |
| *(no existe)* `zone` | `PrintRequestCreateDto.zoneCode` | **Añadido** por exigencia de la sección 9 (el frontend pide Zona). Opcional; si falta, se usa `Documents.IdZone`. |
| *(no existe)* `user` | Claim `userName` del JWT | **No se recibe por body.** La sección 9 pide el campo Usuario en la UI; se muestra en solo lectura desde el token para que la auditoría no sea falsificable. |
| *(no existe)* `reprintReason` | `PrintRequestCreateDto.reprintReason` | **Añadido** para HU-04 del enunciado ("se valorará solicitar o almacenar un motivo de reimpresión"). |

> El request original se conserva compatible: enviar solo `{ "lpn": "..." }` sigue funcionando, resolviendo zona desde el documento origen.

### 2. `tableOrders.json` → datos mock / semilla

Estructura del anexo y su descomposición relacional (una vista de negocio plana se normaliza en 6 tablas):

| Campo del anexo | Tabla destino | Columna | Nota |
|---|---|---|---|
| `requestId` | `Documents` | `RequestId` | Se reutiliza como `tcOrderId` del bloque legacy. |
| `requestDateTime` | `Documents` | `RequestDateTime` | Se conserva el offset `-05:00` como `TIMESTAMPTZ`. |
| `requestedBy` | `Documents` | `RequestedBy` | Solicitante original del documento, distinto del usuario que imprime. |
| `zone` | `Zones` | `Code` + FK `Documents.IdZone` | Se normaliza a catálogo para alimentar `GET api/zones`. |
| `document.documentType` | `Documents` | `DocumentType` | `NOTA_PEDIDO`. |
| `document.documentNumber` | `Documents` | `DocumentNumber` | Se reutiliza como `purchaseOrder` del bloque legacy. |
| `document.status` | `Documents` | `Status` | Enum `DocumentStatus`. **Insumo directo de la Regla 2.** |
| `labels[].etqId` | `Labels` | `EtqId` | Único. |
| `labels[].lpnId` | `Labels` | `LpnId` | Único. **Llave de entrada e insumo de las Reglas 1 y 4.** |
| `labels[].isPreGenerated` | `Labels` | `IsPreGenerated` | Confirma que la generación de ETQ está fuera de alcance. |
| `labels[].templateCode` | `Labels` | `TemplateCode` | `TPL-ETQ-STD-4X6`. |
| `labels[].zpl` | `Labels` | `Zpl` | En el anexo trae el placeholder `"FORMATO ZPL"`; en la semilla se sustituye por el ZPL real del anexo 3. |
| `products[].productCode` | `Products` | `ProductCode` | |
| `products[].productDescription` | `Products` | `ProductDescription` | |
| `products[].requestedQty` | `DocumentProducts` | `RequestedQty` | **Insumo de la Regla 3** (se compara contra `availableQty`). |
| `products[].uom` | `DocumentProducts` | `Uom` | `UND`, `PAR`. |
| `reprintReason` | `PrintRequests` | `ReprintReason` | En el anexo llega `null`; se llena al procesar una reimpresión. |

**Hallazgos concretos de este archivo (ver H1):** el JSON **no parsea** — falta la coma tras `"templateCode": "TPL-ETQ-STD-4X6"` (línea 16). Además el bloque `labels` es un arreglo pero `products` no está asociado a una label específica, sino al documento: se modela `DocumentProducts` colgando del documento, y todas las labels del documento comparten sus productos.

### 3. `responseEtq.json` → contrato de salida

```json
{ "response": { "idEtiqueta": "12345", "purchaseOrder": "PO123456", "tcOrderId": "TC78910",
                "sku": "SKU001", "unidades": 25, "zpl": "^XA...^XZ" } }
```

| Campo del anexo | Origen en la solución | Nota |
|---|---|---|
| `idEtiqueta` | `Labels.EtqId` | El anexo usa numérico plano (`"12345"`) y `tableOrders` usa `ETQ-10001`. Se expone el valor de `EtqId`. |
| `purchaseOrder` | `Documents.DocumentNumber` | El anexo dice `PO123456`; en el mock real es la nota pedido `NP-458721`. |
| `tcOrderId` | `Documents.RequestId` | Identificador transaccional. |
| `sku` | `DocumentProducts[0].ProductCode` | **Limitación estructural del anexo:** es un solo SKU escalar, pero el enunciado exige explícitamente que la ETQ/LPN pueda cargar **varios** productos. |
| `unidades` | `DocumentProducts[0].RequestedQty` | Mismo problema: escalar frente a N productos. |
| `zpl` | `Labels.Zpl` | El ZPL completo del anexo se usa como contenido real de la semilla. |

**Decisión sobre este conflicto (H3):** el response de éxito devuelve un bloque enriquecido con **el arreglo completo de productos**, y adicionalmente un bloque `legacy` con la forma exacta de `responseEtq.json` tomando el primer producto, para no romper a un consumidor existente. Cuando la ETQ tiene más de un producto, el bloque `legacy` incluye `hasMultipleProducts: true` para que el consumidor sepa que está viendo una vista parcial. Perder productos en silencio sería un error funcional; documentar la degradación no lo es.

### Archivos mock resultantes en el repositorio

| Archivo | Origen | Contenido |
|---|---|---|
| `mocks/requestEtq.sample.json` | Anexo 1, tal cual | Ejemplo de request mínimo. |
| `mocks/orders.json` | Anexo 2, **corregido y ampliado** | El documento original `NP-458721` + los casos de prueba adicionales (documento `ANULADA`, documento `DEVUELTA`). |
| `mocks/labels.json` | Anexo 2 (bloque `labels`) | ETQ/LPN pre-generadas con el ZPL real del anexo 3. |
| `mocks/inventoryAvailability.json` | **Creado (H4)** | Producto × zona → `availableQty`, `isStocked`. Cubre CP-04, CP-05 y CP-06. |
| `mocks/responseEtq.sample.json` | Anexo 3, tal cual | Ejemplo del contrato de salida legacy. |

## PROYECTOS NUEVOS

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

| Proyecto | Tipo | Framework | Descripción |
|---|---|---|---|
| `Homecenter.Microservice.Api.LabelPrinting` | Backend Microservice | .NET 8 | Microservicio con APIs REST para autenticación, resolución de ETQ/LPN, validación de reglas de impresión, impresión simulada, auditoría e historial. **Hosting: Render (Docker Web Service).** |
| `homecenter-labelprinting-site` | Frontend | Angular 20 | Aplicación web operativa para impresión de etiquetas y consulta de historial. **Hosting: Cloudflare Pages (build estático).** |
| Base de datos PostgreSQL | Persistencia | SQL relacional | Modelo de usuarios, roles, zonas, documentos, etiquetas, productos, inventario por zona, solicitudes y auditoría de impresión. **Hosting: Render PostgreSQL.** |

### Topología de despliegue

```text
Navegador
   │  HTTPS
   ▼
Cloudflare Pages  ──────────────►  Render Web Service (Docker)  ──────►  Render PostgreSQL
homecenter-label-      CORS +      Homecenter.Microservice.Api.            LabelPrinting
printing-site          JWT         LabelPrinting  ·  /api/health           (managed)
(build estático)
```

**Implicaciones de tener frontend y backend en dominios distintos:**

- **CORS es obligatorio y bloqueante**, no un detalle: el origen de Cloudflare Pages debe estar en `Cors:AllowedOrigins` del API antes de la primera prueba end-to-end. Se incluye también el dominio `*.pages.dev` de preview si se usa.
- El JWT se envía por header `Authorization`, **no** por cookie: evita por completo el problema de cookies cross-site (`SameSite=None`) entre los dos dominios.
- El `apiUrl` del frontend se resuelve por `environment.production.ts` en tiempo de build; el build de Cloudflare debe ejecutarse **después** de conocer la URL definitiva de Render.
- El cold start del free tier de Render afecta la primera petición del frontend: la UI debe mostrar estado de carga y un mensaje claro si el API tarda o no responde.

## REPOSITORIOS

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Cada proyecto vive en **su propio repositorio Git**, con ciclo de vida y despliegue independientes.

| Repositorio | Contenido | Despliegue |
|---|---|---|
| `Homecenter.Microservice.Api.LabelPrinting` | `src/` (6 capas), `tests/`, `mocks/` (datos semilla) y `docs/` (plan, arquitectura, C4, runbook, casos de prueba) | Render — Web Service Docker + Render PostgreSQL |
| `homecenter-labelprinting-site` | Aplicación Angular 20 completa | Cloudflare Pages — build estático |

**Consecuencias de la separación:**

- La documentación vive en el repositorio del microservicio: es donde reside el contrato, el modelo de dominio y las reglas de negocio. El repositorio del frontend solo lleva su propio `README.md`.
- El contrato de API es la **única** frontera entre ambos repositorios: un cambio de DTO obliga a actualizar los contratos técnicos de este documento y los modelos TypeScript del sitio, sin acoplamiento de código compartido.
- Los mocks son datos semilla del backend, no fixtures del frontend: el sitio nunca lee archivos JSON locales, siempre consume el API.
- No existe repositorio contenedor: la carpeta local que hoy agrupa ambos proyectos es solo conveniencia del entorno de desarrollo.

## ESTRUCTURA PROPUESTA DEL BACKEND (.NET 8)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

```text
Homecenter.Microservice.Api.LabelPrinting/
├── src/
│   ├── Homecenter.Microservice.Api.LabelPrinting/                      # API Host: Controllers, Program.cs, middlewares, appsettings
│   ├── Homecenter.Microservice.Api.LabelPrinting.Logic/                # Casos de uso, motor de reglas, impresión simulada, auditoría
│   ├── Homecenter.Microservice.Api.LabelPrinting.Abstractions/         # Interfaces de repositorios, lógica y servicios
│   ├── Homecenter.Microservice.Api.LabelPrinting.EntityFramework/      # DbContext, configuraciones EF, repositorios, seeder
│   ├── Homecenter.Microservice.Api.LabelPrinting.Entities/             # Entidades de dominio
│   └── Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object/ # DTOs, filtros, responses y enums
├── tests/
│   ├── Homecenter.Microservice.Api.LabelPrinting.Logic.Tests/          # xUnit — reglas de negocio R1..R5 y casos de uso
│   └── Homecenter.Microservice.Api.LabelPrinting.Api.Tests/            # Pruebas de integración de endpoints (si el tiempo lo permite)
├── mocks/                                                              # orders.json, labels.json, inventoryAvailability.json
└── Dockerfile
```

**Regla de dependencias (hexagonal):** `Api` → `Logic` → `Abstractions` ← `EntityFramework`. `Entities` y `Data.Transfer.Object` son transversales. El motor de reglas vive en `Logic` y **no conoce EF ni HTTP**, lo que permite probarlo en aislamiento.

## ESTRUCTURA PROPUESTA DEL FRONTEND (Angular 20)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

```text
homecenter-labelprinting-site/
├── src/
│   ├── app/
│   │   ├── auth/
│   │   │   ├── components/          # login
│   │   │   ├── guards/              # auth.guard.ts, role.guard.ts (functional guards)
│   │   │   ├── interceptors/        # jwt.interceptor.ts, error.interceptor.ts
│   │   │   ├── services/            # auth.service.ts, token-storage.service.ts
│   │   │   └── facades/             # auth.facade.ts
│   │   ├── printing/
│   │   │   ├── components/
│   │   │   │   ├── print-form/          # LPN, Zona, Usuario (readonly del JWT), motivo de reimpresión
│   │   │   │   ├── label-preview/       # ETQ, documento, productos y disponibilidad por zona
│   │   │   │   ├── print-result/        # banner Éxito / Rechazo + motivo + badge Impresión|Reimpresión
│   │   │   │   ├── history-filters/
│   │   │   │   └── history-table/       # tabla responsive → tarjetas en móvil
│   │   │   ├── models/              # interfaces TypeScript alineadas a los DTOs
│   │   │   ├── services/            # printing.service.ts, catalog.service.ts
│   │   │   └── facades/             # printing.facade.ts, history.facade.ts
│   │   └── shared/
│   │       ├── ui/                  # badge, alert, spinner, empty-state, pagination
│   │       └── utils/               # crypto.util.ts (cifrado de payload sensible)
│   └── environments/                # environment.ts / environment.production.ts (apiUrl)
└── README.md
```

**Estilo:** componentes **standalone**, estado con **signals**, `HttpClient` con `provideHttpClient(withInterceptors(...))`, reactive forms para validaciones, Tailwind para responsive. La capa de **facades** aísla los componentes del transporte HTTP, igual que en el patrón de referencia.

## MODELO DE DATOS PROPUESTO

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

| Tabla | Propósito |
|---|---|
| `Users` | Usuarios operativos del sistema. |
| `Roles` | Roles disponibles: `Operario`, `Supervisor`, `Admin`. |
| `UserRoles` | Relación entre usuarios y roles. |
| `Zones` | Zonas logísticas de la tienda (ej. `ZONA-PICKING-A`). |
| `Documents` | Documento origen (nota pedido / orden) con su estado. |
| `Labels` | ETQ/LPN pre-generadas, con plantilla y ZPL. |
| `Products` | Catálogo de productos. |
| `DocumentProducts` | Productos asociados al documento/ETQ con cantidad solicitada y UOM. |
| `InventoryAvailability` | Disponibilidad e indicador de abastecimiento por producto y zona. |
| `PrintRequests` | Solicitud de impresión procesada: resultado, tipo de evento y motivo. |
| `PrintAuditLogs` | Trazabilidad detallada por regla evaluada dentro de cada solicitud. |

**Restricciones funcionales del modelo**

- `Labels.LpnId` y `Labels.EtqId` deben ser **únicos**: son la llave funcional de entrada.
- `InventoryAvailability` debe tener índice único por `IdProduct` + `IdZone`.
- `PrintRequests` debe indexar `LpnId` + `DateCreated`: es la consulta que resuelve la detección de reimpresión y el historial.
- `Documents.Status` debe permitir como mínimo `CREADA`, `LIBERADA`, `ANULADA`, `DEVUELTA`.
- `PrintRequests.EventType` solo admite `PRINT` o `REPRINT`; `Result` solo `APPROVED` o `REJECTED`.
- Toda solicitud —aprobada o rechazada— **debe** generar registro en `PrintRequests`. La auditoría nunca es condicional.
- `ReprintReason` es obligatorio cuando `EventType = REPRINT`.
- Las tablas funcionales incluyen `State` para eliminación lógica.
- `Users.PasswordHash` y `Users.PasswordSalt` no se exponen en DTOs ni modelos frontend.

## MAPEO BASE DE DATOS → BACKEND (.NET 8) → FRONTEND (Angular 20)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

**Regla general de nomenclatura:** columnas de base de datos y propiedades C# en PascalCase; propiedades TypeScript en camelCase. Los nombres de clases, DTOs, entidades, servicios y endpoints deben mantenerse alineados entre backend y frontend para evitar inconsistencias de contrato.

### Tabla: Users — Usuarios operativos

| Columna BD | Tipo SQL | Propiedad C# (Entity) | Propiedad C# (DTO) | Propiedad TypeScript | HU |
|---|---|---|---|---|---|
| Id | INT | Id | Id | id | HU-002 |
| UserName | VARCHAR(100) | UserName | UserName | userName | HU-002 |
| FullName | VARCHAR(150) | FullName | FullName | fullName | HU-002 |
| PasswordHash | VARCHAR(500) | PasswordHash | — | — | HU-002 |
| PasswordSalt | VARCHAR(500) | PasswordSalt | — | — | HU-002 |
| IsActive | BOOLEAN | IsActive | IsActive | isActive | HU-002 |
| LastLoginDate | TIMESTAMPTZ NULL | LastLoginDate | LastLoginDate | lastLoginDate | HU-002 |
| State | BOOLEAN | State | — | — | — |
| DateCreated | TIMESTAMPTZ | DateCreated | DateCreated | dateCreated | — |

### Tabla: Roles / UserRoles — Autorización

| Columna BD | Tipo SQL | Propiedad C# (Entity) | Propiedad C# (DTO) | Propiedad TypeScript | HU |
|---|---|---|---|---|---|
| Roles.Id | INT | Id | Id | id | HU-002 |
| Roles.Name | VARCHAR(50) | Name | Name | name | HU-002 |
| Roles.Description | VARCHAR(200) NULL | Description | Description | description | HU-002 |
| UserRoles.IdUser | INT | IdUser | — | — | HU-002 |
| UserRoles.IdRole | INT | IdRole | — | — | HU-002 |

### Tabla: Zones — Zonas logísticas

| Columna BD | Tipo SQL | Propiedad C# (Entity) | Propiedad C# (DTO) | Propiedad TypeScript | HU |
|---|---|---|---|---|---|
| Id | INT | Id | Id | id | HU-005 |
| Code | VARCHAR(50) | Code | Code | code | HU-005 |
| Name | VARCHAR(150) | Name | Name | name | HU-005 |
| State | BOOLEAN | State | — | — | — |

### Tabla: Documents — Documento origen

| Columna BD | Tipo SQL | Propiedad C# (Entity) | Propiedad C# (DTO) | Propiedad TypeScript | HU |
|---|---|---|---|---|---|
| Id | INT | Id | Id | id | HU-004 |
| RequestId | VARCHAR(50) | RequestId | RequestId | requestId | HU-004 |
| DocumentType | VARCHAR(30) | DocumentType | DocumentType | documentType | HU-004 |
| DocumentNumber | VARCHAR(50) | DocumentNumber | DocumentNumber | documentNumber | HU-004 |
| Status | VARCHAR(20) | Status (`DocumentStatus`) | Status | status | HU-006 |
| IdZone | INT | IdZone | ZoneCode | zoneCode | HU-005 |
| RequestedBy | VARCHAR(100) | RequestedBy | RequestedBy | requestedBy | HU-004 |
| RequestDateTime | TIMESTAMPTZ | RequestDateTime | RequestDateTime | requestDateTime | HU-004 |
| State | BOOLEAN | State | — | — | — |

### Tabla: Labels — ETQ/LPN pre-generadas

| Columna BD | Tipo SQL | Propiedad C# (Entity) | Propiedad C# (DTO) | Propiedad TypeScript | HU |
|---|---|---|---|---|---|
| Id | INT | Id | Id | id | HU-004 |
| IdDocument | INT | IdDocument | — | — | HU-004 |
| EtqId | VARCHAR(50) UNIQUE | EtqId | EtqId | etqId | HU-004 |
| LpnId | VARCHAR(50) UNIQUE | LpnId | LpnId | lpnId | HU-004 |
| IsPreGenerated | BOOLEAN | IsPreGenerated | IsPreGenerated | isPreGenerated | HU-004 |
| TemplateCode | VARCHAR(50) | TemplateCode | TemplateCode | templateCode | HU-004 |
| Zpl | TEXT | Zpl | Zpl | zpl | HU-004 |
| State | BOOLEAN | State | — | — | — |

### Tabla: Products / DocumentProducts — Productos asociados

| Columna BD | Tipo SQL | Propiedad C# (Entity) | Propiedad C# (DTO) | Propiedad TypeScript | HU |
|---|---|---|---|---|---|
| Products.Id | INT | Id | Id | id | HU-004 |
| Products.ProductCode | VARCHAR(50) | ProductCode | ProductCode | productCode | HU-004 |
| Products.ProductDescription | VARCHAR(200) | ProductDescription | ProductDescription | productDescription | HU-004 |
| DocumentProducts.IdDocument | INT | IdDocument | — | — | HU-004 |
| DocumentProducts.IdProduct | INT | IdProduct | — | — | HU-004 |
| DocumentProducts.RequestedQty | NUMERIC(18,2) | RequestedQty | RequestedQty | requestedQty | HU-004 |
| DocumentProducts.Uom | VARCHAR(10) | Uom | Uom | uom | HU-004 |

### Tabla: InventoryAvailability — Disponibilidad por zona

| Columna BD | Tipo SQL | Propiedad C# (Entity) | Propiedad C# (DTO) | Propiedad TypeScript | HU |
|---|---|---|---|---|---|
| Id | INT | Id | Id | id | HU-005 |
| IdProduct | INT | IdProduct | ProductCode | productCode | HU-005 |
| IdZone | INT | IdZone | ZoneCode | zoneCode | HU-005 |
| AvailableQty | NUMERIC(18,2) | AvailableQty | AvailableQty | availableQty | HU-005 |
| IsStocked | BOOLEAN | IsStocked | IsStocked | isStocked | HU-005 |
| LastUpdateDate | TIMESTAMPTZ | LastUpdateDate | LastUpdateDate | lastUpdateDate | HU-005 |

### Tabla: PrintRequests — Solicitudes procesadas (auditoría)

| Columna BD | Tipo SQL | Propiedad C# (Entity) | Propiedad C# (DTO) | Propiedad TypeScript | HU |
|---|---|---|---|---|---|
| Id | INT | Id | Id | id | HU-007, HU-008 |
| CorrelationId | UUID | CorrelationId | CorrelationId | correlationId | HU-008 |
| EtqId | VARCHAR(50) NULL | EtqId | EtqId | etqId | HU-008 |
| LpnId | VARCHAR(50) | LpnId | LpnId | lpnId | HU-008 |
| IdZone | INT NULL | IdZone | ZoneCode | zoneCode | HU-008 |
| IdUser | INT | IdUser | UserName | userName | HU-008 |
| DocumentNumber | VARCHAR(50) NULL | DocumentNumber | DocumentNumber | documentNumber | HU-008 |
| Result | VARCHAR(20) | Result (`PrintResult`) | Result | result | HU-008 |
| EventType | VARCHAR(20) | EventType (`PrintEventType`) | EventType | eventType | HU-007 |
| RejectionCode | VARCHAR(50) NULL | RejectionCode | RejectionCode | rejectionCode | HU-008 |
| RejectionMessage | VARCHAR(500) NULL | RejectionMessage | RejectionMessage | rejectionMessage | HU-008 |
| ReprintReason | VARCHAR(300) NULL | ReprintReason | ReprintReason | reprintReason | HU-007 |
| ProcessedAt | TIMESTAMPTZ | ProcessedAt | ProcessedAt | processedAt | HU-008 |
| State | BOOLEAN | State | — | — | — |

### Tabla: PrintAuditLogs — Trazabilidad por regla

| Columna BD | Tipo SQL | Propiedad C# (Entity) | Propiedad C# (DTO) | Propiedad TypeScript | HU |
|---|---|---|---|---|---|
| Id | INT | Id | Id | id | HU-008 |
| IdPrintRequest | INT | IdPrintRequest | — | — | HU-008 |
| RuleCode | VARCHAR(50) | RuleCode | RuleCode | ruleCode | HU-008 |
| Passed | BOOLEAN | Passed | Passed | passed | HU-008 |
| Detail | VARCHAR(500) NULL | Detail | Detail | detail | HU-008 |
| EvaluatedAt | TIMESTAMPTZ | EvaluatedAt | EvaluatedAt | evaluatedAt | HU-008 |

### DTOs principales asociados al mapeo

| DTO Backend | Modelo Frontend | Uso principal | HU |
|---|---|---|---|
| `LoginRequestDto` | `LoginRequestDto` | Solicitud de autenticación | HU-002 |
| `LoginResponseDto` | `LoginResponseDto` | JWT, expiración y datos mínimos de sesión | HU-002 |
| `AuthUserDto` | `AuthUserDto` | Usuario autenticado y sus roles | HU-002 |
| `LabelDetailDto` | `LabelDetailDto` | Preview de ETQ, documento, productos y disponibilidad | HU-004, HU-005 |
| `PrintRequestCreateDto` | `PrintRequestCreateDto` | Solicitud de impresión (`lpn`, `zoneCode`, `reprintReason`) | HU-004, HU-007 |
| `PrintResultDto` | `PrintResultDto` | Resultado: éxito/rechazo, motivo, tipo de evento, ZPL y bloque legacy | HU-004..HU-007 |
| `PrintHistoryItemDto` | `PrintHistoryItemDto` | Fila del historial de impresiones | HU-008 |
| `PrintHistoryFilterDto` | `PrintHistoryFilterDto` | Filtros y paginación del historial | HU-008 |
| `ZoneDto` | `ZoneDto` | Catálogo de zonas para el selector | HU-005 |
| `ApiResponse<T>` | `ApiResponse<T>` | Envelope uniforme de toda respuesta | Transversal |

## REGLAS DE NEGOCIO — MOTOR DE VALIDACIÓN

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Contrato `IPrintRule` con una implementación por regla y un `PrintRuleEngine` que las evalúa **en orden** y corta en la primera violación, registrando cada evaluación en `PrintAuditLogs`.

| Regla | Implementación (`.Logic/Rules/`) | Condición de rechazo | Código | HU |
|---|---|---|---|---|
| R0 | `RequiredDataRule` | Falta LPN, zona o usuario autenticado | `MISSING_REQUIRED_DATA` | HU-004 |
| R1 | `LabelExistsRule` | La ETQ/LPN no existe en los datos mock | `LPN_NOT_FOUND` | HU-004 |
| R2 | `DocumentStatusRule` | Documento en estado `ANULADA` o `DEVUELTA` | `INVALID_DOCUMENT_STATUS` | HU-006 |
| R3 | `ZoneAvailabilityRule` | Algún producto con `availableQty < requestedQty` o `isStocked = false` en la zona solicitada | `INSUFFICIENT_INVENTORY` / `NOT_STOCKED` | HU-005 |
| R4 | `ReprintPolicyRule` | Existe impresión previa aprobada para el LPN → marca `REPRINT`; rechaza si falta motivo o el rol no autoriza | `REPRINT_REASON_REQUIRED` / `REPRINT_NOT_AUTHORIZED` | HU-007 |
| R5 | `PrintSimulator` | — (éxito) genera evento lógico, entrega ZPL y persiste auditoría | — | HU-004 |

**Detalle de R3:** el rechazo debe explicar **qué producto** falló y por qué (`details[]` con `productCode`, `requestedQty`, `availableQty`, `isStocked`). Un rechazo genérico no cumple el criterio de aceptación de la HU-02 del enunciado.

## ROLES DEL SISTEMA

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

El backend valida autorización mediante JWT y políticas/atributos de rol; el frontend oculta o bloquea vistas mediante guards e interceptor, **sin reemplazar** la validación obligatoria del backend.

| Rol | Código Backend | Código Frontend | Permisos Clave | HU |
|---|---|---|---|---|
| Operario de tienda | `Operario` | `Operario` | Iniciar sesión, consultar ETQ/LPN, solicitar impresión, consultar **su propio** historial. | HU-002, HU-004, HU-005, HU-008 |
| Supervisor de operación | `Supervisor` | `Supervisor` | Todo lo del operario, más **autorizar reimpresiones** con motivo y consultar el historial completo de la tienda. | HU-002, HU-007, HU-008 |
| Administrador | `Admin` | `Admin` | Todo lo del supervisor, más consultar indicadores operativos y administrar los datos mock de referencia. | HU-002, HU-007, HU-008 |
| Público / Anónimo | `Anonymous` | `Anonymous` | Solo endpoints públicos: login y health check. Rate limiting por IP. | HU-002 |

### Reglas de autorización por rol

- `Operario` puede imprimir, pero **no** reimprimir: si el LPN ya fue impreso, el backend responde `REPRINT_NOT_AUTHORIZED`.
- `Operario` solo consulta las solicitudes cuyo `IdUser` coincide con el del JWT; el filtro se aplica **en backend**, no en frontend.
- `Supervisor` y `Admin` pueden reimprimir informando `reprintReason` obligatorio.
- `Supervisor` y `Admin` consultan el historial completo sin filtro por usuario.
- Los endpoints protegidos validan token JWT válido, rol y usuario activo.
- El frontend implementa `authGuard` y `roleGuard`, pero la autorización definitiva siempre queda en backend.

## CONFIGURACIONES BACKEND — APPSETTINGS.JSON

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Todo lo que varíe entre ambientes se parametriza desde `appsettings.json` o sus equivalentes (`appsettings.Development.json`, variables de entorno de Render). **No** se versionan valores sensibles reales.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=LabelPrinting;Username=***;Password=***"
  },
  "Jwt": {
    "Issuer": "Homecenter.LabelPrinting",
    "Audience": "homecenter-labelprinting-site",
    "SecretKey": "CHANGE_ME_FROM_ENVIRONMENT",
    "ExpirationMinutes": 60
  },
  "Encryption": {
    "Enabled": true,
    "Algorithm": "AES",
    "Key": "CHANGE_ME_FROM_ENVIRONMENT",
    "IV": "CHANGE_ME_FROM_ENVIRONMENT"
  },
  "RateLimiting": {
    "Enabled": true,
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "QueueLimit": 0,
    "ApplyByAuthenticatedUser": true,
    "ApplyByIpForAnonymous": true,
    "RejectedStatusCode": 429,
    "Policies": {
      "AuthEndpoints":     { "PermitLimit": 10, "WindowSeconds": 60 },
      "PrintingEndpoints": { "PermitLimit": 30, "WindowSeconds": 60 },
      "QueryEndpoints":    { "PermitLimit": 60, "WindowSeconds": 60 }
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://homecenter-labelprinting-site.pages.dev"
    ]
  },
  "Printing": {
    "SimulationMode": "LogicalEvent",
    "OutputDirectory": "./output/zpl",
    "PersistZplFile": true
  },
  "Seed": {
    "Enabled": true,
    "MocksPath": "./mocks"
  },
  "Swagger": { "Enabled": true }
}
```

### Criterios técnicos de configuración

- Las configuraciones se leen con opciones tipadas (`IOptions<JwtOptions>`, `IOptions<RateLimitingOptions>`, etc.), no con strings mágicos dispersos.
- El secret JWT, la llave de cifrado y la cadena de conexión se inyectan como variables de entorno en Render (`Jwt__SecretKey`, `Encryption__Key`, `ConnectionStrings__DefaultConnection`, `Cors__AllowedOrigins__1` con el dominio de Cloudflare Pages).
- El rate limiting debe ajustarse sin recompilar; al excederse, HTTP `429` con mensaje controlado.
- La política `AuthEndpoints` es la más restrictiva; `PrintingEndpoints` protege el endpoint transaccional contra ráfagas del piso de tienda.
- Swagger se habilita solo si `Swagger:Enabled` está activo — en la entrega se deja **encendido** en Render para que el evaluador pueda probar, y se documenta esa decisión explícitamente como excepción consciente.

## CONTRATOS TÉCNICOS PRINCIPALES — API ENDPOINTS

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

| # | Verbo | Ruta | Descripción | Rol |
|---|---|---|---|---|
| 1 | POST | `api/auth/login` | Autenticar usuario y retornar JWT | Público |
| 2 | GET | `api/zones` | Catálogo de zonas para el selector | Operario / Supervisor / Admin |
| 3 | GET | `api/labels/{lpn}` | Resolver ETQ, documento, productos y disponibilidad | Operario / Supervisor / Admin |
| 4 | POST | `api/print-requests` | Procesar solicitud de impresión o reimpresión | Operario (impresión) / Supervisor / Admin (reimpresión) |
| 5 | GET | `api/print-requests/history` | Consultar historial con filtros y paginación | Operario (propio) / Supervisor / Admin (total) |
| 6 | GET | `api/admin/dashboard` | Indicadores operativos de impresión | Admin |
| 7 | GET | `api/health` | Validar disponibilidad del servicio | Técnico / Público controlado |

## CONTRATOS TÉCNICOS DETALLADOS — API ENDPOINTS

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

**CRÍTICO:** Estos contratos deben ser respetados por backend y frontend. Cualquier cambio debe actualizar este documento, los DTOs backend, los modelos TypeScript y los servicios/facades asociados.

Todas las respuestas usan el envelope:

```json
{ "success": true, "data": {}, "error": null, "meta": null }
```

```json
{ "success": false, "data": null,
  "error": { "code": "INSUFFICIENT_INVENTORY", "message": "texto legible", "details": [] },
  "meta": null }
```

### Endpoint 1: Login de Usuario

| Atributo | Valor |
|---|---|
| Verbo | POST |
| Ruta | `api/auth/login` |
| Controller | `AuthController` |
| Método Backend | `LoginAsync(LoginRequestDto dto)` |
| Método Frontend Service | `authService.login(dto: LoginRequestDto): Observable<LoginResponseDto>` |
| Método Frontend Facade | `authFacade.login(dto: LoginRequestDto): Observable<LoginResponseDto>` |
| Auth | Público |
| Rate Limiting | Policy: `AuthEndpoints` |
| HU | HU-002 |

**Request Body — LoginRequestDto**

```json
{ "userName": "string (required)", "password": "string (required)" }
```

**Response 200 OK — LoginResponseDto**

```json
{
  "accessToken": "jwt-token",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "user": { "id": 1, "userName": "operario.tienda", "fullName": "Operario Tienda 01", "roles": ["Operario"] }
}
```

**Validaciones**

- Credenciales inválidas → HTTP `401` con mensaje controlado y genérico (no revelar si el usuario existe).
- Usuario inactivo → HTTP `403`.
- El JWT incluye `sub`, `userName` y `role`, y se firma con la configuración de `appsettings.json`.
- Superar `AuthEndpoints` → HTTP `429`.

### Endpoint 2: Consultar Zonas

| Atributo | Valor |
|---|---|
| Verbo | GET |
| Ruta | `api/zones` |
| Controller | `ZonesController` |
| Método Backend | `GetAllAsync()` |
| Método Frontend Service | `catalogService.getZones(): Observable<ZoneDto[]>` |
| Método Frontend Facade | `printingFacade.loadZones(): Observable<ZoneDto[]>` |
| Auth | `Operario` / `Supervisor` / `Admin` |
| Rate Limiting | Policy: `QueryEndpoints` |
| HU | HU-005, HU-009 |

**Response 200 OK — ZoneDto[]**

```json
[{ "id": 1, "code": "ZONA-PICKING-A", "name": "Zona Picking A" }]
```

### Endpoint 3: Resolver ETQ/LPN

| Atributo | Valor |
|---|---|
| Verbo | GET |
| Ruta | `api/labels/{lpn}` |
| Controller | `LabelsController` |
| Método Backend | `GetByLpnAsync(string lpn, [FromQuery] string zoneCode)` |
| Método Frontend Service | `printingService.getLabel(lpn: string, zoneCode: string): Observable<LabelDetailDto>` |
| Método Frontend Facade | `printingFacade.loadLabel(lpn: string, zoneCode: string): Observable<LabelDetailDto>` |
| Auth | `Operario` / `Supervisor` / `Admin` |
| Rate Limiting | Policy: `QueryEndpoints` |
| HU | HU-004, HU-005 |

**Response 200 OK — LabelDetailDto**

```json
{
  "etqId": "ETQ-10001",
  "lpnId": "LPN-000987654",
  "isPreGenerated": true,
  "templateCode": "TPL-ETQ-STD-4X6",
  "document": { "documentType": "NOTA_PEDIDO", "documentNumber": "NP-458721", "status": "LIBERADA" },
  "zoneCode": "ZONA-PICKING-A",
  "products": [
    { "productCode": "PROD-001", "productDescription": "Martillo 16oz", "requestedQty": 2, "uom": "UND",
      "availableQty": 10, "isStocked": true, "isEligible": true }
  ],
  "hasPreviousPrint": false,
  "canPrint": true
}
```

**Validaciones**

- LPN inexistente → HTTP `404` con `LPN_NOT_FOUND`.
- Este endpoint **no** imprime ni audita: es solo lectura para alimentar el preview de la UI. La validación vinculante ocurre siempre al imprimir (criterio explícito del enunciado).
- `zoneCode` es opcional; si no se envía, se usa la zona del documento origen.

### Endpoint 4: Procesar Solicitud de Impresión

| Atributo | Valor |
|---|---|
| Verbo | POST |
| Ruta | `api/print-requests` |
| Controller | `PrintRequestsController` |
| Método Backend | `ProcessAsync(PrintRequestCreateDto dto)` |
| Método Frontend Service | `printingService.print(dto: PrintRequestCreateDto): Observable<PrintResultDto>` |
| Método Frontend Facade | `printingFacade.print(dto: PrintRequestCreateDto): Observable<PrintResultDto>` |
| Auth | `Operario` (impresión) / `Supervisor`, `Admin` (reimpresión) |
| Rate Limiting | Policy: `PrintingEndpoints` |
| HU | HU-004, HU-005, HU-006, HU-007 |

**Request Body — PrintRequestCreateDto**

```json
{ "lpn": "LPN-000987654", "zoneCode": "ZONA-PICKING-A", "reprintReason": "string (opcional, obligatorio si es reimpresión)" }
```

**Response 200 OK (éxito) — PrintResultDto**

```json
{
  "success": true,
  "data": {
    "correlationId": "0f2b...",
    "result": "APPROVED",
    "eventType": "PRINT",
    "etqId": "ETQ-10001",
    "lpnId": "LPN-000987654",
    "zoneCode": "ZONA-PICKING-A",
    "userName": "operario.tienda",
    "processedAt": "2026-08-23T10:15:00-05:00",
    "zpl": "^XA...^XZ",
    "products": [
      { "productCode": "PROD-001", "productDescription": "Martillo 16oz", "requestedQty": 2, "uom": "UND" },
      { "productCode": "PROD-002", "productDescription": "Guantes de seguridad", "requestedQty": 1, "uom": "PAR" }
    ],
    "legacy": { "idEtiqueta": "ETQ-10001", "purchaseOrder": "NP-458721", "tcOrderId": "REQ-20260605-001",
                "sku": "PROD-001", "unidades": 2, "zpl": "^XA...^XZ", "hasMultipleProducts": true }
  },
  "error": null
}
```

**Response 200 OK (rechazo de negocio)**

```json
{
  "success": false,
  "data": { "correlationId": "0f2b...", "result": "REJECTED", "eventType": "PRINT" },
  "error": {
    "code": "INSUFFICIENT_INVENTORY",
    "message": "La zona ZONA-PICKING-A no tiene disponibilidad suficiente para 1 producto.",
    "details": [{ "productCode": "PROD-002", "requestedQty": 5, "availableQty": 1, "isStocked": true }]
  }
}
```

**Validaciones y flujo interno**

- El usuario se obtiene del JWT; **no** se recibe por body.
- Se ejecuta el motor de reglas R0→R4; la primera violación corta el flujo.
- Si existe impresión previa aprobada para el LPN, se marca `eventType: REPRINT`, se exige `reprintReason` y se valida el rol autorizado.
- **Toda** solicitud —aprobada o rechazada— persiste en `PrintRequests` + `PrintAuditLogs`.
- En éxito se genera el evento lógico de impresión y, si `Printing:PersistZplFile` está activo, el archivo `.zpl` de salida.
- Errores de forma → `400`; sin token → `401`; rol insuficiente → `403`; ráfaga → `429`.

### Endpoint 5: Consultar Historial de Impresiones

| Atributo | Valor |
|---|---|
| Verbo | GET |
| Ruta | `api/print-requests/history` |
| Controller | `PrintRequestsController` |
| Método Backend | `GetHistoryAsync([FromQuery] PrintHistoryFilterDto filter)` |
| Método Frontend Service | `printingService.getHistory(filter: PrintHistoryFilterDto): Observable<PagedResult<PrintHistoryItemDto>>` |
| Método Frontend Facade | `historyFacade.loadHistory(filter: PrintHistoryFilterDto)` |
| Auth | `Operario` (solo propio) / `Supervisor`, `Admin` (todo) |
| Rate Limiting | Policy: `QueryEndpoints` |
| HU | HU-008 |

**Query params — PrintHistoryFilterDto**

`lpn`, `zoneCode`, `userName`, `result`, `eventType`, `dateFrom`, `dateTo`, `page` (default 1), `pageSize` (default 20, máx 100).

**Response 200 OK — PagedResult<PrintHistoryItemDto>**

```json
{
  "success": true,
  "data": [
    { "id": 1, "correlationId": "0f2b...", "etqId": "ETQ-10001", "lpnId": "LPN-000987654",
      "zoneCode": "ZONA-PICKING-A", "userName": "operario.tienda", "processedAt": "2026-08-23T10:15:00-05:00",
      "result": "APPROVED", "eventType": "PRINT", "rejectionCode": null, "rejectionMessage": null, "reprintReason": null }
  ],
  "meta": { "total": 42, "page": 1, "pageSize": 20 }
}
```

**Validaciones**

- El filtro por usuario para el rol `Operario` se **fuerza en backend** desde el JWT; un operario no puede consultar el historial de otro manipulando el query string.
- Orden por `processedAt` descendente.
- `pageSize` acotado para evitar consultas no paginadas.

### Endpoint 6: Dashboard Administrativo

| Atributo | Valor |
|---|---|
| Verbo | GET |
| Ruta | `api/admin/dashboard` |
| Controller | `AdminDashboardController` |
| Método Backend | `GetDashboardAsync()` |
| Método Frontend Service | `adminDashboardService.getDashboard(): Observable<AdminDashboardDto>` |
| Método Frontend Facade | `adminDashboardFacade.loadDashboard()` |
| Auth | `Admin` |
| Rate Limiting | Policy: `QueryEndpoints` |
| HU | HU-008, HU-012 |

**Response 200 OK — AdminDashboardDto**

```json
{ "totalRequests": 120, "approved": 95, "rejected": 25, "reprints": 12,
  "rejectionsByCode": { "LPN_NOT_FOUND": 8, "INVALID_DOCUMENT_STATUS": 5, "INSUFFICIENT_INVENTORY": 12 } }
```

> Estos indicadores son además la base de la respuesta al **escenario de soporte productivo** (sección 7 del enunciado): permiten ver si el pico de fallas es por regla de negocio o por falla técnica.

### Endpoint 7: Health Check

| Atributo | Valor |
|---|---|
| Verbo | GET |
| Ruta | `api/health` |
| Controller | `HealthController` |
| Auth | Técnico / Público controlado |
| Rate Limiting | Policy: `QueryEndpoints` |
| HU | HU-001, HU-012 |

```json
{ "status": "Healthy", "service": "Homecenter.Microservice.Api.LabelPrinting",
  "database": "Healthy", "timestamp": "2026-08-23T14:00:00-05:00" }
```

### Frontend Services — Contrato Consolidado

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  login(dto: LoginRequestDto): Observable<LoginResponseDto>;
}

@Injectable({ providedIn: 'root' })
export class CatalogService {
  getZones(): Observable<ZoneDto[]>;
}

@Injectable({ providedIn: 'root' })
export class PrintingService {
  getLabel(lpn: string, zoneCode: string): Observable<LabelDetailDto>;
  print(dto: PrintRequestCreateDto): Observable<PrintResultDto>;
  getHistory(filter: PrintHistoryFilterDto): Observable<PagedResult<PrintHistoryItemDto>>;
}

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
  getDashboard(): Observable<AdminDashboardDto>;
}
```

### Consideraciones transversales para todos los endpoints

- Todos los endpoints protegidos validan JWT y rol en backend.
- Los endpoints públicos aplican rate limiting por IP; los autenticados, por usuario.
- Todo request recibe un `correlationId` (header `X-Correlation-Id` si viene, generado si no) que viaja al log y a la auditoría — es la llave para el diagnóstico de incidentes.
- Los errores retornan mensajes controlados, sin stack trace ni información sensible.
- No se registran en logs tokens, contraseñas, llaves ni payloads cifrados.

## RESUMEN DE HISTORIAS DE USUARIO

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

| # | Historia de Usuario | Backend | Frontend | SP | Prioridad | Ruta crítica | Bloque objetivo |
|---|---|---:|---:|---:|---|---|---|
| HU-001 | Setup técnico, arquitectura base y repositorio | Sí | Sí | 2 | Alta | Sí | Día 1 — B1 |
| HU-002 | Autenticación JWT, roles y seguridad base | Sí | Sí | 3 | Alta | Sí | Día 1 — B2 |
| HU-003 | Modelo de datos, mocks y seed en PostgreSQL | Sí | No | 2 | Alta | Sí | Día 1 — B2 |
| HU-004 | Procesar solicitud de impresión sobre ETQ/LPN *(HU-01 enunciado)* | Sí | Sí | 3 | Alta | Sí | Día 1 — B3 |
| HU-005 | Validar disponibilidad y abastecimiento por zona *(HU-02)* | Sí | Sí | 2 | Alta | Sí | Día 1 — B3 |
| HU-006 | Bloquear estados inválidos del documento origen *(HU-03)* | Sí | Sí | 1 | Alta | Sí | Día 1 — B3 |
| HU-007 | Marcar y auditar reimpresiones *(HU-04)* | Sí | Sí | 2 | Alta | Sí | Día 1 — B4 |
| HU-008 | Consultar historial de impresiones *(HU-05)* | Sí | Sí | 2 | Alta | Sí | Día 1 — B4 |
| HU-009 | Frontend de impresión e historial *(sección 9)* | No | Sí | 4 | Alta | Sí | Día 2 — B5, B6 |
| HU-010 | Pruebas unitarias de reglas críticas | Sí | No | 2 | Alta | Sí | Día 2 — B6 |
| HU-011 | Hardening: rate limiting, cifrado, CORS, errores | Sí | Sí | 2 | Media | No | Día 2 — B7 |
| HU-012 | Documentación, C4, runbook de soporte y despliegue | Sí | Sí | 3 | Alta | Sí | Día 2 — B8 |
|  | **Total** |  |  | **28 SP** |  |  |  |

---

# SPRINT 1 — Submódulo de Impresión de ETQ

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

- **Duración:** 2 días calendario / 6–8 horas efectivas
- **Inicio:** Sábado 22 de agosto de 2026
- **Fin:** Domingo 23 de agosto de 2026
- **Responsable:** Andres Felipe Galeano Velasco
- **Sprint Goal:** Entregar una solución fullstack desacoplada que valide las 5 reglas de impresión sobre ETQ/LPN pre-generadas, con auditoría persistente, reimpresiones controladas por rol, UI operativa, pruebas de reglas, documentación C4 y despliegue accesible en Render.

## HU-001: Setup técnico, arquitectura base y repositorio

- **Story Points:** 2 · **Prioridad:** Alta · **Bloque:** Día 1 — B1

### User Story

Como desarrollador del sistema, necesito configurar la solución backend, frontend, base de datos y repositorio, para iniciar el desarrollo con una arquitectura clara, separada y mantenible.

### Criterios de aceptación

- [ ] Dos repositorios Git independientes: `Homecenter.Microservice.Api.LabelPrinting` (con `src`, `tests`, `mocks` y `docs`) y `homecenter-labelprinting-site`.
- [ ] Backend `Homecenter.Microservice.Api.LabelPrinting` en .NET 8 con las 6 capas definidas.
- [ ] Frontend `homecenter-labelprinting-site` en Angular 20 con Tailwind configurado.
- [ ] Conexión a PostgreSQL configurada y `api/health` respondiendo `Healthy`.
- [ ] Backend y frontend compilan sin errores.

### Subtareas

**Backend**
- Crear solución y proyectos por capas: API, Logic, Abstractions, EntityFramework, Entities, Data.Transfer.Object.
- Configurar EF Core + Npgsql y `LabelPrintingDbContext`.
- Configurar Swagger, health check y envelope `ApiResponse<T>`.
- Crear secciones iniciales de `appsettings.json`: `Jwt`, `Encryption`, `RateLimiting`, `Cors`, `Printing`, `Seed`, `Swagger`.

**Frontend**
- Crear proyecto Angular 20 standalone, routing y Tailwind.
- Crear estructura `auth/`, `printing/`, `shared/` y `environments/`.

## HU-002: Autenticación JWT, roles y seguridad base

- **Story Points:** 3 · **Prioridad:** Alta · **Bloque:** Día 1 — B2

### User Story

Como usuario operativo, necesito iniciar sesión de forma segura, para acceder al submódulo de impresión según mi rol y que toda acción quede atribuida a mi identidad.

### Criterios de aceptación

- [ ] Un usuario puede iniciar sesión con credenciales válidas y recibe JWT.
- [ ] El token incluye usuario y rol; las contraseñas se almacenan con hash + salt.
- [ ] Existen roles `Operario`, `Supervisor` y `Admin` con usuarios semilla documentados.
- [ ] Las rutas protegidas exigen JWT válido; rol insuficiente responde `403`.
- [ ] El frontend adjunta el JWT mediante interceptor y protege rutas con guards.
- [ ] El usuario de la solicitud de impresión se toma del token, nunca del body.

### Subtareas

**Backend**
- Entidades `User`, `Role`, `UserRole` + configuraciones EF.
- Endpoint `POST api/auth/login` con `JwtOptions` tipadas.
- Hash de contraseña (PBKDF2/BCrypt) y validación de usuario activo.
- Políticas de autorización por rol y seeder de los 3 usuarios de prueba.

**Frontend**
- `LoginComponent`, `AuthService`, `AuthFacade`, `TokenStorageService`.
- `jwtInterceptor`, `errorInterceptor`, `authGuard`, `roleGuard`.

## HU-003: Modelo de datos, mocks y seed en PostgreSQL

- **Story Points:** 2 · **Prioridad:** Alta · **Bloque:** Día 1 — B2

### User Story

Como sistema, necesito cargar datos mock de órdenes, ETQ/LPN, productos e inventario por zona, para operar completamente desacoplado de sistemas corporativos reales.

### Criterios de aceptación

- [ ] Existen `mocks/orders.json`, `mocks/labels.json` e `mocks/inventoryAvailability.json` versionados.
- [ ] El JSON del anexo `tableOrders.json` se corrige (hallazgo H1) y se documenta.
- [ ] Al arrancar, un seeder **idempotente** carga los mocks a PostgreSQL si `Seed:Enabled` está activo.
- [ ] Los datos semilla cubren los 5 escenarios de prueba: éxito, LPN inexistente, documento anulado, sin disponibilidad, no abastecido.

### Subtareas

- Entidades `Zone`, `Document`, `Label`, `Product`, `DocumentProduct`, `InventoryAvailability`, `PrintRequest`, `PrintAuditLog`.
- Configuraciones EF con índices únicos (`Labels.LpnId`, `InventoryAvailability` por producto+zona).
- Migración inicial y `SeedHostedService` idempotente.
- Repositorios sobre las interfaces de `Abstractions`.

## HU-004: Procesar solicitud de impresión sobre una ETQ/LPN pre-generada

*(HU-01 del enunciado)*

- **Story Points:** 3 · **Prioridad:** Alta · **Bloque:** Día 1 — B3

### User Story

Como usuario operativo, quiero enviar una solicitud de impresión usando una ETQ o LPN ya pre-generada, para que el sistema identifique el contexto del pedido y cargue los productos asociados antes de validar la impresión.

### Criterios de aceptación

- [ ] La solución recibe una solicitud con identificador de ETQ o LPN (nunca SKU).
- [ ] Recupera de los datos mock los productos asociados a la ETQ/LPN.
- [ ] Identifica zona solicitada, documento origen y usuario solicitante (desde el JWT).
- [ ] Si faltan datos obligatorios, rechaza informando el motivo (`MISSING_REQUIRED_DATA`).
- [ ] Si la ETQ/LPN no existe, rechaza con `LPN_NOT_FOUND` **(Regla 1)**.
- [ ] En éxito devuelve el ZPL y el bloque `legacy` compatible con `responseEtq.json`.

### Subtareas

- `PrintRequestCreateDto` con validaciones de entrada.
- `ResolveLabelUseCase` y `ProcessPrintRequestUseCase` en `Logic`.
- Reglas `RequiredDataRule` (R0) y `LabelExistsRule` (R1).
- `PrintSimulator`: evento lógico + archivo `.zpl` opcional.
- Endpoints `GET api/labels/{lpn}` y `POST api/print-requests`.

## HU-005: Validar disponibilidad de inventario y abastecimiento por zona

*(HU-02 del enunciado)*

- **Story Points:** 2 · **Prioridad:** Alta · **Bloque:** Día 1 — B3

### User Story

Como sistema, quiero consultar la disponibilidad de inventario por zona para los productos asociados a la ETQ/LPN, con el fin de determinar si la impresión puede ejecutarse.

### Criterios de aceptación

- [ ] Se consulta disponibilidad e indicador de abastecimiento por zona.
- [ ] La validación se ejecuta **al momento de imprimir**, no al resolver la etiqueta.
- [ ] Solo se permite imprimir si **todos** los productos cumplen `availableQty >= requestedQty` **y** `isStocked = true` en la zona solicitada **(Regla 3)**.
- [ ] Si uno o más productos no cumplen, la respuesta detalla **qué producto** y por qué.

### Subtareas

- `ZoneAvailabilityRule` con detalle por producto en `error.details[]`.
- `InventoryRepository.GetByProductsAndZoneAsync(...)` en una sola consulta (evitar N+1).
- Endpoint `GET api/zones` para el selector del frontend.

## HU-006: Bloquear impresión para estados inválidos del documento origen

*(HU-03 del enunciado)*

- **Story Points:** 1 · **Prioridad:** Alta · **Bloque:** Día 1 — B3

### User Story

Como usuario operativo, quiero que la solución valide el estado del documento origen antes de imprimir, para evitar impresiones sobre documentos anulados o devueltos.

### Criterios de aceptación

- [ ] Se evalúa el estado del documento (`CREADA`, `LIBERADA`, `ANULADA`, `DEVUELTA`).
- [ ] Si es `ANULADA` o `DEVUELTA`, la impresión se rechaza **(Regla 2)**.
- [ ] El motivo del rechazo queda en el resultado **y** en la auditoría.

### Subtareas

- Enum `DocumentStatus` y `DocumentStatusRule`.
- Persistencia de `RejectionCode` y `RejectionMessage` en `PrintRequests`.

## HU-007: Marcar y auditar reimpresiones

*(HU-04 del enunciado)*

- **Story Points:** 2 · **Prioridad:** Alta · **Bloque:** Día 1 — B4

### User Story

Como usuario operativo, quiero que el sistema identifique si una ETQ/LPN ya fue impresa y marque la solicitud como reimpresión, para mantener control y trazabilidad.

### Criterios de aceptación

- [ ] Se identifica si existe impresión previa aprobada para la misma ETQ/LPN **(Regla 4)**.
- [ ] Si corresponde, la respuesta marca el evento como `REPRINT`.
- [ ] La auditoría guarda fecha/hora, usuario, resultado y tipo de evento.
- [ ] La reimpresión exige **motivo** (`reprintReason`) y rol `Supervisor`/`Admin`.
- [ ] Un `Operario` que intenta reimprimir recibe `REPRINT_NOT_AUTHORIZED`.

### Subtareas

- `ReprintPolicyRule` consultando `PrintRequests` por LPN + `Result=APPROVED`.
- Validación de rol dentro de la regla, no en el controller (la política es de negocio).
- Campo `ReprintReason` en entidad, DTO y formulario condicional del frontend.

## HU-008: Consultar historial de impresiones

*(HU-05 del enunciado)*

- **Story Points:** 2 · **Prioridad:** Alta · **Bloque:** Día 1 — B4

### User Story

Como usuario o evaluador técnico, quiero consultar el historial de impresiones y reimpresiones, para evidenciar trazabilidad, soporte y control operativo.

### Criterios de aceptación

- [ ] Se pueden consultar todas las solicitudes procesadas.
- [ ] Cada registro expone ETQ, LPN, zona, usuario, fecha/hora, resultado, tipo de evento y motivo.
- [ ] La consulta admite filtros y paginación.
- [ ] Un `Operario` solo ve sus propias solicitudes; `Supervisor`/`Admin` ven todo (filtro forzado en backend).

### Subtareas

- `PrintHistoryFilterDto`, `PagedResult<T>` y `GetPrintHistoryUseCase`.
- Endpoint `GET api/print-requests/history` con orden descendente por fecha.
- Endpoint `GET api/admin/dashboard` con indicadores agregados.

## HU-009: Frontend de impresión e historial

*(Sección 9 del enunciado — obligatorio)*

- **Story Points:** 4 · **Prioridad:** Alta · **Bloque:** Día 2 — B5, B6

### User Story

Como usuario operativo, necesito una interfaz simple para consultar una ETQ/LPN, seleccionar zona, imprimir y consultar el historial, para operar el submódulo sin herramientas técnicas.

### Criterios de aceptación

- [ ] Pantalla de impresión con campos **LPN, Zona y Usuario** (usuario en solo lectura desde el JWT).
- [ ] Resultado visible como **Éxito / Rechazo + Motivo**, con badge `Impresión` o `Reimpresión`.
- [ ] Campo de motivo de reimpresión que aparece y se vuelve obligatorio cuando el LPN ya fue impreso.
- [ ] Pantalla de historial con ETQ, LPN, zona, usuario, fecha, resultado y tipo de evento.
- [ ] Validaciones de formulario y manejo de errores del API visibles al usuario (incluye `429` y `403`).
- [ ] Diseño responsive: la tabla del historial colapsa a tarjetas en móvil.
- [ ] Componentización clara con estados de carga, error y vacío.

### Subtareas

- `PrintFormComponent` (reactive form), `LabelPreviewComponent`, `PrintResultComponent`.
- `HistoryFiltersComponent`, `HistoryTableComponent` con paginación.
- `PrintingFacade` y `HistoryFacade` con signals.
- Componentes `shared/ui`: `badge`, `alert`, `spinner`, `empty-state`, `pagination`.

## HU-010: Pruebas unitarias de reglas críticas

- **Story Points:** 2 · **Prioridad:** Alta · **Bloque:** Día 2 — B6

### User Story

Como evaluador técnico, necesito evidencia de pruebas automatizadas sobre las reglas más críticas, para confiar en que la lógica de negocio está verificada y no solo demostrada.

### Criterios de aceptación

- [ ] Pruebas unitarias para R0..R4 con casos de éxito y de rechazo.
- [ ] Casos límite cubiertos: cantidad exacta (`availableQty == requestedQty`), producto con stock pero `isStocked=false`, documento `DEVUELTA`, segunda impresión → `REPRINT`, reimpresión sin motivo, reimpresión por `Operario`.
- [ ] Pruebas del caso de uso `ProcessPrintRequestUseCase` con repositorios dobles en memoria.
- [ ] Todas las pruebas pasan y se documenta el resultado en `docs/test-cases.md`.

### Subtareas

- Proyecto `...Logic.Tests` con xUnit + FluentAssertions, patrón AAA y nombres descriptivos.
- Builders de datos de prueba para documentos, etiquetas e inventario.
- Ejecución de `dotnet test` con evidencia capturada.

## HU-011: Hardening — rate limiting, cifrado, CORS y manejo de errores

- **Story Points:** 2 · **Prioridad:** Media · **Bloque:** Día 2 — B7

### User Story

Como responsable del sistema, necesito que la solución esté endurecida frente a abuso y fuga de información, para que sea apta para un entorno operativo real.

### Criterios de aceptación

- [ ] Rate limiting habilitado y configurable desde `appsettings.json`, con política restrictiva en login y controlada en impresión.
- [ ] Al superar el límite, el backend responde `429` con mensaje controlado y el frontend lo muestra al usuario.
- [ ] CORS restringido al origen del frontend desde configuración.
- [ ] Middleware global de errores: sin stack trace ni datos sensibles en la respuesta.
- [ ] Secretos (JWT, llave de cifrado, cadena de conexión) fuera del código versionado.
- [ ] Cifrado AES aplicado al payload sensible cuando `Encryption:Enabled` está activo, con su contraparte de descifrado en backend.
- [ ] Logs sin tokens, contraseñas ni payloads cifrados; con `correlationId` en cada request.

### Subtareas

**Backend**
- `RateLimitingOptions` y políticas `AuthEndpoints`, `PrintingEndpoints`, `QueryEndpoints`.
- `EncryptionService` (AES) con llave/IV desde configuración.
- `ExceptionHandlingMiddleware` + `CorrelationIdMiddleware`.

**Frontend**
- `crypto.util.ts` para el payload sensible y manejo de `429`/`403` en el `errorInterceptor`.

## HU-012: Documentación, diagramas, soporte productivo y despliegue

- **Story Points:** 3 · **Prioridad:** Alta · **Bloque:** Día 2 — B8

### User Story

Como evaluador técnico, necesito documentación clara, diagramas, casos de prueba y una URL accesible, para validar la calidad técnica sin depender del candidato.

### Criterios de aceptación

- [ ] `README.md` con instalación, configuración, ejecución local, Docker, variables de entorno, **ambas URLs desplegadas** (API en Render y web en Cloudflare Pages) y **credenciales de prueba** de los 3 roles.
- [ ] `docs/ARCHITECTURE.md` con decisiones de diseño y **supuestos H1–H6**.
- [ ] `docs/c4/` con diagramas C4 (contexto, contenedor, componente) en Mermaid + `docs/domain-model.md`.
- [ ] `docs/INCIDENT-RUNBOOK.md` respondiendo el escenario de soporte productivo (sección 7): diagnóstico, métricas, logs, causa raíz, comunicación, contingencia y cierre.
- [ ] `docs/test-cases.md` con la matriz de casos y evidencia de ejecución.
- [ ] Swagger accesible y `mocks/` versionados como archivos de entrada/salida.
- [ ] API desplegada en Render con Postgres, migración y seed automáticos, health check verde.
- [ ] Frontend desplegado en Cloudflare Pages, consumiendo el API de Render sin errores de CORS ni de consola.

### Subtareas

**Documentación**
- README, ARCHITECTURE, C4 en Mermaid, modelo de dominio, runbook de incidente, casos de prueba.

**Despliegue backend — Render**
- `Dockerfile` multi-stage del API y `render.yaml` (Web Service Docker + Render PostgreSQL).
- Variables de entorno en Render: `ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`, `Encryption__Key`, `Encryption__IV`, `Cors__AllowedOrigins__1`.
- Health check path `/api/health`; documentar que el free tier duerme por inactividad y que la BD free caduca a los 30 días.

**Despliegue frontend — Cloudflare Pages**
- Conectar el repositorio, definir build command `npm run build` y output `dist/homecenter-labelprinting-site/browser`.
- Fijar `environment.production.ts` con la URL definitiva del API de Render **antes** del build.
- Agregar `public/_redirects` con `/* /index.html 200` para que el routing SPA de Angular funcione en recargas profundas.
- Registrar el dominio resultante en `Cors:AllowedOrigins` del API y **redeploy del backend** para que tome el origen.

**Entrega**
- Validar ambas URLs end-to-end, actualizar README con URLs y credenciales, commit final.

## MATRIZ DE ASIGNACIÓN POR BLOQUES

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

| Bloque | Día / Fecha | Duración | Backend | Frontend | Entregable esperado |
|---|---|---|---|---|---|
| B1 | Día 1 — Sáb 22 ago | 0:45 | Solución .NET 8, 6 capas, EF Core, health, appsettings | Proyecto Angular 20, Tailwind, estructura, environments | Proyecto base compilando |
| B2 | Día 1 — Sáb 22 ago | 1:00 | Entidades, migración, seeder de mocks, login JWT + roles | Login, interceptor, guards, storage de sesión | Autenticación y datos semilla operativos |
| B3 | Día 1 — Sáb 22 ago | 1:15 | Motor de reglas R0–R3, `ProcessPrintRequestUseCase`, `GET labels/{lpn}`, `POST print-requests` | — | Reglas 1, 2 y 3 funcionando por Swagger |
| B4 | Día 1 — Sáb 22 ago | 0:45 | R4 reimpresión + auditoría, historial paginado, dashboard | — | Reglas 4 y 5 + trazabilidad completa |
| B5 | Día 2 — Dom 23 ago | 1:15 | — | Formulario de impresión, preview de ETQ, resultado éxito/rechazo | Flujo de impresión operable desde la UI |
| B6 | Día 2 — Dom 23 ago | 1:15 | Pruebas unitarias de reglas críticas | Historial con filtros, paginación y responsive | Pruebas en verde + historial funcional |
| B7 | Día 2 — Dom 23 ago | 0:45 | Rate limiting, cifrado, CORS, middleware de errores | Manejo de `429`/`403`, cifrado de payload | Solución endurecida |
| B8 | Día 2 — Dom 23 ago | 1:00 | Dockerfile, render.yaml, despliegue en Render, CORS del origen de Cloudflare, verificación | Build productivo con `apiUrl` de Render, `_redirects`, publicación en Cloudflare Pages | README, C4, runbook y **ambas URLs** publicadas |
|  |  | **≈ 8:00** |  |  |  |

## BURNDOWN ESPERADO

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

| Bloque | SP completados acumulados | SP restantes | HUs objetivo |
|---|---:|---:|---|
| B1 | 2 | 26 | HU-001 |
| B2 | 7 | 21 | HU-002, HU-003 |
| B3 | 13 | 15 | HU-004, HU-005, HU-006 |
| B4 | 17 | 11 | HU-007, HU-008 |
| B5 | 19 | 9 | HU-009 (parcial) |
| B6 | 23 | 5 | HU-009, HU-010 |
| B7 | 25 | 3 | HU-011 |
| B8 | 28 | 0 | HU-012 |

## RIESGOS DEL SPRINT

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

| # | Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|---|
| R1 | **28 SP en 8 horas es una carga alta** para una sola persona en backend y frontend | Alta | Alto | Ruta crítica marcada en el resumen de HU: si el tiempo aprieta se recorta HU-011 (hardening avanzado) y el dashboard de HU-008 antes que cualquier regla, prueba o documento. |
| R2 | JWT + 3 roles no es requisito del enunciado y puede consumir tiempo del núcleo evaluado | Alta | Alto | Timebox estricto de 1 hora (B2). Si se excede, se degrada a autenticación simple con rol en el token y se documenta la decisión. |
| R3 | Render free tier: cold start y provisión de la BD pueden fallar al final del sprint | Media | Alto | Desplegar un "hello world" con BD conectada al final del Día 1, no en B8. Validar despliegue antes de que exista funcionalidad que perder. |
| R3b | **CORS entre Cloudflare Pages y Render** rompiendo la app justo en la entrega (origen no registrado, redeploy pendiente del backend, routing SPA sin `_redirects`) | Alta | Alto | Publicar el frontend en Cloudflare al final del Día 1 con una llamada real al API ya desplegado; registrar el origen en `Cors:AllowedOrigins` y verificar el preflight `OPTIONS` desde el navegador antes de construir funcionalidad encima. |
| R4 | Ambigüedad de los anexos (JSON roto, sin archivo de inventario, vocabularios distintos) | Alta | Medio | Supuestos H1–H6 documentados y visibles en README y ARCHITECTURE; el evaluador debe ver criterio, no adivinanza. |
| R5 | Reglas de negocio mal ubicadas (lógica en controllers) restarían puntos de arquitectura | Media | Alto | Motor de reglas en `Logic`, sin dependencia de EF ni HTTP, verificado por las pruebas unitarias que corren sin base de datos. |
| R6 | Inconsistencias entre DTOs backend y modelos frontend | Media | Medio | Contratos técnicos detallados definidos **antes** de construir componentes (esta sección es la fuente de verdad). |
| R7 | Documentación relegada al final y entregada incompleta | Media | Alto | README y supuestos se escriben incrementalmente desde B1; B8 solo consolida y publica. |
| R8 | Rate limiting mal calibrado bloqueando al propio evaluador | Baja | Medio | Límites conservadores (`PrintingEndpoints`: 30/min) y documentados en el README. |

## ESTRATEGIA DE PRUEBAS

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

| Caso | Entrada | Resultado esperado | Regla |
|---|---|---|---|
| CP-01 | LPN inexistente | Rechazo `LPN_NOT_FOUND`, auditoría registrada | R1 |
| CP-02 | LPN de documento `ANULADA` | Rechazo `INVALID_DOCUMENT_STATUS` | R2 |
| CP-03 | LPN de documento `DEVUELTA` | Rechazo `INVALID_DOCUMENT_STATUS` | R2 |
| CP-04 | Producto con `availableQty < requestedQty` | Rechazo `INSUFFICIENT_INVENTORY` con detalle del producto | R3 |
| CP-05 | Producto con stock pero `isStocked = false` | Rechazo `NOT_STOCKED` | R3 |
| CP-06 | `availableQty == requestedQty` | **Éxito** (límite inclusivo) | R3 |
| CP-07 | LPN válido, primera impresión | Éxito, `eventType: PRINT`, ZPL devuelto | R5 |
| CP-08 | Mismo LPN, segunda solicitud con rol `Supervisor` + motivo | Éxito, `eventType: REPRINT`, motivo auditado | R4 |
| CP-09 | Mismo LPN, segunda solicitud sin motivo | Rechazo `REPRINT_REASON_REQUIRED` | R4 |
| CP-10 | Mismo LPN, segunda solicitud con rol `Operario` | Rechazo `REPRINT_NOT_AUTHORIZED` | R4 |
| CP-11 | Solicitud sin LPN o sin zona | Rechazo `MISSING_REQUIRED_DATA` / `400` | R0 |
| CP-12 | Historial consultado por `Operario` | Solo sus propias solicitudes | Autorización |
| CP-13 | 11 logins fallidos en un minuto | HTTP `429` | Rate limiting |

## DEFINITION OF DONE

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

- Backend .NET 8 compila y ejecuta correctamente, con capas y dependencias respetadas.
- Frontend Angular 20 compila y ejecuta correctamente, responsive y con validaciones.
- PostgreSQL con esquema creado, mapeo BD → Backend → Frontend documentado y seed idempotente funcional.
- Las 5 reglas del enunciado implementadas, probadas y evidenciadas.
- Toda solicitud —aprobada o rechazada— queda auditada con usuario, fecha/hora, zona, resultado y motivo.
- Reimpresiones detectadas, marcadas, motivadas y restringidas por rol.
- Historial consultable por API y por interfaz web, con filtros y paginación.
- Login JWT, roles `Operario`/`Supervisor`/`Admin`, guards, interceptor, CORS, rate limiting y cifrado aplicados y configurables desde `appsettings.json`.
- Pruebas unitarias de reglas críticas en verde.
- Swagger disponible y contratos técnicos alineados backend/frontend.
- `README.md`, `ARCHITECTURE.md`, diagramas C4, modelo de dominio, `INCIDENT-RUNBOOK.md` y `test-cases.md` completos.
- Ambos repositorios Git públicos con commits representativos.
- API desplegada en Render, health check verde y credenciales de prueba documentadas.

## ENTREGABLES FINALES

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

| Entregable | Descripción | Estado esperado |
|---|---|---|
| Repositorio del microservicio | `Homecenter.Microservice.Api.LabelPrinting` — backend, mocks y documentación | Obligatorio |
| Repositorio del sitio web | `homecenter-labelprinting-site` — frontend Angular | Obligatorio |
| Diagrama C4 simplificado | Contexto, contenedor y componente en Mermaid | Obligatorio |
| README.md | Instalación, configuración, ejecución, Docker, URL y credenciales | Obligatorio |
| Swagger / OpenAPI | Documentación de API navegable | Obligatorio |
| Casos de prueba y evidencia | `docs/test-cases.md` + salida de `dotnet test` | Obligatorio |
| Archivos mock y datos semilla | `mocks/orders.json`, `labels.json`, `inventoryAvailability.json` | Obligatorio |
| Arquitectura, decisiones y supuestos | `docs/ARCHITECTURE.md` con hallazgos H1–H6 | Obligatorio |
| Modelo de dominio y esquema de BD | `docs/domain-model.md` + mapeo BD → Backend → Frontend | Obligatorio |
| Escenario de soporte productivo | `docs/INCIDENT-RUNBOOK.md` (sección 7 del enunciado) | Obligatorio |
| Contratos técnicos detallados | Roles, endpoints, request/response, autorización y rate limiting | Obligatorio |
| Interfaz web | Impresión e historial, responsive y validada | Obligatorio |
| URL API hosteada | Backend en Render con Render PostgreSQL + Swagger | Obligatorio |
| URL web hosteada | Frontend en Cloudflare Pages consumiendo el API de Render | Obligatorio |
| Usuarios de prueba | Credenciales `Operario`, `Supervisor` y `Admin` | Obligatorio |
| Docker / render.yaml | Contenedores y script de despliegue | Valorado |

## VERIFICACIÓN

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. `dotnet build` y `dotnet test` desde la raíz del repositorio `Homecenter.Microservice.Api.LabelPrinting` — compilación limpia y pruebas de reglas en verde.
2. `dotnet run` local contra la instancia PostgreSQL de la máquina (puerto 5432) — API en `:5080/swagger` con datos semilla cargados; `GET /api/health` responde `Healthy` con `database: Healthy`.
3. Login por Swagger con los 3 usuarios semilla; verificar que el token trae el rol correcto.
4. Ejecutar los casos **CP-01 a CP-13** de la estrategia de pruebas y contrastar contra el resultado esperado.
5. `GET /api/print-requests/history` — verificar que todos los casos anteriores quedaron auditados con usuario, zona, fecha, resultado y motivo; verificar que el `Operario` solo ve los suyos.
6. Frontend (`npm start`): imprimir desde el formulario, ver banner de éxito y de rechazo con motivo, forzar el flujo de reimpresión, revisar el historial actualizado, probar en viewport de 375 px y enviar el formulario vacío para validar mensajes.
7. Contra Render: repetir los pasos 3-5 usando la URL pública del API y confirmar CORS desde el frontend local.
8. Contra Cloudflare Pages: abrir la URL pública de la web, hacer login, imprimir, reimprimir y consultar historial end-to-end contra el API de Render; verificar **sin errores de CORS ni de consola**, que una recarga en `/historial` no da 404 (`_redirects` activo) y que el cold start del API se refleja como estado de carga y no como error silencioso.

---

**Creado por:** Andres Felipe Galeano Velasco
**Fecha de creación:** Sábado 22 de agosto de 2026
**Versión del documento:** 1.0
**Estado:** En planificación
