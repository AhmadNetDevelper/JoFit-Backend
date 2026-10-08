using JoFit.Domain.Customers;
using JoFit.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Fulfilment).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ShipRecipient).HasMaxLength(150);
        builder.Property(x => x.ShipPhone).HasMaxLength(30);
        builder.Property(x => x.ShipLine1).HasMaxLength(250);
        builder.Property(x => x.ShipLine2).HasMaxLength(250);
        builder.Property(x => x.ShipCity).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Ignore(x => x.Reference);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PlacedAtUtc);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Orders");
    }
}

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Kind).IsRequired().HasMaxLength(10);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(150);
        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Sku).HasMaxLength(64);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.VariantId);
        builder.HasIndex(x => x.RentalBookingId);
        builder.ToTable("OrderLines");
    }
}

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Recipient).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Line1).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Line2).HasMaxLength(250);
        builder.Property(x => x.City).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => x.CustomerId);
        builder.ToTable("Addresses");
    }
}
