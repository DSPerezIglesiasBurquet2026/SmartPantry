using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;

namespace SmartPantry.Productos;

/// <summary>
/// Servicio de Aplicación manual para la gestión de Productos (RF-08)[cite: 1, 2].
/// Hereda de SmartPantryAppService e implementa IProductoAppService.
/// </summary>
[AllowAnonymous] // Acceso temporal anónimo para probar en Swagger sin autenticación
public class ProductoAppService : SmartPantryAppService, IProductoAppService
{
    private readonly IRepository<Producto, Guid> _productoRepository;

    public ProductoAppService(IRepository<Producto, Guid> productoRepository)
    {
        _productoRepository = productoRepository;
    }

    /// <summary>
    /// Crea un producto de forma manual aplicando las reglas del dominio[cite: 1].
    /// </summary>
    public async Task<ProductoDto> CreateAsync(CreateProductoDto input)
    {
        var producto = new Producto(
            GuidGenerator.Create(),
            input.CodigoBarras,
            input.Nombre,
            input.Marca
        );

        await _productoRepository.InsertAsync(producto);
        return ObjectMapper.Map<Producto, ProductoDto>(producto);
    }

    /// <summary>
    /// Consulta un producto por su Id. Si no existe, ABP lanza un 404 mediante EntityNotFoundException[cite: 1].
    /// </summary>
    public async Task<ProductoDto> GetAsync(Guid id)
    {
        var producto = await _productoRepository.GetAsync(id);
        return ObjectMapper.Map<Producto, ProductoDto>(producto);
    }
}