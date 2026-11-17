using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SmartPantry.ClientesExternos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Xunit;
using System.Linq;

namespace SmartPantry.Productos;

public class ProductoAppService_SearchByText_Tests
{
    private readonly IExternalProductCatalogClient _mockExternalClient;
    private readonly IRepository<Producto, Guid> _mockProductoRepository;
    private readonly ProductoAppService _appService;

    public ProductoAppService_SearchByText_Tests()
    {
        _mockExternalClient = Substitute.For<IExternalProductCatalogClient>();
        _mockProductoRepository = Substitute.For<IRepository<Producto, Guid>>();

        _appService = new ProductoAppService(
            _mockProductoRepository, _mockExternalClient, NullLogger<ProductoAppService>.Instance);
    }

    private static BuscarProductoPorTextoInputDto Input(string texto, int skip = 0, int max = 10) =>
        new() { Texto = texto, SkipCount = skip, MaxResultCount = max };

    private void ClienteDevuelve(PagedResultDto<ExternalProductDto> resultado) =>
        _mockExternalClient
            .SearchByTextAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(resultado);

    private void ClienteLanza(Exception ex) =>
        _mockExternalClient
            .SearchByTextAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .ThrowsAsync(ex);

    // ---------- Búsqueda con resultados ----------

    [Fact]
    public async Task BuscarPorTexto_CuandoHayResultados_DebeMapearCamposYConservarTotalPaginado()
    {
        var externos = new List<ExternalProductDto>
        {
            new()
            {
                Barcode = "3017620422003",
                Name = "Nutella",
                Brand = "Ferrero",
                ImageUrl = "https://images.openfoodfacts.org/nutella.jpg",
                Categories = new() { "en:spreads" },
                Ingredients = "Azúcar, aceite de palma, avellanas",
                Allergens = new() { "en:nuts" },
                NutriScore = "e",
                NovaGroup = "4"
            },
            new()
            {
                Barcode = "7790000000011",
                Name = "Nutella B-ready",
                Brand = "Ferrero"
            }
        };
        // 25 coincidencias en total, pero esta página trae solo 2
        ClienteDevuelve(new PagedResultDto<ExternalProductDto>(25, externos));

        var result = await _appService.BuscarPorTextoAsync(Input("nutella"));

        result.TotalCount.ShouldBe(25);
        result.Items.Count.ShouldBe(2);

        var primero = result.Items[0];
        primero.Estado.ShouldBe(EstadoBusquedaProducto.Encontrado);
        primero.Encontrado.ShouldBeTrue();
        primero.Barcode.ShouldBe("3017620422003");
        primero.Nombre.ShouldBe("Nutella");
        primero.Marca.ShouldBe("Ferrero");
        primero.ImagenUrl.ShouldBe("https://images.openfoodfacts.org/nutella.jpg");
        primero.Categorias.ShouldBe(new[] { "en:spreads" });
        primero.Ingredientes.ShouldBe(new[] { "Azúcar, aceite de palma, avellanas" });
        primero.Alergenos.ShouldBe(new[] { "en:nuts" });
        primero.NutriScore.ShouldBe("e");
        primero.NovaGroup.ShouldBe("4");

        result.Items[1].Barcode.ShouldBe("7790000000011");
        result.Items[1].Estado.ShouldBe(EstadoBusquedaProducto.Encontrado);
    }

    [Fact]
    public async Task BuscarPorTexto_DebePasarAlClienteTextoRecortadoYPaginacion()
    {
        ClienteDevuelve(new PagedResultDto<ExternalProductDto>(0, new List<ExternalProductDto>()));

        await _appService.BuscarPorTextoAsync(Input("  leche  ", skip: 20, max: 10));

        await _mockExternalClient.Received(1).SearchByTextAsync("leche", 20, 10);
    }

    // ---------- RF-09: datos incompletos ----------

