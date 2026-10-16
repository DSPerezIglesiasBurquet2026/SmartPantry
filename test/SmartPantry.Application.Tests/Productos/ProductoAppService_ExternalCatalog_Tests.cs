using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
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
        _mockExternalClient = Substitute.For<IExternalProductCatalogClient>();
        _mockProductoRepository = Substitute.For<IRepository<Producto, Guid>>();

        _appService = new ProductoAppService(
            _mockProductoRepository, _mockExternalClient, NullLogger<ProductoAppService>.Instance);
    }

    [Fact]
    public async Task BuscarPorCodigo_CuandoProductoExiste_DebeRetornarDatosMapeados()
    {
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

        var result = await _appService.GetBuscarPorCodigoAsync(new BuscarProductoPorCodigoInputDto { Barcode = barcode });

        result.ShouldNotBeNull();
        result.Estado.ShouldBe(EstadoBusquedaProducto.Encontrado);
        result.Encontrado.ShouldBeTrue();
        result.Barcode.ShouldBe(barcode);
        result.Nombre.ShouldBe("Nutella");
        result.Marca.ShouldBe("Ferrero");
        result.ImagenUrl.ShouldBe("https://images.openfoodfacts.org/nutella.jpg");
        result.NutriScore.ShouldBe("e");
        result.NovaGroup.ShouldBe("4");
    }

    [Fact]
    public async Task BuscarPorCodigo_CuandoProductoNoExiste_DebeRetornarNoEncontrado()
    {
        var barcode = "0000000000000";
        _mockExternalClient.GetByBarcodeAsync(barcode).Returns((ExternalProductDto?)null);

        var result = await _appService.GetBuscarPorCodigoAsync(new BuscarProductoPorCodigoInputDto { Barcode = barcode });

        result.ShouldNotBeNull();
        result.Estado.ShouldBe(EstadoBusquedaProducto.NoEncontrado);
        result.Encontrado.ShouldBeFalse();
        result.MensajeError.ShouldBe("Producto no encontrado en el catálogo externo.");
    }

    [Fact]
    public async Task BuscarPorCodigo_CuandoFaltanCamposOpcionales_DebeMantenerNulosSinInventarValores()
    {
        // RF-09: Datos incompletos
        var barcode = "1234567890123";
        var mockProductIncompleto = new ExternalProductDto
        {
            Barcode = barcode,
            Name = "Galletitas Genéricas",
            Brand = null,
            ImageUrl = null,
            Ingredients = null,
            NutriScore = null
        };

        _mockExternalClient.GetByBarcodeAsync(barcode).Returns(mockProductIncompleto);

        var result = await _appService.GetBuscarPorCodigoAsync(new BuscarProductoPorCodigoInputDto { Barcode = barcode });

        result.Estado.ShouldBe(EstadoBusquedaProducto.Encontrado);
        result.Nombre.ShouldBe("Galletitas Genéricas");
        result.Marca.ShouldBeNull();
        result.ImagenUrl.ShouldBeNull();
        result.Ingredientes.ShouldBeEmpty();
        result.NutriScore.ShouldBeNull();
    }

    [Fact]
    public async Task BuscarPorCodigo_CuandoExcedeLimitePeticiones_DebeRetornarLimiteSolicitudes()
    {
        var barcode = "3017620422003";
        _mockExternalClient.GetByBarcodeAsync(barcode)
            .ThrowsAsync(new HttpRequestException("límite", null, (HttpStatusCode)429));

        var result = await _appService.GetBuscarPorCodigoAsync(new BuscarProductoPorCodigoInputDto { Barcode = barcode });

        result.ShouldNotBeNull();
        result.Estado.ShouldBe(EstadoBusquedaProducto.LimiteSolicitudes);
        result.Encontrado.ShouldBeFalse();
        result.MensajeError.ShouldContain("límite de solicitudes");
    }

    [Fact]
    public async Task BuscarPorCodigo_CuandoProveedorDevuelve503_DebeRetornarServicioNoDisponible()
    {
        var barcode = "3017620422003";
        _mockExternalClient.GetByBarcodeAsync(barcode)
            .ThrowsAsync(new HttpRequestException(
                "Response status code does not indicate success: 503", null, HttpStatusCode.ServiceUnavailable));

        var result = await _appService.GetBuscarPorCodigoAsync(new BuscarProductoPorCodigoInputDto { Barcode = barcode });

        result.ShouldNotBeNull();
        result.Estado.ShouldBe(EstadoBusquedaProducto.ServicioNoDisponible);
        result.Encontrado.ShouldBeFalse();
        result.MensajeError.ShouldBe("Ocurrió un error al comunicarse con el servicio externo de productos.");
    }

    [Fact]
    public async Task BuscarPorCodigo_CuandoFallaLaRed_DebeRetornarServicioNoDisponible()
    {
        var barcode = "3017620422003";
        _mockExternalClient.GetByBarcodeAsync(barcode)
            .ThrowsAsync(new TaskCanceledException("Timeout"));

        var result = await _appService.GetBuscarPorCodigoAsync(new BuscarProductoPorCodigoInputDto { Barcode = barcode });

        result.Estado.ShouldBe(EstadoBusquedaProducto.ServicioNoDisponible);
        result.MensajeError.ShouldBe("Ocurrió un error al comunicarse con el servicio externo de productos.");
    }

    [Fact]
    public void BuscarPorCodigo_CuandoTieneLetras_DebeFallarLaValidacion()
    {
        var dto = new BuscarProductoPorCodigoInputDto { Barcode = "123ABC789" };
        var validationResults = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(dto);

        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(v => v.ErrorMessage == "El código de barras solo puede contener números.");
    }
}