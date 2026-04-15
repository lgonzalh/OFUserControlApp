-- =============================================================
-- Script para simular el Directorio Activo (View_Usuarios)
-- Generado con datos sintéticos para pruebas
-- =============================================================

-- Limpiar datos previos si existen
TRUNCATE TABLE "View_Usuarios";

-- Insertar datos sintéticos de prueba
INSERT INTO "View_Usuarios" (
    "EMail", 
    "NombreCompleto", 
    "Nombre", 
    "Apellido", 
    "User_SO", 
    "F_Alta", 
    "UserPrincipalName", 
    "JobTitle", 
    "F_Baja"
) VALUES 
-- Usuario Activo 1 (Coincidirá por Correo)
('juan.perez@empresa.com', 'Juan Perez', 'Juan', 'Perez', 'jperez', CURRENT_TIMESTAMP - INTERVAL '30 days', 'juan.perez@empresa.com', 'Desarrollador Senior', NULL),

-- Usuario Activo 2 (Coincidirá por Usuario / User_SO)
('maria.gomez@empresa.com', 'Maria Gomez', 'Maria', 'Gomez', 'mgomez', CURRENT_TIMESTAMP - INTERVAL '60 days', 'maria.gomez@empresa.com', 'Gerente de Proyectos', NULL),

-- Usuario Activo 3 (Sin cruce en el Excel simulado)
('luis.rodriguez@empresa.com', 'Luis Rodriguez', 'Luis', 'Rodriguez', 'lrodriguez', CURRENT_TIMESTAMP - INTERVAL '15 days', 'luis.rodriguez@empresa.com', 'Analista Financiero', NULL),

-- Usuario Inactivo (F_Baja tiene valor, lo que el cruce detectará como 'Inactivo')
('carlos.lopez@empresa.com', 'Carlos Lopez', 'Carlos', 'Lopez', 'clopez', CURRENT_TIMESTAMP - INTERVAL '100 days', 'carlos.lopez@empresa.com', 'Especialista de Soporte', CURRENT_TIMESTAMP - INTERVAL '5 days');

-- =============================================================
-- Datos adicionales para pruebas más robustas
-- =============================================================

INSERT INTO "View_Usuarios" (
    "EMail", 
    "NombreCompleto", 
    "Nombre", 
    "Apellido", 
    "User_SO", 
    "F_Alta", 
    "UserPrincipalName", 
    "JobTitle", 
    "F_Baja"
) VALUES 
-- Usuarios adicionales para pruebas de volumen
('ana.martinez@empresa.com', 'Ana Martinez', 'Ana', 'Martinez', 'amartinez', CURRENT_TIMESTAMP - INTERVAL '45 days', 'ana.martinez@empresa.com', 'Diseñadora UX', NULL),
('pedro.sanchez@empresa.com', 'Pedro Sanchez', 'Pedro', 'Sanchez', 'psanchez', CURRENT_TIMESTAMP - INTERVAL '90 days', 'pedro.sanchez@empresa.com', 'Arquitecto de Software', NULL),
('laura.fernandez@empresa.com', 'Laura Fernandez', 'Laura', 'Fernandez', 'lfernandez', CURRENT_TIMESTAMP - INTERVAL '20 days', 'laura.fernandez@empresa.com', 'Product Owner', NULL),
('diego.torres@empresa.com', 'Diego Torres', 'Diego', 'Torres', 'dtorres', CURRENT_TIMESTAMP - INTERVAL '75 days', 'diego.torres@empresa.com', 'DevOps Engineer', CURRENT_TIMESTAMP - INTERVAL '10 days'),
('sandra.ramos@empresa.com', 'Sandra Ramos', 'Sandra', 'Ramos', 'sramos', CURRENT_TIMESTAMP - INTERVAL '35 days', 'sandra.ramos@empresa.com', 'QA Lead', NULL);

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