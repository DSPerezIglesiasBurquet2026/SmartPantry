# Issue: [Frontend] UI de Notas Privadas y Badges de Advertencia de Vencimiento en Despensa

**Identificador:** `ticket_trabajo_Puma_Semana2de4`  
**Responsable:** Ignacio Iglesias (`puma189`)  
**Semana:** Semana 2 de 4 (14/11/2026 - 20/11/2026)  
**Requerimientos Cubiertos:** RF-13 (Vencimientos próximos), RF-14 (Notas privadas), RF-15 (Advertencias de vencimiento), HU-04, HU-06  
**Nivel de Dificultad:** Media (Integración de campo nota en componentes existentes, cálculo visual de fechas y badges de alerta en Angular)  
**Rama asignada:** `feature/30-frontend-notas-y-alertas-despensa`

---

## 1. Contexto y Objetivo
En la Semana 1, Puma implementó la persistencia y la API para las notas privadas (`ItemDespensa.Nota`). Asimismo, el backend ya cuenta desde el TP-08 con el worker periódico que detecta vencimientos próximos e ítems vencidos (`ProcesadorVencimientosManager`).  
Sin embargo, en el frontend de la despensa (`PantryComponent`):
1. No existe ningún control para visualizar o escribir la nota privada de un ítem (RF-14).
2. Los ítems no muestran visualmente ningún badge ni advertencia que alerte al usuario si un ítem está por vencer o vencido (RF-13, HU-04).

El objetivo de este ticket es actualizar la pantalla "Mi Despensa" para que el usuario pueda gestionar sus notas y ver claramente las alertas de vencimiento.

---

## 2. Tareas Técnicas Detalladas

1. **Gestión de Notas Privadas en la Tabla de Despensa (`angular/src/app/pantry`):**
   - En `pantry.component.html`, agregar una columna o panel colapsable para la nota del ítem:
     * Si el ítem tiene nota: mostrar ícono de bloc de notas `fa fa-sticky-note` con tooltip o texto resumido.
     * Si no tiene nota: botón sutil `[+ Agregar nota]`.
   - Permitir editar la nota tanto en el formulario de edición existente como mediante un modal rápido de notas.
   - En el formulario de edición de ítem, agregar el `textarea` para `nota` con validación de hasta 500 caracteres y contador de caracteres restantes.
2. **Badges Visuales de Advertencia de Vencimiento (RF-13, HU-04):**
   - Implementar un método helper en TypeScript o pipe auxiliar para clasificar el estado de vencimiento según la fecha UTC:
     * **Vencido:** Si la fecha de vencimiento es anterior a hoy (en rojo: `badge bg-danger`, ícono `fa fa-exclamation-circle`).
     * **Próximo a vencer:** Si vence dentro de los próximos 3 días (en amarillo/naranja: `badge bg-warning text-dark`, ícono `fa fa-clock`).
     * **Al día:** Si vence en más de 3 días (en verde sutil o texto normal).
     * **Sin fecha:** Si la fecha es nula, mostrar *"No especificada"* (en cursiva, RF-09).
   - Si el ítem está marcado como "Consumido", el badge de vencimiento se atenúa para no generar falsa alarma histórica.
3. **Filtro Rápido en la Despensa:**
   - Incorporar en el encabezado de la tabla un filtro rápido:
     * *Todos*
     * *Próximos a vencer / Vencidos* (solo ítems con advertencia activa)
     * *No consumidos*
4. **Localización e Internacionalización:**
   - Agregar claves en `es.json` y `en.json`: `Pantry:Note`, `Pantry:AddNote`, `Pantry:StatusExpiringSoon`, `Pantry:StatusExpired`, `Pantry:FilterAll`, etc.
5. **Pruebas Unitarias de Frontend (`pantry.component.spec.ts`):**
   - Validar que un ítem vencido renderiza la clase CSS `badge bg-danger`.
   - Validar que un ítem con fecha a 2 días renderiza `badge bg-warning`.
   - Validar que la edición de nota envía el texto a `itemDespensaService.update`.
   - Validar que una nota mayor a 500 caracteres invalida el formulario.

---

## 3. Criterios de Aceptación (Definition of Done)
- [ ] La tabla de despensa muestra visualmente las notas existentes y permite editarlas.
- [ ] Los productos próximos a vencer (umbral 3 días) y vencidos se destacan con badges visuales claros.
- [ ] Se respeta RF-09 para ítems sin fecha de vencimiento.
- [ ] Los filtros rápidos permiten encontrar rápidamente ítems en riesgo de vencimiento.
- [ ] Las pruebas unitarias de Vitest para notas y badges pasan en verde.

---

## 4. Independencia / Desacople
- **¿Depende de otros?** Consume los DTOs de `ItemDespensaDto.Nota` creados por Puma en la Semana 1. No depende del trabajo de búsqueda de Manuel ni del catálogo de Gena.

---

## 5. Instrucciones de PR
1. Crear rama `feature/30-frontend-notas-y-alertas-despensa` desde `main` actualizado.
2. Commits: `feat: agregar visualizacion de notas y badges de vencimiento (#30)`.
3. Abrir Pull Request hacia `main`, vincular con `Closes #30` y solicitar revisión a `@manuelperezseg` y `@genaburquet`.
