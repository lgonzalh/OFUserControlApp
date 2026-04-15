-- =============================================================
-- Verificacion y ajuste de objetos SQL Server para OFUserControlApp
-- Objetivo: dejar esquema consistente con el codigo C#
-- =============================================================

USE [CONTROLINTERNO];
GO

PRINT '=== Inicio verificacion de esquema OFUserControlApp ===';

-- -------------------------------------------------------------
-- Stg_UsuariosExcel
-- -------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[Stg_UsuariosExcel]', N'U') IS NULL
BEGIN
    PRINT 'Creando tabla dbo.Stg_UsuariosExcel...';
    CREATE TABLE [dbo].[Stg_UsuariosExcel] (
        [Id]            bigint IDENTITY(1,1) NOT NULL,
        [ProcesoId]     uniqueidentifier NOT NULL,
        [Usuario]       nvarchar(256) NULL,
        [Correo]        nvarchar(512) NULL,
        [FuenteArchivo] nvarchar(260) NOT NULL CONSTRAINT [DF_Stg_UsuariosExcel_FuenteArchivo] DEFAULT ('Excel'),
        [HashContenido] varbinary(32) NULL,
        [CargadoEn]     datetime2(0) NOT NULL CONSTRAINT [DF_Stg_UsuariosExcel_CargadoEn] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_Stg_UsuariosExcel] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
ELSE
BEGIN
    PRINT 'Tabla dbo.Stg_UsuariosExcel existe, verificando columnas...';

    IF COL_LENGTH('dbo.Stg_UsuariosExcel', 'ProcesoId') IS NULL
        ALTER TABLE [dbo].[Stg_UsuariosExcel] ADD [ProcesoId] uniqueidentifier NOT NULL CONSTRAINT [DF_Stg_UsuariosExcel_ProcesoId] DEFAULT ('00000000-0000-0000-0000-000000000000');

    IF COL_LENGTH('dbo.Stg_UsuariosExcel', 'FuenteArchivo') IS NULL
        ALTER TABLE [dbo].[Stg_UsuariosExcel] ADD [FuenteArchivo] nvarchar(260) NOT NULL CONSTRAINT [DF_Stg_UsuariosExcel_FuenteArchivo_2] DEFAULT ('Excel');

    IF COL_LENGTH('dbo.Stg_UsuariosExcel', 'HashContenido') IS NULL
        ALTER TABLE [dbo].[Stg_UsuariosExcel] ADD [HashContenido] varbinary(32) NULL;

    IF COL_LENGTH('dbo.Stg_UsuariosExcel', 'CargadoEn') IS NULL
        ALTER TABLE [dbo].[Stg_UsuariosExcel] ADD [CargadoEn] datetime2(0) NOT NULL CONSTRAINT [DF_Stg_UsuariosExcel_CargadoEn_2] DEFAULT (sysutcdatetime());
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Stg_UsuariosExcel_Proceso' AND object_id = OBJECT_ID(N'[dbo].[Stg_UsuariosExcel]'))
    CREATE INDEX [IX_Stg_UsuariosExcel_Proceso] ON [dbo].[Stg_UsuariosExcel]([ProcesoId]);
GO

-- -------------------------------------------------------------
-- Rpt_UsuariosCruce
-- -------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[Rpt_UsuariosCruce]', N'U') IS NULL
BEGIN
    PRINT 'Creando tabla dbo.Rpt_UsuariosCruce...';
    CREATE TABLE [dbo].[Rpt_UsuariosCruce] (
        [Id]           bigint IDENTITY(1,1) NOT NULL,
        [UsuarioExcel] nvarchar(256) NULL,
        [CorreoExcel]  nvarchar(512) NULL,
        [EmailView]    nvarchar(512) NULL,
        [UPNView]      nvarchar(256) NULL,
        [F_Baja]       nvarchar(50) NULL,
        [Estado]       nvarchar(10) NOT NULL,
        [ProcesoId]    uniqueidentifier NOT NULL,
        [GeneradoEn]   datetime2(0) NOT NULL CONSTRAINT [DF_Rpt_UsuariosCruce_GeneradoEn] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_Rpt_UsuariosCruce] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
ELSE
BEGIN
    PRINT 'Tabla dbo.Rpt_UsuariosCruce existe, verificando columnas...';

    IF COL_LENGTH('dbo.Rpt_UsuariosCruce', 'ProcesoId') IS NULL
        ALTER TABLE [dbo].[Rpt_UsuariosCruce] ADD [ProcesoId] uniqueidentifier NOT NULL CONSTRAINT [DF_Rpt_UsuariosCruce_ProcesoId] DEFAULT ('00000000-0000-0000-0000-000000000000');

    IF COL_LENGTH('dbo.Rpt_UsuariosCruce', 'Estado') IS NULL
        ALTER TABLE [dbo].[Rpt_UsuariosCruce] ADD [Estado] nvarchar(10) NOT NULL CONSTRAINT [DF_Rpt_UsuariosCruce_Estado] DEFAULT (N'Inactivo');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Rpt_UsuariosCruce_Proceso' AND object_id = OBJECT_ID(N'[dbo].[Rpt_UsuariosCruce]'))
    CREATE INDEX [IX_Rpt_UsuariosCruce_Proceso] ON [dbo].[Rpt_UsuariosCruce]([ProcesoId]);
