# Instrucciones de instalacion - Supabase

Proyecto: OFUserControlApp
Base de datos: PostgreSQL en Supabase

## Orden de ejecucion de scripts

Ejecuta los scripts en este orden desde el SQL Editor de Supabase.

### 1. Crear estructura

Archivo: `a_CreateTables_PostgreSQL.sql`

Este script crea:

- Tabla `Stg_UsuariosExcel` para datos cargados desde Excel.
- Tabla `Rpt_UsuariosCruce` para resultados del cruce.
- Tabla `LogProceso` para logs de ejecucion.
- Tabla `View_Usuarios` como referencia del Directorio Activo.
- Vista `vw_EstadisticasUsuarios`.
- Procedimientos `usp_CruzarUsuariosExcelConView` y `usp_LimpiarDatosAntiguos`.

### 2. Poblar Directorio Activo

Archivo: `b_Insert_View_Usuarios_FromExcel.sql`

Inserta usuarios de referencia en `View_Usuarios`.

### 3. Datos sinteticos opcionales

Archivo: `c_Insert_View_Usuarios_Sinteticos.sql`

Usalo si quieres probar sin datos reales del Directorio Activo.

## Conexion recomendada

Para desarrollo local en Windows, usa la cadena **Session pooler** de Supabase. La conexion directa `db.<project-ref>.supabase.co:5432` puede resolver solo por IPv6, y muchas redes locales no tienen salida IPv6.

En Supabase:

1. Abre tu proyecto.
2. Entra a **Connect**.
3. Copia la cadena de **Session pooler**.
4. Reemplaza `[YOUR-PASSWORD]` por la contrasena de la base de datos.

Formato esperado por Npgsql:

```text
Host=aws-0-[REGION].pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.[PROJECT_REF];Password=[DB_PASSWORD];SSL Mode=Require;Trust Server Certificate=true;Timeout=300;Command Timeout=300
```

Guarda la cadena en secretos de usuario, no en `appsettings.json`:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=aws-0-[REGION].pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.[PROJECT_REF];Password=[DB_PASSWORD];SSL Mode=Require;Trust Server Certificate=true;Timeout=300;Command Timeout=300"
```

Tambien puedes usar una variable de entorno para una sola sesion:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=aws-0-[REGION].pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.[PROJECT_REF];Password=[DB_PASSWORD];SSL Mode=Require;Trust Server Certificate=true;Timeout=300;Command Timeout=300"
```

## Verificacion

Despues de ejecutar los scripts, usa estas consultas:

```sql
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
ORDER BY table_name;

SELECT COUNT(*) AS "TotalUsuarios"
FROM "View_Usuarios";

SELECT
    COUNT(*) AS "Total",
    COUNT(CASE WHEN "F_Baja" IS NULL THEN 1 END) AS "Activos",
    COUNT(CASE WHEN "F_Baja" IS NOT NULL THEN 1 END) AS "Inactivos"
FROM "View_Usuarios";
```

## Notas importantes

- No guardes contrasenas en archivos versionables.
- Los scripts de carga usan `TRUNCATE`; revisalos antes de ejecutarlos contra datos reales.
- Usa SSL en Supabase.
- Si la app no conecta usando el host `db.<project-ref>.supabase.co`, cambia a **Session pooler**.

## Probar la app

1. Configura la cadena con `dotnet user-secrets`.
2. Ejecuta `dotnet run`.
3. Confirma que el log muestre `Conexion a la base de datos establecida correctamente`.
4. Sube un Excel desde la interfaz y ejecuta el cruce.
