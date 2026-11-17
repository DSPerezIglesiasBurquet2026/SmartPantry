# Issue: [Frontend] Rediseño del Dashboard Home, Navegación, Avisos Éticos y Limpieza de Rutas

**Identificador:** `ticket_trabajo_Puma_Semana3de4`  
**Responsable:** Ignacio Iglesias (`puma189`)  
**Semana:** Semana 3 de 4 (21/11/2026 - 27/11/2026)  
**Requerimientos Cubiertos:** RNF Usabilidad, RNF Ética y datos, RN-1 (Aviso médico), RN-2 (Transparencia de datos)  
**Nivel de Dificultad:** Media (Diseño de layout, routing en ABP Angular, banners éticos y pruebas de presentación)  
**Rama asignada:** `feature/33-dashboard-home-navegacion-limpieza`

---

## 1. Contexto y Objetivo
Actualmente la página de inicio (`HomeComponent`) sigue mostrando el contenido genérico de bienvenida del template de ABP Framework ("Getting Started", enlaces a la documentación de ABP, etc.) y la barra de navegación aún conserva los menús de "Books" y "Authors" correspondientes al tutorial inicial.  
Para la entrega final, la aplicación debe presentar una identidad visual profesional acorde al Trabajo Práctico:
1. Un **Dashboard de bienvenida** claro y responsive para SmartPantry.
2. Accesos directos a las funciones centrales: Mi Despensa, Catálogo de Alimentos y Preferencias.
3. El **aviso ético y legal obligatorio** (RN-1 y RNF Ética y datos): *"La información de productos proviene de Open Food Facts. Esta aplicación no brinda diagnóstico ni recomendación médica."*
4. Limpiar del menú de navegación las páginas del tutorial (`/books`, `/authors`) para que los evaluadores solo vean la aplicación real.

---

## 2. Tareas Técnicas Detalladas

1. **Rediseño del Dashboard Principal (`angular/src/app/home`):**
   - Rediseñar `home.component.html` y `home.component.scss`:
     * Header principal con título de bienvenida y descripción del propósito de SmartPantry.
     * **Banner Ético Destacado (Requisito no funcional de la cátedra):** Card de información visible permanente indicando la atribución a Open Food Facts y la aclaración de no diagnóstico médico.
     * **Tarjetas de Acceso Rápido:**
       1. *Mi Despensa:* Ícono de despensa, descripción breve y botón "Gestionar Despensa".
       2. *Catálogo de Alimentos:* Ícono de búsqueda, descripción y botón "Buscar Productos".
       3. *Perfil Nutricional / Preferencias:* Ícono de alérgenos y botón "Configurar Alertas".
     * Si el usuario está autenticado, mostrar mensaje de bienvenida personalizado con su nombre (`currentUser.userName`).
2. **Limpieza y Organización de Navegación (`angular/src/app/route.provider.ts`):**
   - Reorganizar las rutas visibles en el menú:
     * `Home` (orden 1, ícono `fas fa-home`, ruta `/`).
     * `Mi Despensa` (orden 2, ícono `fas fa-box-open`, ruta `/pantry`).
     * `Catálogo de Productos` (orden 3, ícono `fas fa-search`, ruta `/catalog`).
     * `Preferencias` (orden 4, ícono `fas fa-heartbeat`, ruta `/preferences`).
   - Retirar del menú visible las rutas de `Books` y `Authors` (mantener los archivos en el repositorio para no romper tests de cátedra existentes, pero quitarlos de la navegación de usuario).
3. **Footer y Atribución Global:**
   - En el pie de página o contenedor principal de la aplicación, asegurar la presencia de la leyenda de autoría y descargo de responsabilidad.
4. **Revisión y Completado de Internacionalización:**
   - Auditar `es.json` y `en.json` para garantizar que todas las etiquetas de la aplicación tengan traducción coherente.
5. **Pruebas Unitarias de Frontend (`home.component.spec.ts`):**
   - Validar que el componente renderiza el banner de atribución a Open Food Facts y el aviso médico.
   - Validar que los botones de navegación tienen los enlaces correctos hacia `/pantry`, `/catalog` y `/preferences`.

---

## 3. Criterios de Aceptación (Definition of Done)
- [ ] La Home ya no muestra contenido de tutorial de ABP; presenta un Dashboard de SmartPantry.
- [ ] La leyenda de descargo médico y atribución a Open Food Facts es visible de forma obligatoria.
- [ ] El menú de navegación está limpio (sin Books ni Authors) y ordenado.
- [ ] La aplicación es 100% responsive en dispositivos móviles y de escritorio.
- [ ] Las pruebas unitarias de Vitest para Home pasan en verde.

---

## 4. Independencia / Desacople
- **¿Depende de otros?** NO. Trabaja sobre la interfaz de inicio, componentes de navegación y archivos de localización. No altera contratos de API ni lógica de backend.

---

## 5. Instrucciones de PR
1. Crear rama `feature/33-dashboard-home-navegacion-limpieza` desde `main` actualizado.
2. Commits: `feat: rediseñar home como dashboard y limpiar navegacion (#33)`.
3. Abrir Pull Request hacia `main`, vincular con `Closes #33` y solicitar revisión a `@manuelperezseg` y `@genaburquet`.
