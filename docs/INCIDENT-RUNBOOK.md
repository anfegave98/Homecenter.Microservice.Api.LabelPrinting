# Escenario de soporte productivo

Respuesta a la **sección 7** del enunciado.

> **Caso.** Entre las 9:00 y las 10:00 AM múltiples tiendas reportan que las impresiones
> fallan intermitentemente.

Las consultas de este documento corren contra los mecanismos que la solución
efectivamente expone: `correlationId`, códigos de rechazo, `PrintRequests`,
`PrintAuditLogs`, `/api/admin/dashboard` y `/api/health`.

---

## Lo que el enunciado del caso ya me dice

Antes de tocar nada, tres datos del reporte acotan el problema:

| Dato | Qué descarta y qué sugiere |
|---|---|
| **Múltiples tiendas** | No es un problema local: ni una impresora, ni una red de tienda, ni el inventario de una zona. Apunta a un componente **compartido** — el API, la base de datos o algo aguas arriba |
| **Intermitentemente** | El servicio **no está caído**. Algunas solicitudes pasan. Eso descarta la caída total y apunta a saturación, reinicio parcial o una condición que depende del momento |
| **Ventana 9:00–10:00 AM** | Es el arranque de turno: todos inician sesión casi al mismo tiempo y se liberan las olas. **Correlación con un pico de carga** |

Un fallo intermitente y simultáneo en varias tiendas, concentrado en la hora de mayor
concurrencia, es el perfil de **un límite que se agota**, no el de un componente roto.
Esa es la hipótesis a confirmar o descartar primero.

Y antes de eso, una pregunta que cuesta treinta segundos y ahorra horas:

> **¿Qué ven exactamente en pantalla?**

"Falla" describe dos situaciones opuestas en esta solución:

| Lo que ve el operario | Qué es | Quién lo resuelve |
|---|---|---|
| Un motivo de negocio: *"la zona no tiene disponibilidad"* | HTTP 200, `success: false`. **El sistema funcionó** y decidió que la impresión no procede | Operación |
| *"Se superó el límite de solicitudes"* / *"error inesperado"* / la pantalla se queda cargando | Falla técnica | Tecnología |

Tratar un rechazo correcto como incidente moviliza al equipo equivocado y deja el
problema real sin atender.

---

## Diagnóstico

### Qué revisaría primero

**En este orden, y por esta razón:**

**1. ¿El servicio responde ahora?** (10 segundos)

```bash
curl -s -w "\n%{http_code} · %{time_total}s\n" https://<api>/api/health
```

Con fallo intermitente espero `Healthy` — el servicio vive. Lo que importa aquí es el
**tiempo de respuesta**: si tarda segundos, ya hay saturación. Vale la pena repetirlo
cinco veces seguidas; una respuesta lenta e irregular es en sí misma el síntoma.

**2. ¿Es rechazo o es error?** (2 minutos)

Esta es la bifurcación del diagnóstico. Con un `correlationId` de un caso reportado se
resuelve de inmediato (ver *Cómo identificaría la causa raíz*). Sin él:

```sql
SELECT "Result", "RejectionCode", COUNT(*)
FROM   "PrintRequests"
WHERE  "ProcessedAt" BETWEEN '2026-08-23 09:00' AND '2026-08-23 10:00'
GROUP  BY "Result", "RejectionCode"
ORDER  BY COUNT(*) DESC;
```

**Que un rechazo aparezca aquí ya dice mucho: significa que la solicitud llegó y se
procesó.** Un fallo técnico de transporte no deja fila en `PrintRequests`.

**3. ¿Cuántas solicitudes llegaron respecto a un día normal?** (1 minuto)

```sql
SELECT date_trunc('hour', "ProcessedAt") AS hora, COUNT(*)
FROM   "PrintRequests"
WHERE  "ProcessedAt" >= NOW() - INTERVAL '3 days'
GROUP  BY 1 ORDER BY 1 DESC;
```

**Este es el contraste que confirma o descarta la hipótesis principal.** Si a las 9 AM
hay *menos* solicitudes registradas que un día normal, las que faltan **nunca llegaron a
procesarse**: se quedaron en el `429` o en un error de transporte. Un hueco en el
volumen es la firma del rate limiting.

**4. ¿Hubo reinicios de la instancia?**

