# Casos de prueba y evidencia

Matriz de los casos definidos en el plan (CP-01 a CP-13), con el mecanismo que los
verifica y el resultado obtenido.

## Resumen de ejecución

```
dotnet test
Passed!  -  Failed: 0, Passed: 57, Skipped: 0, Total: 57
```

57 pruebas xUnit sobre `Homecenter.Microservice.Api.LabelPrinting.Logic.Tests`,
organizadas en dos niveles:

- **Reglas en aislamiento** (`Rules/`): cada regla recibe un contexto ya resuelto y se
  verifica su veredicto. No tocan base de datos, HTTP ni contenedor de dependencias.
- **Casos de uso con dobles en memoria** (`UseCases/`): verifican lo que solo aparece
  al ensamblar todo — que se imprima únicamente cuando corresponde, que la auditoría
  se persista siempre y que el rechazo de negocio viaje como respuesta válida.

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
| CP-13 | 11 logins fallidos en un minuto | Rate limiting | **Pendiente — B7** | El middleware aún no existe |

**CP-13 no está cubierto y no se está presentando como si lo estuviera.** El rate
limiting se implementa en el bloque de hardening; hasta entonces no hay nada que probar.

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

## Verificación por mutación

Que 57 pruebas pasen no demuestra que detecten algo. Se introdujeron defectos
deliberados en el código de producción para confirmar que las pruebas fallan cuando
deben. Los tres mutantes fueron detectados y el código se restauró después:

| Mutante introducido | Pruebas que fallaron |
|---|---|
| Límite exclusivo: `availableQty >= requested` → `>` | 2 (regla y caso de uso del límite exacto) |
| `Operario` agregado a los roles autorizados a reimprimir | 3 |
| Historial sin restricción por usuario (`restrictToUserId = null` siempre) | 2 |

El tercero es el más relevante: es la fuga de datos entre operarios, y una prueba que
no la detecte no está protegiendo nada.

## Verificación end-to-end contra el API

Ejecutada contra el servicio corriendo en `localhost:5080` con la base sembrada.

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

## Cómo reproducir

```bash
dotnet test
```

Las pruebas no requieren base de datos ni configuración: el motor de reglas no conoce
EF ni HTTP, y los repositorios se sustituyen por dobles en memoria.
