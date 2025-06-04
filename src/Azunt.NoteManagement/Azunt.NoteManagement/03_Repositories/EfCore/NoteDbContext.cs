using Microsoft.EntityFrameworkCore;

namespace Azunt.NoteManagement
{
    public class NoteDbContext : DbContext
    {
        public NoteDbContext(DbContextOptions<NoteDbContext> options)
            : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Note>()
                .Property(m => m.Created)
                .HasDefaultValueSql("GetDate()");
        }

        public DbSet<Note> Notes { get; set; } = null!;
    }
}