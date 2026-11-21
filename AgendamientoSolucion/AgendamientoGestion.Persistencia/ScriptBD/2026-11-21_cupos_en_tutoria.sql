-- =====================================================================
-- Fecha: 2026-11-21   Rama: feature/aula-opcional
-- La capacidad (máximo de estudiantes) pasa del horario a la tutoría:
--   Tutoria.cupos INT NULL. Las filas existentes quedan en NULL → la API usa los cupos de su horario
--   (Horario.cupos, sin cambios de esquema; desde ahora puede guardarse "").
-- NO destructivo e idempotente (se puede ejecutar varias veces).
-- SIN "USE": ejecutar CONECTADO a la base destino (local o Azure SQL). Hacer copia de seguridad antes.
-- ORDEN: ejecutar ANTES de desplegar la API que lee Tutoria.cupos (si no, toda consulta de tutorías falla).
-- =====================================================================

SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.Tutoria', 'cupos') IS NULL
    EXEC('ALTER TABLE dbo.Tutoria ADD cupos INT NULL;');
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'ck_Tutoria_cupos' AND parent_object_id = OBJECT_ID('dbo.Tutoria'))
    EXEC('ALTER TABLE dbo.Tutoria ADD CONSTRAINT ck_Tutoria_cupos CHECK (cupos IS NULL OR cupos BETWEEN 1 AND 1000);');
GO

-- Comprobación
SELECT COL_LENGTH('dbo.Tutoria', 'cupos') AS columna_cupos,
       OBJECT_ID('dbo.ck_Tutoria_cupos', 'C') AS check_cupos;
GO

-- Reversión (DESTRUCTIVA para estos datos):
--   ALTER TABLE dbo.Tutoria DROP CONSTRAINT ck_Tutoria_cupos;
--   ALTER TABLE dbo.Tutoria DROP COLUMN cupos;
