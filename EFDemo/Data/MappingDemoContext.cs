using EFDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace EFDemo.Data;

public enum MappingStrategy { Tph, Tpt, Tpc }

public class MappingDemoContext : DbContext
{
    private readonly MappingStrategy _strategy;
    public DbSet<User> Users { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Manager> Managers { get; set; }

    public MappingDemoContext(DbContextOptions<MappingDemoContext> options, MappingStrategy strategy) : base(options)
    {
        _strategy = strategy;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        switch (_strategy)
        {
            case MappingStrategy.Tph:
                modelBuilder.Entity<User>()
                    .HasDiscriminator<string>("UserType")
                    .HasValue<Employee>("Employee")
                    .HasValue<Manager>("Manager");
                break;
            case MappingStrategy.Tpt:
                modelBuilder.Entity<User>().ToTable("Users");
                modelBuilder.Entity<Employee>().ToTable("Employees");
                modelBuilder.Entity<Manager>().ToTable("Managers");
                break;
            case MappingStrategy.Tpc:
                modelBuilder.Entity<Employee>().ToTable("Employees");
                modelBuilder.Entity<Manager>().ToTable("Managers");
                modelBuilder.Entity<User>().UseTpcMappingStrategy();
                break;
        }
    }
} 