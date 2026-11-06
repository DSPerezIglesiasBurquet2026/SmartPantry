using SmartPantry.EntityFrameworkCore;
using Xunit;

namespace SmartPantry.Despensa;

// Va en test/SmartPantry.EntityFrameworkCore.Tests/ (misma carpeta donde está EfCoreSampleAppServiceTests).
// Copiá el atributo [Collection(...)] exactamente como lo tiene esa clase de ejemplo.
[Collection(SmartPantryTestConsts.CollectionDefinitionName)]
public class EfCoreItemDespensaAppService_Ownership_Tests
	: ItemDespensaAppService_Ownership_Tests<SmartPantryEntityFrameworkCoreTestModule>
{
}