using System;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Despensa;

public class UpdateItemDespensaDto
{
    // Validación de servidor: cantidad no negativa[cite: 7]
    [Range(0, double.MaxValue, ErrorMessage = "La cantidad no puede ser negativa.")]
    public decimal? Cantidad { get; set; }

    // Validación de servidor: longitud de la unidad[cite: 7]
    [StringLength(50, ErrorMessage = "La longitud máxima de la unidad es de 50 caracteres.")]
    public string Unidad { get; set; }

    // El tipo DateTime? actúa como validación implícita de fecha válida[cite: 7]
    public DateTime? FechaVencimiento { get; set; }
}