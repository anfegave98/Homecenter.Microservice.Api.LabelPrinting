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
    public LabelPrintingDbContext(DbContextOptions<LabelPrintingDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<DocumentProduct> DocumentProducts => Set<DocumentProduct>();
    public DbSet<InventoryAvailability> InventoryAvailability => Set<InventoryAvailability>();
    public DbSet<PrintRequest> PrintRequests => Set<PrintRequest>();
    public DbSet<PrintAuditLog> PrintAuditLogs => Set<PrintAuditLog>();

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
