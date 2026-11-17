# Issue: [Frontend] Flujo de "Agregar Ítem a la Despensa" en Angular

**Identificador:** `ticket_trabajo_Gena_Semana1de4`  
**Responsable:** Genaro Burquet (`genaburquet`)  
**Semana:** Semana 1 de 4 (06/11/2026 - 13/11/2026)  
**Requerimientos Cubiertos:** RF-10 (Agregar a despensa), HU-03  
**Nivel de Dificultad:** Media (Formularios reactivos, modales de Bootstrap/Ngb, integración con proxy ABP y tests con Vitest)  
**Rama asignada:** `feature/26-frontend-agregar-item-despensa`

---

## 1. Contexto y Objetivo
En el TP-10 se implementó la pantalla "Mi despensa" (`PantryComponent`) permitiendo listar, editar, consumir y eliminar ítems. Sin embargo, **el usuario actualmente no tiene forma de agregar un nuevo ítem a su despensa desde la interfaz**.  
En el backend, el endpoint `POST /api/app/item-despensa` (`ItemDespensaAppService.CreateAsync`) ya fue desarrollado y está funcionando, pero el proxy generado de Angular `ItemDespensaService` omitió el método `create`, y no existe el botón ni modal correspondiente en la vista.

El objetivo de este ticket es incorporar en Angular el flujo completo para agregar un producto existente a la despensa del usuario autenticado, con su formulario validado, llamada al servicio y pruebas unitarias.

---

## 2. Tareas Técnicas Detalladas

1. **Actualización de Proxies de Angular (`angular/src/app/proxy/despensa`):**
   - Incorporar en `item-despensa.service.ts` el método `create`:
     ```typescript
     create = (input: CreateItemDespensaDto, config?: Partial<Rest.Config>) =>
       this.restService.request<any, ItemDespensaDto>({
         method: 'POST',
         url: '/api/app/item-despensa',
         body: input,
       },
       { apiName: this.apiName, ...config });
     ```
   - Asegurar que `CreateItemDespensaDto` esté debidamente tipado en `models.ts` (`productoId`, `cantidad`, `unidad`, `fechaVencimiento`).
2. **Componente Visual y Formulario Reactivo (`angular/src/app/pantry`):**
   - En `pantry.component.html`, agregar en el encabezado un botón destacado `[+ Agregar Producto a Despensa]` con ícono `fa fa-plus`.
   - Implementar un modal o panel colapsable con formulario reactivo `createForm`:
     * Selector o campo para el producto a agregar (puede listar los productos guardados mediante `ProductoService.getList` o autocompletar).
     * `cantidad`: input numérico opcional, validador `min(0)`.
     * `unidad`: input de texto opcional, validador `maxLength(50)`.
     * `fechaVencimiento`: input tipo date opcional, validación de fecha real.
   - Botones de acción: `Guardar` (deshabilitado si el formulario es inválido o está enviando) y `Cancelar`.
3. **Manejo de Estados y UX:**
   - Estado de envío (`isSubmitting = true`) con indicador de carga para evitar doble click.
   - Si la operación es exitosa:
     * Cerrar el modal/panel.
     * Limpiar el formulario.
     * Mostrar mensaje de éxito temporal (`Item agregado exitosamente a la despensa`).
     * Refrescar automáticamente la grilla llamando a `list.get()`.
   - Si el guardado falla: mantener abierto el formulario con los datos ingresados para que el usuario no pierda lo escrito y mostrar el mensaje de error controlado.
4. **Localización e Internacionalización (`Domain.Shared` & Angular):**
   - Agregar todas las claves de textos nuevos en `src/SmartPantry.Domain.Shared/Localization/SmartPantry/es.json` y `en.json`:
     * `Pantry:AddItem`, `Pantry:SelectProduct`, `Pantry:ItemAddedSuccess`, etc.
5. **Pruebas Unitarias de Frontend (`pantry.component.spec.ts`):**
   - Agregar pruebas en Vitest:
     * Al hacer click en "+ Agregar", se abre el formulario de alta.
     * Con cantidad negativa o fecha inválida, el formulario es inválido y no invoca al servicio.
     * Con datos válidos, se llama a `itemDespensaService.create`, se resetea el formulario y se recarga el listado.
     * Si el servicio devuelve error, el panel se mantiene visible conservando los valores ingresados.

---

## 3. Criterios de Aceptación (Definition of Done)
- [ ] Existe un botón visible "+ Agregar Producto" en la pantalla de despensa.
- [ ] El formulario reactivo valida cantidad >= 0 y unidad <= 50 caracteres.
- [ ] Se invoca `POST /api/app/item-despensa` con el `productoId` y los campos opcionales.
- [ ] El ítem recién creado aparece de inmediato en la tabla de despensa.
- [ ] Todos los textos están localizados en español e inglés.
- [ ] La suite de pruebas de Vitest pasa al 100% en verde (`yarn test --watch=false`).

---

## 4. Independencia / Desacople
- **¿Depende de otros?** NO. El backend necesario (`ItemDespensaAppService.CreateAsync`) ya está implementado y probado en `main`. Gena puede desarrollar este ticket de forma completamente autónoma.

---

## 5. Instrucciones de PR
1. Crear rama `feature/26-frontend-agregar-item-despensa` desde `main` actualizado.
2. Commits: `feat: agregar boton y modal para crear item en despensa (#26)`.
3. Abrir Pull Request hacia `main`, vincular con `Closes #26` y pedir revisión a `@manuelperezseg` y `@puma189`.
