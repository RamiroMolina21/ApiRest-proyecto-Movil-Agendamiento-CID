-- =====================================================================
-- Fecha: 2026-11-11   Rama: feature/jwt-auth
-- Cambios NO destructivos e idempotentes (se puede ejecutar varias veces):
--   A) Usuario.activo BIT NOT NULL DEFAULT 1  → todas las cuentas existentes quedan activas.
--   B) Tabla AsuntoAtendido (casos del dashboard web marcados como atendidos).
-- Requisito: la API de esta rama necesita ambos cambios (EF mapea Usuario.activo y AsuntoAtendido).
-- Hacer BACKUP DATABASE antes de ejecutar en una BD con datos.
-- =====================================================================

USE proyectoagendamientov4;
GO

SET XACT_ABORT ON;
GO

-- ---------------------------------------------------------------------
-- A) Usuario.activo
-- ---------------------------------------------------------------------
IF COL_LENGTH('dbo.Usuario', 'activo') IS NULL
BEGIN
    -- Con DEFAULT + NOT NULL, SQL Server rellena las filas existentes con 1 (operación de solo metadatos).
    ALTER TABLE dbo.Usuario
        ADD activo BIT NOT NULL CONSTRAINT DF_Usuario_activo DEFAULT (1);
END
GO

-- ---------------------------------------------------------------------
-- B) AsuntoAtendido
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.AsuntoAtendido', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AsuntoAtendido (
        idAsuntoAtendido   INT IDENTITY(1,1) NOT NULL,
        tipo               VARCHAR(40)  NOT NULL,   -- lista cerrada validada por la API
        clave              INT          NOT NULL,   -- idTutoria o idHorario según el tipo
        Usuario_idUsuario  INT          NOT NULL,   -- quién lo atendió
        fecha              DATETIME2    NOT NULL CONSTRAINT DF_AsuntoAtendido_fecha DEFAULT (SYSUTCDATETIME()), -- UTC
        CONSTRAINT PK_AsuntoAtendido PRIMARY KEY (idAsuntoAtendido),
        CONSTRAINT uq_AsuntoAtendido_tipo_clave UNIQUE (tipo, clave),
        CONSTRAINT FK_AsuntoAtendido_Usuario FOREIGN KEY (Usuario_idUsuario)
            REFERENCES dbo.Usuario (idUsuario) ON DELETE NO ACTION ON UPDATE NO ACTION
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AsuntoAtendido_Usuario' AND object_id = OBJECT_ID('dbo.AsuntoAtendido'))
    CREATE INDEX IX_AsuntoAtendido_Usuario ON dbo.AsuntoAtendido (Usuario_idUsuario);
GO

-- Comprobación
SELECT COUNT(*) AS usuarios, SUM(CASE WHEN activo = 1 THEN 1 ELSE 0 END) AS activos FROM dbo.Usuario;
SELECT COUNT(*) AS asuntosAtendidos FROM dbo.AsuntoAtendido;
GO

-- ---------------------------------------------------------------------
-- Reversión (solo si hiciera falta; DESTRUCTIVA, pierde los datos de estas columnas/tabla):
--   DROP TABLE dbo.AsuntoAtendido;
--   ALTER TABLE dbo.Usuario DROP CONSTRAINT DF_Usuario_activo;
--   ALTER TABLE dbo.Usuario DROP COLUMN activo;
-- ---------------------------------------------------------------------
