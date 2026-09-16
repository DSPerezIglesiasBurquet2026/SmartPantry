using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Productos;

public class ProductoDto : EntityDto<Guid>
{
    public string CodigoBarras { get; set; }
    public string Nombre { get; set; }
    public string Marca { get; set; }
}
