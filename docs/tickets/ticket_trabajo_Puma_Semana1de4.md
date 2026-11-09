# Issue: [Backend] Dominio, Persistencia y API de Notas Privadas en Ítems de Despensa

**Identificador:** `ticket_trabajo_Puma_Semana1de4`  
**Responsable:** Ignacio Iglesias (`puma189`)  
**Semana:** Semana 1 de 4 (06/11/2026 - 13/11/2026)  
**Requerimientos Cubiertos:** RF-14 (Notas privadas), HU-06, RF-11 (Administrar despensa)  
**Nivel de Dificultad:** Media (Modelado DDD, migración EF Core, DTOs y prueba de integración de aislamiento)  
**Rama asignada:** `feature/27-backend-notas-privadas-despensa`

---

## 1. Contexto y Objetivo
La especificación funcional (RF-14, HU-06) establece: *"Como usuario, quiero escribir notas privadas en productos de mi despensa para recordar decisiones de compra o consumo"*.  
Además, la Guía de Continuidad incluye como **prueba de integración mínima obligatoria**:  
*"RF-10, RF-11 y RF-14: persistir un ítem o nota propios y rechazar la consulta o modificación desde otro usuario; comprobar también el rechazo del acceso anónimo en un endpoint privado."*

Actualmente la entidad `ItemDespensa` carece del atributo y método de negocio para gestionar notas. El objetivo de este ticket es incorporar las notas en el dominio, persistirlas con EF Core mediante una migración y asegurar su aislamiento en la capa de aplicación y tests.

---

## 2. Tareas Técnicas Detalladas

1. **Modificación en Capa de Dominio (`SmartPantry.Domain/Despensa`):**
   - En `ItemDespensa.cs`:
     * Agregar la propiedad privada con getter público:
       ```csharp
       public string? Nota { get; private set; }
       ```
     * Agregar método de negocio:
       ```csharp
       public void ActualizarNota(string? nuevaNota)
       {
           Nota = nuevaNota?.Trim();
       }
       ```
     * Permitir asignar la nota opcional en el constructor si corresponde.
   - Constantes de validación: definir `MaxNotaLength = 500` en `SmartPantryConsts` o clase equivalente.
2. **Mapeo y Migración en EF Core (`SmartPantry.EntityFrameworkCore`):**
   - En `SmartPantryDbContext.cs`, configurar la propiedad:
     ```csharp
     b.Property(x => x.Nota).HasMaxLength(500);
     ```
   - Crear una nueva migración con dotnet-ef:
     ```bash
     dotnet ef migrations add Add_Nota_To_ItemDespensa --project src/SmartPantry.EntityFrameworkCore --startup-project src/SmartPantry.HttpApi.Host
     ```
   - Probar que aplique limpiamente con `SmartPantry.DbMigrator`.
3. **Contratos y DTOs (`SmartPantry.Application.Contracts/Despensa`):**
   - Agregar `public string? Nota { get; set; }` en:
     * `ItemDespensaDto.cs`
     * `CreateItemDespensaDto.cs` (con `[StringLength(500)]`)
     * `UpdateItemDespensaDto.cs` (con `[StringLength(500)]`)
4. **Capa de Aplicación (`SmartPantry.Application/Despensa`):**
   - En `ItemDespensaAppService.cs`:
     * En `GetListAsync`: proyectar la `item.Nota` en el DTO de salida.
     * En `CreateAsync`: setear la nota en la creación del ítem.
     * En `UpdateAsync`: invocar `item.ActualizarNota(input.Nota)`.
5. **Pruebas Automatizadas de Integración Obligatorias (`SmartPantry.Application.Tests`):**
   - En `ItemDespensaAppService_Ownership_Tests.cs`:
     * Test de persistencia de nota propia: crear/actualizar un ítem con nota y verificar que se recupera con su texto.
     * Test de rechazo a otro usuario: verificar que un segundo usuario NO pueda leer la nota ajena ni modificarla (debe arrojar 404 anti-enumeración).
     * Test de acceso anónimo: comprobar que una invocación sin token sea rechazada con 401 Unauthorized.

---

## 3. Criterios de Aceptación (Definition of Done)
- [ ] La columna `Nota` (nvarchar(500) nullable) existe en la tabla de base de datos tras correr DbMigrator.
- [ ] El usuario autenticado puede guardar y modificar notas de hasta 500 caracteres en sus ítems.
- [ ] Se cumple estrictamente el principio de aislamiento: ningún usuario puede ver ni alterar notas ajenas.
- [ ] La prueba de integración obligatoria de la Guía de Continuidad para RF-10/11/14 está implementada y pasa en verde.
- [ ] Los tests compilan y pasan en CI (`dotnet test`).

---

## 4. Independencia / Desacople
- **¿Depende de otros?** NO. Trabaja sobre la entidad `ItemDespensa` en backend y migraciones. Puma define el modelo y DTOs acordados en `CONVENCIONES_Y_CONTRATOS.md` sin bloquear el trabajo de búsqueda de Manuel ni el formulario de alta de Gena.

---

## 5. Instrucciones de PR
1. Crear rama `feature/27-backend-notas-privadas-despensa` desde `main` actualizado.
2. Commits: `feat: agregar propiedad Nota y migracion en ItemDespensa (#27)`.
3. Abrir Pull Request hacia `main`, vincular con `Closes #27` y solicitar revisión a `@manuelperezseg` y `@genaburquet`.
