using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.Despensa;

public enum TipoAdvertencia
{
    VencimientoProximo,
    Vencido
}

public class AdvertenciaVencimiento : FullAuditedAggregateRoot<Guid>
{
    public Guid ItemDespensaId { get; private set; }
    public TipoAdvertencia Tipo { get; private set; }
    public bool Activa { get; private set; }
    public string Mensaje { get; private set; }

    private AdvertenciaVencimiento() { }

    public AdvertenciaVencimiento(Guid id, Guid itemDespensaId, TipoAdvertencia tipo, string mensaje) : base(id)
    {
        ItemDespensaId = itemDespensaId;
        Tipo = tipo;
        SetMensaje(mensaje);
        Activa = true;
    }

    public void Actualizar(string nuevoMensaje, bool activa)
    {
        SetMensaje(nuevoMensaje);
        Activa = activa;
    }

    private void SetMensaje(string mensaje)
    {
        Check.NotNullOrWhiteSpace(mensaje, nameof(mensaje), maxLength: 250);
        Mensaje = mensaje.Trim();
    }
}