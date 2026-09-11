using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabat.Domain;

namespace Sabat.Infrastructure.Persistence.Configurations;

public sealed class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");
        builder.HasKey(admin => admin.Id);
        builder.Property(admin => admin.Id).HasColumnName("id");
        builder.Property(admin => admin.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(admin => admin.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(254).IsRequired();
        builder.Property(admin => admin.DisplayName).HasColumnName("display_name").HasMaxLength(120).IsRequired();
        builder.Property(admin => admin.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
        builder.Property(admin => admin.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(admin => admin.SessionVersion).HasColumnName("session_version").IsRequired();
        builder.Property(admin => admin.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(admin => admin.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(admin => admin.LastLoginAt).HasColumnName("last_login_at");

        builder.HasIndex(admin => admin.NormalizedEmail).IsUnique();
    }
}
