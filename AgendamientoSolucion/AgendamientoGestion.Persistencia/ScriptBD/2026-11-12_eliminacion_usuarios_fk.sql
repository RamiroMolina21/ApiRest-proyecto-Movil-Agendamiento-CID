-- =====================================================================
-- Fecha: 2026-11-12   Rama: feature/jwt-auth
-- Eliminación definitiva de usuarios sin bloqueos por relaciones.
-- NO destructivo para los datos existentes (no borra ni modifica filas): solo cambia nulabilidad de columnas
-- y la acción ON DELETE de las FKs hacia Usuario. Idempotente: se puede ejecutar varias veces.
--
--   Datos COMPARTIDOS (se conservan desvinculados → columna NULL + ON DELETE SET NULL):
--     Tutoria.Usuario_idUsuario            (tutorías que imparte: quedan "sin asignar")
--     Horario.Usuario_idUsuario            (horarios con tutorías: quedan "sin dueño")
--     Retroalimentacion.Usuario_idUsuario  (historial de la tutoría)
--     Informe.Usuario_idUsuario            (historial de la tutoría)
--     AsuntoAtendido.Usuario_idUsuario     (dashboard: quién lo atendió)
--   Datos PROPIOS (se borran con el usuario → ON DELETE CASCADE):
--     TutoriaEstudiante.Usuario_idUsuario  (sus inscripciones como estudiante)
--     Notificacion.Usuario_idUsuario       (sus notificaciones)
--
-- La API hace todo esto explícitamente en una transacción (UsuarioRepository.EliminarConDependenciasAsync);
-- las acciones de la BD son la red de seguridad si se borra un usuario por fuera de la API.
-- Requisito previo: 2026-11-11_usuario_activo_y_asuntos_atendidos.sql. Hacer BACKUP DATABASE antes.
-- =====================================================================

USE proyectoagendamientov4;
GO

SET XACT_ABORT ON;
GO

-- ---------------------------------------------------------------------
-- Tutoria.Usuario_idUsuario → NULL + ON DELETE SET NULL
-- ---------------------------------------------------------------------
BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_Tutoria_Usuario1' AND parent_object_id = OBJECT_ID('dbo.Tutoria')
               AND delete_referential_action_desc <> 'SET_NULL')
        ALTER TABLE dbo.Tutoria DROP CONSTRAINT fk_Tutoria_Usuario1;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Tutoria') AND name = 'Usuario_idUsuario' AND is_nullable = 0)
        ALTER TABLE dbo.Tutoria ALTER COLUMN Usuario_idUsuario INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_Tutoria_Usuario1' AND parent_object_id = OBJECT_ID('dbo.Tutoria'))
        ALTER TABLE dbo.Tutoria WITH CHECK ADD CONSTRAINT fk_Tutoria_Usuario1 FOREIGN KEY (Usuario_idUsuario)
            REFERENCES dbo.Usuario (idUsuario) ON DELETE SET NULL ON UPDATE NO ACTION;
COMMIT TRANSACTION;
GO

-- ---------------------------------------------------------------------
-- Horario.Usuario_idUsuario → NULL + ON DELETE SET NULL
-- ---------------------------------------------------------------------
BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_Horario_Usuario1' AND parent_object_id = OBJECT_ID('dbo.Horario')
               AND delete_referential_action_desc <> 'SET_NULL')
        ALTER TABLE dbo.Horario DROP CONSTRAINT fk_Horario_Usuario1;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Horario') AND name = 'Usuario_idUsuario' AND is_nullable = 0)
        ALTER TABLE dbo.Horario ALTER COLUMN Usuario_idUsuario INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_Horario_Usuario1' AND parent_object_id = OBJECT_ID('dbo.Horario'))
        ALTER TABLE dbo.Horario WITH CHECK ADD CONSTRAINT fk_Horario_Usuario1 FOREIGN KEY (Usuario_idUsuario)
            REFERENCES dbo.Usuario (idUsuario) ON DELETE SET NULL ON UPDATE NO ACTION;
COMMIT TRANSACTION;
GO

-- ---------------------------------------------------------------------
-- Retroalimentacion.Usuario_idUsuario → NULL + ON DELETE SET NULL
-- ---------------------------------------------------------------------
BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_Retroalimentacion_Usuario1' AND parent_object_id = OBJECT_ID('dbo.Retroalimentacion')
               AND delete_referential_action_desc <> 'SET_NULL')
        ALTER TABLE dbo.Retroalimentacion DROP CONSTRAINT fk_Retroalimentacion_Usuario1;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Retroalimentacion') AND name = 'Usuario_idUsuario' AND is_nullable = 0)
        ALTER TABLE dbo.Retroalimentacion ALTER COLUMN Usuario_idUsuario INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_Retroalimentacion_Usuario1' AND parent_object_id = OBJECT_ID('dbo.Retroalimentacion'))
        ALTER TABLE dbo.Retroalimentacion WITH CHECK ADD CONSTRAINT fk_Retroalimentacion_Usuario1 FOREIGN KEY (Usuario_idUsuario)
            REFERENCES dbo.Usuario (idUsuario) ON DELETE SET NULL ON UPDATE NO ACTION;
COMMIT TRANSACTION;
GO

