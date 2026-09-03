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

1. Restaurar las dependencias cliente: `abp install-libs` (si falla desde ABP Studio, correr `yarn install` manualmente dentro de `/angular`, ya que a nosotros nos falló)
2. Restaurar y compilar el backend: abrir `SmartPantry.slnx` en Visual Studio y compilar, o `dotnet restore ./SmartPantry.slnx` + `dotnet build ./SmartPantry.slnx`
3. Ejecutar `SmartPantry.DbMigrator` para crear la base y los datos iniciales (F5 en VS, o `dotnet run --project ./src/SmartPantry.DbMigrator`)
4. Levantar la API: `dotnet run --project ./src/SmartPantry.HttpApi.Host`
5. Levantar el frontend:
   ```
   cd angular
   yarn start
   ```
6. URLs locales:
   - Para la API: `https://localhost:44373`
   - Para Angular: `http://localhost:4200`

### Verificación

- Backend:
  ```
  dotnet build ./SmartPantry.slnx --configuration Release
  dotnet test ./SmartPantry.slnx --configuration Release
  ```
- Frontend:
  ```
  yarn build
  yarn test --watch=false --browsers=ChromeHeadless
  ```

### Estructura de la solución

- `angular`: aplicación Angular
- `SmartPantry.DbMigrator`: aplicación de consola que aplica migraciones y siembra datos iniciales
- `SmartPantry.HttpApi.Host`: API ASP.NET Core expuesta a los clientes
- `test/`: proyectos de test (`Application.Tests`, `Domain.Tests`, `EntityFrameworkCore.Tests`)