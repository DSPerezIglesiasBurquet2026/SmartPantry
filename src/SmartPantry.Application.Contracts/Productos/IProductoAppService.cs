using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.Productos;

/// <summary>
/// Interfaz del servicio de aplicación para la entidad Producto (RF-08).
/// Hereda de IApplicationService para que ABP lo registre automáticamente en el contenedor de dependencias.
/// </summary>
public interface IProductoAppService : IApplicationService
{
    /// <summary>
    /// Crea un nuevo producto de forma manual aplicando las reglas de dominio.
    /// </summary>
    Task<ProductoDto> CreateAsync(CreateProductoDto input);

    /// <summary>
    /// Recupera un producto guardado por su identificador único (Guid).
    /// </summary>
    Task<ProductoDto> GetAsync(Guid id);
}