# Issue: [Backend] Observabilidad, Métricas de Integración Externa y API de Administración

**Identificador:** `ticket_trabajo_Manuel_Semana3de4`  
**Responsable:** Manuel Perez (`manuelperezseg`)  
**Semana:** Semana 3 de 4 (21/11/2026 - 27/11/2026)  
**Requerimientos Cubiertos:** HU-05 (Métricas de integración externa), RN-5 (No persistir datos sensibles ni payloads completos), RF-04 (Administración), RNF Observabilidad  
**Nivel de Dificultad:** Alta (Interceptores HTTP DelegatingHandler, persistencia agregada, autorización RBAC de administrador)  
**Rama asignada:** `feature/31-observabilidad-metricas-admin`

---

## 1. Contexto y Objetivo
La especificación funcional (HU-05) establece: *"Como administrador, quiero consultar métricas de la integración externa para detectar errores y uso excesivo"*.  
Además, las reglas técnicas dictan:
- **RN-5:** Las búsquedas externas se registran para métricas, sin persistir datos sensibles de autenticación ni el contenido completo de la respuesta como log.
- **RNF Observabilidad:** Se registran duración, resultado y tipo de cada llamada a la API externa.
- **Guía Técnica Sección 3:** Los registros no deben exponer datos individuales y deben calcularse métricas agregadas para administración.

El objetivo de este ticket es capturar automáticamente el rendimiento y los códigos de respuesta de las peticiones a Open Food Facts y Search-a-licious, persistir métricas de telemetría y exponer una API para administradores.

---

## 2. Tareas Técnicas Detalladas

1. **Entidad de Métricas (`SmartPantry.Domain/Metricas`):**
   - Crear la entidad `RegistroLlamadaExterna` (`CreationAuditedEntity<Guid>`):
     * `TipoOperacion`: string o enum (`CodigoBarras`, `TextoLibre`).
     * `DuracionMs`: long (milisegundos transcurridos).
     * `CodigoHttp`: int (código de respuesta HTTP, ej. 200, 404, 429, 500).
     * `EsExitosa`: bool (`CodigoHttp >= 200 && CodigoHttp < 300`).
     * `FechaUtc`: DateTime.
     * **Prohibido:** No almacenar URLs completas con query params privados, ni headers, ni cuerpo de respuestas.
   - Mapear en `SmartPantryDbContext` con índice en `FechaUtc` y generar migración EF Core `Add_RegistroLlamadaExterna`.
2. **Intercepción HTTP Transparente (`SmartPantry.HttpApi.Host`):**
   - Implementar un `DelegatingHandler` o servicio de registro (`MetricsLoggingHttpMessageHandler`):
     * Utilizar `Stopwatch` para medir la duración exacta de la llamada HTTP.
     * Enviar de forma asíncrona o mediante un `Channel`/servicio en segundo plano el registro a la base de datos para no degradar el tiempo de respuesta al cliente.
   - Registrar el handler en la configuración del cliente HTTP tipado en `SmartPantryHttpApiHostModule`:
     ```csharp
     context.Services.AddHttpClient<IExternalProductCatalogClient, OpenFoodFactsProductCatalogClient>()
            .AddHttpMessageHandler<MetricsLoggingHttpMessageHandler>();
     ```
3. **API y Servicio de Administración (`SmartPantry.Application` & Contracts):**
   - Crear `IMetricaIntegracionAppService` y `MetricaIntegracionAppService`:
     * Decorar con `[Authorize("SmartPantry.Admin")]` o verificar que `CurrentUser.IsInRole("admin")`. Si un usuario regular intenta consultar, el backend debe rechazar con 403 Forbidden.
     * Implementar `Task<ResumenMetricasIntegracionDto> GetResumenAsync(DateTime? desde, DateTime? hasta)`:
       - Total de llamadas realizadas.
       - Tasa de éxito / porcentaje de errores.
       - Tiempo promedio de respuesta en ms.
       - Histograma o desglose por código de estado (200, 404, 429, 500).
       - Desglose por tipo de operación (`CodigoBarras` vs `TextoLibre`).
4. **Pruebas Automatizadas (`SmartPantry.Application.Tests`):**
   - Test unitario del handler: verifica que calcula la duración y emite el registro correcto sin guardar datos sensibles.
   - Test del AppService: cálculo correcto de métricas promedio y porcentajes.
   - Test de autorización: un usuario estándar recibe `AbpAuthorizationException` / 403 Forbidden al llamar al endpoint de métricas.

---

## 3. Criterios de Aceptación (Definition of Done)
- [ ] Cada consulta a Open Food Facts y Search-a-licious queda registrada con su duración y código HTTP.
- [ ] No se registran datos sensibles, tokens ni payloads JSON completos (RN-5).
- [ ] El endpoint `GetResumenAsync` calcula estadísticas consolidadas.
- [ ] Solo usuarios con rol administrador pueden invocar el endpoint.
- [ ] Las pruebas unitarias y de autorización pasan en verde.

---

## 4. Independencia / Desacople
- **¿Depende de otros?** NO. Opera a nivel de infraestructura HTTP y backend administrativo. Define el contrato acordado en `CONVENCIONES_Y_CONTRATOS.md` que Gena consumirá en la interfaz administrativa durante la Semana 4.

---

## 5. Instrucciones de PR
1. Crear rama `feature/31-observabilidad-metricas-admin` desde `main` actualizado.
2. Commits: `feat: interceptor de telemetria y api de metricas para admin (#31)`.
3. Abrir Pull Request hacia `main`, vincular con `Closes #31` y solicitar revisión a `@genaburquet` y `@puma189`.
