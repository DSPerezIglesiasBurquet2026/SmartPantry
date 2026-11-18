# Issue: [Frontend] Panel de Métricas de Integración para Administrador y Pulido de Estados UX

**Identificador:** `ticket_trabajo_Gena_Semana4de4`  
**Responsable:** Genaro Burquet (`genaburquet`)  
**Semana:** Semana 4 de 4 (28/11/2026 - 04/12/2026)  
**Requerimientos Cubiertos:** HU-05 (Métricas de integración externa), RF-04 (Administración), RNF Usabilidad  
**Nivel de Dificultad:** Media (Componente administrativo con tarjetas KPI, tablas de desglose, guards de rol y auditoría UX)  
**Rama asignada:** `feature/35-frontend-metricas-admin-pulido-ux`

---

## 1. Contexto y Objetivo
La historia de usuario HU-05 establece: *"Como administrador, quiero consultar métricas de la integración externa para detectar errores y uso excesivo"*.  
En la Semana 3, Manuel implementó la persistencia y la API administrativa `GET /api/app/metrica-integracion-externa/resumen`.  
El objetivo de este ticket es:
1. Construir la pantalla de visualización de métricas `/admin/metrics` restringida exclusivamente a usuarios con rol administrador.
2. Presentar tarjetas con indicadores clave (Total llamadas, Tasa de fallas, Latencia promedio en ms) y tablas de desglose por código HTTP y tipo de consulta.
3. Realizar una auditoría integral de Usabilidad en todo el frontend Angular: garantizar que todas las vistas manejen de forma impecable los 4 estados canónicos (Carga, Resultados, Vacío y Error con botón de reintento).

---

## 2. Tareas Técnicas Detalladas

1. **Creación del Componente de Métricas (`angular/src/app/admin-metrics`):**
   - Crear componente standalone `AdminMetricsComponent` con ruta `/admin/metrics`.
   - Proteger la ruta mediante `authGuard` y un guard de rol o permisos que impida el acceso a usuarios comunes.
   - En `route.provider.ts`, mostrar el enlace *"Métricas de Integración"* solo si el usuario actual posee rol `admin` (utilizando `permissionService` o `currentUser.roles`).
2. **Diseño Visual del Dashboard de Métricas:**
   - **Tarjetas KPI (KPI Cards):**
     * *Total de Peticiones Externas:* Contador con ícono `fa fa-globe`.
     * *Tasa de Éxito:* Porcentaje coloreado (verde si > 95%, amarillo si entre 80-95%, rojo si < 80%).
     * *Errores y Límites:* Contador de llamadas fallidas (especialmente códigos 429 y 5xx).
     * *Latencia Promedio:* Tiempo medio de respuesta en milisegundos (`ms`).
   - **Tablas de Desglose:**
     * Desglose por Código HTTP (200 OK, 404 No encontrado, 429 Rate limit, 500/503 Fallas del proveedor).
     * Desglose por Tipo de Operación (Búsqueda por Código de Barras vs Búsqueda por Texto Libre).
   - Selector de rango de fechas básico (Últimas 24 horas, Última semana, Todo el historial) y botón `[Actualizar Métricas]`.
3. **Auditoría Integral de Estados UX en Angular:**
   - Revisar todas las pantallas del sistema (`PantryComponent`, `CatalogComponent`, `PreferencesComponent`, `AdminMetricsComponent`):
     * *Carga:* Ninguna pantalla debe titilar ni mostrar datos vacíos durante la petición; debe haber spinner o skeleton.
     * *Vacío:* Mensajes amigables con íconos descriptivos cuando no hay ítems en despensa o no hay resultados de búsqueda.
     * *Error:* Cartel de alerta con descripción amigable y botón de reintento; jamás mostrar errores técnicos crudos al usuario.
     * *Formularios:* Validación reactiva sin permitir envíos vacíos o datos fuera de rango.
4. **Localización e Internacionalización:**
   - Agregar claves en `es.json` y `en.json`: `Metrics:Title`, `Metrics:TotalRequests`, `Metrics:SuccessRate`, `Metrics:AvgLatency`, `Metrics:HttpCodes`, `Metrics:Operations`, etc.
5. **Pruebas Unitarias de Frontend (`admin-metrics.component.spec.ts`):**
   - Tests en Vitest:
     * El componente carga y renderiza las tarjetas con los datos del servicio.
     * Si el servicio devuelve error (ej. usuario no admin), se renderiza el estado de error correspondiente.
     * El botón de refrescar vuelve a solicitar los datos al servicio.

---

## 3. Criterios de Aceptación (Definition of Done)
- [ ] La pantalla `/admin/metrics` está disponible y protegida solo para administradores.
- [ ] Las métricas de total, fallas, latencia y códigos de error se visualizan claramente.
- [ ] Se revisaron y pulieron los 4 estados (carga, vacío, error, datos) en todas las pantallas de Angular.
- [ ] La suite de pruebas de Vitest para métricas pasa en verde.
- [ ] La compilación de Angular pasa sin advertencias (`yarn build`).

---

## 4. Independencia / Desacople
- **¿Depende de otros?** Consume la API de métricas implementada por Manuel en la Semana 3. Gena puede mockear la respuesta del servicio en sus pruebas unitarias para avanzar sin bloqueos.

---

## 5. Instrucciones de PR
1. Crear rama `feature/35-frontend-metricas-admin-pulido-ux` desde `main` actualizado.
2. Commits: `feat: panel de metricas de integracion para admin (#35)`.
3. Abrir Pull Request hacia `main`, vincular con `Closes #35` y solicitar revisión a `@manuelperezseg` y `@puma189`.
