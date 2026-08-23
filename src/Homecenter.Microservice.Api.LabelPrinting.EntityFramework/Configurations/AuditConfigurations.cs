using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Configurations;

public sealed class PrintRequestConfiguration : IEntityTypeConfiguration<PrintRequest>
{
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

        builder.HasOne(x => x.Zone)
               .WithMany()
               .HasForeignKey(x => x.IdZone)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.IdUser)
               .OnDelete(DeleteBehavior.Restrict);

        // Indice que sostiene las dos consultas calientes: deteccion de reimpresion
        // por LPN e historial ordenado por fecha.
        builder.HasIndex(x => new { x.LpnId, x.ProcessedAt });
        builder.HasIndex(x => x.CorrelationId);
    }
}

public sealed class PrintAuditLogConfiguration : IEntityTypeConfiguration<PrintAuditLog>
{
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
