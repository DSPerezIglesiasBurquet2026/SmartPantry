# Issue: [Entrega] Documentación Final, Auditoría de Migraciones y Preparación del PR Final hacia Prod

**Identificador:** `ticket_trabajo_Puma_Semana4de4`  
**Responsable:** Ignacio Iglesias (`puma189`)  
**Semana:** Semana 4 de 4 (28/11/2026 - 04/12/2026)  
**Requerimientos Cubiertos:** Entregables y Evaluación (Sección 8 Especificación Funcional & Sección 5 Guía de Continuidad)  
**Nivel de Dificultad:** Media (Documentación exhaustiva de ingeniería, auditoría de base de datos SQL Server, trazabilidad de PRs y entrega formal)  
**Rama asignada:** `feature/36-documentacion-final-migraciones-pr-prod`

---

## 1. Contexto y Objetivo
La cátedra exige que la entrega final del trabajo práctico se realice **exclusivamente mediante un Pull Request desde la rama `main` hacia `prod`**.  
Para que dicho PR sea aceptado para defensa antes de la fecha límite (11/12/2026, con meta grupal de entrega el **04/12/2026**), debe acompañarse de:
1. `README.md` actualizado con instrucciones reales de ejecución local, variables, comandos de pruebas con cobertura y credenciales de usuarios de prueba.
2. Registro de decisiones técnicas y adaptaciones de diseño (en Wiki o `DECISIONES_DE_DISENO.md`).
3. Auditoría de base de datos: comprobar que `DbMigrator` aplica todas las migraciones acumuladas desde cero en SQL Server sin errores.
4. Confección del Pull Request formal `main` -> `prod` con el resumen de funcionalidades entregadas, evidencias y enlaces.

---

## 2. Tareas Técnicas Detalladas

1. **Auditoría Integral de Migraciones en SQL Server (`SmartPantry.DbMigrator`):**
   - Eliminar localmente la base de datos de prueba o apuntar a una base limpia `SmartPantry_CleanTest`.
   - Ejecutar `SmartPantry.DbMigrator` y verificar que:
     * Aplica la totalidad de migraciones secuenciales (`2026...`) sin fallas.
     * Siembra los datos iniciales de ABP y los datos de prueba (`DespensaDataSeedContributor`).
     * No quedan advertencias de esquemas desincronizados con el `DbContext`.
2. **Actualización Exhaustiva del `README.md`:**
   - Detallar los requisitos previos exactos (.NET SDK, Node.js, Yarn, SQL Server).
   - Indicar la cadena de conexión por defecto con LocalDB y cómo levantar `DbMigrator`, `HttpApi.Host` y `Angular`.
   - Incluir la sección de **Usuarios y Credenciales de Prueba**:
     * Administrador: `admin` / `adminpass` (para HU-05 y RF-04).
     * Usuario común: `usuario1` / `password` (para Despensa y Preferencias).
   - Documentar los comandos exactos para correr las pruebas y la cobertura:
     ```bash
     # Backend
     dotnet test --settings CodeCoverage.runsettings --collect:"Code Coverage;Format=Cobertura"
     # Frontend
     cd angular && yarn test --watch=false
     ```
   - Añadir sección con **Evidencias de Integración Externa**: endpoints de Open Food Facts v3 y Search-a-licious utilizados, manejo de fallas 429/503 y registro de telemetría.
3. **Documento de Decisiones de Diseño (`docs/DECISIONES_DE_DISENO.md`):**
   - Redactar un informe técnico consolidando las decisiones tomadas durante el proyecto:
     * DDD táctico en ABP y separación de Aggregate Roots (`Producto`, `ItemDespensa`, `PreferenciaUsuario`).
     * Aislamiento estricto y prevención de enumeración mediante HTTP 404 para recursos ajenos.
     * Criterio de RN-2: ausencia de información = "No verificable" (no riesgo cero).
     * Idempotencia del worker de vencimientos con umbral de 3 días en UTC.
     * Decisión sobre ítems consumidos: preservación en historial sin borrado automático (RN-6).
4. **Preparación y Apertura del PR Final `main` -> `prod` (04/12/2026):**
   - Asegurar que `main` contenga todos los PRs de las 4 semanas integrados y aprobados con CI en verde.
   - Crear el Pull Request desde `main` hacia `prod` utilizando la plantilla de entrega.
   - En el cuerpo del PR incluir:
     * Matriz de Historias de Usuario completadas (HU-01 a HU-06).
     * Evidencia de cobertura de tests unitarios (>= 70%).
     * Evidencia de las 4 pruebas de integración obligatorias en verde.
     * Aportes individuales identificables de Manuel, Gena y Puma.
   - Notificar formalmente al equipo docente vía Slack/correo institucional con la URL del PR abierto.

---

## 3. Criterios de Aceptación (Definition of Done)
- [ ] La base de datos se construye desde cero sin errores corriendo `DbMigrator`.
- [ ] El `README.md` contiene instrucciones completas, probadas y funcionales.
- [ ] El documento `DECISIONES_DE_DISENO.md` está versionado en el repositorio.
- [ ] Todos los tests de backend y frontend pasan al 100% en CI.
- [ ] El Pull Request `main` -> `prod` está abierto y formalizado el día 04/12/2026.

---

## 4. Independencia / Desacople
- **¿Depende de otros?** Puma consolida los entregables finales de documentación y auditoría de base de datos una vez que las funcionalidades han convergido en `main`.

---

## 5. Instrucciones de PR
1. Crear rama `feature/36-documentacion-final-migraciones-pr-prod` desde `main` actualizado.
2. Commits: `docs: actualizar README y guia de decisiones para entrega final (#36)`.
3. Abrir Pull Request hacia `main`, vincular con `Closes #36` y solicitar revisión a `@manuelperezseg` y `@genaburquet`.
4. Una vez integrado en `main`, abrir el PR de release `main` -> `prod`.
