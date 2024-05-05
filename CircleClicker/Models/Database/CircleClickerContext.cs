using Microsoft.EntityFrameworkCore;

namespace CircleClicker.Models.Database;

public partial class CircleClickerContext : DbContext
{
    #region Tables
    /// <summary>
    /// The <c>purchases</c> table.
    /// </summary>
    public virtual DbSet<Purchase> Purchases { get; set; }

    /// <summary>
    /// Use <see cref="Main.Buildings"/> instead.
    /// </summary>
    public virtual DbSet<Building> Buildings { get; set; }

    /// <summary>
    /// Use <see cref="Main.Upgrades"/> instead.
    /// </summary>
    public virtual DbSet<Upgrade> Upgrades { get; set; }

    /// <summary>
    /// The <c>users</c> table.
    /// </summary>
    public virtual DbSet<User> Users { get; set; }

    /// <summary>
    /// The <c>saves</c> table.
    /// </summary>
    public virtual DbSet<Save> Saves { get; set; }

    /// <summary>
    /// The <c>saves_purchases</c> table.
    /// </summary>
    public virtual DbSet<OwnedPurchase> OwnedPurchases { get; set; }

    /// <summary>
    /// The <c>variables</c> table.
    /// </summary>
    public virtual DbSet<Variable> Variables { get; set; }
    #endregion

    public CircleClickerContext() { }

    public CircleClickerContext(DbContextOptions<CircleClickerContext> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql(
            "server=localhost;user=root;database=circle_clicker",
            ServerVersion.Parse("10.4.32-mariadb")
        );
    }

    #region Database model
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("utf8mb4_general_ci").HasCharSet("utf8mb4");

        modelBuilder.Entity<Save>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("saves");

            entity.HasIndex(e => e.UserId, "FK_users");

            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("PK_saves");
            entity.Property(e => e.UserId).HasColumnType("int(11)").HasColumnName("FK_users");
            entity.Property(e => e.Circles).HasColumnName("circles");
            entity.Property(e => e.Triangles).HasColumnName("triangles");
            entity.Property(e => e.Squares).HasColumnName("squares");
            entity.Property(e => e.Clicks).HasColumnType("int(11)").HasColumnName("clicks");
            entity
                .Property(e => e.TriangleClicks)
                .HasColumnType("int(11)")
                .HasColumnName("triangle_clicks");
            entity.Property(e => e.TotalCircles).HasColumnName("total_circles");
            entity.Property(e => e.ManualCircles).HasColumnName("manual_circles");
            entity.Property(e => e.TotalTriangles).HasColumnName("total_triangles");
            entity.Property(e => e.LifetimeCircles).HasColumnName("lifetime_circles");
            entity.Property(e => e.LifetimeManualCircles).HasColumnName("lifetime_manual_circles");
            entity.Property(e => e.LifetimeTriangles).HasColumnName("lifetime_triangles");
            entity.Property(e => e.LifetimeSquares).HasColumnName("lifetime_squares");
            entity
                .Property(e => e.LifetimeClicks)
                .HasColumnType("int(11)")
                .HasColumnName("lifetime_clicks");
            entity
                .Property(e => e.LifetimeTriangleClicks)
                .HasColumnType("int(11)")
                .HasColumnName("lifetime_triangle_clicks");
            entity
                .Property(e => e.CreationDate)
                .HasColumnType("datetime")
                .HasColumnName("creation_date");
            entity
                .Property(e => e.LastSaveDate)
                .HasColumnType("datetime")
                .HasColumnName("last_save_date");

            entity
                .HasOne(d => d.User)
                .WithMany(p => p.Saves)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("saves_ibfk_1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("PK_users");
            entity.Property(e => e.Name).HasMaxLength(32).HasColumnName("name");
            entity.Property(e => e.Password).HasMaxLength(128).HasColumnName("password");
            entity.Property(e => e.IsAdmin).HasColumnName("is_admin");
            entity.Property(e => e.MusicVolume).HasColumnName("music_volume");
            entity.Property(e => e.SoundVolume).HasColumnName("sound_volume");
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("purchases");

            entity.HasDiscriminator<string>("type");
            entity.Property("type").HasMaxLength(32);

            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("PK_purchases");
            entity.Property(e => e.Name).HasMaxLength(50).HasColumnName("name");
            entity.Property(e => e.RequiredId).HasMaxLength(32).HasColumnName("requires");
            entity.Property(e => e.BaseRequirement).HasColumnName("requirement");
            entity.Property(e => e.RequirementScaling).HasColumnName("requirement_scaling");
            entity.Property(e => e.RequirementAdditive).HasColumnName("requirement_additive");
            entity.Property(e => e.CurrencyId).HasMaxLength(32).HasColumnName("currency");
            entity.Property(e => e.BaseCost).HasColumnName("cost");
            entity.Property(e => e.CostScaling).HasColumnName("cost_scaling");
            entity.Property(e => e.MaxAmount).HasColumnType("int(11)").HasColumnName("max_amount");
        });

        modelBuilder.Entity<Building>(entity =>
        {
            entity.Property(e => e.BaseProduction).HasColumnName("production");
        });

        modelBuilder.Entity<Upgrade>(entity =>
        {
            entity.Property(e => e.AffectedId).HasColumnName("affects");
            entity.Property(e => e.BaseEffect).HasColumnName("effect");
        });

        modelBuilder.Entity<OwnedPurchase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("saves_purchases");

            entity.HasIndex(e => e.SaveId, "FK_saves");

            entity.HasIndex(e => e.PurchaseId, "FK_purchases");

            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("PK_saves_upgrades");
            entity.Property(e => e.SaveId).HasColumnType("int(11)").HasColumnName("FK_saves");
            entity
                .Property(e => e.PurchaseId)
                .HasColumnType("int(11)")
                .HasColumnName("FK_purchases");

            entity
                .HasOne(d => d.Save)
                .WithMany(p => p.OwnedPurchases)
                .HasForeignKey(d => d.SaveId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("saves_purchases_ibfk_1");

            entity
                .HasOne(d => d.Purchase)
                .WithMany()
                .HasForeignKey(d => d.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("saves_purchases_ibfk_2");
        });

        modelBuilder.Entity<Variable>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("variables");

            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("PK_variables");
            entity.Property(e => e.Name).HasMaxLength(64).HasColumnName("name");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    #endregion
}
