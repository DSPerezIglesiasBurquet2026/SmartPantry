# SmartPantry

Repositorio del Trabajo Práctico Integrador de Desarrollo de Software 2026.

## Integrantes
- Manuel Perez - manuelperezseg
- Ignacio Iglesias - puma189
- Genaro Burquet - genaburquet

## Cómo ejecutar

### Requisitos

- .NET 10.0 SDK
- Visual Studio 2022 o 2026, con la carga de trabajo *Desarrollo de ASP.NET y web*
- Node.js 24 LTS (24.15.0 o superior)
- Yarn 1.22.x
- SQL Server Developer o SQL Server Express (instalado localmente)
- SQL Server Management Studio (SSMS)
- ABP Studio Desktop
- Git

### Configuración local

Editar solo el valor de `Default` en `ConnectionStrings`, presente en dos archivos:

- `src/SmartPantry.DbMigrator/appsettings.json`
- `src/SmartPantry.HttpApi.Host/appsettings.json`

En nuestro caso usamos LocalDB y la autenticación integrada de Windows:

```json
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\MSSQLLocalDB;Database=SmartPantry;Trusted_Connection=True"
  }
}
```

Si en algún momento se usa un servidor remoto o una cadena con usuario/contraseña, no versionarla: usar User Secrets o la variable de entorno `ConnectionStrings__Default`.

### Puesta en marcha

1. Restaurar las dependencias cliente: ``` abp install-libs ``` (si falla desde ABP Studio, correr ``` yarn install ``` manualmente dentro de `/angular`, ya que a nosotros nos falló)
2. Restaurar y compilar el backend: abrir `SmartPantry.slnx` en Visual Studio y compilar. O ejecutar: 
    ```
    dotnet restore ./SmartPantry.slnx` + `dotnet build ./SmartPantry.slnx
    ```
3. Ejecutar `SmartPantry.DbMigrator` para crear la base y los datos iniciales. O ejecutar:
    ```
    dotnet run --project ./src/SmartPantry.DbMigrator
    ```
4. Levantar la API:
    ```
    dotnet run --project ./src/SmartPantry.HttpApi.Host
    ```

5. Levantar el frontend (ejecutar en otra terminal):
   ```
   cd angular
   yarn start
   ```
6. URLs locales:
   - Para la API: `https://localhost:44352`
   - Para Angular: `http://localhost:4200`

### Verificación

- Backend:
  ```
  dotnet build ./SmartPantry.slnx --configuration Release
  dotnet test ./SmartPantry.slnx --configuration Release
  ```
- Frontend:
  ```
  cd angular
  yarn build
  yarn test --watch=false
  ```

### Estructura de la solución

- `angular`: aplicación Angular
- `SmartPantry.DbMigrator`: aplicación de consola que aplica migraciones y siembra datos iniciales
- `SmartPantry.HttpApi.Host`: API ASP.NET Core expuesta a los clientes
- `test/`: proyectos de test (`Application.Tests`, `Domain.Tests`, `EntityFrameworkCore.Tests`)

## Documentación TP-10: Pantalla "Mi Despensa"

### Decisiones de Diseño e Implementación
- **Uso del ListService:** Se optó por inyectar y utilizar el `ListService` proporcionado por el framework ABP, dado que la operación `GetListAsync` del backend devuelve resultados de forma paginada (`PagedResultDto`). Esto nos permite delegar la responsabilidad del estado de paginación y ordenamiento directamente al servicio de ABP.
- **Manejo de Ítems Consumidos (Regla de Negocio 6):** El equipo decidió que los ítems marcados como consumidos NO se eliminan automáticamente. En su lugar, se conservan a modo de historial visualmente diferenciados (tachados y con una etiqueta "Consumido") y se deshabilitan las opciones de edición para proteger la integridad del dato histórico. La eliminación queda como una acción explícita del usuario.
- **Criterio de Fechas:** Las fechas de vencimiento se renderizan aplicando el formato de la configuración regional local del navegador del usuario (utilizando el pipe de Angular `date:'shortDate'`), aunque en el backend (y worker) operen con criterio UTC.
- **Aislamiento de Datos (Prevención de Enumeración):** Se definió que si un usuario intenta operar (editar, consumir o eliminar) sobre un ítem que pertenece a otro usuario mediante la API, el sistema retorne un error HTTP `404 Not Found` en lugar de un `403 Forbidden`. Esta medida de seguridad evita la enumeración de IDs, impidiendo que un atacante pueda confirmar la existencia de registros ajenos; en el contexto del usuario actual, el ítem ajeno simplemente "no existe".
- **Manejo de Errores HTTP:** 
  * Se aplicó la configuración `{ skipHandleError: true }` exclusivamente en la obtención del listado de ítems. El motivo es que el componente ya posee un estado visual dedicado (`hasError = true`) que renderiza un mensaje amigable y un botón de "Reintentar", evitando así mostrar cuadros de diálogo duplicados (el global de ABP y el local del componente).
  * Para las peticiones de mutación (Editar, Consumir, Eliminar), se dejó el manejo predeterminado de ABP, garantizando que el usuario nunca vea detalles técnicos en crudo.