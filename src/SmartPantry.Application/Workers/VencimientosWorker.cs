using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;
using Volo.Abp.Uow;
using SmartPantry.Despensa;

namespace SmartPantry.Workers;

public class VencimientosWorker : AsyncPeriodicBackgroundWorkerBase
{
    public VencimientosWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        // Milisegundos. 60000 = 1 minuto (para probar localmente).
        // En producción podría ser 86400000 (24 horas).
        Timer.Period = 60000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Logger.LogInformation("Iniciando ejecución del Worker de Vencimientos...");

        try
        {
            // El ServiceProvider del contexto ya es un scope propio de esta ejecución
            var sp = workerContext.ServiceProvider;
            var itemRepository = sp.GetRequiredService<IRepository<ItemDespensa, Guid>>();
            var advertenciaRepository = sp.GetRequiredService<IRepository<AdvertenciaVencimiento, Guid>>();
            var procesador = sp.GetRequiredService<ProcesadorVencimientosManager>();
            var uowManager = sp.GetRequiredService<IUnitOfWorkManager>();

            var fechaActual = DateTime.UtcNow; // criterio UTC, igual que en las pruebas

            // 1. Ids a evaluar: ítems con fecha (consumidos incluidos) más los que ya tienen
            //    una advertencia activa, para poder desactivarla si dejó de corresponder
            Guid[] ids;
            using (var uow = uowManager.Begin(requiresNew: true))
            {
                var conFecha = (await itemRepository.GetListAsync(x => x.FechaVencimiento.HasValue))
                    .Select(x => x.Id);
                var conAdvertenciaActiva = (await advertenciaRepository.GetListAsync(a => a.Activa))
                    .Select(a => a.ItemDespensaId);

                ids = conFecha.Union(conAdvertenciaActiva).ToArray();
                await uow.CompleteAsync();
            }

            // 2. Una unidad de trabajo por ítem: un fallo no revierte ni frena a los demás
            var errores = 0;
            foreach (var id in ids)
            {
                try
                {
                    using var uow = uowManager.Begin(requiresNew: true);
                    var item = await itemRepository.GetAsync(id);
                    await procesador.ProcesarItemAsync(item, fechaActual);
                    await uow.CompleteAsync();
                }
                catch (Exception ex)
                {
                    errores++;
                    Logger.LogError(ex, "Error procesando vencimientos del ítem {ItemId}", id);
                }
            }

            Logger.LogInformation(
                "Worker de Vencimientos finalizado. Ítems evaluados: {Total}, con error: {Errores}",
                ids.Length, errores);
        }
        catch (Exception ex)
        {
            // Se registra sin detener futuras ejecuciones del worker
            Logger.LogError(ex, "Ocurrió un error en el worker de vencimientos.");
        }
    }
}