En el panel de Render: eventos de *deploy*, *restart* o *spin down* dentro de la ventana.
Un reinicio a mitad de hora explica intermitencia perfectamente — las solicitudes en
vuelo se pierden y las siguientes esperan el arranque, la migración y la carga semilla.

### Qué métricas consultaría

**`GET /api/admin/dashboard`** (rol `Admin`) es la primera pantalla útil:

```json
{ "totalRequests": 120, "approved": 95, "rejected": 25, "reprints": 12,
  "rejectionsByCode": { "INSUFFICIENT_INVENTORY": 12, "LPN_NOT_FOUND": 8 } }
```

`rejectionsByCode` responde en un vistazo si el pico es de negocio o técnico:

| Patrón | Lectura para este caso |
|---|---|
| `TOO_MANY_REQUESTS` presente | **Hipótesis principal confirmada.** Ir a la causa **A** |
| `INSUFFICIENT_INVENTORY` o `NOT_STOCKED` dominan **en varias zonas a la vez** | La sincronización de inventario no corrió antes del turno. Es incidente, aunque el rechazo sea correcto (causa **D**) |
| `INVALID_DOCUMENT_STATUS` domina | Se está trabajando sobre documentos no liberados: las olas de las 9 AM aún no cambiaron de estado. Problema aguas arriba |
| `LPN_NOT_FOUND` domina | Etiquetas de una ola que no se cargó |
| Nada concentrado y `rejected` bajo | Las fallas no llegan a registrarse: es transporte, no negocio. Causas **B** o **C** |
| `totalRequests` muy por debajo de lo normal | Confirma el hueco: las solicitudes no están llegando |

Del lado de la plataforma: **CPU, memoria y conexiones a Postgres** en Render durante la
ventana, y el conteo de respuestas por código HTTP. La relación entre `429`, `500` y
`200` reparte el problema entre límite, fallo y operación normal.

### Qué logs inspeccionaría

Todos los eventos llevan el `correlationId` en su ámbito, así que el log se puede leer
por solicitud y no por hora aproximada.

**Primero, los tres patrones que emite esta solución:**

```
Solicitud rechazada por limite de trafico. Ruta=... Particion=... CorrelationId=...
Error no controlado. Ruta=... Metodo=... CorrelationId=...
Impresion rechazada. CorrelationId=... Lpn=... Regla=R3_ZONE_AVAILABILITY Motivo=...
```

- **Muchos `Solicitud rechazada por limite de trafico`** → causa **A**. Y el campo
  `Particion` es decisivo: si todas dicen `anon:` con la **misma IP**, el problema no es
  el límite sino que todas las tiendas están colapsando en una sola partición.
- **`Error no controlado` repetido** → tomar su `correlationId` y leer la excepción
  completa, que sí queda en el log del servidor.
- **Solo `Impresion rechazada`** → no hay falla técnica; hay un problema operativo.

**Segundo, los eventos de arranque.** Si aparecen `Application started` dentro de la
ventana, hubo reinicio: eso solo ya explica la intermitencia.

**Tercero, `Fallo la inicializacion de la base de datos`.** El seeder captura su propia
excepción para no tumbar el servicio, así que un fallo ahí es silencioso desde afuera.

**Lo que por diseño no voy a encontrar:** tokens, contraseñas, llaves ni payloads
cifrados. Si aparecen, eso mismo es un incidente de seguridad aparte.

### Cómo identificaría la causa raíz

**Pidiendo un `correlationId` concreto.** Ante un fallo técnico la interfaz lo muestra
al usuario — *"(referencia: 839b3935…)"* — y viaja en el header `X-Correlation-Id` de
toda respuesta. Es el atajo que evita reconstruir el caso por hora aproximada.

Con ese identificador, la solicitud completa:

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
llegó la evaluación: tres filas con la última en `Passed = false` significa que R0 y R1
pasaron y R2 rechazó.

**Si el `correlationId` no aparece en `PrintRequests`, eso es el hallazgo:** la
solicitud nunca llegó al caso de uso. Se quedó en el rate limiter, en CORS, en
autenticación o en la red.