-- ---------------------------------------------------------------------
-- Informe.Usuario_idUsuario → NULL + ON DELETE SET NULL
-- ---------------------------------------------------------------------
BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_Informe_Usuario1' AND parent_object_id = OBJECT_ID('dbo.Informe')
               AND delete_referential_action_desc <> 'SET_NULL')
        ALTER TABLE dbo.Informe DROP CONSTRAINT fk_Informe_Usuario1;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Informe') AND name = 'Usuario_idUsuario' AND is_nullable = 0)
        ALTER TABLE dbo.Informe ALTER COLUMN Usuario_idUsuario INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_Informe_Usuario1' AND parent_object_id = OBJECT_ID('dbo.Informe'))
        ALTER TABLE dbo.Informe WITH CHECK ADD CONSTRAINT fk_Informe_Usuario1 FOREIGN KEY (Usuario_idUsuario)
            REFERENCES dbo.Usuario (idUsuario) ON DELETE SET NULL ON UPDATE NO ACTION;
COMMIT TRANSACTION;
GO

-- ---------------------------------------------------------------------
-- AsuntoAtendido.Usuario_idUsuario → NULL + ON DELETE SET NULL
-- ---------------------------------------------------------------------
BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AsuntoAtendido_Usuario' AND parent_object_id = OBJECT_ID('dbo.AsuntoAtendido')
               AND delete_referential_action_desc <> 'SET_NULL')
        ALTER TABLE dbo.AsuntoAtendido DROP CONSTRAINT FK_AsuntoAtendido_Usuario;

    -- El índice depende de la columna: se quita para poder cambiar la nulabilidad y se recrea abajo.
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AsuntoAtendido_Usuario' AND object_id = OBJECT_ID('dbo.AsuntoAtendido'))
        AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AsuntoAtendido') AND name = 'Usuario_idUsuario' AND is_nullable = 0)
        DROP INDEX IX_AsuntoAtendido_Usuario ON dbo.AsuntoAtendido;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AsuntoAtendido') AND name = 'Usuario_idUsuario' AND is_nullable = 0)
        ALTER TABLE dbo.AsuntoAtendido ALTER COLUMN Usuario_idUsuario INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AsuntoAtendido_Usuario' AND parent_object_id = OBJECT_ID('dbo.AsuntoAtendido'))
        ALTER TABLE dbo.AsuntoAtendido WITH CHECK ADD CONSTRAINT FK_AsuntoAtendido_Usuario FOREIGN KEY (Usuario_idUsuario)
            REFERENCES dbo.Usuario (idUsuario) ON DELETE SET NULL ON UPDATE NO ACTION;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AsuntoAtendido_Usuario' AND object_id = OBJECT_ID('dbo.AsuntoAtendido'))
        CREATE INDEX IX_AsuntoAtendido_Usuario ON dbo.AsuntoAtendido (Usuario_idUsuario);
COMMIT TRANSACTION;
GO

-- ---------------------------------------------------------------------
-- TutoriaEstudiante.Usuario_idUsuario → ON DELETE CASCADE (dato propio del usuario)
-- ---------------------------------------------------------------------
BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_TutoriaEstudiante_Usuario1' AND parent_object_id = OBJECT_ID('dbo.TutoriaEstudiante')
               AND delete_referential_action_desc <> 'CASCADE')
        ALTER TABLE dbo.TutoriaEstudiante DROP CONSTRAINT fk_TutoriaEstudiante_Usuario1;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_TutoriaEstudiante_Usuario1' AND parent_object_id = OBJECT_ID('dbo.TutoriaEstudiante'))
        ALTER TABLE dbo.TutoriaEstudiante WITH CHECK ADD CONSTRAINT fk_TutoriaEstudiante_Usuario1 FOREIGN KEY (Usuario_idUsuario)
            REFERENCES dbo.Usuario (idUsuario) ON DELETE CASCADE ON UPDATE NO ACTION;
COMMIT TRANSACTION;
GO

-- ---------------------------------------------------------------------
-- Notificacion.Usuario_idUsuario → ON DELETE CASCADE (dato propio del usuario)
-- ---------------------------------------------------------------------
BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_Notificacion_Usuario1' AND parent_object_id = OBJECT_ID('dbo.Notificacion')
               AND delete_referential_action_desc <> 'CASCADE')
        ALTER TABLE dbo.Notificacion DROP CONSTRAINT fk_Notificacion_Usuario1;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_Notificacion_Usuario1' AND parent_object_id = OBJECT_ID('dbo.Notificacion'))
        ALTER TABLE dbo.Notificacion WITH CHECK ADD CONSTRAINT fk_Notificacion_Usuario1 FOREIGN KEY (Usuario_idUsuario)
            REFERENCES dbo.Usuario (idUsuario) ON DELETE CASCADE ON UPDATE NO ACTION;
COMMIT TRANSACTION;
GO

-- Comprobación
SELECT OBJECT_NAME(fk.parent_object_id) AS tabla, fk.name, fk.delete_referential_action_desc AS on_delete, c.is_nullable
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.referenced_object_id = OBJECT_ID('dbo.Usuario')
ORDER BY tabla;
GO

-- ---------------------------------------------------------------------
-- Reversión (solo si no hay filas con Usuario_idUsuario NULL; si las hay, primero hay que reasignarlas):
--   ALTER TABLE dbo.Tutoria DROP CONSTRAINT fk_Tutoria_Usuario1;
--   ALTER TABLE dbo.Tutoria ALTER COLUMN Usuario_idUsuario INT NOT NULL;
--   ALTER TABLE dbo.Tutoria ADD CONSTRAINT fk_Tutoria_Usuario1 FOREIGN KEY (Usuario_idUsuario) REFERENCES dbo.Usuario (idUsuario);
--   (ídem Horario, Retroalimentacion, Informe, AsuntoAtendido; TutoriaEstudiante y Notificacion: recrear sin CASCADE)
-- ---------------------------------------------------------------------
