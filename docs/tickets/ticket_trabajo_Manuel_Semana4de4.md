# Issue: [Backend] Cobertura Unitaria >= 70% (.runsettings) y Verificación de las 4 Pruebas de Integración Obligatorias

**Identificador:** `ticket_trabajo_Manuel_Semana4de4`  
**Responsable:** Manuel Perez (`manuelperezseg`)  
**Semana:** Semana 4 de 4 (28/11/2026 - 04/12/2026)  
**Requerimientos Cubiertos:** RNF Calidad, RF-08, RF-10, RF-11, RF-12, RF-14, RF-15  
**Nivel de Dificultad:** Alta (Métricas de cobertura de código con .NET Cobertura, filtros XML, pruebas de integración sin red, idempotencia de workers)  
**Rama asignada:** `feature/34-cobertura-runsettings-pruebas-obligatorias`

---

## 1. Contexto y Objetivo
La Guía de Continuidad y Organización (versión 2) establece dos condiciones estrictas para la aprobación del Trabajo Práctico:
1. **Cobertura unitaria >= 70%:**  
   *"Los tests unitarios deben alcanzar al menos el 70 % de cobertura de líneas del código propio de Domain y Application que implementa reglas o casos de uso. Excluir del cálculo DTOs, migraciones, código generado y clases dedicadas sólo a configuración o arranque."*
2. **Las 4 Pruebas de Integración Mínimas Obligatorias:**
   - **RF-08:** Guardar y recuperar un producto, y evitar otro registro para el mismo código de barras.
   - **RF-10, RF-11 y RF-14:** Persistir un ítem o nota propios y rechazar la consulta o modificación desde otro usuario (404); comprobar rechazo del acceso anónimo (401).
   - **RF-12:** Con preferencias persistidas, generar la advertencia informativa pertinente al consultar, guardar o agregar un producto, sin convertir datos ausentes en ausencia de riesgo.
   - **RF-15:** Ejecutar dos veces el worker sobre ítems próximos a vencer y comprobar que las advertencias se crean o actualizan sin duplicados (idempotencia).

El objetivo de este ticket es configurar el archivo `CodeCoverage.runsettings`, auditar y alcanzar la cobertura exigida y asegurar que la suite de pruebas de integración corra de forma impecable sin conexión a Internet.

---

## 2. Tareas Técnicas Detalladas

1. **Configuración del Recolector de Cobertura (`CodeCoverage.runsettings`):**
   - Crear en la raíz de la solución el archivo `CodeCoverage.runsettings` siguiendo los lineamientos de Microsoft y la cátedra:
     * `ModulePaths/Include`: ensamblados propios `SmartPantry.Domain.dll` y `SmartPantry.Application.dll`.
     * `IncludeTestAssembly`: `false`.
     * `Sources/Exclude`: filtrar DTOs (`*Dto.cs`), migraciones (`*/Migrations/*`), código generado, módulos (`*Module.cs`) y configuraciones.
   - Probar el comando de recolección:
     ```bash
     dotnet test test/SmartPantry.Application.Tests/SmartPantry.Application.Tests.csproj --settings CodeCoverage.runsettings --collect:"Code Coverage;Format=Cobertura"
     ```
   - Verificar que el reporte `*.cobertura.xml` supere el 70% de líneas cubiertas (`lines-covered / lines-valid >= 0.70`).
2. **Desarrollo de Tests Unitarios Faltantes para superar el 70%:**
   - Agregar pruebas unitarias en `SmartPantry.Domain.Tests` y `SmartPantry.Application.Tests` cubriendo ramas de código (branches), validaciones de frontera y casos de error.
3. **Consolidación de las 4 Pruebas de Integración Obligatorias:**
   - **Suite RF-08 (`SmartPantry.Application.Tests/Productos`):**
     * Guardar un producto nuevo y recuperarlo por ID.
     * Intentar insertar un segundo producto con el mismo `CodigoBarras` y comprobar que la base/repositorio lo rechaza (restricción de índice único).
   - **Suite RF-10, RF-11, RF-14 (`SmartPantry.Application.Tests/Despensa`):**
     * Persistir ítem con nota con el usuario A.
     * Con el usuario B intentar consultar, editar o eliminar dicho ítem y verificar respuesta HTTP 404.
     * Con usuario anónimo, verificar rechazo HTTP 401.
   - **Suite RF-12 (`SmartPantry.Application.Tests/Preferencias`):**
     * Probar evaluación con producto con alérgeno vigilado -> genera advertencia.
     * Probar evaluación con producto sin datos de alérgenos -> genera estado `NoVerificable` (no riesgo cero).
   - **Suite RF-15 (`SmartPantry.EntityFrameworkCore.Tests/Despensa`):**
     * Sembrar un ítem con vencimiento en 2 días.
     * Ejecutar el worker/procesador una vez: verifica que crea la advertencia `VencimientoProximo`.
     * Ejecutar el worker/procesador una segunda vez: verifica que **no se duplica** el registro en la base de datos (se mantiene exactamente una advertencia activa con el mismo Id o actualizada).
4. **Script de Verificación Rápida:**
   - Crear script `verificar-cobertura.ps1` en la raíz para que cualquier integrante pueda correr la suite y ver el porcentaje consolidado antes de entregar.

---

## 3. Criterios de Aceptación (Definition of Done)
- [ ] Archivo `CodeCoverage.runsettings` configurado y versionado en la raíz del repositorio.
- [ ] La cobertura de líneas en Domain y Application supera el 70%.
- [ ] Las 4 suites de pruebas de integración obligatorias están implementadas y pasan al 100% en verde.
- [ ] Los tests de integración no dependen de red ni de servidores externos (utilizan SQLite/LocalDB y mocks).
- [ ] El comando `dotnet test` finaliza con 0 errores y 0 warnings críticos.

---

## 4. Independencia / Desacople
- **¿Depende de otros?** Integra y consolida el código de los features desarrollados en las semanas 1, 2 y 3. Manuel tiene la visión global para cerrar la calidad del backend sin interferir en la entrega de documentación de Puma ni en la UI de métricas de Gena.

---

## 5. Instrucciones de PR
1. Crear rama `feature/34-cobertura-runsettings-pruebas-obligatorias` desde `main` actualizado.
2. Commits: `test: configurar runsettings y completar cobertura 70% (#34)`.
3. Abrir Pull Request hacia `main`, vincular con `Closes #34` y solicitar revisión a `@genaburquet` y `@puma189`.
