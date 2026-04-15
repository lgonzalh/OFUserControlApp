-- =============================================================
-- Script generado desde HM_Listado_de_Usuarios_por_Paí_060825 (1).xlsx
-- Fecha: 2025-01-01 12:00:00
-- Total de usuarios: 1604
-- =============================================================

TRUNCATE TABLE "Stg_UsuariosExcel";

-- NOTA: Este archivo Excel tiene una estructura especial donde:
-- - La columna A ('Listado de Usuario Activos por PaÝs') contiene los emails/usuarios
-- - Las filas 1-2 contienen encabezados que deben ser ignorados
-- - Los datos reales comienzan desde la fila 3

-- Inserción de usuarios desde el archivo Excel
-- Los valores se extraen de la primera columna que contiene los emails

INSERT INTO "Stg_UsuariosExcel" ("ProcesoId", "Usuario", "Correo", "FuenteArchivo", "CargadoEn") VALUES
('HM_LISTADO_2025', 'ADRIANA.MENDEZ@SERVER.COM', 'ADRIANA.MENDEZ@SERVER.COM', 'HM_Listado_de_Usuarios_por_Paí_060825 (1).xlsx', CURRENT_TIMESTAMP),
('HM_LISTADO_2025', 'ALBERTO.PEREZ@SERVER.COM', 'ALBERTO.PEREZ@SERVER.COM', 'HM_Listado_de_Usuarios_por_Paí_060825 (1).xlsx', CURRENT_TIMESTAMP),
('HM_LISTADO_2025', 'ANA.GARCIA@SERVER.COM', 'ANA.GARCIA@SERVER.COM', 'HM_Listado_de_Usuarios_por_Paí_060825 (1).xlsx', CURRENT_TIMESTAMP),
('HM_LISTADO_2025', 'CARLOS.RODRIGUEZ@SERVER.COM', 'CARLOS.RODRIGUEZ@SERVER.COM', 'HM_Listado_de_Usuarios_por_Paí_060825 (1).xlsx', CURRENT_TIMESTAMP),
('HM_LISTADO_2025', 'CARMEN.SANCHEZ@SERVER.COM', 'CARMEN.SANCHEZ@SERVER.COM', 'HM_Listado_de_Usuarios_por_Paí_060825 (1).xlsx', CURRENT_TIMESTAMP),
('HM_LISTADO_2025', 'DAVID.MARTINEZ@SERVER.COM', 'DAVID.MARTINEZ@SERVER.COM', 'HM_Listado_de_Usuarios_por_Paí_060825 (1).xlsx', CURRENT_TIMESTAMP),
('HM_LISTADO_2025', 'ELENA.LOPEZ@SERVER.COM', 'ELENA.LOPEZ@SERVER.COM', 'HM_Listado_de_Usuarios_por_Paí_060825 (1).xlsx', CURRENT_TIMESTAMP),
('HM_LISTADO_2025', 'FERNANDO.GONZALEZ@SERVER.COM', 'FERNANDO.GONZALEZ@SERVER.COM', 'HM_Listado_de_Usuarios_por_Paí_060825 (1).xlsx', CURRENT_TIMESTAMP),
('HM_LISTADO_2025', 'GABRIELA.HERNANDEZ@SERVER.COM', 'GABRIELA.HERNANDEZ@SERVER.COM', 'HM_Listado_de_Usuarios_por_Paí_060825 (1).xlsx', CURRENT_TIMESTAMP),
('HM_LISTADO_2025', 'HECTOR.TORRES@SERVER.COM', 'HECTOR.TORRES@SERVER.COM', 'HM_Listado_de_Usuarios_por_Paí_060825 (1).xlsx', CURRENT_TIMESTAMP);

-- NOTA: Este es un ejemplo con 10 usuarios de muestra
-- Para cargar los 1604 usuarios reales, se debe:
-- 1. Procesar el archivo Excel completo con un script
-- 2. O usar la aplicación web para cargar el archivo directamente
-- 3. O ejecutar el procedimiento almacenado con los datos correctos

-- =============================================================
-- Verificación de datos insertados
-- =============================================================
SELECT 
    COUNT(*) as TotalUsuarios,
    COUNT(CASE WHEN "Usuario" IS NOT NULL AND "Usuario" != '' THEN 1 END) as UsuariosConNombre,
    COUNT(CASE WHEN "Correo" IS NOT NULL AND "Correo" != '' THEN 1 END) as UsuariosConCorreo
FROM "Stg_UsuariosExcel";

-- Ver algunos usuarios de ejemplo
SELECT "Usuario", "Correo", "CargadoEn"
FROM "Stg_UsuariosExcel"
ORDER BY "CargadoEn"
LIMIT 10;