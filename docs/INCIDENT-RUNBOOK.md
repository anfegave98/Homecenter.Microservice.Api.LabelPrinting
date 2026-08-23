# Runbook de soporte productivo

Respuesta al escenario de la sección 7 del enunciado: **la tienda reporta que las
etiquetas no se están imprimiendo**.

Este documento no es teórico. Cada consulta que aparece aquí corre contra los
mecanismos que la solución realmente expone: `correlationId`, códigos de rechazo,
`PrintAuditLogs`, el dashboard y el health check.

---

## Principio: distinguir rechazo de falla

Lo primero que hay que establecer no es *qué falló*, sino **si algo falló**.

"No me deja imprimir" describe dos situaciones opuestas:

| Situación | Qué significa | Quién lo resuelve |
|---|---|---|
| **Rechazo de negocio** — HTTP 200, `success: false` | El sistema funcionó correctamente y decidió que la impresión no procede | La operación: reponer inventario, liberar el documento, escalar a supervisor |
| **Falla técnica** — HTTP 5xx, timeout, 401/429 | El sistema no pudo procesar la solicitud | Tecnología |

Tratar un rechazo como incidente moviliza al equipo equivocado y deja el problema real
sin atender. La distinción es la primera pregunta del triage, no un detalle.

---

## 1. Diagnóstico inicial (primeros 5 minutos)

### Paso 1 — ¿El servicio está vivo?

```bash
curl -s https://<api>/api/health
```

```json
{ "status": "Healthy", "service": "...", "database": "Healthy", "timestamp": "..." }
```

| Respuesta | Interpretación | Acción |
|---|---|---|
| `200` con `Healthy` | El servicio y la base responden | Ir al paso 2: es rechazo de negocio o algo puntual |
| `503` con `database: Unhealthy` | El servicio vive, Postgres no | Ver §4, causa **B** |
| Sin respuesta / timeout largo | Instancia caída **o dormida** | Ver §4, causa **A** |

**Antes de declarar caída:** en el plan gratuito de Render la instancia se suspende por
inactividad. La primera solicitud tras el reposo tarda decenas de segundos. Reintentar
una vez distingue "dormida" de "caída" — y el frontend ya muestra esto como estado de
carga con mensaje explícito, no como error silencioso.

### Paso 2 — ¿Es masivo o puntual?

```
GET /api/admin/dashboard      (rol Admin)
```

```json
{ "totalRequests": 120, "approved": 95, "rejected": 25, "reprints": 12,
  "rejectionsByCode": { "INSUFFICIENT_INVENTORY": 12, "LPN_NOT_FOUND": 8 } }
```

**`rejectionsByCode` es la pantalla que resuelve el incidente.** Si el pico se concentra
en un código de negocio, no hay falla técnica: hay un problema operativo que el sistema
está reportando correctamente.

| Patrón observado | Lectura |
|---|---|
| `INSUFFICIENT_INVENTORY` domina | Desabastecimiento en una zona. **No es un incidente de tecnología** |
| `INVALID_DOCUMENT_STATUS` domina | Se está trabajando sobre documentos anulados o devueltos. Revisar el proceso aguas arriba |
| `LPN_NOT_FOUND` domina | Etiquetas de una ola no cargada, o lectores de código mal calibrados |
| `REPRINT_NOT_AUTHORIZED` domina | Un turno sin supervisor disponible. Es una decisión de diseño operando, no un error |
| `rejected` alto **sin** concentración | Ahora sí sospechar falla técnica |
| `totalRequests` en cero | Nadie está llegando al API: mirar CORS, DNS o el frontend |

---

## 2. Rastrear un caso concreto

Cada respuesta del API trae `X-Correlation-Id`, y el frontend lo muestra al usuario en
los fallos técnicos: *"(referencia: 839b3935…)"*.

**Pídele ese código al operario.** Es lo que evita reconstruir el caso por hora
aproximada.

### Con el correlationId

```sql
SELECT p."CorrelationId", p."LpnId", z."Code" AS zona, u."UserName",
       p."Result", p."EventType", p."RejectionCode", p."RejectionMessage", p."ProcessedAt"
FROM   "PrintRequests" p
LEFT   JOIN "Zones" z ON z."Id" = p."IdZone"
LEFT   JOIN "Users" u ON u."Id" = p."IdUser"
WHERE  p."CorrelationId" = '839b3935-...';
```

Y la traza regla por regla:

```sql
SELECT a."RuleCode", a."Passed", a."Detail", a."EvaluatedAt"
FROM   "PrintAuditLogs" a
JOIN   "PrintRequests" p ON p."Id" = a."IdPrintRequest"
WHERE  p."CorrelationId" = '839b3935-...'
ORDER  BY a."EvaluatedAt";
```