    [Fact]
    public async Task BuscarPorTexto_CuandoFaltanCamposOpcionales_DebeMantenerNulosSinInventarValores()
    {
        var incompleto = new ExternalProductDto
        {
            Barcode = "1234567890123",
            Name = "Galletitas Genéricas",
            Brand = null,
            ImageUrl = null,
            Ingredients = null,
            NutriScore = null,
            NovaGroup = null
        };
        ClienteDevuelve(new PagedResultDto<ExternalProductDto>(1, new List<ExternalProductDto> { incompleto }));

        var result = await _appService.BuscarPorTextoAsync(Input("galletitas"));

        var item = result.Items.ShouldHaveSingleItem();
        item.Estado.ShouldBe(EstadoBusquedaProducto.Encontrado);
        item.Nombre.ShouldBe("Galletitas Genéricas");
        item.Marca.ShouldBeNull();
        item.ImagenUrl.ShouldBeNull();
        item.Ingredientes.ShouldBeEmpty();
        item.Alergenos.ShouldBeEmpty();
        item.Categorias.ShouldBeEmpty();
        item.NutriScore.ShouldBeNull();
        item.NovaGroup.ShouldBeNull();
    }

    // ---------- Sin resultados ----------

    [Fact]
    public async Task BuscarPorTexto_CuandoNoHayCoincidencias_DebeRetornarListaVaciaSinExcepcion()
    {
        ClienteDevuelve(new PagedResultDto<ExternalProductDto>(0, new List<ExternalProductDto>()));

        var result = await _appService.BuscarPorTextoAsync(Input("xyzxyzxyz"));

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    // ---------- Fallas del proveedor ----------

    [Fact]
    public async Task BuscarPorTexto_CuandoExcedeLimitePeticiones_DebeRetornarLimiteSolicitudes()
    {
        ClienteLanza(new HttpRequestException("límite", null, (HttpStatusCode)429));

        var result = await _appService.BuscarPorTextoAsync(Input("leche"));

        result.TotalCount.ShouldBe(0);
        var item = result.Items.ShouldHaveSingleItem();
        item.Estado.ShouldBe(EstadoBusquedaProducto.LimiteSolicitudes);
        item.Encontrado.ShouldBeFalse();
        item.MensajeError.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BuscarPorTexto_CuandoProveedorDevuelve503_DebeRetornarServicioNoDisponible()
    {
        ClienteLanza(new HttpRequestException(
            "Response status code does not indicate success: 503", null, HttpStatusCode.ServiceUnavailable));

        var result = await _appService.BuscarPorTextoAsync(Input("leche"));

        result.TotalCount.ShouldBe(0);
        var item = result.Items.ShouldHaveSingleItem();
        item.Estado.ShouldBe(EstadoBusquedaProducto.ServicioNoDisponible);
        item.Encontrado.ShouldBeFalse();
        item.MensajeError.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BuscarPorTexto_CuandoHayTimeout_DebeRetornarServicioNoDisponible()
    {
        ClienteLanza(new TaskCanceledException("Timeout"));

        var result = await _appService.BuscarPorTextoAsync(Input("leche"));

        var item = result.Items.ShouldHaveSingleItem();
        item.Estado.ShouldBe(EstadoBusquedaProducto.ServicioNoDisponible);
    }

    [Fact]
    public async Task BuscarPorTexto_CuandoSeCaeLaConexion_DebeRetornarServicioNoDisponible()
    {
        // HttpRequestException sin StatusCode: DNS / conexión rechazada
        ClienteLanza(new HttpRequestException("No such host is known."));

        var result = await _appService.BuscarPorTextoAsync(Input("leche"));

        var item = result.Items.ShouldHaveSingleItem();
        item.Estado.ShouldBe(EstadoBusquedaProducto.ServicioNoDisponible);
    }

    // ---------- Validación de entrada ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    [InlineData("  a  ")]
    public void BuscarPorTexto_CuandoTextoEsBlancoOMuyCorto_DebeFallarLaValidacion(string texto)
    {
        var dto = Input(texto);

        var (esValido, resultados) = Validar(dto);

        esValido.ShouldBeFalse();
        resultados.ShouldContain(v => v.MemberNames.Contains(nameof(BuscarProductoPorTextoInputDto.Texto)));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("leche descremada")]
    [InlineData("  yerba  ")]
    public void BuscarPorTexto_CuandoTextoEsValido_NoDebeFallarLaValidacion(string texto)
    {
        var (esValido, resultados) = Validar(Input(texto));

        esValido.ShouldBeTrue();
        resultados.ShouldBeEmpty();
    }

    private static (bool EsValido, List<ValidationResult> Resultados) Validar(BuscarProductoPorTextoInputDto dto)
    {
        var resultados = new List<ValidationResult>();
        var contexto = new ValidationContext(dto);
        var esValido = Validator.TryValidateObject(dto, contexto, resultados, validateAllProperties: true);
        return (esValido, resultados);
    }
}