# Arquitectura y decisiones de diseño

Este documento explica **por qué** la solución está construida así. Lo que hace está en
el código y en Swagger; lo que no se puede leer ahí son las alternativas descartadas y
las razones.

## Regla de dependencias

```
Api  →  Logic  →  Abstractions  ←  EntityFramework
                        ↑
        Entities  ·  Data.Transfer.Object  (transversales)
```

`Logic` contiene el motor de reglas y los casos de uso, y **no conoce Entity Framework
ni HTTP**. Esa restricción no es estética: es lo que permite que las 68 pruebas corran
sin base de datos, sin servidor y en menos de 300 ms. Si el motor de reglas dependiera
del `DbContext`, probar "documento anulado se rechaza" exigiría levantar Postgres, y en
la práctica esa prueba no se escribiría.

`Abstractions` declara las interfaces de repositorios y servicios. `EntityFramework` las
implementa. La flecha apunta hacia adentro: la capa de datos depende del dominio, no al
revés.

## Las cinco decisiones que más definen la solución

### 1. El rechazo de negocio es HTTP 200, no 400 (H6)

Cuando una impresión se rechaza porque el documento está anulado o falta inventario, el
API responde **200 con `success: false`** y el motivo en `error`.

La solicitud se procesó correctamente. Lo que no procede es la impresión. Un `400`
significa "tu petición está mal formada" y llevaría al cliente a revisar su código
cuando el problema es de inventario en la zona. Además, un rechazo de negocio **se
audita**; un error de forma no.

Los errores técnicos sí usan códigos HTTP: `400` forma, `401` sin token, `403` rol
insuficiente, `429` límite, `500` fallo no controlado.

La consecuencia práctica se ve en el frontend: los rechazos de negocio **nunca** pasan
por el interceptor de errores, y por eso `PrintingService.print()` devuelve el envelope
completo en vez de desenvolver `data`.

### 2. El usuario nunca viaja en el body (H2)

La sección 9 del enunciado pide un campo "Usuario" en la pantalla. Está, pero es de
**solo lectura** y se llena desde el JWT.

Si el operario pudiera escribirlo, la auditoría dejaría de ser un control y pasaría a
ser un campo de texto libre: cualquiera podría atribuir una impresión a otra persona. El
campo se muestra porque el enunciado lo pide y porque el operario debe ver con qué
identidad está operando, no para que lo edite.

### 3. Las reglas son clases, no condicionales dentro del caso de uso

Cada regla implementa `IPrintRule` con un `Order`. El motor las ordena y corta en la
primera violación. Agregar una regla nueva es registrar una clase más: no se toca el
motor ni el caso de uso.

El corte temprano importa para el mensaje: validar inventario de un LPN inexistente
produciría un segundo motivo de rechazo que contradice al verdadero. Aun así **se
conserva la traza completa de lo evaluado hasta el corte**, porque la auditoría debe
poder mostrar qué sí se revisó, no solo qué falló.

El orden también es una decisión: el estado del documento se evalúa **antes** que el
inventario, para que un documento anulado no se rechace por falta de stock.

### 4. La autorización del historial se impone en el backend

Un `Operario` solo ve sus propias solicitudes. El filtro se aplica en
`GetPrintHistoryUseCase` a partir del token, no en la interfaz.

Ocultar filas en el frontend dejaría los datos accesibles para cualquiera que llame al
endpoint directamente. La interfaz además oculta el filtro por usuario a ese rol, pero
eso es presentación: aunque se envíe `userName` de otra persona en el query string, el
backend lo ignora.

Hay una prueba dedicada a esto y se verificó por mutación: quitar la restricción hace
fallar dos pruebas.

### 5. El vector de inicialización del cifrado es aleatorio, no configurado

El plan declaraba `Encryption:IV` como valor de configuración. **No se usa.**

Un IV fijo en modo CBC hace que dos textos idénticos produzcan exactamente el mismo
criptograma, revelando cuándo dos valores son iguales sin necesidad de descifrarlos. Un
IV no es un secreto: solo tiene que ser distinto cada vez. `AesEncryptionService` genera
uno aleatorio por operación y lo transmite como prefijo del mensaje —
`Base64( IV[16] || criptograma )`.

El ajuste se conserva por compatibilidad de configuración y solo se valida su formato si
alguien lo define, para que un valor equivocado no quede ahí aparentando estar en uso.

**Alcance real del cifrado del payload de credenciales:** la llave viaja dentro del
bundle de JavaScript, así que cualquiera puede leerla. Frente a un atacante que ya
intercepta el tráfico no agrega secreto — **la confidencialidad en tránsito la aporta
HTTPS**. Lo que sí evita es que las credenciales queden en claro en registros
intermedios (historiales de proxy, capturas de sesión de soporte). Es una capa
adicional, y se documenta así para que nadie la interprete como una garantía que no
puede dar. Por eso viene apagada por defecto.

## Hallazgos de los anexos y supuestos asumidos

