using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using SmartPantry.Productos;

namespace SmartPantry.Despensa;

[Authorize]
public class ItemDespensaAppService : SmartPantryAppService, IItemDespensaAppService
{
    private readonly IRepository<ItemDespensa, Guid> _itemDespensaRepository;
    private readonly IRepository<Producto, Guid> _productoRepository;

    public ItemDespensaAppService(
        IRepository<ItemDespensa, Guid> itemDespensaRepository,
        IRepository<Producto, Guid> productoRepository)
    {
        _itemDespensaRepository = itemDespensaRepository;
        _productoRepository = productoRepository;
    }

    public async Task<PagedResultDto<ItemDespensaDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        // [Authorize] garantiza usuario autenticado; GetId() lanza si no hubiera Id.
        var currentUserId = CurrentUser.GetId();

        var queryableItems = await _itemDespensaRepository.GetQueryableAsync();
        var queryableProductos = await _productoRepository.GetQueryableAsync();

        // Join con el producto y filtro por propietario: el aislamiento por usuario se aplica en el servidor.
        var query = from item in queryableItems
                    join producto in queryableProductos on item.ProductoId equals producto.Id
                    where item.PropietarioId == currentUserId
                    select new { item, producto };

        var totalCount = await AsyncExecuter.CountAsync(query);

        var queryPaginada = query
            .OrderByDescending(x => x.item.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var queryResult = await AsyncExecuter.ToListAsync(queryPaginada);

        var dtos = queryResult.Select(x => new ItemDespensaDto
        {
            Id = x.item.Id,
            ProductoId = x.item.ProductoId,
            ProductoNombre = x.producto.Nombre,
            Cantidad = x.item.Cantidad,
            Unidad = x.item.Unidad,
            FechaVencimiento = x.item.FechaVencimiento,
            Consumido = x.item.Consumido
        }).ToList();

        return new PagedResultDto<ItemDespensaDto>(totalCount, dtos);
    }

    public async Task UpdateAsync(Guid id, UpdateItemDespensaDto input)
    {
        var item = await GetOwnedItemAsync(id);

        // Las reglas de validación (cantidad no negativa, longitud de unidad, fecha válida)
        // deben estar como atributos en UpdateItemDespensaDto.
        item.Cantidad = input.Cantidad;
        item.Unidad = input.Unidad;
        item.ActualizarFechaVencimiento(input.FechaVencimiento);

        await _itemDespensaRepository.UpdateAsync(item);
    }

    public async Task MarcarComoConsumidoAsync(Guid id)
    {
        var item = await GetOwnedItemAsync(id);

        item.MarcarComoConsumido();
        await _itemDespensaRepository.UpdateAsync(item);
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await GetOwnedItemAsync(id);

        await _itemDespensaRepository.DeleteAsync(item);
    }

    public async Task<ItemDespensaDto> CreateAsync(CreateItemDespensaDto input)
    {
        var currentUserId = CurrentUser.GetId();

        // El producto debe existir; si no, 404 en lugar de un error de clave foránea (500).
        var producto = await _productoRepository.FindAsync(input.ProductoId);
        if (producto == null)
        {
            throw new EntityNotFoundException(typeof(Producto), input.ProductoId);
        }

        var item = new ItemDespensa(
            GuidGenerator.Create(),
            currentUserId,
            input.ProductoId,
            input.FechaVencimiento
        );

        item.Cantidad = input.Cantidad;
        item.Unidad = input.Unidad;

        await _itemDespensaRepository.InsertAsync(item);

        return new ItemDespensaDto
        {
            Id = item.Id,
            ProductoId = item.ProductoId,
            ProductoNombre = producto.Nombre,
            Cantidad = item.Cantidad,
            Unidad = item.Unidad,
            FechaVencimiento = item.FechaVencimiento,
            Consumido = item.Consumido
        };
    }

    /// <summary>
    /// Devuelve el ítem solo si pertenece al usuario autenticado.
    /// Un ítem inexistente y un ítem ajeno responden igual (404): no se revela que el ítem existe.
    /// Este criterio se mantiene en todas las operaciones y debe documentarse en el README/PR.
    /// </summary>
    private async Task<ItemDespensa> GetOwnedItemAsync(Guid id)
    {
        var currentUserId = CurrentUser.GetId();

        var item = await _itemDespensaRepository.FindAsync(
            x => x.Id == id && x.PropietarioId == currentUserId);

        if (item == null)
        {
            throw new EntityNotFoundException(typeof(ItemDespensa), id);
        }

        return item;
    }
}