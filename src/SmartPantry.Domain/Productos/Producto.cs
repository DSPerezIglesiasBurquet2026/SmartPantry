using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.Productos;

public class Producto : FullAuditedAggregateRoot<Guid>
{
    public string CodigoBarras { get; private set; }
    public string Nombre { get; private set; }
    public string Marca { get; private set; }

    private Producto() { }

    public Producto(Guid id, string codigoBarras, string nombre, string marca) : base(id)
    {
        SetCodigoBarras(codigoBarras);
        SetNombre(nombre);
        SetMarca(marca);
    }

    public void SetCodigoBarras(string codigoBarras)
    {
        Check.NotNullOrWhiteSpace(codigoBarras, nameof(codigoBarras), ProductoConsts.MaxCodigoBarrasLength);
        CodigoBarras = codigoBarras.Trim();
    }

    public void SetNombre(string nombre)
    {
        Check.NotNullOrWhiteSpace(nombre, nameof(nombre), ProductoConsts.MaxNombreLength);
        Nombre = nombre.Trim();
    }

    public void SetMarca(string marca)
    {
        Check.NotNullOrWhiteSpace(marca, nameof(marca), ProductoConsts.MaxMarcaLength);
        Marca = marca.Trim();
    }
}