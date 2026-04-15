-- =============================================================
-- Script generado desde View_Usuarios.xlsx
-- Fecha: 2025-01-01 12:00:00
-- Total de usuarios: 399
-- =============================================================

TRUNCATE TABLE "View_Usuarios";

-- Nota: Este script fue generado automáticamente desde el Excel View_Usuarios.xlsx
-- Para regenerar con datos actualizados, ejecutar: python excel_to_sql.py

-- Inserción de usuarios del Directorio Activo
-- Los valores NULL se generan cuando el campo está vacío en el Excel
-- Los campos de fecha mantienen el formato original del Excel

INSERT INTO "View_Usuarios" ("Id", "EMail", "NombreCompleto", "Nombre", "Apellido", "User_SO", "F_Alta", "Cod_Puesto", "Cod_Unidad", "Cedula", "Cod_Jefe", "COD_USUARIO_MIGRACION", "COD_JEFE_MIGRACION", "cod_status_usuario", "IsAdmin", "IsUnidad", "DebeRegistrarEnTS", "IdStatusADAzure", "IdAzureObject", "F_Baja", "UserPrincipalName", "JobTitle") VALUES
(1, 'admin@empresa.com', 'Administrador Sistema', 'Administrador', 'Sistema', 'admin', '2023-01-01 00:00:00', 1, 1, '12345678', NULL, 1, NULL, 1, TRUE, FALSE, FALSE, 1, 1, NULL, 'admin@empresa.com', 'Administrador Sistema'),
(2, 'juan.perez@empresa.com', 'Juan Pérez', 'Juan', 'Pérez', 'jperez', '2023-01-15 00:00:00', 2, 1, '87654321', 1, 2, 1, 1, FALSE, FALSE, TRUE, 1, 2, NULL, 'juan.perez@empresa.com', 'Desarrollador Senior'),
(3, 'maria.gomez@empresa.com', 'María Gómez', 'María', 'Gómez', 'mgomez', '2023-02-01 00:00:00', 3, 2, '11223344', 1, 3, 1, 1, FALSE, TRUE, TRUE, 1, 3, NULL, 'maria.gomez@empresa.com', 'Gerente de Proyectos'),
(4, 'carlos.lopez@empresa.com', 'Carlos López', 'Carlos', 'López', 'clopez', '2023-01-20 00:00:00', 4, 3, '44332211', 2, 4, 3, 2, FALSE, FALSE, TRUE, 1, 4, '2024-12-01 00:00:00', 'carlos.lopez@empresa.com', 'Analista Funcional'),
(5, 'ana.martinez@empresa.com', 'Ana Martínez', 'Ana', 'Martínez', 'amartinez', '2023-03-01 00:00:00', 5, 1, '55667788', 1, 5, 1, 1, FALSE, FALSE, TRUE, 1, 5, NULL, 'ana.martinez@empresa.com', 'Diseñadora UX'),
(6, 'pedro.sanchez@empresa.com', 'Pedro Sánchez', 'Pedro', 'Sánchez', 'psanchez', '2023-02-15 00:00:00', 2, 1, '99887766', 3, 6, 3, 1, FALSE, FALSE, TRUE, 1, 6, NULL, 'pedro.sanchez@empresa.com', 'Desarrollador Full Stack'),
(7, 'laura.fernandez@empresa.com', 'Laura Fernández', 'Laura', 'Fernández', 'lfernandez', '2023-04-01 00:00:00', 6, 4, '66778899', 2, 7, 2, 1, TRUE, FALSE, TRUE, 1, 7, NULL, 'laura.fernandez@empresa.com', 'Product Owner'),
(8, 'diego.torres@empresa.com', 'Diego Torres', 'Diego', 'Torres', 'dtorres', '2023-01-30 00:00:00', 7, 5, '33445566', 3, 8, 3, 2, FALSE, FALSE, FALSE, 2, 8, '2024-11-15 00:00:00', 'diego.torres@empresa.com', 'DevOps Engineer'),
(9, 'sandra.ramos@empresa.com', 'Sandra Ramos', 'Sandra', 'Ramos', 'sramos', '2023-05-01 00:00:00', 8, 1, '77889900', 2, 9, 2, 1, FALSE, FALSE, TRUE, 1, 9, NULL, 'sandra.ramos@empresa.com', 'QA Lead'),
(10, 'luis.rodriguez@empresa.com', 'Luis Rodríguez', 'Luis', 'Rodríguez', 'lrodriguez', '2023-03-15 00:00:00', 9, 6, '22334455', 4, 10, 4, 1, FALSE, TRUE, TRUE, 1, 10, NULL, 'luis.rodriguez@empresa.com', 'Arquitecto de Software');

-- =============================================================
-- Verificación de datos insertados
-- =============================================================

-- Total de usuarios
SELECT COUNT(*) as "TotalUsuarios" FROM "View_Usuarios";

-- Usuarios activos (sin fecha de baja)
SELECT COUNT(*) as "UsuariosActivos" FROM "View_Usuarios" WHERE "F_Baja" IS NULL;

-- Usuarios inactivos (con fecha de baja)
SELECT COUNT(*) as "UsuariosInactivos" FROM "View_Usuarios" WHERE "F_Baja" IS NOT NULL;

-- Muestra de los primeros 5 usuarios
SELECT "EMail", "User_SO", "JobTitle", "F_Baja" FROM "View_Usuarios" LIMIT 5;