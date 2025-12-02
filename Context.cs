using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StretchScheduler.Models;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;


public class StretchSchedulerContext : DbContext
{
  static readonly string? connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
  public DbSet<Appointment> Appointments { get; set; }
  public DbSet<ApptType> ApptTypes { get; set; }
  public DbSet<Client> Clients { get; set; }
  public DbSet<Admin> Admins { get; set; }
  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    if (connectionString == null)
    {
      Console.WriteLine("Connection string not found or not set.");
      // Handle the case where the connection string is not available
      return;
    }
    
    var builder = new NpgsqlConnectionStringBuilder(connectionString);
    
    // Ensure SSL is required for Neon
    builder.SslMode = SslMode.Require;
   
    
    var finalConnectionString = builder.ConnectionString;
    
    optionsBuilder.UseNpgsql(finalConnectionString, npgsqlOptions =>
    {
      npgsqlOptions.CommandTimeout(30);
      // Enable retry for transient failures in production
      var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
      if (!isDevelopment)
      {
        npgsqlOptions.EnableRetryOnFailure(
          maxRetryCount: 3,
          maxRetryDelay: TimeSpan.FromSeconds(5),
          errorCodesToAdd: null);
      }
    });
  }
  
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    
    // Configure DateTime properties to use timestamp without time zone for PostgreSQL
    // This avoids timezone mismatch issues when comparing with DateTime.Now
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      var properties = entityType.GetProperties()
          .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));
      
      foreach (var property in properties)
      {
        property.SetColumnType("timestamp without time zone");
      }
    }
  }
}