**El método, en una línea:** contrastar el volumen de solicitudes contra un día normal
para saber si el problema es *lo que se procesó* o *lo que no llegó a procesarse*, y
recién entonces buscar el porqué. Sin ese contraste es fácil investigar a fondo los
rechazos visibles y no notar que faltan la mitad de las solicitudes.

**Y una confirmación antes de cerrar:** correlacionar la ventana horaria con el pico de
concurrencia. Si el fallo se reproduce mañana a las 9 AM, la causa es de capacidad. Si
no vuelve a ocurrir, fue un evento puntual —un reinicio— y la investigación debe ir a
los eventos de la plataforma.

---

## Causas raíz candidatas, ordenadas por probabilidad para este caso

### A. El rate limiting se agota en el pico de las 9 AM  ← hipótesis principal

- **Encaja con los tres datos del caso:** intermitente (unos pasan, otros no), múltiples
  tiendas (comparten el límite) y ventana horaria (el pico de inicio de turno).
- **Confirmación:** presencia de `429` en los logs y hueco en el volumen de
  `PrintRequests` frente a un día normal.
- **Sub-causa más probable:** `UseForwardedHeaders` no está aplicando y todas las
  solicitudes anónimas colapsan en la partición de la IP del proxy de Render. El síntoma
  que lo delata: **el `429` golpea sobre todo al login**, mientras los usuarios ya
  autenticados operan normal — porque a esos se les cuenta por identidad.
- **Contención inmediata, sin recompilar ni redesplegar código:**

  ```
  RateLimiting__Policies__AuthEndpoints__PermitLimit=60
  RateLimiting__PermitLimit=300
  ```

  o `RateLimiting__Enabled=false` como medida temporal. Es configuración: se cambia en
  el panel y el servicio la toma al reiniciar.
- **Solución definitiva:** corregir la propagación de la IP real y recalibrar el límite
  contra la concurrencia observada en el pico, no contra un número elegido a priori.

### B. Reinicio o suspensión de la instancia

- **Encaja con:** intermitencia y simultaneidad entre tiendas.
- **Síntoma:** primera solicitud lenta o fallida y luego todo normal; `Application
  started` en los logs dentro de la ventana.
- **Causa en esta entrega:** el plan gratuito de Render suspende por inactividad. Tras
  el reposo, la instancia debe arrancar, migrar y sembrar antes de responder.
- **Contención:** ninguna a nivel de aplicación; el frontend ya lo comunica como estado
  de carga con mensaje explícito, no como error silencioso.
- **Solución definitiva:** plan de pago o un ping periódico al health check.

### C. Saturación de conexiones a PostgreSQL

- **Encaja con:** intermitencia bajo carga concurrente.
- **Síntoma:** `Error no controlado` con excepción de Npgsql por timeout; health
  intermitentemente `503` con `database: Unhealthy`.
- **Contención:** limitar la concurrencia o ampliar el pool.
- **Nota:** el plan gratuito de Render tiene un tope bajo de conexiones, y a las 9 AM es
  cuando se alcanza.

### D. Los datos de origen no están listos a esa hora

- **Este es el caso en que un rechazo correcto sí es un incidente.** El sistema
  responde bien; lo que está mal es lo que ve.
- **Síntoma:** `INSUFFICIENT_INVENTORY` o `INVALID_DOCUMENT_STATUS` concentrados a
  primera hora y normalizándose después — porque la sincronización corre más tarde.

```sql
-- ¿El inventario dice cero donde el piso tiene mercancía?
SELECT p."ProductCode", z."Code", i."AvailableQty", i."IsStocked", i."LastUpdateDate"
FROM   "InventoryAvailability" i
JOIN   "Products" p ON p."Id" = i."IdProduct"
JOIN   "Zones" z ON z."Id" = i."IdZone"
WHERE  i."LastUpdateDate" < NOW() - INTERVAL '12 hours';
```

`LastUpdateDate` antiguo con operación activa señala que la sincronización dejó de
correr.

### E. CORS tras un despliegue del frontend

- **Síntoma que lo distingue:** la web no funciona pero el API responde bien por Swagger
  o `curl`; en la consola del navegador, *blocked by CORS policy*.
- **No encaja del todo con "intermitente"** salvo que convivan dos versiones del
  frontend, una en un dominio nuevo.
- **Solución:** agregar el origen a `Cors__AllowedOrigins__N` **y redesplegar el
  backend** — la configuración se lee al arrancar.

