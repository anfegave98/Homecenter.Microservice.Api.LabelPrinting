using System.Globalization;
using System.Text;
using System.Text.Json;
using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Seed;

/// <summary>
/// Compone el ZPL de una ETQ para los datos semilla.
///
/// Esto NO es la generación de etiquetas del enunciado, que está explícitamente fuera de
/// alcance: la ETQ llega pre-generada desde el proceso de olas. Lo que se simula aquí es
/// ese proceso ajeno, para que los datos mock sean representativos.
///
/// La razón es concreta. El anexo trae como ZPL el ejemplo genérico de Zebra —una guía de
/// envío para "John Doe" en "Intershipping, Inc."— que no menciona la ETQ, el LPN ni los
/// productos. Servía mientras el ZPL solo se devolvía como texto opaco, pero al renderizarlo
/// como imagen el evaluador vería una etiqueta sin relación con la operación y parecería un
/// defecto. El ZPL original del anexo se conserva íntegro en
/// <c>mocks/_anexo_tableOrders.original.json</c>.
/// </summary>
public static class MockZplComposer
{
    /// <summary>Marca del bloque de metadatos embebido como comentario ZPL.</summary>
    public const string MetadataPrefix = "HC-META:";

    private static readonly JsonSerializerOptions MetadataOptions = new()
    {
        WriteIndented = false
    };

    /// <summary>
    /// Compone el ZPL de la etiqueta a partir del documento y sus productos.
    /// </summary>
    /// <param name="etqId">Identificador de la etiqueta.</param>
    /// <param name="lpnId">Unidad logistica que la etiqueta ampara.</param>
    /// <param name="templateCode">Plantilla declarada por el proceso que la pre-genero.</param>
    /// <param name="document">Documento origen, con su zona ya cargada.</param>
    /// <param name="products">Productos del documento con su cantidad solicitada.</param>
    /// <returns>ZPL valido, listo para enviar a una impresora Zebra de 4x6.</returns>
    public static string Compose(
        string etqId,
        string lpnId,
        string templateCode,
        Document document,
        IReadOnlyCollection<DocumentProduct> products)
    {
        var zoneCode = document.Zone?.Code ?? "SIN-ZONA";
        var builder = new StringBuilder();

        builder.Append("^XA");

        // Los metadatos van como comentario ^FX: es un campo legal de ZPL que la impresora
        // ignora, de modo que el archivo sigue siendo imprimible tal cual. El cliente los
        // lee para dibujar la vista previa, y asi la imagen y el ZPL no pueden divergir:
        // salen del mismo archivo.
        builder.Append("^FX").Append(MetadataPrefix).Append(BuildMetadata(
            etqId, lpnId, templateCode, zoneCode, document, products)).Append("^FS");

        builder.Append("^CI28");
        builder.Append("^PW812^LL1218");

        // Encabezado
        builder.Append("^CF0,70^FO40,45^FDHOMECENTER^FS");
        builder.Append("^CF0,30^FO40,125^FDEtiqueta de unidad logistica^FS");
        builder.Append("^FO40,175^GB732,4,4^FS");

        // Identificacion de la ETQ
        builder.Append("^CF0,28^FO40,205^FDETQ^FS");
        builder.Append("^CF0,52^FO40,240^FD").Append(Sanitize(etqId)).Append("^FS");

        builder.Append("^CF0,28^FO420,205^FDZONA^FS");
        builder.Append("^CF0,52^FO420,240^FD").Append(Sanitize(zoneCode)).Append("^FS");

        // Documento origen
        builder.Append("^CF0,28^FO40,320^FDDOCUMENTO^FS");
        builder.Append("^CF0,40^FO40,355^FD")
               .Append(Sanitize(document.DocumentNumber))
               .Append(" - ")
               .Append(Sanitize(document.DocumentType))
               .Append("^FS");

        builder.Append("^FO40,415^GB732,3,3^FS");

        // Codigo de barras del LPN: es la llave con la que se vuelve a entrar al sistema,
        // asi que es lo unico que debe poder leerse con pistola.
        builder.Append("^BY3,2,140^FO40,450^BCN,140,Y,N,N^FD").Append(Sanitize(lpnId)).Append("^FS");

        // Productos
        var top = 650;
        builder.Append("^CF0,30^FO40,").Append(top).Append("^FDPRODUCTOS (")
               .Append(products.Count.ToString(CultureInfo.InvariantCulture)).Append(")^FS");
        builder.Append("^FO40,").Append(top + 40).Append("^GB732,3,3^FS");

        var line = top + 60;
        foreach (var item in products)
        {
            builder.Append("^CF0,28^FO40,").Append(line).Append("^FD")
                   .Append(Sanitize(item.Product?.ProductCode ?? "N/D"))
                   .Append("  ")
                   .Append(Sanitize(Shorten(item.Product?.ProductDescription ?? string.Empty, 28)))
                   .Append("^FS");

            builder.Append("^CF0,28^FO640,").Append(line).Append("^FD")
                   .Append(item.RequestedQty.ToString("0.##", CultureInfo.InvariantCulture))
                   .Append(' ')
                   .Append(Sanitize(item.Uom))
                   .Append("^FS");

            line += 40;
        }

        // Pie
        builder.Append("^FO40,1090^GB732,3,3^FS");
        builder.Append("^CF0,26^FO40,1110^FDLPN ").Append(Sanitize(lpnId)).Append("^FS");
        builder.Append("^CF0,26^FO40,1150^FDPlantilla ").Append(Sanitize(templateCode)).Append("^FS");
        builder.Append("^CF0,26^FO520,1110^FDSolicitado por^FS");
        builder.Append("^CF0,26^FO520,1150^FD").Append(Sanitize(document.RequestedBy)).Append("^FS");

        builder.Append("^XZ");

        return builder.ToString();
    }

    private static string BuildMetadata(
        string etqId,
        string lpnId,
        string templateCode,
        string zoneCode,
        Document document,
        IReadOnlyCollection<DocumentProduct> products)
    {
        var metadata = new
        {
            etqId,
            lpnId,
            templateCode,
            zoneCode,
            documentNumber = document.DocumentNumber,
            documentType = document.DocumentType,
            requestId = document.RequestId,
            requestedBy = document.RequestedBy,
            products = products.Select(x => new
            {
                productCode = x.Product?.ProductCode ?? "N/D",
                productDescription = x.Product?.ProductDescription ?? string.Empty,
                requestedQty = x.RequestedQty,
                uom = x.Uom
            })
        };

        return JsonSerializer.Serialize(metadata, MetadataOptions);
    }

    /// <summary>
    /// Neutraliza los caracteres de control de ZPL dentro de un dato.
    ///
    /// Un texto que contenga '^' o '~' cortaria el comando en curso y el resto de la
    /// etiqueta saldria corrido o vacio.
    /// </summary>
    private static string Sanitize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "N/D"
            : value.Replace('^', ' ').Replace('~', ' ').Trim();

    private static string Shorten(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + ".";
}
