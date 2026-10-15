using System.Collections.Generic;

namespace SmartPantry.ClientesExternos;

public class ExternalProductDto
{
    public string Barcode { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> Categories { get; set; } = new();
    public string? Ingredients { get; set; }
    public List<string> Allergens { get; set; } = new();
    public string? NutriScore { get; set; }
    public string? NovaGroup { get; set; }
}