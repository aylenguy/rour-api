using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using UrbanStore.API.Models;


namespace UrbanStore.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Guardar listas como JSON en PostgreSQL
        modelBuilder.Entity<Product>()
            .Property(p => p.Images)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Product>()
            .Property(p => p.Sizes)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Product>()
            .Property(p => p.Colors)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Product>()
            .Property(p => p.Details)
            .HasColumnType("jsonb");
    }
}