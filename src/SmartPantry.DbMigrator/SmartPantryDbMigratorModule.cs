using Microsoft.Extensions.DependencyInjection;
using SmartPantry.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace SmartPantry.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(SmartPantryEntityFrameworkCoreModule),
    typeof(SmartPantryApplicationContractsModule)
)]
public class SmartPantryDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        Configure<AbpDbConnectionOptions>(options =>
        {
            options.ConnectionStrings.Default = configuration["ConnectionStrings:Default"];
        });

        Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(opts =>
            {
                opts.UseSqlServer();
            });
        });
    }
}