using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using SmartPantry.Authors;
using SmartPantry.Books;
using SmartPantry.Productos; 
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using SmartPantry.Despensa;

namespace SmartPantry.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ConnectionStringName("Default")]
public class SmartPantryDbContext :
    AbpDbContext<SmartPantryDbContext>,
    IIdentityDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    public DbSet<Author> Authors { get; set; }

    public DbSet<Book> Books { get; set; }

    // 👇 Agrega estas dos líneas para el TP-08
    public DbSet<ItemDespensa> ItemsDespensa { get; set; }
    public DbSet<AdvertenciaVencimiento> AdvertenciasVencimiento { get; set; }

    public DbSet<Producto> Productos { get; set; }

    #region Entities from the modules

    /* Notice: We only implemented IIdentityProDbContext 
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityProDbContext .
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    #endregion

    public SmartPantryDbContext(DbContextOptions<SmartPantryDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureBlobStoring();

        builder.Entity<Author>(b =>
        {
            b.ToTable(SmartPantryConsts.DbTablePrefix + "Authors",
                SmartPantryConsts.DbSchema);
            b.ConfigureByConvention(); //auto configure for the base class props
            b.Property(x => x.Name).IsRequired().HasMaxLength(AuthorConsts.MaxNameLength);
            b.Property(x => x.ShortBio).HasMaxLength(AuthorConsts.MaxShortBioLength);
        });

        builder.Entity<Book>(b =>
        {
            b.ToTable(SmartPantryConsts.DbTablePrefix + "Books",
                SmartPantryConsts.DbSchema);
            b.ConfigureByConvention(); //auto configure for the base class props
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.HasOne<Author>().WithMany().HasForeignKey(x => x.AuthorId).IsRequired();
        });

        /* Configure your own tables/entities inside here */

        // 👈 Mapeo de la entidad Producto para TP05 / RF-08
        builder.Entity<Producto>(b =>
        {
            b.ToTable(SmartPantryConsts.DbTablePrefix + "Productos", SmartPantryConsts.DbSchema);
            b.ConfigureByConvention(); // Configura auditorías e Id base de ABP

            // Mapeo de restricciones con las constantes del dominio
            b.Property(x => x.CodigoBarras).IsRequired().HasMaxLength(ProductoConsts.MaxCodigoBarrasLength);
            b.Property(x => x.Nombre).IsRequired().HasMaxLength(ProductoConsts.MaxNombreLength);
            b.Property(x => x.Marca).IsRequired().HasMaxLength(ProductoConsts.MaxMarcaLength);

            // Regla de negocio: El código de barras no se duplica en el sistema
            b.HasIndex(x => x.CodigoBarras).IsUnique();
        });

        // ... (código de builder.Entity<Producto>)

        // 👇 Mapeo para el TP-08: ItemDespensa y Advertencias
        builder.Entity<ItemDespensa>(b =>
        {
            b.ToTable(SmartPantryConsts.DbTablePrefix + "ItemsDespensa", SmartPantryConsts.DbSchema);
            b.ConfigureByConvention(); // Auditorías e Id base de ABP

            b.Property(x => x.PropietarioId).IsRequired();
            b.Property(x => x.ProductoId).IsRequired();
            b.Property(x => x.Consumido).IsRequired();

            // Relación con Producto para mantener integridad referencial
            b.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).IsRequired();
        });

        builder.Entity<AdvertenciaVencimiento>(b =>
        {
            b.ToTable(SmartPantryConsts.DbTablePrefix + "AdvertenciasVencimiento", SmartPantryConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ItemDespensaId).IsRequired();
            b.Property(x => x.Tipo).IsRequired();
            b.Property(x => x.Activa).IsRequired();
            b.Property(x => x.Mensaje).IsRequired().HasMaxLength(250);

            // Relación con ItemDespensa
            b.HasOne<ItemDespensa>().WithMany().HasForeignKey(x => x.ItemDespensaId).IsRequired();

            // REGLA DE NEGOCIO (TP-08): Índice único por Ítem y Tipo de Advertencia
            // Esto refuerza la ausencia de duplicados a nivel estructural en la base de datos
            b.HasIndex(x => new { x.ItemDespensaId, x.Tipo }).IsUnique();
        });
    }
}