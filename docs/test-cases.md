# Casos de prueba y evidencia

Matriz de los casos definidos en el plan (CP-01 a CP-13), con el mecanismo que los
verifica y el resultado obtenido.

## Resumen de ejecución

```
dotnet test
Passed!  -  Failed: 0, Passed: 68, Skipped: 0, Total: 68
```

68 pruebas xUnit sobre `Homecenter.Microservice.Api.LabelPrinting.Logic.Tests`,
organizadas en tres niveles:

- **Reglas en aislamiento** (`Rules/`): cada regla recibe un contexto ya resuelto y se
  verifica su veredicto. No tocan base de datos, HTTP ni contenedor de dependencias.
- **Casos de uso con dobles en memoria** (`UseCases/`): verifican lo que solo aparece
  al ensamblar todo — que se imprima únicamente cuando corresponde, que la auditoría
  se persista siempre y que el rechazo de negocio viaje como respuesta válida.
- **Cifrado** (`Services/`): AES-256 se prueba a fondo porque un error ahí no se
  manifiesta como excepción sino como datos que parecen protegidos y no lo están.

## Matriz de casos

| Caso | Escenario | Regla | Verificado por | Resultado |
|---|---|---|---|---|
| CP-01 | LPN inexistente | R1 | `LabelExistsRuleTests` · `ProcessPrintRequestUseCaseTests.Rechaza_un_LPN_inexistente_sin_imprimir` | `LPN_NOT_FOUND`, auditado |
| CP-02 | Documento `ANULADA` | R2 | `DocumentStatusRuleTests` · caso de uso (Theory) | `INVALID_DOCUMENT_STATUS` |
| CP-03 | Documento `DEVUELTA` | R2 | `DocumentStatusRuleTests` · caso de uso (Theory) | `INVALID_DOCUMENT_STATUS` |
| CP-04 | `availableQty < requestedQty` | R3 | `ZoneAvailabilityRuleTests.Rechaza_con_INSUFFICIENT_INVENTORY...` | `INSUFFICIENT_INVENTORY` con detalle por producto |
| CP-05 | Stock disponible pero `isStocked = false` | R3 | `ZoneAvailabilityRuleTests.Rechaza_con_NOT_STOCKED...` | `NOT_STOCKED` |
| CP-06 | `availableQty == requestedQty` | R3 | `ZoneAvailabilityRuleTests.Aprueba_cuando_la_cantidad_solicitada_iguala_exactamente_la_disponible` | **Éxito** (límite inclusivo) |
| CP-07 | Primera impresión válida | R5 | `ProcessPrintRequestUseCaseTests.Aprueba_la_primera_impresion_y_entrega_el_zpl` | `APPROVED` / `PRINT`, ZPL entregado |
| CP-08 | Reimpresión con rol y motivo | R4 | `ReprintPolicyRuleTests` (Theory Supervisor/Admin) · caso de uso | `APPROVED` / `REPRINT`, motivo auditado |
| CP-09 | Reimpresión sin motivo | R4 | `ReprintPolicyRuleTests` (Theory null/vacío/espacios) | `REPRINT_REASON_REQUIRED` |
| CP-10 | Reimpresión solicitada por `Operario` | R4 | `ReprintPolicyRuleTests` · caso de uso | `REPRINT_NOT_AUTHORIZED` |
| CP-11 | Solicitud sin LPN o sin usuario | R0 | `RequiredDataRuleTests` | `MISSING_REQUIRED_DATA` |
| CP-12 | Historial consultado por `Operario` | Autorización | `GetPrintHistoryUseCaseTests` + verificación contra API real | Solo sus solicitudes (`scope: OWN`) |
| CP-13 | 11 logins fallidos en un minuto | Rate limiting | Verificación contra el API corriendo | HTTP `429` exactamente en el intento 11 |

CP-13 se verifica contra el servicio corriendo y no con una prueba unitaria: lo que
hay que validar es la tubería HTTP completa, que es donde vive el limitador.

## Casos adicionales no exigidos por el plan

Se agregaron porque cubren decisiones de diseño que una matriz por regla no alcanza:

| Escenario | Por qué importa |
|---|---|
| Zona solicitada inexistente | Se distingue de un fallo de inventario: el operario debe corregir la zona, no el pedido |
| Varios productos incumpliendo a la vez | Se reportan todos, no solo el primero |
| Coexistencia de faltante y no abastecido | Un solo código representa el rechazo: prevalece `INSUFFICIENT_INVENTORY` |
| Producto sin fila de inventario en la zona | No se trata como disponibilidad cero implícita |
| ETQ sin productos asociados | Imprimirla ampararía mercancía inexistente |
| Orden de evaluación del motor | Un documento anulado no debe rechazarse por falta de stock |
| Corte temprano con traza conservada | La auditoría muestra qué **sí** se revisó, no solo qué falló |
| Auditoría del rechazo y del LPN inexistente | Sin ese registro, un LPN mal digitado repetidamente sería invisible |
| `correlationId` presente en el rechazo de negocio | El rechazo viaja como HTTP 200 y aun así debe poder rastrearse |
| Usuario auditado tomado del token | Si viniera del body, la auditoría dejaría de ser un control |
| Solicitud sin zona (contrato del anexo) | `requetEtq.json` solo trae el LPN: omitirla debe seguir funcionando |
| Bloque `legacy` con `hasMultipleProducts` | El consumidor del anexo no se rompe, pero tampoco se le oculta la degradación |
| Dos cifrados del mismo texto difieren | Es la prueba que justifica el IV aleatorio: con IV fijo serían idénticos |
| Una llave distinta no descifra | Si esto fallara, el cifrado no estaría protegiendo nada |
| Mensaje cifrado manipulado | No se descifra silenciosamente en basura |

