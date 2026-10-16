using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using SmartPantry.Productos;
using SmartPantry.Despensa;

namespace SmartPantry.Data;

public class DespensaDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<Producto, Guid> _productoRepository;
    private readonly IRepository<ItemDespensa, Guid> _itemRepository;
    private readonly IGuidGenerator _guidGenerator;

    public DespensaDataSeedContributor(
        IRepository<Producto, Guid> productoRepository,
        IRepository<ItemDespensa, Guid> itemRepository,
        IGuidGenerator guidGenerator)
    {
        _productoRepository = productoRepository;
        _itemRepository = itemRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        // No duplicar la semilla
        if (await _itemRepository.GetCountAsync() > 0)
        {
            return;
        }

        var hoy = DateTime.UtcNow.Date;                 // criterio UTC, igual que el procesador
        var propietarioFalsoId = _guidGenerator.Create(); // en TP08 no hay ICurrentUser

        // Umbral = 3 días (límite inclusivo)
        await CrearItemAsync("7791234567001", "Leche Descremada", propietarioFalsoId, hoy.AddDays(2));  // dentro del umbral
        await CrearItemAsync("7791234567002", "Yogur Natural", propietarioFalsoId, hoy.AddDays(3));     // día límite
        await CrearItemAsync("7791234567003", "Queso Cremoso", propietarioFalsoId, hoy.AddDays(10));    // fuera del umbral
        await CrearItemAsync("7791234567004", "Pan Lactal", propietarioFalsoId, hoy.AddDays(-1));       // vencido
        await CrearItemAsync("7791234567005", "Arroz", propietarioFalsoId, null);                       // sin fecha
        await CrearItemAsync("7791234567006", "Galletitas", propietarioFalsoId, hoy.AddDays(2), consumido: true); // consumido
    }

    private async Task CrearItemAsync(
        string codigoBarras, string nombre, Guid propietarioId, DateTime? fechaVencimiento, bool consumido = false)
    {
        // Reusar el producto si ya existe (el código de barras es único)
        var producto = await _productoRepository.FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras)
            ?? await _productoRepository.InsertAsync(
                new Producto(_guidGenerator.Create(), codigoBarras, nombre, "Marca Seed"),
                autoSave: true);

        var item = new ItemDespensa(_guidGenerator.Create(), propietarioId, producto.Id, fechaVencimiento);
        if (consumido)
        {
            item.MarcarComoConsumido();
        }

        await _itemRepository.InsertAsync(item);
    }
}