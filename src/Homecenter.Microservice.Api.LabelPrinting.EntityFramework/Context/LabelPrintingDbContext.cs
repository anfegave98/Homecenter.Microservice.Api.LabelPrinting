using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;

/// <summary>
/// Contexto de persistencia del submodulo de impresion de ETQ.
/// Las configuraciones de entidad se descubren por ensamblado para evitar
/// un OnModelCreating monolitico.
/// </summary>
public class LabelPrintingDbContext : DbContext
{
    /// <summary>Crea una instancia con sus dependencias.</summary>
    public LabelPrintingDbContext(DbContextOptions<LabelPrintingDbContext> options)
        : base(options)
    {
    }

    /// <summary>Acceso a la tabla Users.</summary>
    public DbSet<User> Users => Set<User>();
    /// <summary>Acceso a la tabla Roles.</summary>
    public DbSet<Role> Roles => Set<Role>();
    /// <summary>Acceso a la tabla UserRoles.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    /// <summary>Acceso a la tabla Zones.</summary>
    public DbSet<Zone> Zones => Set<Zone>();
    /// <summary>Acceso a la tabla Documents.</summary>
    public DbSet<Document> Documents => Set<Document>();
    /// <summary>Acceso a la tabla Labels.</summary>
    public DbSet<Label> Labels => Set<Label>();
    /// <summary>Acceso a la tabla Products.</summary>
    public DbSet<Product> Products => Set<Product>();
    /// <summary>Acceso a la tabla DocumentProducts.</summary>
    public DbSet<DocumentProduct> DocumentProducts => Set<DocumentProduct>();
    /// <summary>Acceso a la tabla InventoryAvailability.</summary>
    public DbSet<InventoryAvailability> InventoryAvailability => Set<InventoryAvailability>();
    /// <summary>Acceso a la tabla PrintRequests.</summary>
    public DbSet<PrintRequest> PrintRequests => Set<PrintRequest>();
    /// <summary>Acceso a la tabla PrintAuditLogs.</summary>
    public DbSet<PrintAuditLog> PrintAuditLogs => Set<PrintAuditLog>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LabelPrintingDbContext).Assembly);

        ApplyUtcConversion(modelBuilder);
    }

    /// <summary>
    /// Fuerza a UTC toda fecha que entre a la base.
    ///
    /// PostgreSQL solo acepta offset 0 en 'timestamp with time zone', y los datos de
    /// origen vienen en hora de Colombia (-05:00). Se resuelve como convencion del
    /// modelo y no en cada punto de escritura: asi ningun camino futuro puede saltarse
    /// la regla. La conversion a hora local es responsabilidad de la presentacion.
    /// </summary>
    private static void ApplyUtcConversion(ModelBuilder modelBuilder)
    {
        var converter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
            value => value.ToUniversalTime(),
            value => value);

        var nullableConverter = new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
            value => value.HasValue ? value.Value.ToUniversalTime() : value,
            value => value);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(converter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(nullableConverter);
                }
            }
        }
    }
}
