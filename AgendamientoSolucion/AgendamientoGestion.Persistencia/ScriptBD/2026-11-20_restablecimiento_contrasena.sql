-- =====================================================================
-- Fecha: 2026-11-20   Rama: feature/marca-cdi
-- "¿Olvidaste tu contraseña?" (POST /api/Auth/olvide-contrasena y /restablecer-contrasena).
-- NO destructivo e idempotente (se puede ejecutar varias veces):
--   1) Usuario.credencialesCambiadasEn DATETIME2 NULL: momento (UTC) del último restablecimiento; los JWT
--      emitidos antes dejan de valer. Las filas existentes quedan en NULL (sin efecto).
--   2) Tabla RestablecimientoContrasena.
-- SIN "USE": ejecutar CONECTADO a la base destino (local o Azure SQL). Hacer copia de seguridad antes.
-- =====================================================================

SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.Usuario', 'credencialesCambiadasEn') IS NULL
    ALTER TABLE dbo.Usuario ADD credencialesCambiadasEn DATETIME2 NULL;
GO

-- RestablecimientoContrasena ("¿Olvidaste tu contraseña?"): solo el hash SHA-256 del token, un solo uso.
IF OBJECT_ID('dbo.RestablecimientoContrasena', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RestablecimientoContrasena (
        idRestablecimiento INT IDENTITY(1,1) NOT NULL,
        Usuario_idUsuario  INT          NOT NULL,
        tokenHash          CHAR(64)     NOT NULL,   -- SHA-256 en hexadecimal; el token en claro nunca se guarda
        fechaCreacion      DATETIME2    NOT NULL,   -- UTC
        fechaExpiracion    DATETIME2    NOT NULL,   -- UTC
        fechaUso           DATETIME2    NULL,       -- UTC; usado o invalidado
        CONSTRAINT PK_RestablecimientoContrasena PRIMARY KEY (idRestablecimiento),
        CONSTRAINT FK_RestablecimientoContrasena_Usuario FOREIGN KEY (Usuario_idUsuario)
            REFERENCES dbo.Usuario (idUsuario) ON DELETE CASCADE ON UPDATE NO ACTION
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'uq_RestablecimientoContrasena_tokenHash' AND object_id = OBJECT_ID('dbo.RestablecimientoContrasena'))
    CREATE UNIQUE INDEX uq_RestablecimientoContrasena_tokenHash ON dbo.RestablecimientoContrasena (tokenHash);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RestablecimientoContrasena_Usuario' AND object_id = OBJECT_ID('dbo.RestablecimientoContrasena'))
    CREATE INDEX IX_RestablecimientoContrasena_Usuario ON dbo.RestablecimientoContrasena (Usuario_idUsuario);
GO
-- Comprobación
SELECT COL_LENGTH('dbo.Usuario', 'credencialesCambiadasEn') AS columna_credenciales,
       OBJECT_ID('dbo.RestablecimientoContrasena', 'U') AS tabla_restablecimiento;
GO

-- Reversión (DESTRUCTIVA para estos datos):
--   DROP TABLE dbo.RestablecimientoContrasena;
--   ALTER TABLE dbo.Usuario DROP COLUMN credencialesCambiadasEn;