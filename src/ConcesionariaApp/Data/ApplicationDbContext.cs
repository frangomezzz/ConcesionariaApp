using ConcesionariaApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ConcesionariaApp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<Usuario, IdentityRole<int>, int>(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<ComisionPorTipoVehiculo> ComisionesPorTipoVehiculo => Set<ComisionPorTipoVehiculo>();
    public DbSet<ComisionPorAntiguedad> ComisionesPorAntiguedad => Set<ComisionPorAntiguedad>();
    public DbSet<RecargoPorCuotas> RecargosPorCuotas => Set<RecargoPorCuotas>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.Property(x => x.UserName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NormalizedUserName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Vehiculo>(entity =>
        {
            entity.Property(x => x.Marca).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Modelo).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Patente).HasMaxLength(10);
            entity.HasIndex(x => x.Patente).IsUnique().HasFilter("[Patente] IS NOT NULL");
            entity.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.PrecioBase).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.Property(x => x.DNI).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.DNI).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.Property(x => x.MetodoPago).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.PrecioBase).HasPrecision(18, 2);
            entity.Property(x => x.PrecioFinal).HasPrecision(18, 2);
            entity.Property(x => x.PorcentajeComisionAplicado).HasPrecision(8, 3);
            entity.Property(x => x.ComisionCalculada).HasPrecision(18, 2);
            entity.HasOne(x => x.Vehiculo).WithMany().HasForeignKey(x => x.VehiculoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Cliente).WithMany(x => x.Ventas).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Vendedor).WithMany(x => x.Ventas).HasForeignKey(x => x.VendedorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AnuladoPorUsuario).WithMany().HasForeignKey(x => x.AnuladoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ComisionPorTipoVehiculo>(entity =>
        {
            entity.HasKey(x => x.Tipo);
            entity.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.PorcentajeBase).HasPrecision(8, 3);
        });

        modelBuilder.Entity<ComisionPorAntiguedad>().Property(x => x.PorcentajeAdicional).HasPrecision(8, 3);
        modelBuilder.Entity<RecargoPorCuotas>().Property(x => x.PorcentajeRecargo).HasPrecision(8, 3);

        modelBuilder.Entity<RegistroAuditoria>(entity =>
        {
            entity.Property(x => x.Accion).HasMaxLength(80).IsRequired();
            entity.Property(x => x.EntidadAfectada).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DetalleJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.Fecha, x.UsuarioId, x.Accion });
            entity.HasOne(x => x.Usuario).WithMany(x => x.RegistrosAuditoria).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
