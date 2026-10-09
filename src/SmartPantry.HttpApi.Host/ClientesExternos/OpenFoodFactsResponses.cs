using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartPantry.ClientesExternos;

internal class OpenFoodFactsResponse
{
    [JsonPropertyName("product")]
    public OpenFoodFactsProductJson? Product { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}

internal class OpenFoodFactsProductJson
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("brands")]
    public string? Brands { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("categories_tags")]
    public List<string>? CategoriesTags { get; set; }

    [JsonPropertyName("ingredients_text")]
    public string? IngredientsText { get; set; }

    [JsonPropertyName("allergens_tags")]
    public List<string>? AllergensTags { get; set; }

    [JsonPropertyName("nutriscore_grade")]
    public string? NutriscoreGrade { get; set; }

    // Cambiado de object? a JsonElement? para manejar enteros, strings o nulos sin lanzar excepción 500
    [JsonPropertyName("nova_group")]
    public JsonElement? NovaGroup { get; set; }
}