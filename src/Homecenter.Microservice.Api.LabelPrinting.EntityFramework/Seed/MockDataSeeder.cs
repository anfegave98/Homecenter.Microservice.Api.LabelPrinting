using System.Text.Json;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Configuration;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Seed;

/// <summary>
/// Carga los datos mock a PostgreSQL.
///
/// Es idempotente por llave natural: cada elemento se inserta solo si no existe.
/// Esto importa porque en Render el contenedor puede reiniciarse en cualquier momento
/// y un seeder que duplique datos corromperia las validaciones de inventario.
/// </summary>
public sealed class MockDataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly LabelPrintingDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SeedOptions _options;
    private readonly ILogger<MockDataSeeder> _logger;
    private readonly string _contentRootPath;

    /// <summary>Crea una instancia con sus dependencias.</summary>
    public MockDataSeeder(
        LabelPrintingDbContext context,
        IPasswordHasher passwordHasher,
        IOptions<SeedOptions> options,
        ILogger<MockDataSeeder> logger,
        string contentRootPath)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
        _contentRootPath = contentRootPath;
    }

    /// <summary>Carga los datos mock si el seed esta habilitado por configuracion.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Seed deshabilitado por configuracion.");
            return;
        }

        var mocksPath = ResolveMocksPath();
        if (mocksPath is null)
        {
            _logger.LogWarning("No se encontro la carpeta de mocks '{Path}'. Se omite la carga de datos semilla.", _options.MocksPath);
            return;
        }

        _logger.LogInformation("Cargando datos semilla desde {Path}", mocksPath);

        await SeedRolesAndUsersAsync(cancellationToken);
        await SeedZonesAsync(mocksPath, cancellationToken);
        await SeedProductsAsync(mocksPath, cancellationToken);
        await SeedDocumentsAsync(mocksPath, cancellationToken);
        await SeedInventoryAsync(mocksPath, cancellationToken);

        _logger.LogInformation("Datos semilla cargados.");
    }

    /// <summary>
    /// Busca la carpeta de mocks desde el content root y, si no la encuentra, sube por
    /// los directorios padre. El API se ejecuta desde src/... en local y desde la raiz
    /// en el contenedor: una sola ruta relativa no sirve para ambos casos.
    /// </summary>
    private string? ResolveMocksPath()
    {
        var direct = Path.GetFullPath(Path.Combine(_contentRootPath, _options.MocksPath));
        if (Directory.Exists(direct))
        {
            return direct;
        }

        var current = new DirectoryInfo(_contentRootPath);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "mocks");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private async Task<T[]> ReadAsync<T>(string mocksPath, string fileName, CancellationToken cancellationToken)
    {
        var file = Path.Combine(mocksPath, fileName);
        if (!File.Exists(file))
        {
            _logger.LogWarning("Archivo mock no encontrado: {File}", file);
            return Array.Empty<T>();
        }

        await using var stream = File.OpenRead(file);
        var payload = await JsonSerializer.DeserializeAsync<T[]>(stream, JsonOptions, cancellationToken);
        return payload ?? Array.Empty<T>();
    }

    private async Task SeedRolesAndUsersAsync(CancellationToken cancellationToken)
    {
        var roleDefinitions = new[]
        {
            (Name: RoleName.Operario, Description: "Consulta ETQ/LPN y solicita impresiones. Consulta su propio historial."),
            (Name: RoleName.Supervisor, Description: "Autoriza reimpresiones con motivo y consulta el historial completo."),
            (Name: RoleName.Admin, Description: "Administra la operacion y consulta indicadores.")
        };

        foreach (var definition in roleDefinitions)
        {
            if (await _context.Roles.AnyAsync(x => x.Name == definition.Name, cancellationToken))
            {
                continue;
            }

            _context.Roles.Add(new Role { Name = definition.Name, Description = definition.Description });
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Usuarios de prueba documentados en el README para el evaluador.
        var userDefinitions = new[]
        {
            (UserName: "operario.tienda", FullName: "Operario Tienda 01", Password: "Operario123*", Role: RoleName.Operario),
            (UserName: "supervisor.tienda", FullName: "Supervisor Tienda 01", Password: "Supervisor123*", Role: RoleName.Supervisor),
            (UserName: "admin.tienda", FullName: "Administrador Tienda", Password: "Admin123*", Role: RoleName.Admin)
        };

        foreach (var definition in userDefinitions)
        {
            if (await _context.Users.AnyAsync(x => x.UserName == definition.UserName, cancellationToken))
            {
                continue;
            }

            var (hash, salt) = _passwordHasher.Hash(definition.Password);
            var user = new User
            {
                UserName = definition.UserName,
                FullName = definition.FullName,
                PasswordHash = hash,
                PasswordSalt = salt,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            var role = await _context.Roles.FirstAsync(x => x.Name == definition.Role, cancellationToken);
            _context.UserRoles.Add(new UserRole { IdUser = user.Id, IdRole = role.Id });
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedZonesAsync(string mocksPath, CancellationToken cancellationToken)
    {
        var zones = await ReadAsync<ZoneSeed>(mocksPath, "zones.json", cancellationToken);

        foreach (var zone in zones)
        {
            if (await _context.Zones.AnyAsync(x => x.Code == zone.Code, cancellationToken))
            {
                continue;
            }

            _context.Zones.Add(new Zone { Code = zone.Code, Name = zone.Name });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedProductsAsync(string mocksPath, CancellationToken cancellationToken)
    {
        var products = await ReadAsync<ProductSeed>(mocksPath, "products.json", cancellationToken);

        foreach (var product in products)
        {
            if (await _context.Products.AnyAsync(x => x.ProductCode == product.ProductCode, cancellationToken))
            {
                continue;
            }

            _context.Products.Add(new Product
            {
                ProductCode = product.ProductCode,
                ProductDescription = product.ProductDescription
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDocumentsAsync(string mocksPath, CancellationToken cancellationToken)
    {
        var orders = await ReadAsync<OrderSeed>(mocksPath, "orders.json", cancellationToken);

        foreach (var order in orders)
        {
            if (await _context.Documents.AnyAsync(x => x.DocumentNumber == order.Document.DocumentNumber, cancellationToken))
            {
                continue;
            }

            var zone = await _context.Zones.FirstOrDefaultAsync(x => x.Code == order.Zone, cancellationToken);
            if (zone is null)
            {
                _logger.LogWarning("Zona '{Zone}' no encontrada para el documento {Document}.", order.Zone, order.Document.DocumentNumber);
                continue;
            }

            if (!Enum.TryParse<DocumentStatus>(order.Document.Status, ignoreCase: true, out var status))
            {
                _logger.LogWarning("Estado '{Status}' no reconocido para el documento {Document}.", order.Document.Status, order.Document.DocumentNumber);
                continue;
            }

            var document = new Document
            {
                RequestId = order.RequestId,
                DocumentType = order.Document.DocumentType,
                DocumentNumber = order.Document.DocumentNumber,
                Status = status,
                IdZone = zone.Id,
                RequestedBy = order.RequestedBy,
                RequestDateTime = order.RequestDateTime
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var label in order.Labels)
            {
                _context.Labels.Add(new Label
                {
                    IdDocument = document.Id,
                    EtqId = label.EtqId,
                    LpnId = label.LpnId,
                    IsPreGenerated = label.IsPreGenerated,
                    TemplateCode = label.TemplateCode,
                    Zpl = label.Zpl
                });
            }

            foreach (var item in order.Products)
            {
                var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductCode == item.ProductCode, cancellationToken);
                if (product is null)
                {
                    _logger.LogWarning("Producto '{Product}' no encontrado para el documento {Document}.", item.ProductCode, document.DocumentNumber);
                    continue;
                }

                _context.DocumentProducts.Add(new DocumentProduct
                {
                    IdDocument = document.Id,
                    IdProduct = product.Id,
                    RequestedQty = item.RequestedQty,
                    Uom = item.Uom
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedInventoryAsync(string mocksPath, CancellationToken cancellationToken)
    {
        var rows = await ReadAsync<InventorySeed>(mocksPath, "inventoryAvailability.json", cancellationToken);

        foreach (var row in rows)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductCode == row.ProductCode, cancellationToken);
            var zone = await _context.Zones.FirstOrDefaultAsync(x => x.Code == row.ZoneCode, cancellationToken);

            if (product is null || zone is null)
            {
                _logger.LogWarning("Inventario omitido: producto '{Product}' o zona '{Zone}' inexistente.", row.ProductCode, row.ZoneCode);
                continue;
            }

            if (await _context.InventoryAvailability.AnyAsync(x => x.IdProduct == product.Id && x.IdZone == zone.Id, cancellationToken))
            {
                continue;
            }

            _context.InventoryAvailability.Add(new InventoryAvailability
            {
                IdProduct = product.Id,
                IdZone = zone.Id,
                AvailableQty = row.AvailableQty,
                IsStocked = row.IsStocked
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    // Formas de lectura de los archivos mock. Son privadas al seeder a proposito:
    // el formato del anexo no debe filtrarse al dominio.
    private sealed record ZoneSeed(string Code, string Name);

    private sealed record ProductSeed(string ProductCode, string ProductDescription);

    private sealed record InventorySeed(string ProductCode, string ZoneCode, decimal AvailableQty, bool IsStocked);

    private sealed record DocumentSeed(string DocumentType, string DocumentNumber, string Status);

    private sealed record LabelSeed(string EtqId, string LpnId, bool IsPreGenerated, string TemplateCode, string Zpl);

    private sealed record ProductLineSeed(string ProductCode, string ProductDescription, decimal RequestedQty, string Uom);

    private sealed record OrderSeed(
        string RequestId,
        DateTimeOffset RequestDateTime,
        string RequestedBy,
        string Zone,
        DocumentSeed Document,
        LabelSeed[] Labels,
        ProductLineSeed[] Products);
}
