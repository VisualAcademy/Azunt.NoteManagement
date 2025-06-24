using Azunt.NoteManagement.Models.Configurations;
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
            // 직접 속성 구성
            // modelBuilder.Entity<Note>().Property(m => m.Created).HasDefaultValueSql("GetDate()");

            // NoteConfiguration 클래스 적용
            modelBuilder.ApplyConfiguration(new NoteConfiguration());
        }

        public DbSet<Note> Notes { get; set; } = null!;
    }
}