using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace SmartPantry.Productos;

[AllowAnonymous]
public class ProductoAppService : SmartPantryAppService, IProductoAppService
{
    private readonly IRepository<Producto, Guid> _productoRepository;
    // Instanciamos el mapper de Mapperly
    private readonly ProductoToProductoDtoMapper _mapper = new();

    public ProductoAppService(IRepository<Producto, Guid> productoRepository)
    {
        _productoRepository = productoRepository;
    }

    public async Task<ProductoDto> CreateAsync(CreateProductoDto input)
    {
        // Mapeo MANUAL de DTO a Entidad para respetar las reglas de dominio
        var producto = new Producto(
            GuidGenerator.Create(),
            input.CodigoBarras,
            input.Nombre,
            input.Marca
        );

        await _productoRepository.InsertAsync(producto);

        // Mapeo AUTOMÁTICO de Entidad a DTO usando Mapperly
        return _mapper.Map(producto);
    }

    public async Task<ProductoDto> GetAsync(Guid id)
    {
        var producto = await _productoRepository.GetAsync(id);

        // Mapeo AUTOMÁTICO de Entidad a DTO usando Mapperly
        return _mapper.Map(producto);
    }

    public async Task<ProductoDto> UpdateAsync(Guid id, CreateUpdateProductoDto input)
    {
        var producto = await _productoRepository.GetAsync(id);

        // Mapeo MANUAL de DTO a Entidad usando los métodos de mutación
        producto.SetCodigoBarras(input.CodigoBarras);
        producto.SetNombre(input.Nombre);
        producto.SetMarca(input.Marca);

        await _productoRepository.UpdateAsync(producto);

        // Mapeo AUTOMÁTICO de Entidad a DTO usando Mapperly
        return _mapper.Map(producto);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _productoRepository.DeleteAsync(id);
    }

    public async Task<PagedResultDto<ProductoDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var totalCount = await _productoRepository.GetCountAsync();
        var sorting = string.IsNullOrWhiteSpace(input.Sorting) ? nameof(Producto.Nombre) : input.Sorting;

        var productos = await _productoRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting
        );

        // Mapeo AUTOMÁTICO de Lista a Lista usando Mapperly
        var dtos = _mapper.Map(productos);

        return new PagedResultDto<ProductoDto>(totalCount, dtos);
    }
}