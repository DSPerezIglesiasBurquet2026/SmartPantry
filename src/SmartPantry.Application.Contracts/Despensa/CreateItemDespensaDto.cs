using System;

namespace SmartPantry.Despensa;

public class CreateItemDespensaDto
{
	public Guid ProductoId { get; set; }
	public decimal? Cantidad { get; set; }
	public string? Unidad { get; set; }
	public DateTime? FechaVencimiento { get; set; }
}