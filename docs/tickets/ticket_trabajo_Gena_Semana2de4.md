# Issue: [Frontend] Pantalla de Catálogo de Productos: Búsqueda por Código y Texto con Vista de Detalle

**Identificador:** `ticket_trabajo_Gena_Semana2de4`  
**Responsable:** Genaro Burquet (`genaburquet`)  
**Semana:** Semana 2 de 4 (14/11/2026 - 20/11/2026)  
**Requerimientos Cubiertos:** RF-05 (Búsqueda por código), RF-06 (Búsqueda por texto), RF-07 (Detalle de producto), RF-09 (Datos incompletos), HU-02  
**Nivel de Dificultad:** Media (Componente Angular rico, pestañas de búsqueda, modal de detalle, manejo de estados vacíos/error y Nutri-Score)  
**Rama asignada:** `feature/29-frontend-catalogo-busqueda-detalle`

---

## 1. Contexto y Objetivo
Actualmente la aplicación solo cuenta con la pantalla de Despensa. Para cumplir con HU-02 y los requerimientos RF-05, RF-06 y RF-07, el usuario debe contar con un Catálogo donde pueda:
1. Buscar por código de barras externo (Open Food Facts).
2. Buscar por texto libre (nombre, marca o categoría) mediante Search-a-licious (utilizando el endpoint desarrollado por Manuel en la Semana 1).
3. Abrir el Detalle del Producto para observar imagen, categorías, ingredientes, alérgenos, Nutri-Score y grupo NOVA.
4. Cumplir con RF-09: cualquier dato ausente de la API externa debe mostrarse como *"No especificado"* (en cursiva), sin inventar datos.
5. Permitir la acción directa de "Agregar a mi despensa" desde el detalle del producto.

---

## 2. Tareas Técnicas Detalladas

1. **Creación del Componente de Catálogo (`angular/src/app/catalog`):**
   - Crear componente standalone `CatalogComponent` con su ruta `/catalog` protegida por `authGuard`.
   - Agregar el enlace de navegación en `route.provider.ts` con ícono `fa fa-search` y etiqueta `"Catálogo de Alimentos"`.
2. **Interfaz de Búsqueda Dual:**
   - Crear selector con dos modos o pestañas:
     * **Por Código de Barras:** Input con validación de solo dígitos (3 a 18 caracteres) y botón buscar. Invoca `productoService.getBuscarPorCodigo`.
     * **Por Texto Libre:** Input de búsqueda con debounce o botón de buscar y paginación. Invoca `productoService.buscarPorTexto`.
   - Renderizar los 4 estados de la interfaz:
     * *Cargando:* Spinner de carga mientras responde la API externa.
     * *Resultados:* Cards o grilla con miniatura, nombre, marca y badges de Nutri-Score / NOVA.
     * *Vacío:* Mensaje amigable cuando no se encuentran productos.
     * *Error:* Alertas para HTTP 429 (Límite de solicitudes) y 503 (Servicio no disponible) con botón de reintentar.
3. **Modal de Detalle Completo de Producto (RF-07 & RF-09):**
   - Al hacer click en "Ver detalle":
     * Mostrar imagen de alta resolución (o placeholder si no existe).
     * Nombre del producto y marca.
     * Badges coloreados según grado Nutri-Score (A: verde oscuro a E: rojo) y grupo NOVA (1 a 4).
     * Lista de ingredientes y alérgenos destacados.
     * Si un dato no vino en la respuesta de Open Food Facts, mostrar `No especificado` en cursiva con clase de texto atenuada (`text-muted fst-italic`).
     * **Aviso visible obligatorio (RN-1):** *"Esta información es provista por Open Food Facts y no constituye consejo médico."*
4. **Acción de Agregar a Despensa:**
   - Botón directo `[+ Agregar a mi despensa]`: abre el formulario con el `productoId` preseleccionado (llamando a crear producto si aún no existe en la base interna según RF-08) y permite ingresar cantidad y vencimiento.
5. **Pruebas Unitarias de Frontend (`catalog.component.spec.ts`):**
   - Tests en Vitest:
     * Búsqueda por código inválido no dispara la petición.
     * Búsqueda exitosa muestra la card con el producto.
     * Estado de error muestra la alerta correspondiente.
     * Detalle con propiedades nulas muestra *"No especificado"*.
     * Apertura y cierre del modal de detalle.

---

## 3. Criterios de Aceptación (Definition of Done)
- [ ] La pantalla `/catalog` permite buscar tanto por código como por texto.
- [ ] Los resultados muestran Nutri-Score y NOVA con diseño visual responsive.
- [ ] Los campos ausentes muestran *"No especificado"* (RF-09).
- [ ] Se incluye la leyenda ética y de fuente de datos (Open Food Facts).
- [ ] Se puede agregar un producto del catálogo directamente a la despensa.
- [ ] La suite de pruebas de Vitest para el catálogo pasa al 100%.

---

## 4. Independencia / Desacople
- **¿Depende de otros?** Utiliza el endpoint de búsqueda por código (listo desde TP-07) y el de texto libre (entregado por Manuel en el PR de Semana 1). Para los tests unitarios, Gena utiliza un mock del servicio `ProductoService`, pudiendo desarrollar y probar su vista incluso antes del merge del backend.

---

## 5. Instrucciones de PR
1. Crear rama `feature/29-frontend-catalogo-busqueda-detalle` desde `main` actualizado.
2. Commits: `feat: implementar vista de catalogo de productos y modal de detalle (#29)`.
3. Abrir Pull Request hacia `main`, vincular con `Closes #29` y solicitar revisión a `@manuelperezseg` y `@puma189`.
