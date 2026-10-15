using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SmartPantry.ClientesExternos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace SmartPantry.Productos;

[AllowAnonymous]
public class ProductoAppService : SmartPantryAppService, IProductoAppService
{
    private readonly IRepository<Producto, Guid> _productoRepository;
    private readonly IExternalProductCatalogClient _externalCatalogClient;

    private readonly ProductoToProductoDtoMapper _mapper = new();

    public ProductoAppService(
        IRepository<Producto, Guid> productoRepository,
        IExternalProductCatalogClient externalCatalogClient)
    {
        _productoRepository = productoRepository;
        _externalCatalogClient = externalCatalogClient;
    }

    /// <summary>
    /// Consulta un producto por código de barras en la API externa de Open Food Facts (RF-05, TP 07)
    /// </summary>
    public async Task<ResultadoBusquedaProductoDto> GetBuscarPorCodigoAsync(BuscarProductoPorCodigoInputDto input)
    {
        try
        {
            var externalProduct = await _externalCatalogClient.GetByBarcodeAsync(input.Barcode);

            if (externalProduct == null)
            {
                return new ResultadoBusquedaProductoDto
                {
                    Barcode = input.Barcode,
                    Encontrado = false,
                    MensajeError = "Producto no encontrado en el catálogo externo."
                };
            }

            return new ResultadoBusquedaProductoDto
            {
                Barcode = externalProduct.Barcode,
                Nombre = externalProduct.Name,
                Marca = externalProduct.Brand,
                ImagenUrl = externalProduct.ImageUrl,
                Categorias = externalProduct.Categories ?? new List<string>(),
                Ingredientes = !string.IsNullOrWhiteSpace(externalProduct.Ingredients)
                    ? new List<string> { externalProduct.Ingredients }
                    : new List<string>(),
                Alergenos = externalProduct.Allergens ?? new List<string>(),
                NutriScore = externalProduct.NutriScore,
                NovaGroup = externalProduct.NovaGroup,
                Encontrado = true
            };
        }
        catch (HttpRequestException ex)
        {
            return new ResultadoBusquedaProductoDto
            {
                Barcode = input.Barcode,
                Encontrado = false,
                MensajeError = ex.Message
            };
        }
        catch (Exception)
        {
            return new ResultadoBusquedaProductoDto
            {
                Barcode = input.Barcode,
                Encontrado = false,
                MensajeError = "Ocurrió un error al comunicarse con el servicio externo de productos."
            };
        }
    }

    public async Task<ProductoDto> CreateAsync(CreateProductoDto input)
    {
        var producto = new Producto(
            GuidGenerator.Create(),
            input.CodigoBarras,
            input.Nombre,
            input.Marca
        );

        await _productoRepository.InsertAsync(producto);
        return _mapper.Map(producto);
    }

    public async Task<ProductoDto> GetAsync(Guid id)
    {
        var producto = await _productoRepository.GetAsync(id);
        return _mapper.Map(producto);
    }

    public async Task<ProductoDto> UpdateAsync(Guid id, CreateUpdateProductoDto input)
    {
        var producto = await _productoRepository.GetAsync(id);

        producto.SetCodigoBarras(input.CodigoBarras);
        producto.SetNombre(input.Nombre);
        producto.SetMarca(input.Marca);

        await _productoRepository.UpdateAsync(producto);
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

        var dtos = _mapper.Map(productos);
        return new PagedResultDto<ProductoDto>(totalCount, dtos);
    }
}