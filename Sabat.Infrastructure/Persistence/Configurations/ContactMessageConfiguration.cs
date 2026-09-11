using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabat.Domain;

namespace Sabat.Infrastructure.Persistence.Configurations;

public sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("contact_messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).HasColumnName("id");
        builder.Property(message => message.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(message => message.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(message => message.Message).HasColumnName("message").HasColumnType("text").IsRequired();
        builder.Property(message => message.Language).HasColumnName("language").HasMaxLength(2).IsRequired();
        builder.Property(message => message.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(message => message.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(message => message.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(message => new { message.Status, message.CreatedAt });
        builder.HasIndex(message => message.Email);
    }
}
