# SmartPantry — Documento de Normalización, Convenciones y Contratos de Arquitectura

**Proyecto:** SmartPantry — Despensa Inteligente y Perfil Nutricional  
**Cátedra:** Desarrollo de Software — UTN FRCU (Año 2026)  
**Rol:** Product Owner / Arquitectura Técnica  
**Fecha de Inicio del Plan:** 06/11/2026  
**Fecha Objetivo de Finalización:** 04/12/2026 (1 semana antes de la fecha límite de cátedra del 11/12/2026)  
**Integrantes:**
- **Manuel Perez** (`manuelperezseg`) — Lead Developer / Backend Core / Arquitectura
- **Genaro Burquet** (`genaburquet`) — Frontend Developer / UI & Proxies
- **Ignacio Iglesias** (`puma189`) — Fullstack Developer / Features Transversales & Docs

---

## 1. Propósito y Filosofía de Trabajo

Este documento establece las reglas técnicas, contratos de datos y convenciones de trabajo para el sprint final de 4 semanas.  
Su objetivo principal es **eliminar dependencias directas y bloqueantes** entre los tres integrantes, permitiendo que cada uno trabaje en paralelo en ramas independientes sin pisarse ni esperar a que otro termine para poder avanzar.

---

## 2. Matriz de Cobertura de Requerimientos (Trazabilidad)

| Requerimiento Funcional / Historia de Usuario | Descripción | Responsable Asignado | Semana | Ticket Asignado |
|---|---|---|---|---|
| **RF-06 / HU-02** | Búsqueda por texto (Search-a-licious API v3 POST) | Manuel | Semana 1 | `ticket_trabajo_Manuel_Semana1de4` |
| **RF-10 / HU-03** | Frontend: Agregar a despensa (modal, form reactivo, proxy ABP) | Gena | Semana 1 | `ticket_trabajo_Gena_Semana1de4` |
| **RF-14 / HU-06** | Dominio y Persistencia: Notas privadas en Ítems de Despensa | Puma | Semana 1 | `ticket_trabajo_Puma_Semana1de4` |
| **RF-03 / RF-12 / RN-1 / RN-2** | Motor de Reglas de Advertencias Alérgenos vs Preferencias + Domain Service | Manuel | Semana 2 | `ticket_trabajo_Manuel_Semana2de4` |
| **RF-05 / RF-06 / RF-07 / RF-09** | Frontend: Catálogo de Productos (Búsqueda Código/Texto, Detalle y Nutri-Score/NOVA) | Gena | Semana 2 | `ticket_trabajo_Gena_Semana2de4` |
| **RF-13 / RF-14 / RF-15** | Frontend: UI de Notas en Despensa + Badges de Alertas de Vencimiento | Puma | Semana 2 | `ticket_trabajo_Puma_Semana2de4` |
| **HU-05 / RN-5 / RNF Observabilidad** | Backend: Métricas de Integración Externa, Handler Resiliente y API Admin | Manuel | Semana 3 | `ticket_trabajo_Manuel_Semana3de4` |
| **RF-03 / HU-01** | Frontend: Pantalla de Preferencias Alimentarias (Alérgenos vigilados y Switch Alertas) | Gena | Semana 3 | `ticket_trabajo_Gena_Semana3de4` |
| **RNF Usabilidad / Ética** | Frontend: Rediseño Dashboard Home, Avisos Médicos/Éticos y Limpieza Rutas ABP | Puma | Semana 3 | `ticket_trabajo_Puma_Semana3de4` |
| **RNF Calidad / Cobertura** | Backend: Cobertura Unit >= 70% (.runsettings) + 4 Pruebas Integración Obligatorias | Manuel | Semana 4 | `ticket_trabajo_Manuel_Semana4de4` |
| **HU-05 / RF-04** | Frontend: Dashboard de Métricas Externas para Administrador + Pulido UX | Gena | Semana 4 | `ticket_trabajo_Gena_Semana4de4` |
| **Entregables / Auditoría** | Verificación de Migraciones SQL Server, Documentación README/Wiki y PR `main` -> `prod` | Puma | Semana 4 | `ticket_trabajo_Puma_Semana4de4` |

---

## 3. Normalización de Datos y Contratos de API (Evitar Bloqueos)

Para que el frontend y el backend puedan desarrollarse en simultáneo, se acuerdan de antemano las siguientes estructuras y tipos de datos:

### 3.1. DTO de Búsqueda por Texto (Search-a-licious)
- **Endpoint:** `GET /api/app/producto/buscar-por-texto?texto={texto}&skipCount={skipCount}&maxResultCount={maxResultCount}`
- **Entrada:** `BuscarProductoPorTextoInputDto`
  ```csharp
  public class BuscarProductoPorTextoInputDto : PagedResultRequestDto
  {
      [Required]
      [MinLength(2)]
      public string Texto { get; set; } = string.Empty;
  }
  ```
- **Salida:** `PagedResultDto<ResultadoBusquedaProductoDto>`
  Reutiliza `ResultadoBusquedaProductoDto` creado en TP-07 con los campos mapeados (`Barcode`, `Nombre`, `Marca`, `ImagenUrl`, `Categorias`, `Ingredientes`, `Alergenos`, `NutriScore`, `NovaGroup`, `Estado`).

