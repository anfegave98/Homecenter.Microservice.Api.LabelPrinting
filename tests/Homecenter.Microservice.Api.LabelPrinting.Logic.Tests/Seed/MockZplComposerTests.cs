using System.Text.Json;
using FluentAssertions;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Seed;
using Xunit;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.Seed;

/// <summary>
/// Composición del ZPL de los datos semilla.
///
/// El ZPL es un formato posicional sin validación: un dato con un carácter de control o
/// un bloque mal cerrado no lanza excepción, simplemente sale una etiqueta corrida o en
/// blanco. Estas pruebas existen porque ese fallo es invisible hasta que alguien imprime.
/// </summary>
public sealed class MockZplComposerTests
{
    [Fact]
    public void Abre_y_cierra_el_formato()
    {
        // Arrange & Act
        var zpl = Compose();

        // Assert
        zpl.Should().StartWith("^XA");
        zpl.Should().EndWith("^XZ");
    }

    [Fact]
    public void Incluye_los_datos_de_la_operacion()
    {
        // Lo que distingue esta etiqueta del ejemplo genérico del anexo: aquí sí aparecen
        // la ETQ, el LPN, el documento, la zona y los productos.
        // Arrange & Act
        var zpl = Compose();

        // Assert
        zpl.Should().Contain("ETQ-10001");
        zpl.Should().Contain("LPN-000987654");
        zpl.Should().Contain("NP-458721");
        zpl.Should().Contain("ZONA-PICKING-A");
        zpl.Should().Contain("PROD-001");
        zpl.Should().Contain("Martillo 16oz");
    }

    [Fact]
    public void Codifica_el_lpn_como_codigo_de_barras()
    {
        // Es la llave con la que se vuelve a entrar al sistema: si no es legible con
        // pistola, la etiqueta obliga a teclear el LPN a mano.
        // Arrange & Act
        var zpl = Compose();

        // Assert
        zpl.Should().Contain("^BCN,140,Y,N,N^FDLPN-000987654^FS");
    }

    [Fact]
    public void Embebe_los_metadatos_como_comentario_legible()
    {
        // La vista previa se dibuja con esto. Va dentro del propio archivo para que imagen
        // y ZPL no puedan divergir: salen de la misma fuente.
        // Arrange
        var zpl = Compose();

        // Act
        var metadata = ExtractMetadata(zpl);

        // Assert
        metadata.GetProperty("etqId").GetString().Should().Be("ETQ-10001");
        metadata.GetProperty("lpnId").GetString().Should().Be("LPN-000987654");
        metadata.GetProperty("zoneCode").GetString().Should().Be("ZONA-PICKING-A");
        metadata.GetProperty("products").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public void Neutraliza_los_caracteres_de_control_de_zpl()
    {
        // Un '^' dentro de una descripción cortaría el comando en curso y el resto de la
        // etiqueta saldría corrido.
        // Arrange
        var document = BuildDocument();
        var products = BuildProducts(descripcion: "Tornillo ^ 3~8 pulgadas");

        // Act
        var zpl = MockZplComposer.Compose("ETQ-10001", "LPN-000987654", "TPL-ETQ-STD-4X6", document, products);

        // Assert: se cuentan los comandos de apertura de campo, que deben seguir siendo
        // los mismos que sin el dato problemático.
        var limpio = MockZplComposer.Compose(
            "ETQ-10001", "LPN-000987654", "TPL-ETQ-STD-4X6", document, BuildProducts());

        CountFieldStarts(zpl).Should().Be(CountFieldStarts(limpio));
    }

    [Fact]
    public void Resuelve_una_etiqueta_sin_productos_sin_romper_el_formato()
    {
        // Arrange
        var document = BuildDocument();
        document.DocumentProducts = new List<DocumentProduct>();

        // Act
        var zpl = MockZplComposer.Compose(
            "ETQ-10001", "LPN-000987654", "TPL-ETQ-STD-4X6", document, Array.Empty<DocumentProduct>());

        // Assert
        zpl.Should().StartWith("^XA").And.EndWith("^XZ");
        zpl.Should().Contain("PRODUCTOS (0)");
    }

    // -----------------------------------------------------------------------
    // Montaje
    // -----------------------------------------------------------------------

    private static string Compose()
    {
        var document = BuildDocument();
        return MockZplComposer.Compose(
            "ETQ-10001", "LPN-000987654", "TPL-ETQ-STD-4X6", document, BuildProducts());
    }

    private static JsonElement ExtractMetadata(string zpl)
    {
        var start = zpl.IndexOf(MockZplComposer.MetadataPrefix, StringComparison.Ordinal)
                  + MockZplComposer.MetadataPrefix.Length;

        var end = zpl.IndexOf("^FS", start, StringComparison.Ordinal);

        return JsonDocument.Parse(zpl[start..end]).RootElement;
    }

    private static int CountFieldStarts(string zpl) =>
        zpl.Split("^FD").Length;

    private static Document BuildDocument() => new()
    {
        Id = 1,
        RequestId = "REQ-20260605-001",
        DocumentType = "NOTA_PEDIDO",
        DocumentNumber = "NP-458721",
        Status = DocumentStatus.Liberada,
        RequestedBy = "usuario.operacion",
        RequestDateTime = DateTimeOffset.UtcNow,
        Zone = new Zone { Id = 1, Code = "ZONA-PICKING-A", Name = "Zona Picking A" }
    };

    private static DocumentProduct[] BuildProducts(string descripcion = "Martillo 16oz") => new[]
    {
        new DocumentProduct
        {
            Id = 1,
            IdProduct = 1,
            RequestedQty = 2m,
            Uom = "UND",
            Product = new Product { Id = 1, ProductCode = "PROD-001", ProductDescription = descripcion }
        },
        new DocumentProduct
        {
            Id = 2,
            IdProduct = 2,
            RequestedQty = 1m,
            Uom = "PAR",
            Product = new Product { Id = 2, ProductCode = "PROD-002", ProductDescription = "Guantes de seguridad" }
        }
    };
}
