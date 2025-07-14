using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Dapper;
using EFDemo.Data;
using EFDemo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EFDemo.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [InProcess]
    public class QueryBenchmarks
    {
        private AppDbContext _context;
        private string _connectionString;

        [GlobalSetup]
        public void Setup()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            _connectionString = configuration.GetConnectionString("DefaultConnection");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_connectionString)
                .Options;
            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            // Ensure at least 100 people for the quick test
            const int requiredCount = 100;
            var currentCount = _context.People.Count();
            if (currentCount < requiredCount)
            {
                _context.People.RemoveRange(_context.People.ToList());
                _context.SaveChanges();
                var people = new List<Person>(requiredCount);
                for (int i = 0; i < requiredCount; i++)
                {
                    if (i % 2 == 0)
                    {
                        people.Add(new Student
                        {
                            Name = $"Student {i}",
                            School = $"School {i % 10}",
                            Address = new Address { Street = $"{i} Main St", City = $"City {i % 5}" }
                        });
                    }
                    else
                    {
                        people.Add(new Teacher
                        {
                            Name = $"Teacher {i}",
                            Subject = $"Subject {i % 4}",
                            Address = new Address { Street = $"{i} Oak Ave", City = $"City {i % 5}" }
                        });
                    }
                }
                _context.People.AddRange(people);
                _context.SaveChanges();
            }
        }

        //[Benchmark(Description = "EF Core - Full Entities (Tracking)")]
        //public async Task<int> EfCore_Tracking()
        //{
        //    var people = await _context.People.ToListAsync();
        //    return people.Count;
        //}

        //[Benchmark(Description = "EF Core - Full Entities (No-Tracking)")]
        //public async Task<int> EfCore_AsNoTracking()
        //{
        //    var people = await _context.People.AsNoTracking().ToListAsync();
        //    return people.Count;
        //}

        //[Benchmark(Description = "EF Core - Projection")]
        //public async Task<int> EfCore_Projection()
        //{
        //    var people = await _context.People
        //        .Select(p => new { p.Id, p.Name })
        //        .ToListAsync();
        //    return people.Count;
        //}

        [Benchmark(Description = "Dapper - Full Entities")]
        public async Task<int> Dapper_Query()
        {
            try
            {
                using IDbConnection db = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                db.Open();
                var people = await db.QueryAsync<Person>("SELECT * FROM People");
                return people.AsList().Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dapper benchmark failed: {ex.Message}\nConnection string: {_connectionString}");
                throw;
            }
        }

        [Benchmark(Description = "Dapper - Count Only")]
        public int Dapper_CountOnly()
        {
            using IDbConnection db = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
            db.Open();
            return db.ExecuteScalar<int>("SELECT COUNT(*) FROM People");
        }
    }
}