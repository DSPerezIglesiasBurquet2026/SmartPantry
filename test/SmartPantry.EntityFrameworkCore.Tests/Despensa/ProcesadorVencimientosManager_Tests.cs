using System;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Xunit;
using SmartPantry.EntityFrameworkCore;
using SmartPantry.Productos;

namespace SmartPantry.Despensa;

public class ProcesadorVencimientosManager_Tests : SmartPantryEntityFrameworkCoreTestBase
{
    // "Hoy" controlado y en UTC: el mismo criterio que usa el procesador (fechas calendario UTC).
    // Umbral: 3 días, inclusivo.
    private static readonly DateTime FechaActual = new(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc);

    private readonly ProcesadorVencimientosManager _procesador;
    private readonly IRepository<AdvertenciaVencimiento, Guid> _advertenciaRepository;
    private readonly IRepository<ItemDespensa, Guid> _itemRepository;
    private readonly IRepository<Producto, Guid> _productoRepository;

    public ProcesadorVencimientosManager_Tests()
    {
        _procesador = GetRequiredService<ProcesadorVencimientosManager>();
        _advertenciaRepository = GetRequiredService<IRepository<AdvertenciaVencimiento, Guid>>();
        _itemRepository = GetRequiredService<IRepository<ItemDespensa, Guid>>();
        _productoRepository = GetRequiredService<IRepository<Producto, Guid>>();
    }

    // Persiste un Producto y un ItemDespensa (la FK ItemDespensa -> Producto lo exige).
    // La fecha es nullable para poder probar ítems sin fecha de vencimiento.
    private async Task<ItemDespensa> CrearItemPersistidoAsync(DateTime? fechaVencimiento)
    {
        var codigo = Random.Shared.NextInt64(1_000_000_000_000, 9_999_999_999_999).ToString();
        var producto = new Producto(Guid.NewGuid(), codigo, "Producto de prueba", "Marca de prueba");

        var item = new ItemDespensa(Guid.NewGuid(), Guid.NewGuid(), producto.Id, fechaVencimiento);

        await WithUnitOfWorkAsync(async () =>
        {
            await _productoRepository.InsertAsync(producto, autoSave: true); // primero el producto
            await _itemRepository.InsertAsync(item, autoSave: true);         // después el ítem
        });

        return item;
    }

