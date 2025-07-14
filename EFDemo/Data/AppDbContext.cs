using EFDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace EFDemo.Data;

public class AppDbContext : DbContext
{
    public DbSet<Person> People => Set<Person>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Address> Addresses { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TPH (default)
        modelBuilder.Entity<Person>()
            .HasDiscriminator<string>("PersonType")
            .HasValue<Student>("Student")
            .HasValue<Teacher>("Teacher");

        // Configure one-to-one relationship between Person and Address
        modelBuilder.Entity<Person>()
            .HasOne(p => p.Address)
            .WithOne(a => a.Person)
            .HasForeignKey<Address>(a => a.PersonId);

        // OutboxMessage configuration
        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.Property(x => x.Type).IsRequired();
            b.Property(x => x.Payload).IsRequired();
            b.HasIndex(x => new { x.IsProcessed, x.OccurredOn });
        });

        // Uncomment for TPT or TPC as needed
        // modelBuilder.Entity<Student>().ToTable("Students");
        // modelBuilder.Entity<Teacher>().ToTable("Teachers");
        // modelBuilder.Entity<Student>().UseTpcMappingStrategy();
        // modelBuilder.Entity<Teacher>().UseTpcMappingStrategy();
    }
} 