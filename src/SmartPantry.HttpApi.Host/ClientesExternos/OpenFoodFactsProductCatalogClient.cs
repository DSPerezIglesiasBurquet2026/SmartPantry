using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.ClientesExternos;

public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly string _searchUrl;

    private static readonly string[] SearchFields =
    {
        "code", "product_name", "brands", "image_url", "categories_tags",
        "ingredients_text", "allergens_tags", "nutriscore_grade", "nova_group"
    };

    public OpenFoodFactsProductCatalogClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        // Host distinto al de Product Opener: URL absoluta, configurable
        _searchUrl = configuration["OpenFoodFacts:SearchaliciousUrl"]
                     ?? "https://search.openfoodfacts.org/search";
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
            throw new HttpRequestException(
                "Se ha alcanzado el límite de solicitudes a Open Food Facts. Intente más tarde.",
                inner: null,
                statusCode: (HttpStatusCode)429);
        }

        response.EnsureSuccessStatusCode();

        var jsonResult = await response.Content.ReadFromJsonAsync<OpenFoodFactsResponse>();

        if (jsonResult?.Product == null)
        {
            return null;
        }

        // Mapeo al DTO interno sin inventar datos si vienen nulos (RF-09)
        return MapToDto(jsonResult.Product);
    }


    public async Task<PagedResultDto<ExternalProductDto>> SearchByTextAsync(
        string query, int skipCount, int maxResultCount)
    {
        maxResultCount = Math.Clamp(maxResultCount, 1, 100);
        skipCount = Math.Max(skipCount, 0);

        var body = new
        {
            q = query,
            page = (skipCount / maxResultCount) + 1,   // ABP: skip → Search-a-licious: página (base 1)
            page_size = maxResultCount,
            langs = new[] { "es", "en" },
            fields = SearchFields
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(_searchUrl, body);
        }
        catch (TaskCanceledException ex)   // timeout del HttpClient
        {
            throw new HttpRequestException(
                "Tiempo de espera agotado al consultar Search-a-licious.",
                ex, HttpStatusCode.GatewayTimeout);
        }
        // Conexión caída: HttpRequestException con StatusCode == null, se propaga tal cual

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new PagedResultDto<ExternalProductDto>(0, new List<ExternalProductDto>());

            if (response.StatusCode == (HttpStatusCode)429)
                throw new HttpRequestException(
                    "Se ha alcanzado el límite de solicitudes a Open Food Facts. Intente más tarde.",
                    inner: null, statusCode: (HttpStatusCode)429);

            response.EnsureSuccessStatusCode(); // 5xx → HttpRequestException con StatusCode

            var result = await response.Content.ReadFromJsonAsync<SearchaliciousResponse>();

            var items = (result?.Hits ?? new())
                .Select(MapHitToDto)
                .ToList();

            return new PagedResultDto<ExternalProductDto>(result?.Count ?? 0, items);
        }
    }

    // Extraído de GetByBarcodeAsync: úsalo también allá (return MapToDto(jsonResult.Product);)
    private static ExternalProductDto MapToDto(OpenFoodFactsProductJson p)   // ← poné el nombre real de tu clase
    {
        string? novaGroupValue = null;
        if (p.NovaGroup.HasValue)
        {
            var element = p.NovaGroup.Value;
            novaGroupValue = element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : element.ToString();
        }

        return new ExternalProductDto
        {
            Barcode = p.Code,
            Name = p.ProductName,
            Brand = p.Brands,
            ImageUrl = p.ImageUrl,
            Categories = p.CategoriesTags ?? new(),
            Ingredients = p.IngredientsText,
            Allergens = p.AllergensTags ?? new(),
            NutriScore = p.NutriscoreGrade,
            NovaGroup = novaGroupValue
        };
    }

    private static ExternalProductDto MapHitToDto(SearchaliciousHit h) => new()
    {
        Barcode = h.Code,
        Name = h.ProductName,
        Brand = TextoOpcional(h.Brands),
        ImageUrl = h.ImageUrl,
        Categories = h.CategoriesTags ?? new(),
        Ingredients = TextoOpcional(h.IngredientsText),
        Allergens = h.AllergensTags ?? new(),
        NutriScore = NutriScoreValido(h.NutriscoreGrade),
        NovaGroup = h.NovaGroup is { } e
            ? (e.ValueKind == JsonValueKind.String ? e.GetString() : e.ToString())
            : null
    };

    // Search-a-licious devuelve algunos campos como arreglo de strings (brands) y la API
    // de barcode como string: se acepta ambas formas.
    private static string? TextoOpcional(JsonElement? e)
    {
        if (e is not { } el) return null;
        return el.ValueKind switch
        {
            JsonValueKind.String => string.IsNullOrWhiteSpace(el.GetString()) ? null : el.GetString(),
            JsonValueKind.Array => el.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(x.GetString()))
                .Select(x => x.GetString()!)
                .ToList() is { Count: > 0 } l ? string.Join(", ", l) : null,
            _ => null
        };
    }

    // RF-09: "unknown" / "not-applicable" no son un grado real
    private static string? NutriScoreValido(string? grade) =>
        grade is "a" or "b" or "c" or "d" or "e" ? grade : null;

    private sealed class SearchaliciousResponse
    {
        [JsonPropertyName("hits")] public List<SearchaliciousHit>? Hits { get; set; }
        [JsonPropertyName("count")] public long Count { get; set; }
    }

    private sealed class SearchaliciousHit
    {
        [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;
        [JsonPropertyName("product_name")] public string? ProductName { get; set; }
        [JsonPropertyName("brands")] public JsonElement? Brands { get; set; }
        [JsonPropertyName("image_url")] public string? ImageUrl { get; set; }
        [JsonPropertyName("categories_tags")] public List<string>? CategoriesTags { get; set; }
        [JsonPropertyName("ingredients_text")] public JsonElement? IngredientsText { get; set; }
        [JsonPropertyName("allergens_tags")] public List<string>? AllergensTags { get; set; }
        [JsonPropertyName("nutriscore_grade")] public string? NutriscoreGrade { get; set; }
        [JsonPropertyName("nova_group")] public JsonElement? NovaGroup { get; set; }
    }
}