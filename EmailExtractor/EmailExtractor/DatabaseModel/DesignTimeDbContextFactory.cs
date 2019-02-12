using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmailExtractor.DatabaseModel
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MailExtractorContext>
    {
        public MailExtractorContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<MailExtractorContext>();
            builder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=EmailExtractor;Trusted_Connection=True;");
            return new MailExtractorContext(builder.Options);
        }
    }
}