| # | Hallazgo | Decisión |
|---|---|---|
| **H1** | `tableOrders.json` **no parsea**: falta la coma después de `"templateCode": "TPL-ETQ-STD-4X6"` (línea 16) | Se corrige en `mocks/orders.json` y se conserva el original en `mocks/_anexo_tableOrders.original.json` como evidencia |
| **H2** | `requetEtq.json` solo trae `lpn`, pero la sección 9 exige LPN + Zona + Usuario | `zone` se acepta como override opcional; si falta, se usa la del documento origen. El usuario sale del JWT |
| **H3** | `responseEtq.json` usa otro vocabulario y expone **un solo SKU escalar**, pero una ETQ puede arrastrar varios productos | La respuesta trae el arreglo completo **más** un bloque `legacy` con la forma exacta del anexo, marcando `hasMultipleProducts: true` cuando hay más de uno |
| **H4** | **No existe archivo de inventario**, pero la Regla 3 depende de él | Se crea `mocks/inventoryAvailability.json` (producto × zona → `availableQty`, `isStocked`) |
| **H5** | El enunciado no define quién puede reimprimir | Se exige rol `Supervisor`/`Admin` y motivo obligatorio. Una etiqueta duplicada en piso es un problema operativo real |
| **H6** | El enunciado no define el código HTTP del rechazo de negocio | HTTP `200` con envelope `success: false`. Ver decisión 1 |

Sobre **H3**: perder productos en silencio sería un error funcional. Documentar la
degradación no lo es — por eso el bloque `legacy` declara explícitamente cuándo está
mostrando una vista parcial.

## El ZPL de los anexos se conserva tal cual

El ZPL del anexo 3 es el ejemplo genérico de Zebra ("Intershipping, Inc.", "John Doe",
"Springfield TN"). **Se usa sin modificar** como contenido de la semilla.

La generación de la etiqueta está **fuera del alcance** declarado del enunciado: la ETQ
ya viene pre-generada y lo que se evalúa es la validación y la trazabilidad al
imprimirla. Sustituirlo por un ZPL con datos de Homecenter habría sido inventar un
entregable no pedido y habría oscurecido que el contenido proviene literalmente del
anexo entregado.

## Decisiones de despliegue

### No se usa `UseHttpsRedirection`

Render termina TLS en su proxy y reenvía la petición al contenedor por HTTP. Un
redirect a HTTPS dentro del contenedor produciría un bucle o rompería el health check,
porque la aplicación nunca ve una conexión HTTPS directa. La seguridad del transporte la
garantiza el proxy, no el proceso.

Por la misma razón se configura `UseForwardedHeaders`: sin ello `RemoteIpAddress` es la
del proxy y **todas las solicitudes anónimas compartirían una sola partición de rate
limiting**, de modo que un atacante dejaría sin login a todos los demás.

### Swagger queda habilitado en producción

Es una excepción consciente al hardening, no un descuido: el evaluador debe poder probar
la API sin herramientas adicionales. Está gobernado por `Swagger:Enabled` y se apaga con
una variable de entorno.

### CORS expone dos headers

`WithExposedHeaders(X-Correlation-Id, Retry-After)`. El navegador oculta los headers de
respuesta cross-origin salvo los declarados, y el frontend vive en un dominio distinto
al API. Sin esto, el identificador de correlación llegaría como `null` en producción — y
un identificador que el usuario no puede citar no sirve para diagnosticar nada.

### El JWT viaja en `Authorization`, no en cookie

Frontend en Cloudflare Pages y API en Render son dominios distintos. Un token en cookie
exigiría `SameSite=None` y toda la fricción de cookies cross-site. El header lo evita por
completo.

### El archivo ZPL no se persiste en el contenedor

`Printing__PersistZplFile=false` en Render. El sistema de archivos es efímero: el archivo
se perdería en cada reinicio y no constituye evidencia. El evento lógico de impresión y
la auditoría en base de datos sí persisten, que es lo que el enunciado exige. En local
queda encendido, donde sí sirve para inspeccionar la salida.

## Rate limiting: por qué por usuario y no por IP

En una tienda varios operarios salen por la misma conexión a internet. Contarlos juntos
haría que uno con el dedo pegado al botón dejara sin servicio a todos los demás. La
partición usa la identidad autenticada cuando existe, y la IP solo para lo anónimo.

El health check está **exento**. Render lo sondea para decidir si la instancia sigue
viva: un `429` ahí le haría concluir que el servicio está caído y reiniciarlo. El rate
limiting causaría la caída que pretende evitar.

## Qué haría distinto con más tiempo

Reconocer los límites del entregable es parte de la entrega:

- **Pruebas de integración de endpoints.** Hay pruebas de reglas y de casos de uso, pero
  la capa HTTP (autorización por atributo, model binding, rate limiting) se verificó
  manualmente contra el servicio corriendo, no de forma automatizada.
- **La detección de reimpresión tiene una condición de carrera.** Dos solicitudes
  simultáneas sobre el mismo LPN pueden ambas leer "sin impresión previa" y aprobarse.
  La solución correcta es un índice único parcial o bloqueo optimista sobre `PrintRequests`.
- **El dashboard administrativo agrega en cada llamada.** Con volumen real haría falta
  una vista materializada o un contador incremental.
- **El cifrado del payload no protege contra intercepción**, por lo ya explicado. Si el
  requisito real fuera ese, la respuesta es TLS mutuo o firma del lado del servidor, no
  una llave publicada en el bundle.
