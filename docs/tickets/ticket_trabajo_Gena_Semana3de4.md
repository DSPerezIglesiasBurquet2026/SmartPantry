# Issue: [Frontend] Pantalla de Configuración de Preferencias Alimentarias del Usuario

**Identificador:** `ticket_trabajo_Gena_Semana3de4`  
**Responsable:** Genaro Burquet (`genaburquet`)  
**Semana:** Semana 3 de 4 (21/11/2026 - 27/11/2026)  
**Requerimientos Cubiertos:** RF-03 (Preferencias), HU-01, RN-1 (Aviso de no diagnóstico)  
**Nivel de Dificultad:** Media (Formulario reactivo con tags/chips, selección múltiple, consumo de proxy ABP y tests unitarios)  
**Rama asignada:** `feature/32-frontend-preferencias-alimentarias`

---

## 1. Contexto y Objetivo
La historia de usuario HU-01 establece: *"Como usuario, quiero configurar mis preferencias alimentarias para identificar productos que necesito vigilar"*.  
El requerimiento funcional RF-03 especifica que el usuario administra:
1. Una lista de alérgenos vigilados (ej: gluten, maní, frutos secos, crustáceos, soja, leche).
2. Una lista de ingredientes adicionales a vigilar (ingreso libre de términos, ej: aceite de palma, jarabe de maíz, azúcar).
3. Una opción (switch) para activar o desactivar la visualización de alertas en pantalla.
4. Leyenda visible obligatoria (RN-1): advertencia clara de que la configuración no equivale a diagnóstico médico.

En la Semana 2, Manuel implementó el backend y contratos de `PreferenciaUsuarioAppService`. El objetivo de este ticket es crear la interfaz de usuario en Angular para que el usuario pueda consultar y actualizar sus preferencias.

---

## 2. Tareas Técnicas Detalladas

1. **Creación del Componente de Preferencias (`angular/src/app/preferences`):**
   - Crear el componente standalone `PreferencesComponent` con su ruta `/preferences` protegida con `authGuard`.
   - Agregar el acceso en el menú de navegación (`route.provider.ts`) con ícono `fa fa-heartbeat` o `fa fa-sliders-h` y etiqueta *"Mis Preferencias"*.
2. **Diseño de la Interfaz y Formulario Reactivo:**
   - **Sección de Alérgenos Comunes:** Selector con badges/chips interactivos para los alérgenos más habituales de Open Food Facts (Gluten, Soja, Maní, Leche, Huevos, Pescado, Crustáceos, Mostaza, Sésamo) permitiendo marcar/desmarcar con un click.
   - **Sección de Ingredientes Personalizados:** Campo de texto con botón `[+ Agregar]` que permite sumar palabras clave a una lista de chips con botón de eliminar (`(x)`).
   - **Sección de Configuración de Alertas:** Switch toggle de Bootstrap *"Mostrar alertas de incompatibilidad en pantalla"*.
   - **Banner de Ética Médica (RN-1):** Cuadro destacado con alerta visual:  
     `"Aviso importante: La configuración de alérgenos e ingredientes vigilados es una herramienta informativa de ayuda y no constituye diagnóstico ni indicación médica."`
3. **Flujo de Carga y Guardado:**
   - Al iniciar el componente: invocar `preferenciaUsuarioService.getMiPreferencia()` y poblar el formulario.
   - Al guardar:
     * Validar el formulario y enviar a `preferenciaUsuarioService.updateMiPreferencia()`.
     * Mostrar feedback visual de éxito (*"Preferencias guardadas exitosamente"*).
     * Si ocurre un error de red: preservar los cambios en pantalla y notificar al usuario.
4. **Localización e Internacionalización:**
   - Cargar todas las cadenas en `es.json` y `en.json`: `Preferences:Title`, `Preferences:Allergens`, `Preferences:CustomIngredients`, `Preferences:ShowAlerts`, `Preferences:MedicalDisclaimer`, `Preferences:SavedSuccess`.
5. **Pruebas Unitarias de Frontend (`preferences.component.spec.ts`):**
   - Tests en Vitest:
     * Carga inicial obtiene los datos del servicio y los refleja en pantalla.
     * Agregar un nuevo ingrediente personalizado lo suma a la lista visual.
     * Eliminar un chip lo remueve de la lista.
     * El click en "Guardar" invoca al servicio con el payload correspondiente.
     * El banner de advertencia médica está presente en el DOM.

---

## 3. Criterios de Aceptación (Definition of Done)
- [ ] La pantalla `/preferences` carga las preferencias del usuario actual.
- [ ] Permite seleccionar alérgenos y escribir ingredientes personalizados de manera intuitiva.
- [ ] El switch de visualización de alertas persiste su estado.
- [ ] El banner de no diagnóstico médico (RN-1) está visible de forma destacada.
- [ ] Todos los textos están traducidos en español e inglés.
- [ ] Las pruebas unitarias de Vitest pasan en verde.

---

## 4. Independencia / Desacople
- **¿Depende de otros?** Consume el endpoint acordado en `CONVENCIONES_Y_CONTRATOS.md` y desarrollado por Manuel en la Semana 2. En sus pruebas unitarias utiliza un mock de `PreferenciaUsuarioService`, por lo que Gena puede desarrollar la vista de forma independiente.

---

## 5. Instrucciones de PR
1. Crear rama `feature/32-frontend-preferencias-alimentarias` desde `main` actualizado.
2. Commits: `feat: implementar pantalla de perfil y preferencias alimentarias (#32)`.
3. Abrir Pull Request hacia `main`, vincular con `Closes #32` y solicitar revisión a `@manuelperezseg` y `@puma189`.