### F. Reimpresión duplicada bajo concurrencia

- No explica el reporte, pero **el pico de concurrencia es justo cuando se manifiesta**
  la condición de carrera documentada en `ARCHITECTURE.md`: dos solicitudes simultáneas
  sobre el mismo LPN pueden ambas leer "sin impresión previa".

```sql
SELECT "LpnId", COUNT(*) FROM "PrintRequests"
WHERE  "Result" = 'APPROVED' AND "EventType" = 'PRINT'
GROUP  BY "LpnId" HAVING COUNT(*) > 1;
```

Conviene revisarlo mientras se investiga: una etiqueta duplicada en piso es un problema
operativo real y silencioso.

---

## Comunicación

### Cómo informaría al usuario operativo

Al operario de tienda le sirven tres cosas: **qué puede hacer ahora, qué no debe rehacer
y cuándo vuelvo a hablarle.** Sin jerga, sin causa raíz especulativa.

> **9:20 AM — Impresión de etiquetas**
> Estamos presentando intermitencia en la impresión de etiquetas desde las 9:00.
> Algunas solicitudes están pasando y otras no.
>
> **Qué hacer:** si la pantalla muestra un error, **reintenta una vez**. Si te indica un
> motivo (inventario, documento anulado), ese motivo es real y no se resuelve
> reintentando.
>
> **Importante:** todo lo que alcanzó a imprimirse **quedó registrado**. No hay que
> rehacer nada ni volver a imprimir por precaución.
>
> Próxima actualización: 9:50 AM.

Tres decisiones detrás de ese mensaje:

- **"Reintenta una vez"** es accionable y es correcto para las causas A y B. "Estamos
  trabajando en ello" a secas deja al operario sin saber qué hacer con el bulto que
  tiene en la mano.
- **Distinguir error de motivo** evita que reintenten indefinidamente contra un rechazo
  legítimo, lo que además agrava la saturación si la causa es A.
- **"Quedó registrado"** ataca el miedo real: que se pierda trazabilidad y haya que
  reimprimir todo. La auditoría persiste apruebe o rechace, así que es verdad y conviene
  decirlo.

**Comprometer una hora de actualización y cumplirla** importa más que la actualización
en sí: sin ella, cada tienda llama por separado.

**Al negocio**, el mismo hecho pero medido: cuántas solicitudes, desde cuándo, si hay
mercancía detenida.

```sql
SELECT COUNT(*) FILTER (WHERE "Result" = 'APPROVED') AS aprobadas,
       COUNT(*) FILTER (WHERE "Result" = 'REJECTED') AS rechazadas,
       COUNT(DISTINCT "IdZone") AS zonas_afectadas,
       MIN("ProcessedAt") AS desde
FROM   "PrintRequests"
WHERE  "ProcessedAt" >= NOW() - INTERVAL '2 hours';
```

**Al equipo técnico:** `correlationId` de un caso representativo, código predominante,
la entrada de log, y **qué ya se descartó** — para que nadie repita el mismo camino.

**Lo que no comunicaría:** una causa raíz antes de tenerla. "Estamos investigando" es
una respuesta legítima; una causa equivocada dispara acciones correctivas equivocadas y
cuesta más que el silencio.

### Cómo manejaría la contingencia

**Primero, contener sin romper la trazabilidad.** Para la hipótesis principal la
contención es inmediata y reversible: subir los límites por variable de entorno. Es
configuración, no despliegue de código.

Si el servicio no se restablece en un plazo aceptable:

**1. El ZPL está en la base y puede enviarse a la impresora por fuera del sistema.**

```sql
SELECT l."Zpl" FROM "Labels" l WHERE l."LpnId" = 'LPN-000987654';
```

**2. Registrar en papel o planilla lo que se imprima así**, y volcarlo a la auditoría al
restablecer. Una etiqueta impresa sin traza es exactamente el problema que este
submódulo existe para evitar.

**3. Priorizar por zona o documento.** La validación de inventario es por zona, así que
si el problema afecta a unas zonas, el resto de la operación continúa.

