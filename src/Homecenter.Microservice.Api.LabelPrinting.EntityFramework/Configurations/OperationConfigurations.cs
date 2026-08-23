using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Configurations;

public sealed class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        builder.ToTable("Zones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RequestId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DocumentType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DocumentNumber).HasMaxLength(50).IsRequired();

        // El estado se persiste como texto: una migracion de datos no debe depender
        // del orden de los miembros del enum, y el valor queda legible en la base.
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(x => x.Zone)
               .WithMany()
               .HasForeignKey(x => x.IdZone)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.DocumentNumber).IsUnique();
    }
}

public sealed class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.ToTable("Labels");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EtqId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.LpnId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TemplateCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Zpl).IsRequired();

        builder.HasOne(x => x.Document)
               .WithMany(x => x.Labels)
               .HasForeignKey(x => x.IdDocument)
               .OnDelete(DeleteBehavior.Cascade);

        // ETQ y LPN son llaves funcionales de entrada: duplicarlas haria ambigua
        // la resolucion de la etiqueta.
        builder.HasIndex(x => x.EtqId).IsUnique();
        builder.HasIndex(x => x.LpnId).IsUnique();
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProductDescription).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.ProductCode).IsUnique();
    }
}

public sealed class DocumentProductConfiguration : IEntityTypeConfiguration<DocumentProduct>
{
    public void Configure(EntityTypeBuilder<DocumentProduct> builder)
    {
        builder.ToTable("DocumentProducts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RequestedQty).HasPrecision(18, 2);
        builder.Property(x => x.Uom).HasMaxLength(10).IsRequired();

        builder.HasOne(x => x.Document)
               .WithMany(x => x.DocumentProducts)
               .HasForeignKey(x => x.IdDocument)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
               .WithMany()
               .HasForeignKey(x => x.IdProduct)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.IdDocument, x.IdProduct }).IsUnique();
    }
}

public sealed class InventoryAvailabilityConfiguration : IEntityTypeConfiguration<InventoryAvailability>
{
    public void Configure(EntityTypeBuilder<InventoryAvailability> builder)
    {
        builder.ToTable("InventoryAvailability");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AvailableQty).HasPrecision(18, 2);

        builder.HasOne(x => x.Product)
               .WithMany()
               .HasForeignKey(x => x.IdProduct)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Zone)
               .WithMany()
               .HasForeignKey(x => x.IdZone)
               .OnDelete(DeleteBehavior.Cascade);

        // Un producto tiene una sola fila de disponibilidad por zona.
        builder.HasIndex(x => new { x.IdProduct, x.IdZone }).IsUnique();
    }
}
