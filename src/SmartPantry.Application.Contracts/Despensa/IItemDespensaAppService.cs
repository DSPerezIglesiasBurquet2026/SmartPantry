using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.Despensa;

public interface IItemDespensaAppService : IApplicationService
{
    // Listar de forma paginada[cite: 7]
    Task<PagedResultDto<ItemDespensaDto>> GetListAsync(PagedAndSortedResultRequestDto input);

    // Actualizar cantidad, unidad y fecha de vencimiento[cite: 7]
    Task UpdateAsync(Guid id, UpdateItemDespensaDto input);

    // Marcar un ítem como consumido[cite: 7]
    Task MarcarComoConsumidoAsync(Guid id);

    // Eliminarlo[cite: 7]
    Task DeleteAsync(Guid id);
}