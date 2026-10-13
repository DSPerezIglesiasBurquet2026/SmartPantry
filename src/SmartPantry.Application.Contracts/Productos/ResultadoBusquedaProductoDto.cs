using System.Collections.Generic;

namespace SmartPantry.Productos;

public class ResultadoBusquedaProductoDto
{
    public string Barcode { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Marca { get; set; }
    public string? ImagenUrl { get; set; }
    public List<string> Categorias { get; set; } = new();
    public List<string> Ingredientes { get; set; } = new();
    public List<string> Alergenos { get; set; } = new();
    public string? NutriScore { get; set; }
    public string? NovaGroup { get; set; }

    // Banderas e información de estado para la aplicación propia
    public bool Encontrado { get; set; }
    public string? MensajeError { get; set; }
}