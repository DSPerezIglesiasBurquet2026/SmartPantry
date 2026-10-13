using System.Threading.Tasks;

namespace SmartPantry.ClientesExternos;

public interface IExternalProductCatalogClient
{
    Task<ExternalProductDto?> GetByBarcodeAsync(string barcode);
}