GO

-- -------------------------------------------------------------
-- LogProceso
-- -------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[LogProceso]', N'U') IS NULL
BEGIN
    PRINT 'Creando tabla dbo.LogProceso...';
    CREATE TABLE [dbo].[LogProceso] (
        [Id]        bigint IDENTITY(1,1) NOT NULL,
        [ProcesoId] uniqueidentifier NOT NULL,
        [Etapa]     nvarchar(50) NOT NULL,
        [Mensaje]   nvarchar(500) NULL,
        [Nivel]     nvarchar(20) NOT NULL,
        [FechaUtc]  datetime2(0) NOT NULL CONSTRAINT [DF_LogProceso_FechaUtc] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_LogProceso] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LogProceso_ProcesoId' AND object_id = OBJECT_ID(N'[dbo].[LogProceso]'))
    CREATE INDEX [IX_LogProceso_ProcesoId] ON [dbo].[LogProceso]([ProcesoId]);
GO

-- -------------------------------------------------------------
-- Stored Procedure de cruce
-- -------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[usp_CruzarUsuariosExcelConView]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_CruzarUsuariosExcelConView];
GO

CREATE PROCEDURE [dbo].[usp_CruzarUsuariosExcelConView]
    @ProcesoId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DELETE FROM [dbo].[Rpt_UsuariosCruce]
        WHERE [ProcesoId] = @ProcesoId;

        INSERT INTO [dbo].[Rpt_UsuariosCruce]
        (
            [UsuarioExcel],
            [CorreoExcel],
            [EmailView],
            [UPNView],
            [F_Baja],
            [Estado],
            [ProcesoId]
        )
        SELECT
            S.[Usuario],
            S.[Correo],
            V.[EMail],
            V.[UserPrincipalName],
            CONVERT(nvarchar(50), V.[F_Baja], 126),
            CASE WHEN V.[EMail] IS NOT NULL THEN N'Habilitado' ELSE N'Inactivo' END,
            @ProcesoId
        FROM [dbo].[Stg_UsuariosExcel] S
        LEFT JOIN [dbo].[View_Usuarios] V
            ON (S.[Correo] IS NOT NULL AND S.[Correo] = V.[EMail])
            OR (S.[Usuario] IS NOT NULL AND S.[Usuario] = V.[User_SO])
        WHERE S.[ProcesoId] = @ProcesoId;

        SELECT
            'SUCCESS' AS [Status],
            COUNT(1) AS [TotalProcesados],
            SUM(CASE WHEN [Estado] = N'Habilitado' THEN 1 ELSE 0 END) AS [Habilitados],
            SUM(CASE WHEN [Estado] = N'Inactivo' THEN 1 ELSE 0 END) AS [Inactivos],
            CAST(NULL AS nvarchar(4000)) AS [ErrorMessage]
        FROM [dbo].[Rpt_UsuariosCruce]
        WHERE [ProcesoId] = @ProcesoId;
    END TRY
    BEGIN CATCH
        SELECT
            'ERROR' AS [Status],
            0 AS [TotalProcesados],
            0 AS [Habilitados],
            0 AS [Inactivos],
            ERROR_MESSAGE() AS [ErrorMessage];
    END CATCH
END;
GO

-- -------------------------------------------------------------
-- Vista de estadisticas
-- -------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[vw_EstadisticasUsuarios]', N'V') IS NOT NULL
    DROP VIEW [dbo].[vw_EstadisticasUsuarios];
GO

CREATE VIEW [dbo].[vw_EstadisticasUsuarios]
AS
SELECT
    [ProcesoId],
    COUNT(1) AS [TotalRegistros],
    SUM(CASE WHEN [Estado] = N'Habilitado' THEN 1 ELSE 0 END) AS [Habilitados],
    SUM(CASE WHEN [Estado] = N'Inactivo' THEN 1 ELSE 0 END) AS [Inactivos],
    MIN([GeneradoEn]) AS [FechaInicio],
    MAX([GeneradoEn]) AS [FechaFin]
FROM [dbo].[Rpt_UsuariosCruce]
GROUP BY [ProcesoId];
GO

PRINT '=== Verificacion de esquema completada ===';