**Lo que no haría bajo ninguna presión: apagar la validación de reglas para "destrabar"
la operación.** Imprimir sobre documentos anulados genera mercancía mal etiquetada en
piso, y ese costo —devoluciones, inventario descuadrado, reprocesos— supera con holgura
al de una hora de demora. Apagar el rate limiting es reversible y no corrompe datos;
apagar las reglas de negocio, no.

---

## Cierre

### Cómo documentaría el incidente

Un incidente no termina cuando el servicio responde. Termina cuando queda registrado de
forma que el siguiente sea más corto.

**Ficha del incidente:**

| Campo | Contenido |
|---|---|
| **Ventana** | 09:00–10:00, 23/08/2026 |
| **Impacto medido** | N solicitudes afectadas, X zonas, Y tiendas — con la consulta, no estimado |
| **Síntoma reportado** | "Las impresiones fallan intermitentemente" |
| **Causa raíz** | La confirmada, con la **evidencia**: `correlationId` de referencia, entrada de log, consulta que la demuestra |
| **Descartadas** | Qué se revisó y por qué se descartó — evita repetir el camino |
| **Detección** | Cómo nos enteramos: ¿lo reportó la tienda o lo vimos nosotros? |
| **Contención** | Qué se hizo, a qué hora, y si fue reversible |
| **Solución definitiva** | Con **responsable y fecha**. Sin eso, el análisis es un documento que nadie vuelve a abrir |

**"Detección" merece su propia línea.** Que el incidente lo reporte la tienda y no la
monitorización es en sí un hallazgo: significa que no hay alerta sobre la tasa de `429`
ni sobre la caída de volumen, y esa es una acción preventiva tan válida como la
corrección técnica.

**Acciones preventivas que salen de este caso concreto:**

- Alerta sobre la tasa de `429` por ventana, y sobre caídas bruscas del volumen de
  solicitudes respecto al mismo día de la semana anterior.
- Recalibrar los límites contra la concurrencia real del pico de las 9 AM.
- Verificar que `UseForwardedHeaders` esté propagando la IP real: es la diferencia entre
  limitar por tienda y limitar a todas juntas.

### Checklist de cierre

- [ ] Restablecimiento confirmado **con una solicitud real de extremo a extremo**, no solo con el health check.
- [ ] Auditoría del período revisada y sin huecos.
- [ ] Impresiones manuales de contingencia volcadas a `PrintRequests`.
- [ ] Cierre comunicado a los **mismos** destinatarios que recibieron el aviso inicial.
- [ ] Causa raíz registrada con su `correlationId` de referencia.
- [ ] Acción preventiva con responsable y fecha.

### Verificación de restablecimiento

```bash
curl -s https://<api>/api/health
```

```bash
curl -s -X POST https://<api>/api/print-requests \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"lpn":"LPN-000987654"}'
```

Un health check verde solo dice que el proceso vive y la base responde. **Que la
solicitud completa funcione es lo único que prueba el restablecimiento** — y con un
fallo intermitente conviene repetirla varias veces, porque una sola respuesta correcta
no descarta la intermitencia.

---

## Referencia rápida de códigos

| Código | HTTP | ¿Incidente? | Significado |
|---|---|---|---|
| `MISSING_REQUIRED_DATA` | 200 | No | Faltan datos en la solicitud |
| `LPN_NOT_FOUND` | 200 | Solo si es masivo | La ETQ/LPN no existe |
| `ZONE_NOT_FOUND` | 200 | Revisar catálogo | Zona inexistente o inactiva |
| `INVALID_DOCUMENT_STATUS` | 200 | No, salvo masivo | Documento anulado o devuelto |
| `INSUFFICIENT_INVENTORY` | 200 | Si es masivo, revisar sincronización | Cantidad insuficiente en la zona |
| `NOT_STOCKED` | 200 | Si es masivo, revisar sincronización | Producto no abastecido en la zona |
| `REPRINT_REASON_REQUIRED` | 200 | No | Falta el motivo de reimpresión |
| `REPRINT_NOT_AUTHORIZED` | 200 | No | El rol no autoriza reimprimir |
| `TOO_MANY_REQUESTS` | 429 | **Sí, si es generalizado** | Límite de tráfico superado |
| `INTERNAL_ERROR` | 500 | **Sí** | Fallo no controlado — usar el `correlationId` |
| `SERVICE_UNHEALTHY` | 503 | **Sí** | Un componente no responde |
