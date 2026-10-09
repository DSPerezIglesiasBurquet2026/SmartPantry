using System;
using System.Net.Http;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SmartPantry.ClientesExternos;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SmartPantry.Productos;

public class ProductoAppService_ExternalCatalog_Tests
{
    private readonly IExternalProductCatalogClient _mockExternalClient;
    private readonly IRepository<Producto, Guid> _mockProductoRepository;
    private readonly ProductoAppService _appService;

    public ProductoAppService_ExternalCatalog_Tests()
    {
        // Se crean las sustituciones/mocks de las dependencias (NSubstitute)
        _mockExternalClient = Substitute.For<IExternalProductCatalogClient>();
        _mockProductoRepository = Substitute.For<IRepository<Producto, Guid>>();

        // Instanciamos el servicio inyectando los mocks
        _appService = new ProductoAppService(_mockProductoRepository, _mockExternalClient);
    }

    [Fact]
    public async Task BuscarPorCodigo_CuandoProductoExiste_DebeRetornarDatosMapeados()
    {
        // Arrange
        var barcode = "3017620422003";
        var mockProductDto = new ExternalProductDto
        {
            Barcode = barcode,
            Name = "Nutella",
            Brand = "Ferrero",
            ImageUrl = "https://images.openfoodfacts.org/nutella.jpg",
            Categories = new() { "en:spreads" },
            Ingredients = "Azúcar, aceite de palma, avellanas",
            Allergens = new() { "en:nuts" },
            NutriScore = "e",
            NovaGroup = "4"
        };

        _mockExternalClient.GetByBarcodeAsync(barcode).Returns(mockProductDto);

        // Act
        var result = await _appService.GetBuscarPorCodigoAsync(new BuscarProductoPorCodigoInputDto { Barcode = barcode });

        // Assert
        result.ShouldNotBeNull();
        result.Encontrado.ShouldBeTrue();
        result.Barcode.ShouldBe(barcode);
        result.Nombre.ShouldBe("Nutella");
        result.Marca.ShouldBe("Ferrero");
        result.ImagenUrl.ShouldBe("https://images.openfoodfacts.org/nutella.jpg");
        result.NutriScore.ShouldBe("e");
        result.NovaGroup.ShouldBe("4");
    }

    [Fact]
    public async Task BuscarPorCodigo_CuandoProductoNoExiste_DebeRetornarEncontradoFalse()
    {
        // Arrange
        var barcode = "0000000000000";
        _mockExternalClient.GetByBarcodeAsync(barcode).Returns((ExternalProductDto?)null);

        // Act
        var result = await _appService.GetBuscarPorCodigoAsync(new BuscarProductoPorCodigoInputDto { Barcode = barcode });

        // Assert
        result.ShouldNotBeNull();
        result.Encontrado.ShouldBeFalse();
        result.MensajeError.ShouldBe("Producto no encontrado en el catálogo externo.");
    }

    [Fact]
    public async Task BuscarPorCodigo_CuandoFaltanCamposOpcionales_DebeMantenerNulosSinInventarValores()
    {
        // Arrange (RF-09: Datos incompletos)
        var barcode = "1234567890123";
        var mockProductIncompleto = new ExternalProductDto
        {
            Barcode = barcode,
            Name = "Galletitas Genéricas",
            Brand = null, // Marca no informada
            ImageUrl = null, // Imagen no informada
            Ingredients = null,
            NutriScore = null
        };

        _mockExternalClient.GetByBarcodeAsync(barcode).Returns(mockProductIncompleto);

        // Act
        var result = await _appService.GetBuscarPorCodigoAsync(new BuscarProductoPorCodigoInputDto { Barcode = barcode });

        // Assert
        result.Encontrado.ShouldBeTrue();
        result.Nombre.ShouldBe("Galletitas Genéricas");
        result.Marca.ShouldBeNull();
        result.ImagenUrl.ShouldBeNull();
        result.Ingredientes.ShouldBeEmpty();
        result.NutriScore.ShouldBeNull();
    }

    [Fact]
    public async Task BuscarPorCodigo_CuandoExcedeLimitePeticiones_DebeManejarErrorHTTP429()
    {
        // Arrange
        var barcode = "3017620422003";
        _mockExternalClient.GetByBarcodeAsync(barcode)
            .ThrowsAsync(new HttpRequestException("Se ha alcanzado el límite de solicitudes a Open Food Facts. Intente más tarde."));

        // Act
        var result = await _appService.GetBuscarPorCodigoAsync(new BuscarProductoPorCodigoInputDto { Barcode = barcode });

        // Assert
        result.ShouldNotBeNull();
        result.Encontrado.ShouldBeFalse();
        result.MensajeError.ShouldContain("límite de solicitudes");
    }

    [Fact]
    public async Task BuscarPorCodigo_CuandoServicioExternoNoEstaDisponible_DebeManejarErrorSinLanzarExcepcion()
    {
        // Arrange
        var barcode = "3017620422003";
        _mockExternalClient.GetByBarcodeAsync(barcode)
            .ThrowsAsync(new Exception("Error no controlado del proveedor."));

        // Act
        var result = await _appService.GetBuscarPorCodigoAsync(new BuscarProductoPorCodigoInputDto { Barcode = barcode });

        // Assert
        result.ShouldNotBeNull();
        result.Encontrado.ShouldBeFalse();
        result.MensajeError.ShouldBe("Ocurrió un error al comunicarse con el servicio externo de productos.");
    }
}