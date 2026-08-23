# Submódulo de Impresión de ETQ · Homecenter

Prueba técnica — Dev Experto GTL Tienda.

Solución fullstack desacoplada que recibe una solicitud de impresión sobre una **ETQ/LPN
pre-generada**, resuelve el documento origen y sus productos, valida las reglas de
negocio al momento de imprimir, simula la impresión y deja **trazabilidad completa** de
impresiones y reimpresiones.

La generación de la etiqueta **no** hace parte del alcance: la ETQ ya existe.

| | |
|---|---|
| **Backend** | .NET 8 · Web API · PostgreSQL · EF Core |
| **Frontend** | Angular 20 · standalone + signals · Tailwind |
| **Pruebas** | 68 xUnit, verificadas por mutación |
| **Repositorio frontend** | [homecenter-labelprinting-site](https://github.com/anfegave98/homecenter-labelprinting-site) |

---

## Credenciales de prueba

| Usuario | Contraseña | Puede |
|---|---|---|
| `operario.tienda` | `Operario123*` | Imprimir y ver **su propio** historial |
| `supervisor.tienda` | `Supervisor123*` | Además: reimprimir con motivo, ver el historial completo |
| `admin.tienda` | `Admin123*` | Además: consultar indicadores operativos |

---

## Ejecución local

### Requisitos

.NET 8 SDK · Node 20+ · PostgreSQL 14+ (o Docker)

### Backend

```bash
dotnet run --project src/Homecenter.Microservice.Api.LabelPrinting
```

Arranca en `http://localhost:5080`, con Swagger en `/swagger`.

**No hace falta configurar nada.** `appsettings.Development.json` está versionado a
propósito con valores locales desechables, y al arrancar se crea el esquema y se cargan
los datos semilla de forma idempotente. Ver [docs/SECRETS.md](docs/SECRETS.md) para el
razonamiento y para los valores de producción.

Si tu Postgres local usa otras credenciales, crea `appsettings.Local.json` (está en
`.gitignore` y tiene precedencia).

### Frontend

```bash
npm install && npm start
```

Queda en `http://localhost:4200`, ya declarado como origen permitido en el backend.

### Pruebas

```bash
dotnet test
```

No requieren base de datos ni configuración: el motor de reglas no conoce EF ni HTTP.

### Docker

```bash
docker build -t homecenter-labelprinting-api .
```

---

## Datos de prueba

Seis LPN, cada uno construido para un caso de prueba distinto:

| LPN | Zona | Documento | Qué demuestra |
|---|---|---|---|
| `LPN-000987654` | ZONA-PICKING-A | LIBERADA | **Éxito.** Único con 2 productos → activa `hasMultipleProducts` |
| `LPN-000987655` | ZONA-PICKING-A | **ANULADA** | `INVALID_DOCUMENT_STATUS` |
| `LPN-000987656` | ZONA-PICKING-B | **DEVUELTA** | `INVALID_DOCUMENT_STATUS` |
| `LPN-000987657` | ZONA-PICKING-B | LIBERADA | `INSUFFICIENT_INVENTORY` — pide 5, hay 1 |
| `LPN-000987658` | ZONA-DESPACHO | LIBERADA | `NOT_STOCKED` — hay 50 unidades pero `isStocked: false` |
| `LPN-000987659` | ZONA-PICKING-A | **CREADA** | Éxito en el **límite exacto**: pide 4, hay 4 |

Dos casos que parecen errores y no lo son:

- `LPN-000987658` tiene 50 unidades disponibles y se rechaza igual. Correcto: la regla
  exige `isStocked = true` **además** de la cantidad.
- `LPN-000987659` está en `CREADA` y **sí imprime**. Solo `ANULADA` y `DEVUELTA` bloquean.

Para `LPN_NOT_FOUND` sirve cualquier cadena inventada.

---

## Reglas de negocio

Una clase por regla implementando `IPrintRule`. El motor las ordena y **corta en la
primera violación**, conservando la traza de lo evaluado hasta el corte.

| Regla | Rechaza cuando | Código |
|---|---|---|
| **R0** `RequiredDataRule` | Falta LPN o usuario autenticado | `MISSING_REQUIRED_DATA` |
| **R1** `LabelExistsRule` | La ETQ/LPN o la zona no existen | `LPN_NOT_FOUND` · `ZONE_NOT_FOUND` |
| **R2** `DocumentStatusRule` | Documento `ANULADA` o `DEVUELTA` | `INVALID_DOCUMENT_STATUS` |
| **R3** `ZoneAvailabilityRule` | Algún producto sin cantidad suficiente o no abastecido | `INSUFFICIENT_INVENTORY` · `NOT_STOCKED` |
| **R4** `ReprintPolicyRule` | Reimpresión sin rol autorizado o sin motivo | `REPRINT_NOT_AUTHORIZED` · `REPRINT_REASON_REQUIRED` |

R3 no da un rechazo genérico: `error.details` trae **qué producto** falló, cuánto se
pidió, cuánto hay y por qué.

---

## Endpoints

| Verbo | Ruta | Rol | Límite |
|---|---|---|---|
| POST | `api/auth/login` | Público | 10/min |
| GET | `api/zones` | Autenticado | 60/min |
| GET | `api/labels/{lpn}` | Autenticado | 60/min |
| POST | `api/print-requests` | Autenticado | 30/min |
| GET | `api/print-requests/history` | Autenticado *(operario: solo lo suyo)* | 60/min |
| GET | `api/admin/dashboard` | Admin | 60/min |
| GET | `api/health` | Público | sin límite |

Todas las respuestas usan el mismo envelope:

```json
{ "success": true, "data": {}, "error": null, "meta": null }
```

### El detalle que más conviene entender

**Un rechazo de negocio responde HTTP 200 con `success: false`.**

```json
{ "success": false,
  "data": { "correlationId": "0f2b...", "result": "REJECTED", "eventType": "PRINT" },
  "error": { "code": "INSUFFICIENT_INVENTORY",
             "message": "La zona ZONA-PICKING-B no cumple las condiciones...",
             "details": [{ "productCode": "PROD-004", "requestedQty": 5, "availableQty": 1 }] } }
```

La solicitud se procesó bien; lo que no procede es la impresión. Un `400` haría pensar
que la petición está mal formada cuando el problema es de inventario. Los errores
técnicos sí usan códigos HTTP: `400` forma, `401` sin token, `403` rol, `429` límite,
`500` fallo. El razonamiento completo está en
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

---

## Estructura

```
src/
├── Homecenter.Microservice.Api.LabelPrinting/                      Host: controllers, middleware, configuración
├── Homecenter.Microservice.Api.LabelPrinting.Logic/                Casos de uso, motor de reglas, cifrado
├── Homecenter.Microservice.Api.LabelPrinting.Abstractions/         Interfaces
├── Homecenter.Microservice.Api.LabelPrinting.EntityFramework/      DbContext, repositorios, seeder
├── Homecenter.Microservice.Api.LabelPrinting.Entities/             Entidades de dominio
└── Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object/ DTOs y configuración tipada
tests/  ·  mocks/  ·  docs/
```

`Api → Logic → Abstractions ← EntityFramework`. **`Logic` no conoce EF ni HTTP**, y por
eso las 68 pruebas corren sin base de datos en menos de 300 ms.

---

## Documentación

| Documento | Contenido |
|---|---|
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Decisiones de diseño, supuestos **H1–H6** y limitaciones conocidas |
| [c4/](docs/c4/README.md) | Diagramas C4 (contexto, contenedor, componente) y flujo de impresión |
| [domain-model.md](docs/domain-model.md) | Modelo ER, mapeo BD → Backend → Frontend, estados |
| [test-cases.md](docs/test-cases.md) | Matriz **CP-01 a CP-13** y evidencia de ejecución |
| [INCIDENT-RUNBOOK.md](docs/INCIDENT-RUNBOOK.md) | Escenario de soporte productivo (sección 7) |
| [SECRETS.md](docs/SECRETS.md) | Inventario de secretos, formatos y rotación |

---

## Hallazgos de los anexos

Los tres archivos del enunciado son la fuente de verdad del contrato. Ninguno se
descartó, y cada ambigüedad quedó documentada como decisión:

| # | Hallazgo | Decisión |
|---|---|---|
| H1 | `tableOrders.json` **no parsea**: falta una coma en la línea 16 | Corregido en la semilla; el original se conserva en `mocks/_anexo_tableOrders.original.json` |
| H2 | El anexo solo trae `lpn`, pero la UI exige LPN + Zona + Usuario | Zona es override opcional; el usuario sale del JWT, no del body |
| H3 | `responseEtq.json` expone **un solo SKU**, pero una ETQ puede llevar varios | Se devuelve el arreglo completo **más** el bloque `legacy` con `hasMultipleProducts` |
| H4 | **No hay archivo de inventario** y la Regla 3 lo necesita | Se creó `mocks/inventoryAvailability.json` |
| H5 | No se define quién puede reimprimir | Rol `Supervisor`/`Admin` + motivo obligatorio |
| H6 | No se define el HTTP del rechazo de negocio | `200` con `success: false` |

El ZPL de los anexos **se usa sin modificar**, incluido su contenido genérico de Zebra:
la generación de la etiqueta está fuera de alcance y sustituirlo habría oscurecido que
proviene literalmente del anexo entregado.

---

## Calidad

- **68 pruebas xUnit** cubriendo CP-01 a CP-12. CP-13 (rate limiting) se verifica contra
  el servicio corriendo, porque lo que hay que validar es la tubería HTTP completa.
- **Verificación por mutación:** se introdujeron tres defectos deliberados en el código
  de producción para confirmar que las pruebas fallan cuando deben. Los tres fueron
  detectados. Que las pruebas pasen no demuestra que detecten algo.
- `TreatWarningsAsErrors` y documentación XML obligatoria: la documentación es condición
  para compilar, no una intención.

Las limitaciones conocidas —incluida una condición de carrera en la detección de
reimpresión bajo concurrencia— están declaradas en
[ARCHITECTURE.md](docs/ARCHITECTURE.md), no escondidas.

---

**Andres Felipe Galeano Velasco**
