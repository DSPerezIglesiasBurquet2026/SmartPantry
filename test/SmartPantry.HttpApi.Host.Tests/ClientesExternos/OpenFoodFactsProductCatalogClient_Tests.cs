using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace SmartPantry.ClientesExternos;

public class OpenFoodFactsProductCatalogClient_Tests
{
    // Misma URL base que se configura en SmartPantryHttpApiHostModule
    private static readonly Uri BaseUrl = new("https://world.openfoodfacts.org/");

    private static (OpenFoodFactsProductCatalogClient cliente, FakeHttpMessageHandler handler) Crear(
        HttpStatusCode status, string json = "{}")
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var http = new HttpClient(handler) { BaseAddress = BaseUrl };
        return (new OpenFoodFactsProductCatalogClient(http, new ConfigurationBuilder().Build()), handler);
    }

    // ---------- Ruta consultada ----------

    [Fact]
    public async Task ConsultaLaRutaYLosCamposEsperados()
    {
        var (cliente, handler) = Crear(HttpStatusCode.OK, JsonSinDatos);

        await cliente.GetByBarcodeAsync("3017620422003");

        handler.UltimoMetodo.ShouldBe(HttpMethod.Get);
        handler.UltimaUri!.Host.ShouldBe("world.openfoodfacts.org");
        handler.UltimaUri.AbsolutePath.ShouldBe("/product/3017620422003.json");
        handler.UltimaUri.Query.ShouldBe(
            "?fields=code,product_name,brands,image_url,categories_tags,ingredients_text," +
            "allergens_tags,nutriscore_grade,nova_group");
    }

    // ---------- Mapeo ----------

    [Fact]
    public async Task ConDatosCompletos_MapeaTodosLosCampos()
    {
        var (cliente, _) = Crear(HttpStatusCode.OK, JsonCompleto);

        var producto = await cliente.GetByBarcodeAsync("3017620422003");

        producto.ShouldNotBeNull();
        producto.Barcode.ShouldBe("3017620422003");
        producto.Name.ShouldBe("Nutella");
        producto.Brand.ShouldBe("Ferrero");
        producto.ImageUrl.ShouldBe("https://images.openfoodfacts.org/nutella.jpg");
        producto.Categories.ShouldContain("en:spreads");
        producto.Ingredients.ShouldBe("Azúcar, aceite de palma, avellanas");
        producto.Allergens.ShouldContain("en:nuts");
        producto.NutriScore.ShouldBe("e");
        producto.NovaGroup.ShouldBe("4");
    }

    [Fact]
    public async Task SinNombreMarcaNiImagen_DejaLosCamposAusentes()
    {
        var (cliente, _) = Crear(HttpStatusCode.OK, JsonSinDatos);

        var producto = await cliente.GetByBarcodeAsync("3017620422003");

        producto.ShouldNotBeNull();
        producto.Barcode.ShouldBe("3017620422003");
        producto.Name.ShouldBeNull();
        producto.Brand.ShouldBeNull();
        producto.ImageUrl.ShouldBeNull();
        producto.Ingredients.ShouldBeNull();
        producto.NutriScore.ShouldBeNull();
        producto.NovaGroup.ShouldBeNull();
        producto.Categories.ShouldBeEmpty();
        producto.Allergens.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("\"nova_group\": 4", "4")]        // el proveedor lo informa como número
    [InlineData("\"nova_group\": \"4\"", "4")]    // o como texto
    [InlineData("\"nova_group\": null", null)]    // o explícitamente nulo
    [InlineData("\"otro_campo\": 1", null)]       // o no lo informa
    public async Task NovaGroup_SeInterpretaSegunElTipoDelJson(string fragmento, string? esperado)
    {
        var json = $$"""{ "status": 1, "product": { "code": "3017620422003", {{fragmento}} } }""";
        var (cliente, _) = Crear(HttpStatusCode.OK, json);

        var producto = await cliente.GetByBarcodeAsync("3017620422003");

        producto.ShouldNotBeNull();
        producto.NovaGroup.ShouldBe(esperado);
    }

    // ---------- Respuestas HTTP ----------

    [Fact]
    public async Task Http404_DevuelveNull()
    {
        var (cliente, _) = Crear(HttpStatusCode.NotFound);

        (await cliente.GetByBarcodeAsync("0000000000000")).ShouldBeNull();
    }

    [Fact]
    public async Task Http200SinProducto_DevuelveNull()
    {
        var (cliente, _) = Crear(HttpStatusCode.OK, """{ "status": 0, "status_verbose": "product not found" }""");

        (await cliente.GetByBarcodeAsync("0000000000000")).ShouldBeNull();
    }

    [Fact]
    public async Task Http429_LanzaExcepcionDeLimiteDeSolicitudes()
    {
        var (cliente, _) = Crear((HttpStatusCode)429);

        var ex = await Should.ThrowAsync<HttpRequestException>(
            () => cliente.GetByBarcodeAsync("3017620422003"));

        ex.Message.ShouldContain("límite de solicitudes");
        ex.StatusCode.ShouldBe((HttpStatusCode)429);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ErrorDelProveedor_LanzaExcepcionConElCodigoHttp(HttpStatusCode status)
    {
        var (cliente, _) = Crear(status);

        var ex = await Should.ThrowAsync<HttpRequestException>(
            () => cliente.GetByBarcodeAsync("3017620422003"));

        ex.StatusCode.ShouldBe(status);
        ex.Message.ShouldNotContain("límite de solicitudes"); // no se confunde con el 429
    }

    // ---------- JSON de ejemplo ----------

    private const string JsonCompleto = """
        { "status": 1,
          "product": {
            "code": "3017620422003",
            "product_name": "Nutella",
            "brands": "Ferrero",
            "image_url": "https://images.openfoodfacts.org/nutella.jpg",
            "categories_tags": ["en:spreads"],
            "ingredients_text": "Azúcar, aceite de palma, avellanas",
            "allergens_tags": ["en:nuts"],
            "nutriscore_grade": "e",
            "nova_group": 4 } }
        """;

    private const string JsonSinDatos = """
        { "status": 1, "product": { "code": "3017620422003" } }
        """;


    // ---------- SearchByTextAsync ----------

    [Fact]
    public async Task Search_HaceUnPostALaUrlDeBusqueda()
    {
        var (cliente, handler) = Crear(HttpStatusCode.OK, """{ "hits": [], "count": 0 }""");

        await cliente.SearchByTextAsync("leche", 0, 10);

        handler.UltimoMetodo.ShouldBe(HttpMethod.Post);
        handler.UltimaUri!.ToString().ShouldBe("https://search.openfoodfacts.org/search");
    }

    [Fact]
    public async Task Search_Http429_LanzaExcepcionDeLimiteDeSolicitudes()
    {
        var (cliente, _) = Crear((HttpStatusCode)429);

        var ex = await Should.ThrowAsync<HttpRequestException>(
            () => cliente.SearchByTextAsync("leche", 0, 10));

        ex.StatusCode.ShouldBe((HttpStatusCode)429);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Search_ErrorDelProveedor_LanzaExcepcionConElCodigoHttp(HttpStatusCode status)
    {
        var (cliente, _) = Crear(status);

        var ex = await Should.ThrowAsync<HttpRequestException>(
            () => cliente.SearchByTextAsync("leche", 0, 10));

        ex.StatusCode.ShouldBe(status);
    }

    [Fact]
    public async Task Search_Http404_DevuelveListaVacia()
    {
        var (cliente, _) = Crear(HttpStatusCode.NotFound);

        var resultado = await cliente.SearchByTextAsync("leche", 0, 10);

        resultado.TotalCount.ShouldBe(0);
        resultado.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Search_ConHitsDeSearchalicious_MapeaBrandsComoArregloYDescartaNutriScoreUnknown()
    {
        // Forma real de la respuesta: brands es un arreglo y nutriscore_grade puede ser "unknown"
        var json = """
        { "hits": [
            { "code": "8411700026139", "product_name": "Leche Puleva", "brands": ["Puleva"],
              "nutriscore_grade": "c", "nova_group": 1, "allergens_tags": ["en:milk"] },
            { "code": "8433329018685", "product_name": "Leche semidesnatada", "brands": ["El Corte Inglés"],
              "nutriscore_grade": "unknown" } ],
          "count": 10000, "page": 1, "page_size": 2 }
        """;
        var (cliente, _) = Crear(HttpStatusCode.OK, json);

        var resultado = await cliente.SearchByTextAsync("leche", 0, 2);

        resultado.TotalCount.ShouldBe(10000);
        resultado.Items.Count.ShouldBe(2);
        resultado.Items[0].Brand.ShouldBe("Puleva");
        resultado.Items[0].NutriScore.ShouldBe("c");
        resultado.Items[0].NovaGroup.ShouldBe("1");
        resultado.Items[1].NutriScore.ShouldBeNull();   // RF-09: "unknown" no se inventa como grado
        resultado.Items[1].Ingredients.ShouldBeNull();
    }
}