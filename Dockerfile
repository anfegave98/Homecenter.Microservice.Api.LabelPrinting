# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Etapa de compilacion
#
# Los .csproj se copian y restauran ANTES que el codigo fuente: asi la capa de
# restauracion se reutiliza mientras no cambien las dependencias, y editar una clase
# no obliga a volver a bajar todos los paquetes NuGet.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY Directory.Build.props ./
COPY src/Homecenter.Microservice.Api.LabelPrinting/*.csproj                       src/Homecenter.Microservice.Api.LabelPrinting/
COPY src/Homecenter.Microservice.Api.LabelPrinting.Logic/*.csproj                 src/Homecenter.Microservice.Api.LabelPrinting.Logic/
COPY src/Homecenter.Microservice.Api.LabelPrinting.Abstractions/*.csproj          src/Homecenter.Microservice.Api.LabelPrinting.Abstractions/
COPY src/Homecenter.Microservice.Api.LabelPrinting.EntityFramework/*.csproj       src/Homecenter.Microservice.Api.LabelPrinting.EntityFramework/
COPY src/Homecenter.Microservice.Api.LabelPrinting.Entities/*.csproj              src/Homecenter.Microservice.Api.LabelPrinting.Entities/
COPY src/Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object/*.csproj  src/Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object/

RUN dotnet restore src/Homecenter.Microservice.Api.LabelPrinting/Homecenter.Microservice.Api.LabelPrinting.csproj

COPY src/ src/

RUN dotnet publish src/Homecenter.Microservice.Api.LabelPrinting/Homecenter.Microservice.Api.LabelPrinting.csproj \
    --configuration Release \
    --no-restore \
    --output /app

# ---------------------------------------------------------------------------
# Etapa de ejecucion
#
# Imagen aspnet y no sdk: el runtime pesa una fraccion y no lleva compilador ni
# herramientas de desarrollo dentro del contenedor publicado.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app ./

# Los datos semilla viajan en la imagen: el seeder los busca relativos al content root.
COPY mocks/ ./mocks/

# El puerto se fija por variable y no en codigo. Render inyecta PORT; este valor debe
# coincidir con el declarado en render.yaml.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

# Usuario sin privilegios: si el proceso se ve comprometido, no corre como root dentro
# del contenedor. La carpeta de salida ZPL se crea antes de bajar de privilegios porque
# despues el usuario ya no puede escribir en /app.
RUN mkdir -p /app/output/zpl \
    && adduser --system --group --no-create-home appuser \
    && chown -R appuser:appuser /app/output
USER appuser

ENTRYPOINT ["dotnet", "Homecenter.Microservice.Api.LabelPrinting.dll"]
