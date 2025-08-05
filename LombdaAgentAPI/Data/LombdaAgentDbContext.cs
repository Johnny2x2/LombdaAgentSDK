using LombdaAgentAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LombdaAgentAPI.Data
{
    /// <summary>
    /// Database context for the LombdaAgent API
    /// </summary>
    public class LombdaAgentDbContext : DbContext
    {
        public LombdaAgentDbContext(DbContextOptions<LombdaAgentDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Accounts table
        /// </summary>
        public DbSet<Account> Accounts { get; set; }

        /// <summary>
        /// API tokens table
        /// </summary>
        public DbSet<ApiToken> ApiTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Account entity
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
            });

            // Configure ApiToken entity
            modelBuilder.Entity<ApiToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => e.TokenPrefix);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");

                // Configure relationship
                entity.HasOne(e => e.Account)
                      .WithMany(e => e.ApiTokens)
                      .HasForeignKey(e => e.AccountId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        /// <summary>
        /// Initialize the database with default data if needed
        /// </summary>
        public async Task InitializeAsync()
        {
            // Ensure database is created
            await Database.EnsureCreatedAsync();
        }
    }
}