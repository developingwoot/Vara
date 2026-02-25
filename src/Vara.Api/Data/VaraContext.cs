using Microsoft.EntityFrameworkCore;
using Vara.Api.Models.Entities;

namespace Vara.Api.Data;

public class VaraContext : DbContext
{
    public VaraContext(DbContextOptions<VaraContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.SubscriptionTier)
                  .HasColumnName("subscription_tier")
                  .HasDefaultValue("free");
            entity.Property(e => e.SubscriptionExpiresAt).HasColumnName("subscription_expires_at");
            entity.Property(e => e.Settings)
                  .HasColumnName("settings")
                  .HasColumnType("jsonb")
                  .HasDefaultValueSql("'{}'");

            // idx_users_email, idx_users_subscription_tier from spec
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("idx_users_email");
            entity.HasIndex(e => e.SubscriptionTier).HasDatabaseName("idx_users_subscription_tier");
        });
    }
}
