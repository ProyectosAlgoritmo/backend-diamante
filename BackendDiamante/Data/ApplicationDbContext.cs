using System;
using System.Collections.Generic;
using BackendDiamante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendDiamante.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ─── Roles & Permissions (scaffolded) ─────────────────────────────────────
    public virtual DbSet<Module>         Modules         { get; set; }
    public virtual DbSet<Permission>     Permissions     { get; set; }
    public virtual DbSet<Role>           Roles           { get; set; }
    public virtual DbSet<RolePermission> RolePermissions { get; set; }
    public virtual DbSet<Submodule>      Submodules      { get; set; }

    // ─── Auth (new dev) ───────────────────────────────────────────────────────
    public DbSet<User>               Users              { get; set; }
    public DbSet<RefreshToken>       RefreshTokens      { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    // ─── Notifications ─────────────────────────────────────────────────────────
    public DbSet<Notification>     Notifications     { get; set; }
    public DbSet<NotificationUser> NotificationUsers { get; set; }

    // ─── Business (Centros de Costo) ──────────────────────────────────────────
    public virtual DbSet<Company>             Companies             { get; set; }
    public virtual DbSet<Sector>              Sectors               { get; set; }
    public virtual DbSet<Operator>            Operators             { get; set; }
    public virtual DbSet<CostCenter>          CostCenters           { get; set; }
    public virtual DbSet<CostCenterOperator>  CostCenterOperators   { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Module>(entity =>
        {
            entity.ToTable("Modules", "security");

            entity.HasIndex(e => e.Code, "UQ_Modules_Code").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions", "security");

            entity.HasIndex(e => e.Code, "UQ_Permissions_Code").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.Submodule).WithMany(p => p.Permissions)
                .HasForeignKey(d => d.SubmoduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Permissions_Submodules");
        });

        // ─── Roles ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles", "security");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions", "security");

            entity.HasIndex(e => new { e.RoleId, e.PermissionId }, "UQ_RolePermissions_RolePermission").IsUnique();

            entity.Property(e => e.GrantedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermissions_Permissions");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermissions_Roles");
        });

        modelBuilder.Entity<Submodule>(entity =>
        {
            entity.ToTable("Submodules", "security");

            entity.HasIndex(e => e.Code, "UQ_Submodules_Code").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Module).WithMany(p => p.Submodules)
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Submodules_Modules");
        });

        // ─── Users ────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "security");
            entity.HasKey(e => e.Id);

            // Campos originales (auth)
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Campos extendidos (modulo Usuarios)
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.HasIndex(e => e.Username).IsUnique().HasFilter("[Username] IS NOT NULL");
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.DocumentId).HasMaxLength(30);
            entity.HasIndex(e => e.DocumentId).IsUnique().HasFilter("[DocumentId] IS NOT NULL");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Activo");
            entity.Property(e => e.Certificates).HasColumnType("NVARCHAR(MAX)");
        });

        // ─── RefreshTokens ────────────────────────────────────────────────────
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens", "security");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── PasswordResetTokens ─────────────────────────────────────────────
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens", "security");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.PasswordResetTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── Notifications ────────────────────────────────────────────────────
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications", "security");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NotificationUser>(entity =>
        {
            entity.ToTable("NotificationUsers", "security");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.NotificationId, e.UserId }, "UQ_NotificationUsers").IsUnique();

            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(e => e.Notification)
                  .WithMany(n => n.NotificationUsers)
                  .HasForeignKey(e => e.NotificationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── Business: Companies ──────────────────────────────────────────────
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Companies", "business");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        // ─── Business: Sectors ────────────────────────────────────────────────
        modelBuilder.Entity<Sector>(entity =>
        {
            entity.ToTable("Sectors", "business");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        // ─── Business: Operators ──────────────────────────────────────────────
        modelBuilder.Entity<Operator>(entity =>
        {
            entity.ToTable("Operators", "business");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Role).HasMaxLength(100);
            entity.Property(e => e.Shift).HasMaxLength(50);

            entity.HasOne(d => d.Sector).WithMany(p => p.Operators)
                .HasForeignKey(d => d.SectorId)
                .HasConstraintName("FK_Operators_Sectors");
        });

        // ─── Business: CostCenters ────────────────────────────────────────────
        modelBuilder.Entity<CostCenter>(entity =>
        {
            entity.ToTable("CostCenters", "business");

            entity.HasIndex(e => e.Code, "UQ_CostCenters_Code").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Company).WithMany(p => p.CostCenters)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CostCenters_Companies");
        });

        // ─── Business: CostCenterOperators ────────────────────────────────────
        modelBuilder.Entity<CostCenterOperator>(entity =>
        {
            entity.ToTable("CostCenterOperators", "business");

            entity.HasIndex(e => new { e.CostCenterId, e.OperatorId }, "UQ_CostCenterOperators").IsUnique();

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.CostCenter).WithMany(p => p.CostCenterOperators)
                .HasForeignKey(d => d.CostCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CostCenterOperators_CostCenters");

            entity.HasOne(d => d.Operator).WithMany(p => p.CostCenterOperators)
                .HasForeignKey(d => d.OperatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CostCenterOperators_Operators");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