    [Fact]
    public async Task ProcesarItem_DentroDelUmbral_CreaAdvertencia()
    {
        // Arrange: vence el 15, "hoy" es el 13 (quedan 2 días, entra en el umbral de 3)
        var item = await CrearItemPersistidoAsync(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc));
        var fechaActual = new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc);

        // Act
        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, fechaActual));

        // Assert
        await WithUnitOfWorkAsync(async () =>
        {
            var advertencia = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == item.Id);
            advertencia.ShouldNotBeNull();
            advertencia.Activa.ShouldBeTrue();
            advertencia.Tipo.ShouldBe(TipoAdvertencia.VencimientoProximo);
        });
    }

    [Fact]
    public async Task ProcesarItem_DosEjecuciones_IdempotenciaNoDuplicaAdvertencias()
    {
        // Arrange
        var item = await CrearItemPersistidoAsync(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc));
        var fechaActual = new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc);

        // Act: dos ejecuciones, cada una en su propia unidad de trabajo (como el worker)
        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, fechaActual));
        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, fechaActual));

        // Assert: una sola advertencia por ítem
        await WithUnitOfWorkAsync(async () =>
        {
            var total = await _advertenciaRepository.CountAsync(a => a.ItemDespensaId == item.Id);
            total.ShouldBe(1);
        });
    }

    [Fact]
    public async Task ProcesarItem_FueraDelUmbral_NoCreaAdvertencia()
    {
        // Arrange: falta más de un mes
        var item = await CrearItemPersistidoAsync(new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc));
        var fechaActual = new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc);

        // Act
        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, fechaActual));

        // Assert
        await WithUnitOfWorkAsync(async () =>
        {
            var advertencia = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == item.Id);
            advertencia.ShouldBeNull();
        });
    }

    [Fact]
    public async Task ProcesarItem_ItemConsumido_DesactivaAdvertenciaExistente()
    {
        // Arrange
        var item = await CrearItemPersistidoAsync(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc));
        var fechaActual = new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc);

        // Primera pasada: se crea la advertencia (quedan 2 días)
        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, fechaActual));

        // El usuario marca el ítem como consumido
        item.MarcarComoConsumido();

        // Act: segunda pasada del worker
        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, fechaActual));

        // Assert: la advertencia sigue existiendo pero quedó desactivada
        await WithUnitOfWorkAsync(async () =>
        {
            var advertencia = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == item.Id);
            advertencia.ShouldNotBeNull();
            advertencia.Activa.ShouldBeFalse();
        });
    }

    // ---------- Tests nuevos ----------

    [Theory]
    [InlineData(0, true)]    // vence hoy
    [InlineData(2, true)]    // dentro del umbral
    [InlineData(3, true)]    // día límite (el umbral es inclusivo)
    [InlineData(4, false)]   // un día fuera del umbral
    [InlineData(30, false)]  // lejos
    [InlineData(-1, false)]  // vencido: no genera "próximo a vencer"
    public async Task ProcesarItem_SegunDiasRestantes_CreaONoAdvertencia(int dias, bool debeCrear)
    {
        var item = await CrearItemPersistidoAsync(FechaActual.AddDays(dias));

        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, FechaActual));

        await WithUnitOfWorkAsync(async () =>
        {
            var advertencia = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == item.Id);
            if (debeCrear)
            {
                advertencia.ShouldNotBeNull();
                advertencia.Activa.ShouldBeTrue();
            }
            else
            {
                advertencia.ShouldBeNull();
            }
        });
    }

    [Fact]
    public async Task ProcesarItem_SinFecha_NoCreaAdvertencia()
    {
        var item = await CrearItemPersistidoAsync(null);

        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, FechaActual));

        await WithUnitOfWorkAsync(async () =>
        {
            var total = await _advertenciaRepository.CountAsync(a => a.ItemDespensaId == item.Id);
            total.ShouldBe(0);
        });
    }

    [Fact]
    public async Task ProcesarItem_ItemSeVence_DesactivaAdvertencia()
    {
        // Vence el 15: el 13 genera advertencia, el 16 ya está vencido
        var item = await CrearItemPersistidoAsync(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc));

        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, FechaActual));
        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, FechaActual.AddDays(3)));

        await WithUnitOfWorkAsync(async () =>
        {
            var advertencia = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == item.Id);
            advertencia.ShouldNotBeNull();
            advertencia.Activa.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task ProcesarItem_CambioDeFecha_ActualizaDesactivaYReactivaLaMismaAdvertencia()
    {
        var item = await CrearItemPersistidoAsync(FechaActual.AddDays(3));

        // 1. CREACIÓN: quedan 3 días (día límite)
        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, FechaActual));

        Guid idOriginal = Guid.Empty;
        string mensajeOriginal = string.Empty;
        await WithUnitOfWorkAsync(async () =>
        {
            var advertencia = await _advertenciaRepository.SingleAsync(a => a.ItemDespensaId == item.Id);
            advertencia.Activa.ShouldBeTrue();
            idOriginal = advertencia.Id;
            mensajeOriginal = advertencia.Mensaje;
        });

        // 2. ACTUALIZACIÓN: la fecha se acerca (queda 1 día), cambia el mensaje, mismo registro
        item.ActualizarFechaVencimiento(FechaActual.AddDays(1));
        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, FechaActual));
        await WithUnitOfWorkAsync(async () =>
        {
            var advertencia = await _advertenciaRepository.SingleAsync(a => a.ItemDespensaId == item.Id);
            advertencia.Id.ShouldBe(idOriginal);
            advertencia.Activa.ShouldBeTrue();
            advertencia.Mensaje.ShouldNotBe(mensajeOriginal);
        });

        // 3. DESACTIVACIÓN: la fecha se aleja y deja de corresponder
        item.ActualizarFechaVencimiento(FechaActual.AddDays(60));
        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, FechaActual));
        await WithUnitOfWorkAsync(async () =>
        {
            var advertencia = await _advertenciaRepository.SingleAsync(a => a.ItemDespensaId == item.Id);
            advertencia.Id.ShouldBe(idOriginal);
            advertencia.Activa.ShouldBeFalse();
        });

        // 4. REACTIVACIÓN: vuelve a entrar en el umbral, se reutiliza la misma advertencia
        item.ActualizarFechaVencimiento(FechaActual.AddDays(2));
        await WithUnitOfWorkAsync(() => _procesador.ProcesarItemAsync(item, FechaActual));
        await WithUnitOfWorkAsync(async () =>
        {
            var total = await _advertenciaRepository.CountAsync(a => a.ItemDespensaId == item.Id);
            total.ShouldBe(1); // sigue habiendo una sola advertencia por ítem

            var advertencia = await _advertenciaRepository.SingleAsync(a => a.ItemDespensaId == item.Id);
            advertencia.Id.ShouldBe(idOriginal);
            advertencia.Activa.ShouldBeTrue();
        });
    }
}