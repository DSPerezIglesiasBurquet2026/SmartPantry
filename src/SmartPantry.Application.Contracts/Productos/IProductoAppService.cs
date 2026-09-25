using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.Productos;

public interface IProductoAppService : IApplicationService
{
    // Asegurarse de usar CreateUpdateProductoDto aquí
    Task<ProductoDto> CreateAsync(CreateProductoDto input);

    Task<ProductoDto> GetAsync(Guid id);

    // Asegurarse de usar CreateUpdateProductoDto aquí también
    Task<ProductoDto> UpdateAsync(Guid id, CreateUpdateProductoDto input);

    Task DeleteAsync(Guid id);

    Task<PagedResultDto<ProductoDto>> GetListAsync(PagedAndSortedResultRequestDto input);
}