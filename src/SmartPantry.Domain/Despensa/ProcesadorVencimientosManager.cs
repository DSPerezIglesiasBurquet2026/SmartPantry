using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace SmartPantry.Despensa;

public class ProcesadorVencimientosManager : DomainService
{
    private readonly IRepository<AdvertenciaVencimiento, Guid> _advertenciaRepository;

    public ProcesadorVencimientosManager(IRepository<AdvertenciaVencimiento, Guid> advertenciaRepository)
    {
        _advertenciaRepository = advertenciaRepository;
    }

    /// <summary>
    /// Criterio de fechas: UTC. Se comparan fechas calendario (parte .Date) de la fecha de
    /// vencimiento y de fechaActualUtc. Umbral: 3 días, inclusivo (vence hoy o dentro de 3 días
    /// genera advertencia; ya vencido, consumido o sin fecha no genera y desactiva la existente).
    /// La fecha actual se recibe como parámetro para poder controlarla en las pruebas.
    /// </summary>
    public async Task ProcesarItemAsync(ItemDespensa item, DateTime fechaActualUtc)
    {
        // 1. Buscar si ya existe una advertencia de "Vencimiento Próximo" para este ítem
        var advertenciaExistente = await _advertenciaRepository.FirstOrDefaultAsync(a =>
            a.ItemDespensaId == item.Id && a.Tipo == TipoAdvertencia.VencimientoProximo);

        // 2. Umbral: 3 días de anticipación
        int diasUmbral = 3;

        // 3. Lógica de evaluación
        bool ameritaAdvertencia = false;
        string mensaje = string.Empty;

        if (!item.Consumido && item.FechaVencimiento.HasValue)
        {
            var diasRestantes = (item.FechaVencimiento.Value.Date - fechaActualUtc.Date).TotalDays;

            if (diasRestantes <= diasUmbral && diasRestantes >= 0)
            {
                ameritaAdvertencia = true;
                mensaje = $"El producto vencerá en {diasRestantes} día(s).";
            }
            // diasRestantes < 0 (vencido): no corresponde "próximo a vencer", ameritaAdvertencia queda en false
        }

        // 4. Idempotencia: crear, actualizar o desactivar
        if (ameritaAdvertencia)
        {
            if (advertenciaExistente == null)
            {
                var nuevaAdvertencia = new AdvertenciaVencimiento(
                    GuidGenerator.Create(),
                    item.Id,
                    TipoAdvertencia.VencimientoProximo,
                    mensaje);
                await _advertenciaRepository.InsertAsync(nuevaAdvertencia);
            }
            else
            {
                // Ya existía: se actualiza (o reactiva) la misma advertencia
                advertenciaExistente.Actualizar(mensaje, activa: true);
                await _advertenciaRepository.UpdateAsync(advertenciaExistente);
            }
        }
        else
        {
            if (advertenciaExistente != null && advertenciaExistente.Activa)
            {
                // Ya no corresponde (consumido, sin fecha, fecha lejana o vencido)
                advertenciaExistente.Actualizar(advertenciaExistente.Mensaje, activa: false);
                await _advertenciaRepository.UpdateAsync(advertenciaExistente);
            }
        }
    }
}