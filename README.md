# OFUserControlApp

Aplicacion web para validacion de usuarios OF vs Directorio Activo mediante procesamiento de archivos Excel.

## Descripcion

La aplicacion permite cargar archivos Excel con listados de usuarios y cruzarlos con la vista `View_Usuarios` para identificar usuarios habilitados e inactivos.

## Tecnologias

- Backend: ASP.NET Core 10.0, Entity Framework Core
- Frontend: Razor Views + SignalR
- Base de Datos: PostgreSQL (Supabase)
- Procesamiento: EPPlus

## Arquitectura

- Domain: entidades y contratos
- Application: logica de negocio y DTOs
- Infrastructure: repositorios y acceso a datos
- Presentation: MVC + API + SignalR

## Configuracion

### 1. Esquema de base de datos

Ejecuta el script:

- `Database/Scripts/CreateTables_PostgreSQL.sql`

Este script crea:

- `Stg_UsuariosExcel`
- `Rpt_UsuariosCruce`
- `LogProceso`
- `vw_EstadisticasUsuarios`

Tambien incluye procedimientos equivalentes heredados, aunque el repositorio actual realiza el cruce directamente por SQL.

### 2. Cadena de conexion

Configura `appsettings.json` o `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_SUPABASE_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;Timeout=300;Command Timeout=300"
  }
}
```

Recomendado: usar secretos de usuario o variables de entorno para el password.

### 3. Permisos requeridos

El usuario de PostgreSQL debe tener al menos:

- `SELECT/INSERT/DELETE` en `Stg_UsuariosExcel`
- `SELECT/INSERT/DELETE` en `Rpt_UsuariosCruce`
- `SELECT` en `View_Usuarios`
- `SELECT/INSERT` en `LogProceso` (si se usa logging por SQL)

## Ejecucion

```bash
dotnet restore
dotnet build
dotnet run
```

## Flujo funcional

1. Carga de archivo `.xls`.
2. Extraccion de usuarios.
3. Insercion en tabla staging.
4. Cruce con `View_Usuarios`.
5. Consulta de resultados y resumen.
6. Exportacion a Excel.
7. Limpieza de datos temporales del proceso.

## Solucion de problemas

- Error de conexion: valida host, puerto, usuario, password y SSL.
- Error de carga en BD: valida que las tablas del script PostgreSQL existan.
- Error de cruce: valida que exista la vista `View_Usuarios` con columnas `EMail` y `User_SO`.

