using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Productos;

public class BuscarProductoPorTextoInputDto : PagedResultRequestDto, IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public string Texto { get; set; } = string.Empty;

    // MinLength no alcanza: "  " (dos espacios) lo pasaría. Se valida sobre el texto recortado.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((Texto ?? string.Empty).Trim().Length < 2)
        {
            yield return new ValidationResult(
                "El texto de búsqueda debe tener al menos 2 caracteres.",
                new[] { nameof(Texto) });
        }
    }
}