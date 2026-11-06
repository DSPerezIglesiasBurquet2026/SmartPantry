using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Despensa;

public class ItemDespensaDto : AuditedEntityDto<Guid>
{
    public Guid ProductoId { get; set; }

    // El documento exige que el listado incluya el nombre del producto
    public string ProductoNombre { get; set; }

    public decimal? Cantidad { get; set; }
    public string Unidad { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public bool Consumido { get; set; }
}