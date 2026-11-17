# Issue: [Backend] Motor de Reglas de Advertencias Informativas y Persistencia de Preferencias Alimentarias

**Identificador:** `ticket_trabajo_Manuel_Semana2de4`  
**Responsable:** Manuel Perez (`manuelperezseg`)  
**Semana:** Semana 2 de 4 (14/11/2026 - 20/11/2026)  
**Requerimientos Cubiertos:** RF-03 (Preferencias), RF-12 (Advertencias), RN-1 (Aviso de no diagnóstico), RN-2 (Datos ausentes no son ausencia de riesgo), HU-01, HU-04  
**Nivel de Dificultad:** Muy Alta (DDD Domain Services, reglas de negocio críticas, EF Core Value Objects / colecciones, prueba de integración obligatoria)  
**Rama asignada:** `feature/28-preferencias-motor-advertencias`

---

## 1. Contexto y Objetivo
El núcleo de valor de SmartPantry consiste en contrastar la información de los alimentos con las preferencias y restricciones del usuario (RF-03, RF-12).  
Las reglas de negocio de la cátedra son estrictas:
- **RN-1:** Una preferencia o alerta no equivale a un diagnóstico médico.
- **RN-2:** Una advertencia se genera sólo con la información disponible. Si faltan ingredientes o alérgenos en el producto externo, **el resultado debe indicarse como "no verificable", jamás como ausencia de riesgo**.
- **Prueba de integración mínima obligatoria (Guía de Continuidad):** *"RF-12: con preferencias persistidas, generar la advertencia informativa pertinente al consultar, guardar o agregar un producto, sin convertir datos ausentes en ausencia de riesgo."*

El objetivo de este ticket es crear la entidad de preferencias por usuario, el servicio de dominio desacoplado que evalúa las incompatibilidades y el AppService para gestionar el perfil alimentario.

---

## 2. Tareas Técnicas Detalladas

1. **Entidad y Persistencia de Preferencias (`SmartPantry.Domain/Preferencias`):**
   - Crear Aggregate Root `PreferenciaUsuario` (`FullAuditedAggregateRoot<Guid>`):
     * `UserId`: Guid del usuario autenticado (con índice único en EF Core).
     * `AlergenosVigilados`: Colección de alérgenos (ej: gluten, soja, maní, crustáceos).
     * `IngredientesVigilados`: Colección de ingredientes (ej: azúcar, aceite de palma).
     * `MostrarAlertas`: Booleano para activar/desactivar advertencias en pantalla.
   - Configurar en `SmartPantryDbContext` el mapeo (usando Value Converters o tabla relacional de strings) y crear migración `Add_PreferenciaUsuario`.
2. **Servicio de Dominio Evaluador de Advertencias (`SmartPantry.Domain/Preferencias`):**
   - Crear `IComparadorPreferenciasDomainService` e implementación `ComparadorPreferenciasDomainService`:
     ```csharp
     public EvaluacionAdvertenciaResultado EvaluarCompatibilidad(
         List<string>? ingredientesProducto,
         List<string>? alergenosProducto,
         PreferenciaUsuario preferencias);
     ```
   - **Reglas del evaluador:**
     * Si `preferencias.MostrarAlertas == false`: no genera advertencias.
     * Si `ingredientesProducto` está vacío o es nulo Y `alergenosProducto` está vacío o es nulo: retornar `EstadoVerificacionAlergenos.NoVerificable` con mensaje: *"Información nutricional incompleta: no es posible verificar alérgenos."* (RN-2).
     * Si hay coincidencia exacta o parcial insensible a mayúsculas/acentos con algún elemento vigilado: retornar `EstadoVerificacionAlergenos.Incompatible` listando las coincidencias detectadas.
     * Si se cuenta con información y no hubo coincidencias: retornar `EstadoVerificacionAlergenos.Seguro`.
     * Todo resultado debe incluir la leyenda: *"Aviso: Esta advertencia es solo informativa y no reemplaza el criterio médico."* (RN-1).
3. **Capa de Aplicación (`SmartPantry.Application` & Contracts):**
   - Crear `IPreferenciaUsuarioAppService` y `PreferenciaUsuarioAppService`:
     * `GetMiPreferenciaAsync()`: obtiene o inicializa las preferencias del `CurrentUser.GetId()`.
     * `UpdateMiPreferenciaAsync(UpdatePreferenciaUsuarioDto input)`: actualiza las listas y switch.
   - Enriquecer `ResultadoBusquedaProductoDto` con `EvaluacionAdvertenciaProductoDto` para que al buscar por código o texto el backend ya devuelva la advertencia evaluada según las preferencias del usuario.
4. **Pruebas Unitarias y de Integración Obligatorias:**
   - **Unitarias en Dominio:**
     * Coincidencia detectada genera `Incompatible`.
     * Ausencia de datos en producto genera `NoVerificable` (RN-2).
     * Producto sin alérgenos vigilados genera `Seguro`.
     * Usuario con alertas apagadas no recibe advertencia.
   - **Prueba de Integración Obligatoria RF-12 (`SmartPantry.Application.Tests`):**
     * Con un usuario con preferencias persistidas (ej: "gluten"), consultar un producto con gluten -> verificar advertencia activa.
     * Consultar un producto con alérgenos nulos -> verificar que el resultado sea `NoVerificable` y NO ausencia de riesgo.

---

## 3. Criterios de Aceptación (Definition of Done)
- [ ] Entidad `PreferenciaUsuario` creada y migrada en la base de datos.
- [ ] La regla RN-2 está garantizada: datos ausentes = `NoVerificable`.
- [ ] La leyenda de RN-1 está presente en los mensajes generados.
- [ ] AppService protegido con `[Authorize]` y vinculado a `CurrentUser`.
- [ ] La prueba de integración obligatoria de RF-12 corre y pasa en verde.
- [ ] Cobertura de tests unitarios del nuevo servicio de dominio >= 85%.

---

## 4. Independencia / Desacople
- **¿Depende de otros?** NO. Implementa el módulo de preferencias y el servicio de dominio de forma aislada. Define el contrato acordado en `CONVENCIONES_Y_CONTRATOS.md` que Gena consumirá en la UI en la Semana 3.

---

## 5. Instrucciones de PR
1. Crear rama `feature/28-preferencias-motor-advertencias` desde `main` actualizado.
2. Commits: `feat: implementar modelo de preferencias y motor de advertencias (#28)`.
3. Abrir Pull Request hacia `main`, vincular con `Closes #28` y solicitar revisión a `@genaburquet` y `@puma189`.
