-- =====================================================================
-- Script NO EJECUTADO automáticamente. Revisar y ejecutar manualmente.
-- Fecha: 2026-11-11   Rama: feature/jwt-auth
--
-- Objetivo (defensa en profundidad; la API ya valida esto en código y
-- NO depende de este script para arrancar):
--   1) UNIQUE en Usuario.correo  → evita cuentas duplicadas (login ambiguo).
--   2) UNIQUE filtrado en EncuestaSatisfaccion(Tutoria_idTutoria, estudianteCorreo)
--      → evita encuestas duplicadas por tutoría/estudiante (bug A2).
--
-- El script NO borra datos. Si encuentra duplicados se detiene y los lista:
-- decidan qué filas conservar y límpienlas a mano antes de reintentar.
-- Con la intercalación por defecto de SQL Server (CI) la unicidad del correo
-- es insensible a mayúsculas, que es lo deseado.
-- =====================================================================

USE proyectoagendamientov4;
GO

SET XACT_ABORT ON;
GO

-- ---------------------------------------------------------------------
-- 0) Diagnóstico: duplicados existentes
-- ---------------------------------------------------------------------
SELECT correo, COUNT(*) AS cantidad
FROM Usuario
GROUP BY correo
HAVING COUNT(*) > 1;

SELECT Tutoria_idTutoria, estudianteCorreo, COUNT(*) AS cantidad
FROM EncuestaSatisfaccion
WHERE estudianteCorreo IS NOT NULL
GROUP BY Tutoria_idTutoria, estudianteCorreo
HAVING COUNT(*) > 1;
GO

-- ---------------------------------------------------------------------
-- 1) UNIQUE Usuario.correo
-- ---------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM Usuario GROUP BY correo HAVING COUNT(*) > 1)
    THROW 50001, 'Hay correos duplicados en Usuario (ver diagnóstico). Límpielos antes de crear el índice.', 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'uq_Usuario_correo' AND object_id = OBJECT_ID('dbo.Usuario'))
    CREATE UNIQUE INDEX uq_Usuario_correo ON dbo.Usuario(correo);
GO

-- ---------------------------------------------------------------------
-- 2) UNIQUE filtrado EncuestaSatisfaccion(Tutoria_idTutoria, estudianteCorreo)
-- ---------------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM EncuestaSatisfaccion
    WHERE estudianteCorreo IS NOT NULL
    GROUP BY Tutoria_idTutoria, estudianteCorreo
    HAVING COUNT(*) > 1)
    THROW 50002, 'Hay encuestas duplicadas por tutoría/estudiante (ver diagnóstico). Límpielas antes de crear el índice.', 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'uq_Encuesta_Tutoria_Estudiante' AND object_id = OBJECT_ID('dbo.EncuestaSatisfaccion'))
    CREATE UNIQUE INDEX uq_Encuesta_Tutoria_Estudiante
        ON dbo.EncuestaSatisfaccion(Tutoria_idTutoria, estudianteCorreo)
        WHERE estudianteCorreo IS NOT NULL;
GO

-- ---------------------------------------------------------------------
-- Reversión (si hiciera falta):
--   DROP INDEX uq_Usuario_correo ON dbo.Usuario;
--   DROP INDEX uq_Encuesta_Tutoria_Estudiante ON dbo.EncuestaSatisfaccion;
-- ---------------------------------------------------------------------
