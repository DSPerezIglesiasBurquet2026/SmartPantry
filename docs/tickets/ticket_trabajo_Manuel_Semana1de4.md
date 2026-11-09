# Issue: [Backend] Integración de Búsqueda de Productos por Texto con Search-a-licious API

**Identificador:** `ticket_trabajo_Manuel_Semana1de4`  
**Responsable:** Manuel Perez (`manuelperezseg`)  
**Semana:** Semana 1 de 4 (06/11/2026 - 13/11/2026)  
**Requerimientos Cubiertos:** RF-06 (Búsqueda por texto), RF-09 (Datos incompletos), HU-02  
**Nivel de Dificultad:** Alta (Arquitectura HTTP, cliente resiliente, DTOs paginados, manejo de fallas de proveedor)  
**Rama asignada:** `feature/25-searchalicious-busqueda-texto`

---

## 1. Contexto y Objetivo
En el TP-07 se implementó la consulta por código de barras mediante Product Opener API v3 en `IExternalProductCatalogClient`. La especificación funcional v2 y la Guía Técnica exigen que el usuario pueda buscar productos por nombre, marca o categoría en texto libre utilizando **Search-a-licious** (API de Open Food Facts para búsqueda) mediante peticiones HTTP POST, traduciendo sus resultados a DTOs propios sin acoplar la aplicación al JSON externo y manejando de forma controlada códigos 429, 5xx y timeouts.

El objetivo de este ticket es implementar la búsqueda externa por texto en el cliente HTTP, exponerla en `ProductoAppService.BuscarPorTextoAsync` de forma paginada y blindarla con pruebas unitarias usando mocks.

---

## 2. Tareas Técnicas Detalladas

1. **Extensión de la abstracción del cliente externo (`SmartPantry.Application`):**
   - Modificar `IExternalProductCatalogClient` para incorporar:
     ```csharp
     Task<PagedResultDto<ExternalProductDto>> SearchByTextAsync(string query, int skipCount, int maxResultCount);
     ```
2. **Implementación de Search-a-licious (`SmartPantry.HttpApi.Host`):**
   - En `OpenFoodFactsProductCatalogClient.cs` (o cliente dedicado si se prefiere separar):
     * Configurar la llamada HTTP POST hacia el endpoint de Search-a-licious (`/search` o endpoint configurado en `appsettings.json`).
     * Mapear la respuesta externa a una colección paginada de `ExternalProductDto`.
     * Preservar la regla **RF-09**: si faltan propiedades (ingredientes, alérgenos, Nutri-Score, NOVA), dejarlas como `null` o listas vacías, jamás inventar valores.
     * Capturar y transformar excepciones: HTTP 429 (`LimiteSolicitudes`), HTTP 5xx / conexión caída (`ServicioNoDisponible`), 404 / sin coincidencias (`NoEncontrado` o lista vacía).
3. **Exposición en Capa de Aplicación (`SmartPantry.Application` & `SmartPantry.Application.Contracts`):**
   - Crear DTO de entrada: `BuscarProductoPorTextoInputDto` con validaciones (Texto requerido, longitud mínima 2 caracteres, paginación ABP).
   - En `ProductoAppService`, implementar:
     ```csharp
     public async Task<PagedResultDto<ResultadoBusquedaProductoDto>> BuscarPorTextoAsync(BuscarProductoPorTextoInputDto input)
     ```
   - Decorar con `[Authorize]` para garantizar que solo usuarios autenticados consuman la búsqueda (requisito de seguridad TP-09).
4. **Pruebas Automatizadas Unitarias (`SmartPantry.Application.Tests`):**
   - Crear suite de tests `ProductoAppService_SearchByText_Tests.cs` simulando `IExternalProductCatalogClient` con NSubstitute/Moq:
     * Caso búsqueda con resultados: mapeo correcto de campos y conteo total paginado.
     * Caso búsqueda sin resultados: devuelve lista vacía sin arrojar excepción.
     * Caso de falla HTTP 429: respuesta controlada con estado `LimiteSolicitudes`.
     * Caso de error 5xx o timeout: respuesta controlada con estado `ServicioNoDisponible`.
     * Caso de entrada inválida (texto en blanco): falla la validación de FluentValidation/ABP.

---

## 3. Criterios de Aceptación (Definition of Done)
- [ ] La búsqueda por texto consulta Search-a-licious mediante POST paginado.
- [ ] No se exponen estructuras crudas del proveedor externo a Angular ni al contrato público.
- [ ] Las ausencias de datos en la respuesta se manejan como `null` sin inventar texto.
- [ ] Errores 429, 503 o caídas de red retornan respuestas controladas comprensibles para el usuario.
- [ ] Los tests unitarios cubren el 100% de los escenarios del nuevo método y pasan en verde (`dotnet test`).
- [ ] El endpoint se prueba satisfactoriamente en Swagger autenticado con token JWT.

---

## 4. Independencia / Desacople
- **¿Depende de otros?** NO. Trabaja íntegramente en la capa de integración externa y backend de productos. Gena y Puma pueden trabajar en paralelo en sus respectivas tareas sin bloquearse.

---

## 5. Instrucciones de PR
1. Crear rama `feature/25-searchalicious-busqueda-texto` desde `main` actualizado.
2. Hacer commits con formato semántico: `feat: integrar Search-a-licious en cliente externo (#25)`.
3. Abrir Pull Request hacia `main`, incluir `Closes #25` y solicitar revisión a `@genaburquet` y `@puma189`.
