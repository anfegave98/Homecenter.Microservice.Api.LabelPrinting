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

Cada LPN está construido para un caso de prueba distinto:

| LPN | Zona | Documento | Qué demuestra |
|---|---|---|---|
| `LPN-000987654` | ZONA-PICKING-A | LIBERADA | **Éxito.** Único con 2 productos → activa `hasMultipleProducts` |
| `LPN-000987655` | ZONA-PICKING-A | **ANULADA** | `INVALID_DOCUMENT_STATUS` |
| `LPN-000987656` | ZONA-PICKING-B | **DEVUELTA** | `INVALID_DOCUMENT_STATUS` |
| `LPN-000987657` | ZONA-PICKING-B | LIBERADA | `INSUFFICIENT_INVENTORY` — pide 5, hay 1 |
| `LPN-000987658` | ZONA-DESPACHO | LIBERADA | `NOT_STOCKED` — hay 50 unidades pero `isStocked: false` |
| `LPN-000987659` | ZONA-PICKING-A | **CREADA** | Éxito en el **límite exacto**: pide 4, hay 4 |
| `LPN-000987660` … `LPN-000987677` | ZONA-PICKING-A | LIBERADA | Repuesto de casos de **éxito** (2 productos) |
| `LPN-000987678` … `LPN-000987680` | ZONA-PICKING-A | **CREADA** | Repuesto del caso **límite exacto** |

Los rechazos son repetibles: una solicitud rechazada no consume la etiqueta. Los casos de
éxito **sí** se consumen —una vez impresa, la siguiente solicitud sobre ese LPN es una
reimpresión— y por eso hay repuesto: quien evalúa necesita poder ver el camino feliz aunque
otra persona ya lo haya recorrido.

Dos casos que parecen errores y no lo son:

- `LPN-000987658` tiene 50 unidades disponibles y se rechaza igual. Correcto: la regla
  exige `isStocked = true` **además** de la cantidad.
- `LPN-000987659` está en `CREADA` y **sí imprime**. Solo `ANULADA` y `DEVUELTA` bloquean.

Para `LPN_NOT_FOUND` sirve cualquier cadena inventada.

> `mocks/orders.json` es la fuente que lee el seeder. `mocks/labels.json` es la vista
> plana de las mismas etiquetas, versionada como archivo mock de entrada según el
> enunciado; **debe mantenerse en sincronía**, pero no es lo que se carga.

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
| **R4** `ReprintPolicyRule` | Reimpresión sin motivo; **deriva** a autorización si el rol no la ejecuta | `REPRINT_REASON_REQUIRED` · `REPRINT_PENDING_APPROVAL` |

La impresión se simula en dos tiempos. Al procesar la solicitud se confirma el evento
lógico y se audita; el archivo llega a manos del operario cuando descarga la etiqueta
desde `GET api/print-requests/{id}/label`. El enunciado admite ambas formas de simular
—confirmación lógica o archivo de salida— y aquí cumplen papeles distintos: la primera
es la decisión de negocio, la segunda es el entregable.

Una solicitud aprobada da derecho a **una** descarga, registrada con fecha y usuario.
Permitir bajarla indefinidamente convertiría el control de reimpresiones en un trámite
decorativo: cualquiera obtendría copias sin motivo ni autorización, que es justo lo que
la Regla 4 existe para impedir.

R3 no da un rechazo genérico: `error.details` trae **qué producto** falló, cuánto se
pidió, cuánto hay y por qué.

R4 es la única regla que **no cierra** la solicitud. Un operario que necesita reimprimir
—etiqueta rota, atasco de impresora— la envía con su motivo y queda en
`PENDING_APPROVAL` hasta que un `Supervisor` o `Admin` la resuelva. Negarle el paso sin
salida sería peor control y no mejor: es él quien detecta el problema, y quien terminaría
pidiendo prestada una sesión ajena para imprimir.

Autorizar **no imprime a ciegas**: las reglas se vuelven a evaluar con los datos del
momento de la decisión. Si el documento se anuló o el inventario de la zona se agotó
mientras la solicitud esperaba, la respuesta es un rechazo con ese motivo y no con el
visto bueno. Un permiso no puede volver válida una impresión que dejó de serlo.

---

## Endpoints

| Verbo | Ruta | Rol | Límite |
|---|---|---|---|
| POST | `api/auth/login` | Público | 10/min |
| GET | `api/zones` | Autenticado | 60/min |
| GET | `api/labels/{lpn}` | Autenticado | 60/min |
| POST | `api/print-requests` | Autenticado | 30/min |
| GET | `api/print-requests/pending` | `Supervisor` · `Admin` | 60/min |
| POST | `api/print-requests/{id}/approve` | `Supervisor` · `Admin` | 30/min |
| POST | `api/print-requests/{id}/reject` | `Supervisor` · `Admin` | 30/min |
| GET | `api/print-requests/{id}/label` | Autenticado *(operario: solo lo suyo)* | 60/min |
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

