using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.TestSupport;

/// <summary>
/// Arma escenarios de impresion completos con una linea de codigo por variacion.
///
/// Existe para que cada prueba declare unicamente lo que la distingue. Si un test
/// tiene que construir a mano documento, etiqueta, zona, productos e inventario,
/// la condicion que realmente esta probando se pierde entre el andamiaje.
/// </summary>
public sealed class PrintScenarioBuilder
{
    private const int ZoneId = 1;

    private readonly List<DocumentProduct> _products = new();
    private readonly Dictionary<int, InventoryAvailability> _availability = new();

    private string _lpn = "LPN-000987654";
    private string _etq = "ETQ-10001";
    private string _zoneCode = "ZONA-PICKING-A";
    private DocumentStatus _status = DocumentStatus.Liberada;
    private string _userName = "operario.tienda";
    private string[] _roles = { RoleName.Operario };
    private string? _reprintReason;
    private bool _hasPreviousPrint;
    private bool _labelExists = true;
    private bool _zoneExists = true;

    /// <summary>Escenario base: etiqueta valida, documento LIBERADA, un producto disponible.</summary>
    public static PrintScenarioBuilder Valid() => new PrintScenarioBuilder().WithProduct("PROD-001", requested: 2, available: 10);

    public PrintScenarioBuilder WithLpn(string lpn)
    {
        _lpn = lpn;
        return this;
    }

    public PrintScenarioBuilder WithDocumentStatus(DocumentStatus status)
    {
        _status = status;
        return this;
    }

    public PrintScenarioBuilder WithUser(string userName, params string[] roles)
    {
        _userName = userName;
        _roles = roles.Length > 0 ? roles : new[] { RoleName.Operario };
        return this;
    }

    public PrintScenarioBuilder WithReprintReason(string? reason)
    {
        _reprintReason = reason;
        return this;
    }

    public PrintScenarioBuilder AlreadyPrinted(bool value = true)
    {
        _hasPreviousPrint = value;
        return this;
    }

    /// <summary>Simula un LPN que no existe en los datos: ni etiqueta ni documento resueltos.</summary>
    public PrintScenarioBuilder WithoutLabel()
    {
        _labelExists = false;
        return this;
    }

    /// <summary>Simula una zona solicitada que no esta registrada o esta inactiva.</summary>
    public PrintScenarioBuilder WithoutZone()
    {
        _zoneExists = false;
        return this;
    }

    /// <summary>Agrega un producto con su disponibilidad en la zona evaluada.</summary>
    public PrintScenarioBuilder WithProduct(
        string productCode,
        decimal requested,
        decimal available,
        bool isStocked = true)
    {
        var idProduct = _products.Count + 1;

        _products.Add(new DocumentProduct
        {
            Id = idProduct,
            IdProduct = idProduct,
            RequestedQty = requested,
            Uom = "UND",
            Product = new Product
            {
                Id = idProduct,
                ProductCode = productCode,
                ProductDescription = $"Descripcion de {productCode}"
            }
        });

        _availability[idProduct] = new InventoryAvailability
        {
            IdProduct = idProduct,
            IdZone = ZoneId,
            AvailableQty = available,
            IsStocked = isStocked
        };

        return this;
    }

    /// <summary>Producto de la ETQ que no tiene fila de inventario en la zona.</summary>
    public PrintScenarioBuilder WithProductWithoutInventoryRecord(string productCode, decimal requested)
    {
        var idProduct = _products.Count + 1;

        _products.Add(new DocumentProduct
        {
            Id = idProduct,
            IdProduct = idProduct,
            RequestedQty = requested,
            Uom = "UND",
            Product = new Product
            {
                Id = idProduct,
                ProductCode = productCode,
                ProductDescription = $"Descripcion de {productCode}"
            }
        });

        return this;
    }

    /// <summary>Construye el contexto que reciben las reglas.</summary>
    public PrintRuleContext Build()
    {
        var zone = _zoneExists
            ? new Zone { Id = ZoneId, Code = _zoneCode, Name = "Zona de prueba" }
            : null;

        Document? document = null;
        Label? label = null;

        if (_labelExists)
        {
            document = new Document
            {
                Id = 1,
                RequestId = "REQ-20260605-001",
                DocumentType = "NOTA_PEDIDO",
                DocumentNumber = "NP-458721",
                Status = _status,
                IdZone = ZoneId,
                RequestedBy = "usuario.operacion",
                RequestDateTime = DateTimeOffset.UtcNow,
                Zone = zone!,
                DocumentProducts = _products
            };

            label = new Label
            {
                Id = 1,
                IdDocument = 1,
                EtqId = _etq,
                LpnId = _lpn,
                IsPreGenerated = true,
                TemplateCode = "TPL-ETQ-STD-4X6",
                Zpl = "^XA^FDprueba^FS^XZ",
                Document = document
            };
        }

        return new PrintRuleContext
        {
            RequestedKey = _lpn,
            RequestedZoneCode = _zoneCode,
            ReprintReason = _reprintReason,
            UserName = _userName,
            UserRoles = _roles,
            Label = label,
            Document = document,
            Zone = zone,
            Products = _products,
            Availability = _availability,
            HasPreviousPrint = _hasPreviousPrint
        };
    }
}
