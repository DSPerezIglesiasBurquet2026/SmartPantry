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
using System.Linq;

namespace SmartPantry.Productos;

//[AllowAnonymous] reemplazo por authorize para que solo usuarios autenticados puedan acceder a los endpoints de productos
[Authorize]
public class ProductoAppService : SmartPantryAppService, IProductoAppService
{
    private const string MensajeLimiteSolicitudes =
        "Se ha alcanzado el límite de solicitudes a Open Food Facts. Intente más tarde.";
    private const string MensajeServicioNoDisponible =
        "Ocurrió un error al comunicarse con el servicio externo de productos.";

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

            return MapearResultado(externalProduct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == (HttpStatusCode)429)
        {
            _logger.LogWarning(ex, "Límite de solicitudes de Open Food Facts alcanzado");
            return new ResultadoBusquedaProductoDto
            {
                Barcode = input.Barcode,
                Estado = EstadoBusquedaProducto.LimiteSolicitudes,
                MensajeError = MensajeLimiteSolicitudes
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
                MensajeError = MensajeServicioNoDisponible
            };
        }
    }

    public async Task<PagedResultDto<ResultadoBusquedaProductoDto>> BuscarPorTextoAsync(
        BuscarProductoPorTextoInputDto input)
    {
        try
        {
            var externos = await _externalCatalogClient.SearchByTextAsync(
                input.Texto.Trim(), input.SkipCount, input.MaxResultCount);

            var items = externos.Items.Select(MapearResultado).ToList();
            return new PagedResultDto<ResultadoBusquedaProductoDto>(externos.TotalCount, items);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == (HttpStatusCode)429)
        {
            _logger.LogWarning(ex, "Límite de solicitudes de Search-a-licious alcanzado");
            return ResultadoDeEstado(EstadoBusquedaProducto.LimiteSolicitudes, MensajeLimiteSolicitudes);
        }
        catch (Exception ex)
        {
            // 5xx, red caída, timeout, JSON inesperado
            _logger.LogError(ex, "Error buscando productos por texto en Search-a-licious");
            return ResultadoDeEstado(EstadoBusquedaProducto.ServicioNoDisponible, MensajeServicioNoDisponible);
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

    private static ResultadoBusquedaProductoDto MapearResultado(ExternalProductDto p) => new()
    {
        Barcode = p.Barcode,
        Nombre = p.Name,
        Marca = p.Brand,
        ImagenUrl = p.ImageUrl,
        Categorias = p.Categories ?? new List<string>(),
        Ingredientes = !string.IsNullOrWhiteSpace(p.Ingredients)
            ? new List<string> { p.Ingredients }
            : new List<string>(),
        Alergenos = p.Allergens ?? new List<string>(),
        NutriScore = p.NutriScore,
        NovaGroup = p.NovaGroup,
        Estado = EstadoBusquedaProducto.Encontrado
    };

    // La búsqueda devuelve una página; para informar un estado de error se usa un único ítem con total 0
    private static PagedResultDto<ResultadoBusquedaProductoDto> ResultadoDeEstado(
        EstadoBusquedaProducto estado, string mensaje) =>
        new(0, new List<ResultadoBusquedaProductoDto>
        {
            new() { Estado = estado, MensajeError = mensaje }
        });
}