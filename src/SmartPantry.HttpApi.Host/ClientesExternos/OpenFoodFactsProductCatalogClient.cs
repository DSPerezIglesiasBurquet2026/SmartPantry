using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using SmartPantry.ClientesExternos;

namespace SmartPantry.ClientesExternos;

public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    private readonly HttpClient _httpClient;

    public OpenFoodFactsProductCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
    {
        // Petición a la v3 solicitando sólo los campos requeridos por el TP
        var requestUri = $"product/{barcode}.json?fields=code,product_name,brands,image_url,categories_tags,ingredients_text,allergens_tags,nutriscore_grade,nova_group";

        var response = await _httpClient.GetAsync(requestUri);

        // Caso 1: Producto no existe en Open Food Facts
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        // Caso 2: Límite de peticiones superado (HTTP 429)
        if (response.StatusCode == (HttpStatusCode)429)
        {
            throw new HttpRequestException("Se ha alcanzado el límite de solicitudes a Open Food Facts. Intente más tarde.");
        }

        response.EnsureSuccessStatusCode();

        var jsonResult = await response.Content.ReadFromJsonAsync<OpenFoodFactsResponse>();

        if (jsonResult?.Product == null)
        {
            return null;
        }

        // Extraer NovaGroup de forma segura desde JsonElement
        string? novaGroupValue = null;
        if (jsonResult.Product.NovaGroup.HasValue)
        {
            var element = jsonResult.Product.NovaGroup.Value;
            novaGroupValue = element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : element.ToString();
        }

        // Mapeo al DTO interno sin inventar datos si vienen nulos (RF-09)
        return new ExternalProductDto
        {
            Barcode = jsonResult.Product.Code,
            Name = jsonResult.Product.ProductName,
            Brand = jsonResult.Product.Brands,
            ImageUrl = jsonResult.Product.ImageUrl,
            Categories = jsonResult.Product.CategoriesTags ?? new(),
            Ingredients = jsonResult.Product.IngredientsText,
            Allergens = jsonResult.Product.AllergensTags ?? new(),
            NutriScore = jsonResult.Product.NutriscoreGrade,
            NovaGroup = novaGroupValue
        };
    }
}