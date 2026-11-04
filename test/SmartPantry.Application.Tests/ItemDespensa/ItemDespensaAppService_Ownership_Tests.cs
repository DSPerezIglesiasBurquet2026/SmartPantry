using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using SmartPantry.Productos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Xunit;

namespace SmartPantry.Despensa;

/// <summary>
/// Un usuario no puede consultar ni modificar ítems de la despensa de otro usuario.
/// Criterio documentado: un ítem ajeno responde igual que uno inexistente (404, EntityNotFoundException).
/// Nota: en los tests [Authorize] no se evalúa (AlwaysAllowAuthorizationService); lo que se prueba
/// es el filtro por propietario del AppService, que depende de ICurrentUser.
/// </summary>
public abstract class ItemDespensaAppService_Ownership_Tests<TStartupModule>
    : SmartPantryApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IItemDespensaAppService _itemService;
    private readonly IRepository<Producto, Guid> _productoRepository;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    private static readonly Guid UsuarioA = Guid.NewGuid();
    private static readonly Guid UsuarioB = Guid.NewGuid();

    public ItemDespensaAppService_Ownership_Tests()
    {
        _itemService = GetRequiredService<IItemDespensaAppService>();
        _productoRepository = GetRequiredService<IRepository<Producto, Guid>>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    // ---------- Helpers (datos de prueba: viven solo en el proyecto de tests) ----------

    /// <summary>Ejecuta el bloque como el usuario indicado (simula ICurrentUser).</summary>
    private IDisposable ComoUsuario(Guid userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(AbpClaimTypes.UserId, userId.ToString()),
            new Claim(AbpClaimTypes.UserName, $"usuario-{userId:N}")
        }, "Test");

        return _principalAccessor.Change(new ClaimsPrincipal(identity));
    }

    /// <summary>CreateAsync exige que el Producto exista, así que se siembra uno mínimo.</summary>
    private async Task<Guid> CrearProductoDePruebaAsync()
    {
        var productoId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _productoRepository.InsertAsync(
                new Producto(productoId, "7790000000017", "Producto de prueba", "Marca de prueba"),
                autoSave: true);
        });

        return productoId;
    }

    private async Task<ItemDespensaDto> CrearItemComoAsync(Guid userId, Guid productoId)
    {
        using (ComoUsuario(userId))
        {
            return await _itemService.CreateAsync(new CreateItemDespensaDto
            {
                ProductoId = productoId,
                Cantidad = 2,
                Unidad = "unidades",
                FechaVencimiento = DateTime.Today.AddDays(30)
            });
        }
    }

    private async Task<ItemDespensaDto?> BuscarItemComoAsync(Guid userId, Guid itemId)
    {
        using (ComoUsuario(userId))
        {
            var lista = await _itemService.GetListAsync(new PagedAndSortedResultRequestDto
            {
                MaxResultCount = 50
            });

            return lista.Items.FirstOrDefault(i => i.Id == itemId);
        }
    }

    // ---------- Consultar ----------

    [Fact]
    public async Task Listado_De_Otro_Usuario_No_Incluye_El_Item_Ajeno()
    {
        var productoId = await CrearProductoDePruebaAsync();
        var itemDeA = await CrearItemComoAsync(UsuarioA, productoId);

        (await BuscarItemComoAsync(UsuarioB, itemDeA.Id)).ShouldBeNull();
        (await BuscarItemComoAsync(UsuarioA, itemDeA.Id)).ShouldNotBeNull();
    }

    // ---------- Modificar ----------

    [Fact]
    public async Task Actualizar_Item_Ajeno_Responde_404_Y_No_Modifica_El_Item()
    {
        var productoId = await CrearProductoDePruebaAsync();
        var itemDeA = await CrearItemComoAsync(UsuarioA, productoId);

        using (ComoUsuario(UsuarioB))
        {
            var ex = await Should.ThrowAsync<EntityNotFoundException>(async () =>
                await _itemService.UpdateAsync(itemDeA.Id, new UpdateItemDespensaDto
                {
                    Cantidad = 99,
                    Unidad = "kg",
                    FechaVencimiento = DateTime.Today.AddDays(60)
                }));

            ex.EntityType.ShouldBe(typeof(ItemDespensa));
        }

        var sinCambios = await BuscarItemComoAsync(UsuarioA, itemDeA.Id);
        sinCambios.ShouldNotBeNull();
        sinCambios.Cantidad.ShouldBe(2m);
        sinCambios.Unidad.ShouldBe("unidades");
    }

    [Fact]
    public async Task MarcarComoConsumido_Item_Ajeno_Responde_404_Y_No_Lo_Consume()
    {
        var productoId = await CrearProductoDePruebaAsync();
        var itemDeA = await CrearItemComoAsync(UsuarioA, productoId);

        using (ComoUsuario(UsuarioB))
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
                await _itemService.MarcarComoConsumidoAsync(itemDeA.Id));
        }

        var sinCambios = await BuscarItemComoAsync(UsuarioA, itemDeA.Id);
        sinCambios.ShouldNotBeNull();
        sinCambios.Consumido.ShouldBeFalse();
    }

    [Fact]
    public async Task Eliminar_Item_Ajeno_Responde_404_Y_El_Item_Sigue_Existiendo()
    {
        var productoId = await CrearProductoDePruebaAsync();
        var itemDeA = await CrearItemComoAsync(UsuarioA, productoId);

        using (ComoUsuario(UsuarioB))
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
                await _itemService.DeleteAsync(itemDeA.Id));
        }

        (await BuscarItemComoAsync(UsuarioA, itemDeA.Id)).ShouldNotBeNull();
    }

    // ---------- Criterio documentado: ajeno == inexistente ----------

    [Fact]
    public async Task Item_Ajeno_Y_Item_Inexistente_Responden_Igual()
    {
        var productoId = await CrearProductoDePruebaAsync();
        var itemDeA = await CrearItemComoAsync(UsuarioA, productoId);

        using (ComoUsuario(UsuarioB))
        {
            var exAjeno = await Should.ThrowAsync<EntityNotFoundException>(async () =>
                await _itemService.DeleteAsync(itemDeA.Id));

            var exInexistente = await Should.ThrowAsync<EntityNotFoundException>(async () =>
                await _itemService.DeleteAsync(Guid.NewGuid()));

            exAjeno.GetType().ShouldBe(exInexistente.GetType());
            exAjeno.EntityType.ShouldBe(exInexistente.EntityType);
        }
    }

    // ---------- Control positivo: el propietario sí puede ----------

    [Fact]
    public async Task Propietario_Si_Puede_Modificar_Y_Consumir_Su_Item()
    {
        var productoId = await CrearProductoDePruebaAsync();
        var itemDeA = await CrearItemComoAsync(UsuarioA, productoId);

        using (ComoUsuario(UsuarioA))
        {
            await _itemService.UpdateAsync(itemDeA.Id, new UpdateItemDespensaDto
            {
                Cantidad = 5,
                Unidad = "kg",
                FechaVencimiento = DateTime.Today.AddDays(45)
            });

            await _itemService.MarcarComoConsumidoAsync(itemDeA.Id);
        }

        var modificado = await BuscarItemComoAsync(UsuarioA, itemDeA.Id);
        modificado.ShouldNotBeNull();
        modificado.Cantidad.ShouldBe(5m);
        modificado.Unidad.ShouldBe("kg");
        modificado.Consumido.ShouldBeTrue();
    }
}