using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Productos;

public class CreateProductoDto
{
    [Required]
    [StringLength(ProductoConsts.MaxCodigoBarrasLength)]
    public string CodigoBarras { get; set; }

    [Required]
    [StringLength(ProductoConsts.MaxNombreLength)]
    public string Nombre { get; set; }

    [Required]
    [StringLength(ProductoConsts.MaxMarcaLength)]
    public string Marca { get; set; }
}