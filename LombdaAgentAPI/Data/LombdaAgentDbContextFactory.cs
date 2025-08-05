using LombdaAgentAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LombdaAgentAPI.Data
{
    /// <summary>
    /// Design-time factory for LombdaAgentDbContext to support Entity Framework migrations
    /// </summary>
    public class LombdaAgentDbContextFactory : IDesignTimeDbContextFactory<LombdaAgentDbContext>
    {
        public LombdaAgentDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LombdaAgentDbContext>();
            
            // Use SQLite with a default connection string for design-time
            optionsBuilder.UseSqlite("Data Source=lombdaagent.db");

            return new LombdaAgentDbContext(optionsBuilder.Options);
        }
    }
}