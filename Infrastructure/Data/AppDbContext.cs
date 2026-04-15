using Microsoft.EntityFrameworkCore;
using OFUserControlApp.Domain.Entities;

namespace OFUserControlApp.Infrastructure.Data;

/// <summary>
/// Contexto de base de datos principal
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UsuarioCruce> UsuariosCruce { get; set; }
    public DbSet<ViewUsuario> ViewUsuarios { get; set; }
    public DbSet<UsuarioExcel> UsuariosExcel { get; set; }
    public DbSet<LogProceso> LogProcesos { get; set; }
    public DbSet<EstadisticasUsuarios> EstadisticasUsuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar tabla de resultados del cruce
        modelBuilder.Entity<UsuarioCruce>(entity =>
        {
            entity.ToTable("Rpt_UsuariosCruce");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UsuarioExcel).HasMaxLength(256);
            entity.Property(e => e.CorreoExcel).HasMaxLength(512);
            entity.Property(e => e.EmailView).HasMaxLength(512);
            entity.Property(e => e.UPNView).HasMaxLength(256);
            entity.Property(e => e.F_Baja).HasMaxLength(50);
            entity.Property(e => e.Estado).HasMaxLength(10);
            entity.Property(e => e.ProcesoId).HasColumnType("uuid");
            entity.Property(e => e.GeneradoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.ProcesoId).HasDatabaseName("IX_Rpt_UsuariosCruce_Proceso");
        });

        // Configurar tabla de staging de usuarios Excel
        modelBuilder.Entity<UsuarioExcel>(entity =>
        {
            entity.ToTable("Stg_UsuariosExcel");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProcesoId).HasColumnType("uuid");
            entity.Property(e => e.Usuario).HasMaxLength(256);
            entity.Property(e => e.Correo).HasMaxLength(512);
            entity.Property(e => e.FuenteArchivo).HasMaxLength(260);
            entity.Property(e => e.HashContenido).HasColumnType("bytea");
            entity.Property(e => e.CargadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.ProcesoId).HasDatabaseName("IX_Stg_UsuariosExcel_Proceso");
        });

        // Configurar tabla de log de procesos
        modelBuilder.Entity<LogProceso>(entity =>
        {
            entity.ToTable("LogProceso");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProcesoId).HasColumnType("uuid");
            entity.Property(e => e.Etapa).HasMaxLength(50);
            entity.Property(e => e.Mensaje).HasMaxLength(500);
            entity.Property(e => e.Nivel).HasMaxLength(20);
            entity.Property(e => e.FechaUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.ProcesoId).HasDatabaseName("IX_LogProceso_ProcesoId");
        });

        // Configurar vista de estadísticas (solo lectura)
        modelBuilder.Entity<EstadisticasUsuarios>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_EstadisticasUsuarios");
            entity.Property(e => e.ProcesoId).HasColumnType("uuid");
        });

        // Configurar tabla View_Usuarios (Directorio Activo)
        modelBuilder.Entity<ViewUsuario>(entity =>
        {
            entity.ToTable("View_Usuarios");
            entity.HasKey(e => e.Id);
            
            // Mapeo de columnas basado en la estructura real
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.EMail).HasColumnName("EMail").HasMaxLength(1000);
            entity.Property(e => e.NombreCompleto).HasColumnName("NombreCompleto").HasMaxLength(1000);
            entity.Property(e => e.Nombre).HasColumnName("Nombre").HasMaxLength(1000);
            entity.Property(e => e.Apellido).HasColumnName("Apellido").HasMaxLength(1000);
            entity.Property(e => e.User_SO).HasColumnName("User_SO").HasMaxLength(1000);
            entity.Property(e => e.F_Alta).HasColumnName("F_Alta");
            entity.Property(e => e.Cod_Puesto).HasColumnName("Cod_Puesto");
            entity.Property(e => e.Cod_Unidad).HasColumnName("Cod_Unidad");
            entity.Property(e => e.Cedula).HasColumnName("Cedula").HasMaxLength(1000);
            entity.Property(e => e.Cod_Jefe).HasColumnName("Cod_Jefe");
            entity.Property(e => e.COD_USUARIO_MIGRACION).HasColumnName("COD_USUARIO_MIGRACION");
            entity.Property(e => e.COD_JEFE_MIGRACION).HasColumnName("COD_JEFE_MIGRACION");
            entity.Property(e => e.cod_status_usuario).HasColumnName("cod_status_usuario");
            entity.Property(e => e.IsAdmin).HasColumnName("IsAdmin");
            entity.Property(e => e.IsUnidad).HasColumnName("IsUnidad");
            entity.Property(e => e.DebeRegistrarEnTS).HasColumnName("DebeRegistrarEnTS");
            entity.Property(e => e.IdStatusADAzure).HasColumnName("IdStatusADAzure");
            entity.Property(e => e.IdAzureObject).HasColumnName("IdAzureObject");
            entity.Property(e => e.F_Baja).HasColumnName("F_Baja");
            entity.Property(e => e.UserPrincipalName).HasColumnName("UserPrincipalName").HasMaxLength(1000);
            entity.Property(e => e.JobTitle).HasColumnName("JobTitle").HasMaxLength(1000);
            
            // Ignorar propiedades calculadas
            entity.Ignore(e => e.Usuario);
            entity.Ignore(e => e.Correo);
        });
    }
}
