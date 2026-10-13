using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.Productos;

public interface IProductoAppService : IApplicationService
{
    // Búsqueda por código en catálogo externo (TP 07 / RF-05)
    // Al iniciar con 'Get', ABP configura el endpoint como un verbo GET HTTP
    Task<ResultadoBusquedaProductoDto> GetBuscarPorCodigoAsync(BuscarProductoPorCodigoInputDto input);

    // Operaciones CRUD existentes de persistencia interna (TP05 / TP06)
    Task<ProductoDto> CreateAsync(CreateProductoDto input);

    Task<ProductoDto> GetAsync(Guid id);

    Task<ProductoDto> UpdateAsync(Guid id, CreateUpdateProductoDto input);

    Task DeleteAsync(Guid id);

    Task<PagedResultDto<ProductoDto>> GetListAsync(PagedAndSortedResultRequestDto input);
}