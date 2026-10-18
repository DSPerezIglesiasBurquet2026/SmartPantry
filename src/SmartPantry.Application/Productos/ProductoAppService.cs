using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SmartPantry.ClientesExternos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using System.Net;
using Microsoft.Extensions.Logging;

namespace SmartPantry.Productos;

[AllowAnonymous]
public class ProductoAppService : SmartPantryAppService, IProductoAppService
{
    private readonly IRepository<Producto, Guid> _productoRepository;
    private readonly IExternalProductCatalogClient _externalCatalogClient;
    private readonly ILogger<ProductoAppService> _logger;

    private readonly ProductoToProductoDtoMapper _mapper = new();

    public ProductoAppService(
        IRepository<Producto, Guid> productoRepository,
        IExternalProductCatalogClient externalCatalogClient,
        ILogger<ProductoAppService> logger)
    {
        _productoRepository = productoRepository;
        _externalCatalogClient = externalCatalogClient;
        _logger = logger;
    }

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
                    Estado = EstadoBusquedaProducto.NoEncontrado,
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
                Estado = EstadoBusquedaProducto.Encontrado
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == (HttpStatusCode)429)
        {
            _logger.LogWarning(ex, "Límite de solicitudes de Open Food Facts alcanzado");
            return new ResultadoBusquedaProductoDto
            {
                Barcode = input.Barcode,
                Estado = EstadoBusquedaProducto.LimiteSolicitudes,
                MensajeError = "Se ha alcanzado el límite de solicitudes a Open Food Facts. Intente más tarde."
            };
        }
        catch (Exception ex)
        {
            // 5xx, red caída, timeout, etc.
            _logger.LogError(ex, "Error consultando Open Food Facts");
            return new ResultadoBusquedaProductoDto
            {
                Barcode = input.Barcode,
                Estado = EstadoBusquedaProducto.ServicioNoDisponible,
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