### 3.2. Contrato de Preferencias Alimentarias (RF-03, HU-01)
- **Endpoint:** 
  * `GET /api/app/preferencia-usuario/mi-preferencia`
  * `PUT /api/app/preferencia-usuario/mi-preferencia`
- **DTOs:**
  ```csharp
  public class PreferenciaUsuarioDto
  {
      public List<string> AlergenosVigilados { get; set; } = new();
      public List<string> IngredientesVigilados { get; set; } = new();
      public bool MostrarAlertas { get; set; } = true;
  }

  public class UpdatePreferenciaUsuarioDto
  {
      public List<string> AlergenosVigilados { get; set; } = new();
      public List<string> IngredientesVigilados { get; set; } = new();
      public bool MostrarAlertas { get; set; } = true;
  }
  ```

### 3.3. Contrato de Advertencia Informativa de Producto (RF-12, RN-1, RN-2)
- **Modelo de Advertencia Informativa:**
  ```csharp
  public enum EstadoVerificacionAlergenos
  {
      Seguro = 0,         // No se detectaron coincidencias con la información provista
      Incompatible = 1,   // Se detectó alérgeno o ingrediente vigilado
      NoVerificable = 2   // Faltan ingredientes o alérgenos en Open Food Facts (RN-2)
  }

  public class EvaluacionAdvertenciaProductoDto
  {
      public EstadoVerificacionAlergenos Estado { get; set; }
      public List<string> Coincidencias { get; set; } = new();
      public string Mensaje { get; set; } = string.Empty;
      public bool EsAdvertenciaInformativa => true; // RN-1
  }
  ```

### 3.4. Contrato de Extensión de Ítem de Despensa con Notas (RF-14)
- **Propiedad añadida:** `public string? Nota { get; set; }` (máximo 500 caracteres).
- **Mapeo a DTOs:**
  * `ItemDespensaDto.Nota` (string?)
  * `CreateItemDespensaDto.Nota` (string?, opcional)
  * `UpdateItemDespensaDto.Nota` (string?, opcional)

### 3.5. Contrato de Métricas de Integración Externa (HU-05, RN-5)
- **Endpoint:** `GET /api/app/metrica-integracion-externa/resumen` (Solo Admin)
- **DTO:**
  ```csharp
  public class ResumenMetricasIntegracionDto
  {
      public long TotalLlamadas { get; set; }
      public long LlamadasExitosas { get; set; }
      public long LlamadasFallidas { get; set; }
      public double TiempoPromedioRespuestaMs { get; set; }
      public Dictionary<string, int> ConteoPorCodigoEstado { get; set; } = new();
      public Dictionary<string, int> ConteoPorTipoOperacion { get; set; } = new(); // "Codigo", "Texto"
  }
  ```

---

## 4. Reglas de Negocio y Principios Innegociables

1. **Aislamiento Estricto por Usuario (RN-3):**
   - Siempre obtener la identidad del usuario mediante `CurrentUser.GetId()`.
   - Si un usuario consulta, edita o elimina un ítem o nota ajeno, el backend **debe responder HTTP 404 (Not Found)** para evitar la enumeración de recursos privados.
2. **Ética Médica y No Diagnóstico (RN-1):**
   - Cualquier pantalla con advertencias o perfil debe mostrar visiblemente:  
     `"Esta aplicación es informativa y no brinda diagnóstico ni indicación médica."`
3. **Manejo de Información Incompleta (RN-2 & RF-09):**
   - Nunca inventar valores. Si un producto externo no tiene NutriScore, NOVA o ingredientes, se debe reflejar como `null` y mostrar en pantalla `"No especificado"` (en cursiva).
   - Si un producto carece de lista de ingredientes o alérgenos en el catálogo externo, el análisis de advertencia **debe catalogarse como `NoVerificable`**, nunca como seguro.
4. **Protección de Secretos y Logs (RN-5 & RNF Seguridad):**
   - No versionar contraseñas personales ni tokens.
   - En el logging de llamadas HTTP externas, registrar únicamente: Tipo de operación, duración (ms) y código de respuesta HTTP. **Queda prohibido registrar cuerpos completos de respuestas o tokens**.
5. **Criterio de Fechas:**
   - Base de datos y workers: siempre en `UTC` (`DateTime.UtcNow`).
   - Frontend Angular: siempre formatear con pipe local del navegador `item.fechaVencimiento | date:'shortDate'`.

---

## 5. Protocolo de Trabajo Git & Code Review

Para cumplir con la pauta de la cátedra de trazabilidad individual y commits verificables:
1. **Ramas:**
   - Crear desde `main` actualizado: `feature/<numero-issue>-<descripcion-corta>`.
2. **Commits:**
   - Mensajes semánticos: `feat: agregar formulario modal en despensa (#N)`, `test: añadir pruebas unitarias (#N)`.
3. **Pull Requests:**
   - Abrir PR hacia `main` usando la plantilla de PR.
   - Incluir `Closes #N`.
   - Asignar a los compañeros como reviewers.
   - **Regla:** Al menos 1 aprobación de otro integrante y checks de CI en verde antes de hacer merge.
4. **Sin push directo:** Prohibido realizar push directo a `main`, `stg` o `prod`.
