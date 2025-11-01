using AgendamientoGestion.Entidades.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Persistencia.DbContexts
{
    public class AgendamientoDbContext : DbContext
    {
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Tutoria> Tutorias { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Retroalimentacion> Retroalimentaciones { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<Informe> Informes { get; set; }
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<TutoriaEstudiante> TutoriaEstudiantes { get; set; }

        public AgendamientoDbContext(DbContextOptions<AgendamientoDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario");
                entity.HasKey(e => e.idUsuario);
                entity.Property(e => e.nombres).IsRequired().HasMaxLength(60);
                entity.Property(e => e.apellidos).IsRequired().HasMaxLength(60);
                entity.Property(e => e.correo).IsRequired().HasMaxLength(60);
                entity.Property(e => e.contrasenaHash).IsRequired().HasMaxLength(100);
                entity.Property(e => e.fechaRegistro).IsRequired();

                // Relación con Rol
                entity.HasOne(e => e.Rol)
                    .WithMany(r => r.Usuarios)
                    .HasForeignKey(e => e.Rol_idRol)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configuración de Rol
            modelBuilder.Entity<Rol>(entity =>
            {
                entity.ToTable("Rol");
                entity.HasKey(e => e.idRol);
                entity.Property(e => e.tipoRol).IsRequired().HasMaxLength(45);
            });

            // Configuración de Horario
            modelBuilder.Entity<Horario>(entity =>
            {
                entity.ToTable("Horario");
                entity.HasKey(e => e.idHorario);
                entity.Property(e => e.fechaInicio).IsRequired();
                entity.Property(e => e.fechaFin).IsRequired();
                entity.Property(e => e.horaInicio).IsRequired();
                entity.Property(e => e.horaFin).IsRequired();
                entity.Property(e => e.cupos).IsRequired().HasMaxLength(60);
                entity.Property(e => e.espacio).IsRequired().HasMaxLength(60);
                entity.Property(e => e.estado).IsRequired().HasMaxLength(60);

                // Relación con Usuario
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.Horarios)
                    .HasForeignKey(e => e.Usuario_idUsuario)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configuración de Tutoria
            modelBuilder.Entity<Tutoria>(entity =>
            {
                entity.ToTable("Tutoria");
                entity.HasKey(e => e.idTutoria);
                entity.Property(e => e.idioma).IsRequired().HasMaxLength(45);
                entity.Property(e => e.nivel).IsRequired().HasMaxLength(45);
                entity.Property(e => e.tema).IsRequired().HasMaxLength(45);
                entity.Property(e => e.modalidad).IsRequired().HasMaxLength(45);
                entity.Property(e => e.estado).IsRequired().HasMaxLength(45);
                entity.Property(e => e.fechaHora).IsRequired();

                // Relación con Usuario
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.Tutorias)
                    .HasForeignKey(e => e.Usuario_idUsuario)
                    .OnDelete(DeleteBehavior.NoAction);

                // Relación con Horario
                entity.HasOne(e => e.Horario)
                    .WithMany(h => h.Tutorias)
                    .HasForeignKey(e => e.Horario_idHorario)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configuración de Notificacion
            modelBuilder.Entity<Notificacion>(entity =>
            {
                entity.ToTable("Notificacion");
                entity.HasKey(e => e.idNotificacion);
                entity.Property(e => e.fecha).IsRequired();
                entity.Property(e => e.asunto).IsRequired().HasMaxLength(45);
                entity.Property(e => e.descripcion).IsRequired().HasMaxLength(250);

                // Relación con Usuario
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.Notificaciones)
                    .HasForeignKey(e => e.Usuario_idUsuario)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configuración de Retroalimentacion
            modelBuilder.Entity<Retroalimentacion>(entity =>
            {
                entity.ToTable("Retroalimentacion");
                entity.HasKey(e => e.idRetroalimentacion);
                entity.Property(e => e.comentario).IsRequired().HasMaxLength(300);
                entity.Property(e => e.calificacion).HasColumnType("decimal(3,2)");
                entity.Property(e => e.fecha).IsRequired();

                // Relación con Tutoria
                entity.HasOne(e => e.Tutoria)
                    .WithMany(t => t.Retroalimentaciones)
                    .HasForeignKey(e => e.Tutoria_idTutoria)
                    .OnDelete(DeleteBehavior.NoAction);

                // Relación con Usuario
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.Retroalimentaciones)
                    .HasForeignKey(e => e.Usuario_idUsuario)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configuración de Informe
            modelBuilder.Entity<Informe>(entity =>
            {
                entity.ToTable("Informe");
                entity.HasKey(e => e.idInforme);
                entity.Property(e => e.descripcion).IsRequired().HasMaxLength(250);
                entity.Property(e => e.fechaGeneracion).IsRequired();

                // Relación con Tutoria
                entity.HasOne(e => e.Tutoria)
                    .WithMany(t => t.Informes)
                    .HasForeignKey(e => e.Tutoria_idTutoria)
                    .OnDelete(DeleteBehavior.NoAction);

                // Relación con Usuario
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.Informes)
                    .HasForeignKey(e => e.Usuario_idUsuario)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configuración de TutoriaEstudiante
            modelBuilder.Entity<TutoriaEstudiante>(entity =>
            {
                entity.ToTable("TutoriaEstudiante");
                entity.HasKey(e => e.idTutoriaEstudiante);

                // Relación con Tutoria
                entity.HasOne(e => e.Tutoria)
                    .WithMany(t => t.TutoriaEstudiantes)
                    .HasForeignKey(e => e.Tutoria_idTutoria)
                    .OnDelete(DeleteBehavior.NoAction);

                // Relación con Usuario
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.TutoriaEstudiantes)
                    .HasForeignKey(e => e.Usuario_idUsuario)
                    .OnDelete(DeleteBehavior.NoAction);

                // Índice único para evitar duplicados
                entity.HasIndex(e => new { e.Tutoria_idTutoria, e.Usuario_idUsuario })
                    .IsUnique();
            });
        }
    }
}