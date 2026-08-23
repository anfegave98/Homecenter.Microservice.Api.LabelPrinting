using Microsoft.EntityFrameworkCore;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;

/// <summary>
/// Contexto de persistencia del submodulo de impresion de ETQ.
/// Las entidades y sus configuraciones se incorporan en HU-003; aqui queda el punto
/// de entrada y el descubrimiento automatico de IEntityTypeConfiguration del ensamblado.
/// </summary>
public class LabelPrintingDbContext : DbContext
{
    public LabelPrintingDbContext(DbContextOptions<LabelPrintingDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LabelPrintingDbContext).Assembly);
    }
}