Como el motor **corta en la primera violación**, la cantidad de filas dice hasta dónde
llegó la evaluación. Tres filas con la última en `Passed = false` significa que R0 y R1
pasaron y R2 rechazó.

### Sin el correlationId

```sql
SELECT * FROM "PrintRequests"
WHERE  "LpnId" = 'LPN-000987654'
ORDER  BY "ProcessedAt" DESC
LIMIT  20;
```

### En los logs de Render

Filtrar por el `correlationId`: todas las entradas de esa solicitud lo llevan en su
ámbito de log.

```
Impresion rechazada. CorrelationId=839b... Lpn=LPN-... Regla=R2_DOCUMENT_STATUS Motivo=INVALID_DOCUMENT_STATUS
Error no controlado. Ruta=/api/print-requests Metodo=POST CorrelationId=839b...
```

**Lo que no vas a encontrar en los logs, por diseño:** tokens, contraseñas, llaves ni
payloads cifrados.

---

## 3. Cuándo un rechazo sí es un incidente

Un rechazo correcto puede seguir siendo un problema si los **datos de origen** están
mal. La pregunta a hacerse:

> ¿El sistema rechazó porque la realidad lo justifica, o porque lo que ve no coincide
> con la realidad?

```sql
-- ¿El inventario dice cero donde el piso tiene mercancía?
SELECT p."ProductCode", z."Code", i."AvailableQty", i."IsStocked", i."LastUpdateDate"
FROM   "InventoryAvailability" i
JOIN   "Products" p ON p."Id" = i."IdProduct"
JOIN   "Zones" z ON z."Id" = i."IdZone"
WHERE  p."ProductCode" = 'PROD-004';
```

`LastUpdateDate` antiguo con operación activa apunta a que la sincronización de
inventario dejó de correr. **Ese sí es un incidente**, y su síntoma es un rechazo
perfectamente correcto.

---

## 4. Causas raíz frecuentes

### A. La instancia está dormida (Render free tier)

- **Síntoma:** primera solicitud del día falla o tarda ~30–60 s; luego todo normal.
- **Verificación:** reintentar; revisar en Render si hubo *spin down*.
- **Contención:** reintentar. El frontend ya lo comunica como estado de carga.
- **Solución definitiva:** plan de pago o un ping periódico al health check.

### B. La base de datos no responde

- **Síntoma:** health `503` con `database: Unhealthy`; los endpoints devuelven `500`.
- **Verificación:** estado de la instancia Postgres en Render.
- **Causa habitual en esta entrega:** **la base gratuita caduca a los 30 días.**
- **Contención:** ninguna a nivel de aplicación — sin base no hay auditoría, y operar
  sin auditoría sería peor que no operar.

### C. Error de CORS

- **Síntoma:** la web no funciona pero el API responde bien por Swagger o `curl`. En la
  consola del navegador: *blocked by CORS policy*.
- **Causa:** el origen de Cloudflare Pages no está en `Cors:AllowedOrigins`, o se
  redesplegó el frontend a un dominio nuevo.
- **Solución:** agregar el origen a `Cors__AllowedOrigins__N` **y redesplegar el
  backend** — la configuración se lee al arrancar.

### D. Todos reciben 429

- **Síntoma:** `TOO_MANY_REQUESTS` generalizado y simultáneo.
- **Causa probable:** `UseForwardedHeaders` dejó de aplicar y todas las solicitudes
  anónimas colapsaron en la partición de la IP del proxy.
- **Verificación:** ¿el `429` afecta también a usuarios autenticados? Si solo golpea al
  login, es la partición anónima.
- **Contención inmediata:** subir `RateLimiting__PermitLimit` o poner
  `RateLimiting__Enabled=false`. **Es configuración: no requiere recompilar.**

### E. Sesiones cortadas en masa

- **Síntoma:** todos redirigidos a `/login?expired=true`.
- **Causa:** se rotó `Jwt__SecretKey`. Los tokens emitidos con la anterior dejan de
  validar de inmediato.
- **Contención:** es esperado tras una rotación. Con `ExpirationMinutes: 60` el impacto
  máximo es de una hora.

### F. Reimpresión duplicada bajo concurrencia

- **Síntoma:** dos eventos `PRINT` aprobados para el mismo LPN casi simultáneos.
- **Causa:** condición de carrera conocida y documentada en `ARCHITECTURE.md` — dos
  solicitudes concurrentes pueden ambas leer "sin impresión previa".
- **Detección:**

```sql
SELECT "LpnId", COUNT(*) FROM "PrintRequests"
WHERE  "Result" = 'APPROVED' AND "EventType" = 'PRINT'
GROUP  BY "LpnId" HAVING COUNT(*) > 1;
```