## Verificación por mutación

Que las pruebas pasen no demuestra que detecten algo. Se introdujeron defectos
deliberados en el código de producción para confirmar que fallan cuando deben. Los
tres mutantes fueron detectados y el código se restauró después:

| Mutante introducido | Pruebas que fallaron |
|---|---|
| Límite exclusivo: `availableQty >= requested` → `>` | 2 (regla y caso de uso del límite exacto) |
| `Operario` agregado a los roles autorizados a reimprimir | 3 |
| Historial sin restricción por usuario (`restrictToUserId = null` siempre) | 2 |

El tercero es el más relevante: es la fuga de datos entre operarios, y una prueba que
no la detecte no está protegiendo nada.

## Verificación end-to-end contra el API

Ejecutada contra el servicio corriendo en `localhost:5080` con la base sembrada.

### Reglas, historial e interfaz

| Verificación | Resultado observado |
|---|---|
| Los 5 LPN semilla procesados por un operario | Cada uno devolvió el código de rechazo esperado por su caso de prueba |
| Reimpresión por supervisor con motivo | `success: true`, `eventType: REPRINT` |
| Historial como `Supervisor` | `total: 27`, `scope: ALL` |
| Historial como `Operario` | `total: 21`, `scope: OWN` — solo filas propias |
| Filtros combinados (resultado + tipo de evento) | 27 → 4 registros, todos aprobados y de reimpresión |
| Paginación | Página 1: `1–20 de 27`; página 2: `21–27 de 27`; botones deshabilitados en los extremos |
| Filtrar estando en página 2 | Regresa a página 1 en lugar de mostrar una página vacía |
| Filtro sin coincidencias | Estado vacío explícito, sin tabla ni paginación |
| Filtro por usuario para rol `Operario` | No se renderiza en la interfaz; el backend lo ignora aunque se envíe |
| Viewport de 375 px | Tabla oculta, 20 tarjetas renderizadas, sin desborde horizontal |
| Consola del navegador | Sin errores |

**El control de CP-12 se verificó contra el API, no solo en la interfaz.** Ocultar el
filtro de usuario es una decisión de presentación; lo vinculante es que el backend
imponga la restricción desde el token, que es lo que confirman tanto la prueba
unitaria como la diferencia de 27 contra 21 registros en la respuesta real.

### Hardening

| Verificación | Resultado observado |
|---|---|
| CP-13: 13 logins fallidos seguidos | Intentos 1–10 → `401`; **intento 11 → `429`** con `TOO_MANY_REQUESTS` |
| Respuesta del `429` | Envelope estándar + `Retry-After: 60` + `X-Correlation-Id` |
| Partición por usuario | Operario bloqueado en la solicitud 31 (límite 30); **supervisor sigue en `200`** |
| Health check exento del límite | 20 llamadas consecutivas → 20 × `200` |
| `correlationId` generado por el servicio | Presente en el header de toda respuesta |
| `correlationId` propuesto por el cliente | Se respeta, para rastrear una operación entre servicios |
| `correlationId` desmedido (300 caracteres) | Se descarta y se genera uno propio |
| Excepción real no controlada (base inalcanzable) | `500` con envelope; **cero rastro de pila, Npgsql o SQL en el cuerpo**; detalle completo solo en el log |
| Login con `encryptedPayload` en el formato del frontend | `success: true`, token emitido — interoperabilidad confirmada |
| Payload cifrado manipulado | `400 INVALID_ENCRYPTED_PAYLOAD`, sin filtrar el motivo criptográfico |
| Solicitud de login vacía | `400 MISSING_CREDENTIALS` |
| Credenciales en claro | Siguen funcionando: el contrato admite ambas formas |
| Log del servidor | Cero coincidencias de contraseñas o del payload cifrado |

La verificación del manejador global se hizo levantando una instancia aparte apuntada
a una base inalcanzable, para provocar una excepción **real** en lugar de simularla.

## Cómo reproducir

```bash
dotnet test
```

Las pruebas no requieren base de datos ni configuración: el motor de reglas no conoce
EF ni HTTP, y los repositorios se sustituyen por dobles en memoria.
