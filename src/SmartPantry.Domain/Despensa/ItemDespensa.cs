using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.Despensa;

public class ItemDespensa : FullAuditedAggregateRoot<Guid>
{
    public Guid PropietarioId { get; private set; }
    public Guid ProductoId { get; private set; }
    public DateTime? FechaVencimiento { get; private set; }
    public bool Consumido { get; private set; }

    private ItemDespensa() { }

    public ItemDespensa(Guid id, Guid propietarioId, Guid productoId, DateTime? fechaVencimiento = null) : base(id)
    {
        PropietarioId = propietarioId;
        ProductoId = productoId;
        FechaVencimiento = fechaVencimiento;
        Consumido = false;
    }

    public void ActualizarFechaVencimiento(DateTime? nuevaFecha)
    {
        FechaVencimiento = nuevaFecha;
    }

    public void MarcarComoConsumido()
    {
        Consumido = true;
    }
}