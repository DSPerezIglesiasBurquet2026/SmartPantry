using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Productos;

public class BuscarProductoPorCodigoInputDto
{
    [Required(ErrorMessage = "El código de barras es obligatorio.")]
    [StringLength(18, MinimumLength = 3, ErrorMessage = "El código de barras debe tener entre 3 y 18 caracteres.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "El código de barras solo puede contener números.")]
    public string Barcode { get; set; } = string.Empty;
}