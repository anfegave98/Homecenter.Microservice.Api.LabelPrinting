# Gestión de secretos

Este documento explica dónde vive cada valor sensible, con qué formato y cómo se
configura en el ambiente publicado.

## Principio

Ningún secreto de producción está en el repositorio. Los archivos versionados
contienen **marcadores de posición** o **valores locales desechables**, y el servicio
**se niega a arrancar** si detecta que un marcador llegó a ejecución
(`SecretsValidator`, invocado en `Program.cs` antes de construir la aplicación).

Esa validación es el control real. Olvidar una clave no es el riesgo grave: el riesgo
grave es desplegar con `CHANGE_ME_FROM_ENVIRONMENT` puesto, firmar tokens con un valor
que está publicado en GitHub y que nada en el log lo delate.

## Inventario de valores sensibles

| Valor | Sección | Formato exigido | Origen en producción |
|---|---|---|---|
| Cadena de conexión | `ConnectionStrings:DefaultConnection` | Cadena Npgsql | `ConnectionStrings__DefaultConnection` |
| Clave de firma JWT | `Jwt:SecretKey` | Texto, **≥ 32 bytes UTF-8** | `Jwt__SecretKey` |
| Llave AES | `Encryption:Key` | **Base64 de 32 bytes** (AES-256) | `Encryption__Key` |


> **`Encryption__IV` ya no se define.** `AesEncryptionService` genera un vector de
> inicializacion aleatorio por operacion y lo transmite como prefijo del mensaje: un IV
> fijo en CBC hace que dos textos identicos produzcan el mismo criptograma, revelando
> cuando dos valores son iguales sin descifrarlos. El ajuste se conserva por
> compatibilidad y solo se valida su formato si alguien lo define.

El doble guion bajo (`__`) es la convención de .NET para expresar jerarquía de
configuración en variables de entorno: `Jwt__SecretKey` sobrescribe `Jwt:SecretKey`.

### Por qué la clave JWT se mide en bytes UTF-8

`JwtTokenGenerator` construye la `SymmetricSecurityKey` con
`Encoding.UTF8.GetBytes(SecretKey)`. La validación mide exactamente lo mismo, para que
no exista la posibilidad de que pase el arranque y falle al firmar.

### Por qué la llave AES solo se valida con el cifrado encendido

No se valida. Si `Encryption:Enabled` es `false`, sus llaves no participan en ninguna
operación y exigirlas bloquearía el arranque por una funcionalidad que no se usa. La
validación se activa junto con el interruptor.

## Dónde vive cada valor

### Desarrollo local

`appsettings.Development.json` está versionado **a propósito** y trae valores locales
desechables para que `git clone` + `dotnet run` funcione sin pasos previos. Es una
decisión consciente para facilitar la evaluación, no un descuido:

- La contraseña de Postgres es la de una instancia local (`postgres/postgres`).
- La clave JWT de desarrollo es aleatoria pero **pública**, y solo firma tokens contra
  una base de datos local con datos mock.
- Ninguno de esos valores se usa en el ambiente publicado.

Si prefieres no versionar ni siquiera esos valores, `appsettings.Local.json` está en
`.gitignore` y tiene precedencia sobre el archivo de Development.

### Producción (Render)

Se definen como variables de entorno del Web Service. `render.yaml` las declara con
`sync: false`, que en Render significa "el valor se captura en el panel y no se
versiona".

```bash
# Genera un juego nuevo de claves (no reutilices las de desarrollo)
node -e "const c=require('crypto');console.log('Jwt__SecretKey     =',c.randomBytes(64).toString('base64'));console.log('Encryption__Key    =',c.randomBytes(32).toString('base64'));"
```

La cadena de conexión no se escribe a mano: Render la inyecta desde la base de datos
gestionada mediante `fromDatabase` en `render.yaml`.

## Rotación

1. Genera el valor nuevo con el comando anterior.
2. Actualízalo en el panel de Render y redespliega.
3. Los tokens emitidos con la clave anterior dejan de validar de inmediato: los
   usuarios activos deben iniciar sesión otra vez. Con `ExpirationMinutes: 60` el
   impacto máximo es de una hora de sesiones cortadas.

Si un secreto queda expuesto, rotarlo es obligatorio y no opcional: el commit que lo
publicó permanece en el historial de Git aunque el archivo se corrija después.

## Qué nunca se registra en logs

Tokens, contraseñas, llaves, vectores de inicialización y payloads cifrados. Los logs
de auditoría identifican al usuario por `userName` e `IdUser`, y cada solicitud por su
`correlationId`.