## Por qué no se partió de las plantillas base

La prueba entrega dos plantillas —`PLANTILLA_BASE_ANGULAR_FRONT_APPS_2025` y
`PYTHON_BACKEND_TEMPLATE`— cuyo uso no es obligatorio. Se construyó desde cero, y esta
es la razón de cada caso.

### La plantilla Angular no puede ejecutarse de forma autónoma

Es un **micro-frontend del Hub de Proveedores**: su propia documentación lo dice sin
ambigüedad — *"No tiene login propio: recibe la sesión de la app contenedora"* por
`postMessage`, y consume las APIs con ese token.

Ese supuesto es incompatible con la prueba en dos puntos concretos:

- **HU-01 exige identificar al usuario solicitante**, y la sección 9 pide el campo
  Usuario en pantalla. Sin aplicación contenedora no hay sesión que heredar, así que el
  login propio no es un extra: es un requisito.
- **El evaluador no tiene el Hub.** Una aplicación que espera un `postMessage` que nunca
  llega no se puede abrir y probar, que es justamente lo que la entrega necesita.

A eso se suma que su despliegue apunta a Azure Blob Storage u OpenShift con una
`subscriptionKey` de Azure API Management, y esta entrega se publica en Cloudflare Pages
y Render. Adaptarla habría significado desmontar sus supuestos centrales.

### La plantilla de backend es Python; la solución es .NET 8

El enunciado no fija lenguaje —§4 dice expresamente que *"el candidato puede proponer su
propia arquitectura"*— y el rol es sobre el ecosistema .NET.

### Pero sus convenciones sí se adoptaron

No usar el código no significa ignorar el criterio de casa. La estructura por capas de
esta solución replica la de la plantilla Python casi uno a uno:

| `PYTHON_BACKEND_TEMPLATE` | Esta solución |
|---|---|
| `api/controllers/` | `...LabelPrinting/Controllers/` |
| `application/services/` | `...Logic/UseCases/` |
| `domain/repositories/` | `...Abstractions/Repositories/` |
| `infrastructure/database/` | `...EntityFramework/` |
| `api/response/api_response.py` | `Common/ApiResponse.cs` |
| `api/response/metadata.py` | `meta` del envelope |
| Variables de entorno por defecto, `.env` en local | `appsettings` + variables de entorno |

**El punto más revelador está en su `api_response.py`**, que distingue tres desenlaces:

```python
create_successful(...)    # is_successful=True,  is_error=False
create_unsuccessful(...)  # is_successful=False, is_error=False   ← rechazo, no error
create_error(...)         # is_successful=False, is_error=True
```

La plantilla de casa **ya separa "no fue exitoso" de "hubo un error"**. Es exactamente la
distinción del supuesto **H6** de esta solución: un rechazo de negocio responde HTTP 200
con `success: false`, mientras que los fallos técnicos usan códigos HTTP. La decisión se
tomó por el razonamiento explicado en [ARCHITECTURE.md](docs/ARCHITECTURE.md) y resultó
coincidir con la convención del equipo.

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
| H5 | No se define quién puede reimprimir | Motivo obligatorio para todos; el rol `Supervisor`/`Admin` reimprime directo, el resto envía la solicitud a autorización |
| H6 | No se define el HTTP del rechazo de negocio | `200` con `success: false` |

### Sobre el ZPL de la semilla

El anexo trae como ZPL el ejemplo genérico de Zebra: una guía de envío para *John Doe*
en *Intershipping, Inc.*, con código de barras `12345678`. No menciona la ETQ, el LPN ni
los productos.

Eso bastaba mientras el ZPL se devolvía como texto opaco. Al renderizarlo como imagen
descargable deja de servir: el evaluador abriría el `.png` y vería una etiqueta sin
relación con la operación, lo que parece un defecto más que una demostración. Y generar
la imagen por separado con los datos reales sería peor, porque el `.zpl` y el `.png` de
la misma descarga contarían historias distintas.

Por eso el ZPL de cada ETQ **se compone en la semilla** a partir de su documento
(`MockZplComposer`), con ETQ, LPN en código de barras, documento origen, zona y
productos. Esto **no** es la generación de etiquetas que el enunciado deja fuera de
alcance: es la simulación del proceso de olas que las pre-genera, y vive en la capa de
datos mock, no en la lógica de negocio. **El ZPL original del anexo se conserva íntegro**
en `mocks/_anexo_tableOrders.original.json`.

El `.zpl` lleva además un bloque de metadatos como comentario `^FX` —un campo legal que
la impresora ignora— con los datos de la etiqueta. La vista previa `.png` se dibuja con
ese bloque, así que imagen y archivo salen de la misma fuente y no pueden divergir.

> El código de barras del `.png` es una representación: **no es Code 128 legible con
> pistola**. La lectura real la da el `.zpl`, que sí lleva el `^BC` que la impresora
> interpreta.

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
