using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Configurations;

/// <summary>Configuracion de persistencia de la entidad PrintRequest.</summary>
public sealed class PrintRequestConfiguration : IEntityTypeConfiguration<PrintRequest>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PrintRequest> builder)
    {
        builder.ToTable("PrintRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CorrelationId).IsRequired();
        builder.Property(x => x.EtqId).HasMaxLength(50);
        builder.Property(x => x.LpnId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DocumentNumber).HasMaxLength(50);
        builder.Property(x => x.Result).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.EventType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.RejectionCode).HasMaxLength(50);
        builder.Property(x => x.RejectionMessage).HasMaxLength(500);
        builder.Property(x => x.ReprintReason).HasMaxLength(300);
        builder.Property(x => x.ApprovalNote).HasMaxLength(300);

        builder.HasOne(x => x.Zone)
               .WithMany()
               .HasForeignKey(x => x.IdZone)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.IdUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Approver)
               .WithMany()
               .HasForeignKey(x => x.IdApprover)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DownloadedBy)
               .WithMany()
               .HasForeignKey(x => x.IdDownloadedBy)
               .OnDelete(DeleteBehavior.Restrict);

        // Indice que sostiene las dos consultas calientes: deteccion de reimpresion
        // por LPN e historial ordenado por fecha.
        builder.HasIndex(x => new { x.LpnId, x.ProcessedAt });
        builder.HasIndex(x => x.CorrelationId);

        // La bandeja de pendientes se consulta cada vez que un supervisor abre la
        // pantalla, y son pocas filas dentro de un historial que crece sin techo:
        // sin este indice la consulta termina recorriendo toda la tabla.
        builder.HasIndex(x => new { x.Result, x.ProcessedAt });
    }
}

/// <summary>Configuracion de persistencia de la entidad PrintAuditLog.</summary>
public sealed class PrintAuditLogConfiguration : IEntityTypeConfiguration<PrintAuditLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PrintAuditLog> builder)
    {
        builder.ToTable("PrintAuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RuleCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(500);

        builder.HasOne(x => x.PrintRequest)
               .WithMany(x => x.AuditLogs)
               .HasForeignKey(x => x.IdPrintRequest)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