- **Solución definitiva:** índice único parcial o bloqueo optimista sobre `PrintRequests`.

---

## 5. Comunicación

Distinta según a quién se le habla:

**A la tienda (durante el incidente).** Qué pueden y no pueden hacer, y hasta cuándo. Sin
jerga:

> Las impresiones de etiquetas están intermitentes desde las 10:15. Estamos trabajando
> en ello. **Las solicitudes que alcanzaron a procesarse quedaron registradas**, así que
> no hay que rehacer lo ya impreso. Próxima actualización a las 11:00.

**Al negocio.** Impacto medible: cuántas solicitudes afectadas, desde cuándo, si hay
mercancía detenida.

```sql
SELECT COUNT(*) FILTER (WHERE "Result" = 'APPROVED') AS aprobadas,
       COUNT(*) FILTER (WHERE "Result" = 'REJECTED') AS rechazadas,
       MIN("ProcessedAt") AS desde
FROM   "PrintRequests"
WHERE  "ProcessedAt" >= NOW() - INTERVAL '2 hours';
```

**Al equipo técnico.** `correlationId` de un caso representativo, código de rechazo
predominante, entrada de log, y qué se descartó ya.

**Lo que no se debe comunicar:** una causa raíz antes de tenerla. "Estamos
investigando" es una respuesta legítima; una causa equivocada dispara acciones
correctivas equivocadas.

---

## 6. Contingencia

Si el submódulo no puede restablecerse en un plazo aceptable:

1. **El ZPL está en la base.** Se puede extraer y enviar a la impresora por fuera del
   sistema:

```sql
SELECT l."Zpl" FROM "Labels" l WHERE l."LpnId" = 'LPN-000987654';
```

2. **Registrar manualmente lo impreso.** Ese registro debe volcarse a la auditoría al
   restablecer el servicio: una etiqueta impresa sin traza es exactamente el problema
   que este submódulo existe para evitar.

3. **Priorizar por documento.** Si solo una zona está afectada, el resto de la operación
   sigue: la validación de inventario es por zona.

**Lo que no se debe hacer:** apagar la validación de reglas para "destrabar" la
operación. Imprimir sobre documentos anulados genera mercancía mal etiquetada en piso, y
ese costo supera al de la demora.

---

## 7. Cierre

Un incidente no termina cuando el servicio responde. Termina cuando:

- [ ] Se confirmó el restablecimiento **con una solicitud real**, no solo con el health check.
- [ ] Se verificó que la auditoría del período quedó completa y sin huecos.
- [ ] Las impresiones manuales de contingencia se reflejaron en `PrintRequests`.
- [ ] Se comunicó el cierre a los mismos destinatarios que recibieron el aviso inicial.
- [ ] Se registró la causa raíz, con su `correlationId` de referencia.
- [ ] Se definió la acción preventiva **con responsable y fecha**. Sin eso, el análisis
      es un documento que nadie vuelve a abrir.

### Verificación de restablecimiento

```bash
curl -s https://<api>/api/health

# Login y solicitud real de extremo a extremo
curl -s -X POST https://<api>/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"operario.tienda","password":"..."}'

curl -s -X POST https://<api>/api/print-requests \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"lpn":"LPN-000987654"}'
```

Un health check verde solo dice que el proceso vive y la base responde. **Que la
solicitud completa funcione es lo único que prueba el restablecimiento.**

---

## Referencia rápida de códigos

| Código | HTTP | ¿Incidente? | Significado |
|---|---|---|---|
| `MISSING_REQUIRED_DATA` | 200 | No | Faltan datos en la solicitud |
| `LPN_NOT_FOUND` | 200 | Solo si es masivo | La ETQ/LPN no existe |
| `ZONE_NOT_FOUND` | 200 | Revisar catálogo | Zona inexistente o inactiva |
| `INVALID_DOCUMENT_STATUS` | 200 | No | Documento anulado o devuelto |
| `INSUFFICIENT_INVENTORY` | 200 | Revisar sincronización | Cantidad insuficiente en la zona |
| `NOT_STOCKED` | 200 | Revisar sincronización | Producto no abastecido en la zona |
| `REPRINT_REASON_REQUIRED` | 200 | No | Falta el motivo de reimpresión |
| `REPRINT_NOT_AUTHORIZED` | 200 | No | El rol no autoriza reimprimir |
| `TOO_MANY_REQUESTS` | 429 | Si es generalizado | Límite de tráfico superado |
| `INTERNAL_ERROR` | 500 | **Sí** | Fallo no controlado — usar el `correlationId` |
| `SERVICE_UNHEALTHY` | 503 | **Sí** | Un componente no responde |
