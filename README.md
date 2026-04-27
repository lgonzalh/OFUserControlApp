# OFUserControlApp

## English

OFUserControlApp is a small ASP.NET Core web application for validating OF user lists against an Active Directory reference table.

### Features

- Upload Excel user lists.
- Preview the first 10 records before processing.
- Compare uploaded users against `View_Usuarios`.
- Review totals and detailed results.
- Export the generated results.

### Stack

- ASP.NET Core 10
- Entity Framework Core
- Razor Views
- SignalR
- PostgreSQL on Supabase

### Setup

Run the database script in Supabase:

```text
Database/Scripts/a_CreateTables_PostgreSQL.sql
```

Configure the connection string with user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=aws-0-[REGION].pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.[PROJECT_REF];Password=[DB_PASSWORD];SSL Mode=Require;Trust Server Certificate=true;Timeout=300;Command Timeout=300"
```

Run the application:

```powershell
dotnet restore
dotnet build
dotnet run --launch-profile OFUserControlApp
```

Open:

```text
http://localhost:5000/
```

## Espanol

OFUserControlApp es una aplicacion web ASP.NET Core para validar listados de usuarios OF contra una tabla de referencia de Directorio Activo.

### Funcionalidades

- Cargar listados de usuarios en Excel.
- Ver una previsualizacion de los primeros 10 registros.
- Cruzar usuarios cargados contra `View_Usuarios`.
- Revisar totales y resultados detallados.
- Exportar los resultados generados.

### Tecnologias

- ASP.NET Core 10
- Entity Framework Core
- Razor Views
- SignalR
- PostgreSQL en Supabase

### Configuracion

Ejecuta el script de base de datos en Supabase:

```text
Database/Scripts/a_CreateTables_PostgreSQL.sql
```

Configura la cadena de conexion con secretos de usuario:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=aws-0-[REGION].pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.[PROJECT_REF];Password=[DB_PASSWORD];SSL Mode=Require;Trust Server Certificate=true;Timeout=300;Command Timeout=300"
```

Ejecuta la aplicacion:

```powershell
dotnet restore
dotnet build
dotnet run --launch-profile OFUserControlApp
```

Abre:

```text
http://localhost:5000/
```
