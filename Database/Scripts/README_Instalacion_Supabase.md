# INSTRUCCIONES DE INSTALACIÓN - SUPABASE
# =============================================
# Proyecto: OFUserControlApp
# Base de datos: PostgreSQL en Supabase
# =============================================

## 📋 ORDEN DE EJECUCIÓN DE SCRIPTS

Los scripts deben ejecutarse en el siguiente orden en la consola SQL de Supabase:

### 1️⃣ PRIMERO: Crear estructura de tablas
**Archivo:** `a_CreateTables_PostgreSQL.sql`

Este script crea:
- Tabla `Stg_UsuariosExcel` (para datos de Excel)
- Tabla `Rpt_UsuariosCruce` (resultados del cruce)
- Tabla `LogProceso` (logs de ejecución)
- Tabla `View_Usuarios` (Directorio Activo - referencia para cruces)
- Vista `vw_EstadisticasUsuarios` (estadísticas)
- Procedimientos almacenados `usp_CruzarUsuariosExcelConView` y `usp_LimpiarDatosAntiguos`

### 2️⃣ SEGUNDO: Poblar Directorio Activo
**Archivo:** `b_Insert_View_Usuarios_FromExcel.sql`

Este script inserta los usuarios del Directorio Activo (obtenidos del Excel View_Usuarios.xlsx) en la tabla `View_Usuarios`.

### 3️⃣ OPCIONAL: Datos de prueba
**Archivo:** `c_Insert_View_Usuarios_Sinteticos.sql`

Este script contiene datos sintéticos de prueba si no tienes el Excel real del Directorio Activo.

## 🔧 CONFIGURACIÓN DE CONEXIÓN EN SUPABASE

### Datos de conexión que necesitarás en tu aplicación:
```
Host: [tu-proyecto].supabase.co
Puerto: 5432
Base de datos: postgres
Usuario: postgres
Contraseña: N_v8!jvWY_!%E?9
SSL: require
```

### Ejemplo de cadena de conexión:
```
Host=[tu-proyecto].supabase.co;Port=5432;Database=postgres;Username=postgres;Password=N_v8!jvWY_!%E?9;SSL Mode=Require
```

## 📊 PROCESO DE TRABAJO

1. **Ejecutar scripts en orden:** a_ → b_ (o c_ si usas datos de prueba)
2. **Verificar tablas creadas:** Puedes usar las queries de verificación al final de cada script
3. **Configurar conexión:** Actualizar la cadena de conexión en tu aplicación
4. **Probar la aplicación:** 
   - Subir archivo Excel con usuarios a validar
   - Ejecutar el proceso de cruce
   - Ver resultados en la tabla `Rpt_UsuariosCruce`

## 🧪 QUERIES DE VERIFICACIÓN

Después de ejecutar los scripts, puedes verificar:

```sql
-- Verificar tablas creadas
SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';

-- Verificar usuarios del Directorio Activo
SELECT COUNT(*) as TotalUsuarios FROM "View_Usuarios";

-- Verificar usuarios activos vs inactivos
SELECT 
    COUNT(*) as Total,
    COUNT(CASE WHEN "F_Baja" IS NULL THEN 1 END) as Activos,
    COUNT(CASE WHEN "F_Baja" IS NOT NULL THEN 1 END) as Inactivos
FROM "View_Usuarios";
```

## ⚠️ NOTAS IMPORTANTES

- **Seguridad:** Nunca compartas la contraseña de la BD en commits o repositorios públicos
- **Backup:** Considera hacer backup antes de ejecutar `TRUNCATE` en producción
- **Índices:** Los índices se crean automáticamente con los scripts para optimizar búsquedas
- **SSL:** Siempre usa SSL en producción (Require)

## 🚀 SIGUIENTES PASOS

Una vez ejecutados los scripts y configurada la conexión:
1. Levantar la aplicación ASP.NET
2. Subir archivo Excel con usuarios a validar
3. Ejecutar el proceso de cruce
4. Revisar resultados en la interfaz web