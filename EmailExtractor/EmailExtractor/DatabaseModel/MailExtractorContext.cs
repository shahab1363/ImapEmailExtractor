using Microsoft.EntityFrameworkCore;

namespace EmailExtractor.DatabaseModel
{
    public class MailExtractorContext : DbContext
    {
        public DbSet<MailItem> MailItems { get; set; }

        public MailExtractorContext(DbContextOptions<MailExtractorContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<MailAddress>()
            //    .HasIndex(x => x.EmailAddress)
            //    .IsUnique();

            modelBuilder.Entity<MailItem>()
                .HasIndex(x => x.EmailAddress);
            modelBuilder.Entity<MailItem>()
                .HasIndex(x => new {x.EmailAddress, x.Folder, x.UniqueId, x.Server})
                .IsUnique();


            base.OnModelCreating(modelBuilder);
        }
    }
}
