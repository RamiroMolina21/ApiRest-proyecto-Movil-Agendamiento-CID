

CREATE DATABASE proyectoagendamientov3;
GO

USE proyectoagendamientov3;
GO

-- =========================================
-- TABLA Rol
-- =========================================
CREATE TABLE Rol (
    idRol INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    tipoRol VARCHAR(45) NOT NULL
);
GO

-- =========================================
-- TABLA Usuario
-- =========================================
CREATE TABLE Usuario (
    idUsuario INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    nombres VARCHAR(60) NOT NULL,
    apellidos VARCHAR(60) NOT NULL,
    correo VARCHAR(60) NOT NULL,
    contrasenaHash VARCHAR(100) NOT NULL,
    fechaRegistro DATETIME NOT NULL,
    Rol_idRol INT NOT NULL,
    CONSTRAINT fk_Usuario_Rol1
        FOREIGN KEY (Rol_idRol)
        REFERENCES Rol(idRol)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

-- =========================================
-- TABLA Notificacion
-- =========================================
CREATE TABLE Notificacion (
    idNotificacion INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    fecha DATETIME NOT NULL,
    asunto VARCHAR(45) NOT NULL,
    descripcion VARCHAR(250) NOT NULL,
    Usuario_idUsuario INT NOT NULL,
    CONSTRAINT fk_Notificacion_Usuario1
        FOREIGN KEY (Usuario_idUsuario)
        REFERENCES Usuario(idUsuario)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

-- =========================================
-- TABLA Horario
-- =========================================
CREATE TABLE Horario (
    idHorario INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    fechaInicio DATE NOT NULL,
    fechaFin DATE NOT NULL,
    horaInicio DATETIME NOT NULL,
    horaFin DATETIME NOT NULL,
    cupos VARCHAR(60) NOT NULL,
    espacio VARCHAR(60) NOT NULL,
    estado VARCHAR(60) NOT NULL,
    Usuario_idUsuario INT NOT NULL,
    CONSTRAINT fk_Horario_Usuario1
        FOREIGN KEY (Usuario_idUsuario)
        REFERENCES Usuario(idUsuario)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

-- =========================================
-- TABLA Tutoria
-- =========================================
CREATE TABLE Tutoria (
    idTutoria INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    idioma VARCHAR(45) NOT NULL,
    nivel VARCHAR(45) NOT NULL,
    tema VARCHAR(45) NOT NULL,
    modalidad VARCHAR(45) NOT NULL,
    estado VARCHAR(45) NOT NULL,
    fechaHora DATETIME NOT NULL,
    Usuario_idUsuario INT NOT NULL,
    Horario_idHorario INT NOT NULL,
    CONSTRAINT fk_Tutoria_Usuario1
        FOREIGN KEY (Usuario_idUsuario)
        REFERENCES Usuario(idUsuario)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT fk_Tutoria_Horario1
        FOREIGN KEY (Horario_idHorario)
        REFERENCES Horario(idHorario)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

-- =========================================
-- TABLA Informe
-- =========================================
CREATE TABLE Informe (
    idInforme INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    descripcion VARCHAR(250) NOT NULL,
    fechaGeneracion DATETIME NOT NULL,
    Tutoria_idTutoria INT NOT NULL,
    Usuario_idUsuario INT NOT NULL,
    CONSTRAINT fk_Informe_Tutoria1
        FOREIGN KEY (Tutoria_idTutoria)
        REFERENCES Tutoria(idTutoria)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT fk_Informe_Usuario1
        FOREIGN KEY (Usuario_idUsuario)
        REFERENCES Usuario(idUsuario)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

-- =========================================
-- TABLA Retroalimentacion
-- =========================================
CREATE TABLE Retroalimentacion (
    idRetroalimentacion INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    comentario VARCHAR(300) NOT NULL,
    calificacion DECIMAL(3,2) NOT NULL,
    fecha DATETIME NOT NULL,
    Tutoria_idTutoria INT NOT NULL,
    Usuario_idUsuario INT NOT NULL,
    CONSTRAINT fk_Retroalimentacion_Tutoria1
        FOREIGN KEY (Tutoria_idTutoria)
        REFERENCES Tutoria(idTutoria)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT fk_Retroalimentacion_Usuario1
        FOREIGN KEY (Usuario_idUsuario)
        REFERENCES Usuario(idUsuario)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

-- =========================================
-- TABLA TUtoriaEstudiante (TABLA INTERMEDIA)
-- =========================================
CREATE TABLE TutoriaEstudiante (
    idTutoriaEstudiante INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Tutoria_idTutoria INT NOT NULL,
    Usuario_idUsuario INT NOT NULL,
    CONSTRAINT fk_TutoriaEstudiante_Tutoria1
        FOREIGN KEY (Tutoria_idTutoria)
        REFERENCES Tutoria(idTutoria)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT fk_TutoriaEstudiante_Usuario1
        FOREIGN KEY (Usuario_idUsuario)
        REFERENCES Usuario(idUsuario)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT uk_TutoriaEstudiante_Unique
        UNIQUE (Tutoria_idTutoria, Usuario_idUsuario)
);
GO    



-- =========================================
-- INSERTS (SIN ID MANUAL)
-- =========================================

-- === Tabla Rol ===
INSERT INTO Rol (tipoRol) VALUES
('Administrador'),
('Tutor'),
('Docente'),
('Estudiante'),
('Auxiliar');
GO

-- === Tabla Usuario ===
INSERT INTO Usuario (nombres, apellidos, correo, contrasenaHash, fechaRegistro, Rol_idRol) VALUES
('Carlos', 'P�rez', 'carlos.perez@example.com', 'hash12345', GETDATE(), 2),
('Mar�a', 'G�mez', 'maria.gomez@example.com', 'hash54321', GETDATE(), 2),
('Luis', 'Rodr�guez', 'luis.rodriguez@example.com', 'hash11111', GETDATE(), 3),
('Ana', 'Mart�nez', 'ana.martinez@example.com', 'hash22222', GETDATE(), 3),
('Jorge', 'L�pez', 'jorge.lopez@example.com', 'hash33333', GETDATE(), 4);
GO

-- === Tabla Notificacion ===
INSERT INTO Notificacion (fecha, asunto, descripcion, Usuario_idUsuario) VALUES
(GETDATE(), 'Nueva tutor�a', 'Tienes una nueva tutor�a programada.', 2),
(GETDATE(), 'Cambio de horario', 'Tu tutor�a ha cambiado de horario.', 3),
(GETDATE(), 'Retroalimentaci�n disponible', 'Tu retroalimentaci�n est� lista.', 3),
(GETDATE(), 'Nueva notificaci�n', 'Revisa tus mensajes.', 1),
(GETDATE(), 'Recordatorio', 'No olvides asistir a la tutor�a de ma�ana.', 4);
GO

-- === Tabla Horario ===
INSERT INTO Horario (fechaInicio, fechaFin, horaInicio, horaFin, cupos, espacio, estado, Usuario_idUsuario) VALUES
('2025-10-20', '2025-10-20', '2025-10-20 08:00:00', '2025-10-20 09:00:00', '10', 'Sala A', 'Disponible', 2),
('2025-10-21', '2025-10-21', '2025-10-21 09:00:00', '2025-10-21 10:00:00', '8', 'Sala B', 'Ocupado', 2),
('2025-10-22', '2025-10-22', '2025-10-22 10:00:00', '2025-10-22 11:00:00', '12', 'Sala C', 'Disponible', 2),
('2025-10-23', '2025-10-23', '2025-10-23 11:00:00', '2025-10-23 12:00:00', '6', 'Sala D', 'Disponible', 2),
('2025-10-24', '2025-10-24', '2025-10-24 12:00:00', '2025-10-24 13:00:00', '5', 'Sala E', 'Cancelado', 2);
GO

-- === Tabla Tutoria ===
INSERT INTO Tutoria (idioma, nivel, tema, modalidad, estado, fechaHora, Usuario_idUsuario, Horario_idHorario) VALUES
('Ingl�s', 'B�sico', 'Saludos y presentaciones', 'Virtual', 'Programada', '2025-10-20 08:00:00', 3, 1),
('Franc�s', 'Intermedio', 'Conversaci�n', 'Presencial', 'Completada', '2025-10-21 09:00:00', 3, 2),
('Alem�n', 'Avanzado', 'Gram�tica', 'Virtual', 'Cancelada', '2025-10-22 10:00:00', 4, 3),
('Italiano', 'B�sico', 'Pronunciaci�n', 'Presencial', 'Programada', '2025-10-23 11:00:00', 4, 4),
('Portugu�s', 'Intermedio', 'Conversaci�n informal', 'Virtual', 'Programada', '2025-10-24 12:00:00', 3, 5);
GO

-- === Tabla Informe ===
INSERT INTO Informe (descripcion, fechaGeneracion, Tutoria_idTutoria, Usuario_idUsuario) VALUES
('Informe de asistencia tutor�a ingl�s', GETDATE(), 1, 2),
('Resumen tutor�a franc�s', GETDATE(), 2, 2),
('Evaluaci�n tutor�a alem�n', GETDATE(), 3, 2),
('Reporte tutor�a italiano', GETDATE(), 4, 2),
('Informe tutor�a portugu�s', GETDATE(), 5, 2);
GO

-- === Tabla Retroalimentacion ===
INSERT INTO Retroalimentacion (comentario, calificacion, fecha, Tutoria_idTutoria, Usuario_idUsuario) VALUES
('Excelente clase, muy clara.', 4.80, GETDATE(), 1, 3),
('Buen manejo del tema.', 4.50, GETDATE(), 2, 3),
('Podr�a mejorar la pronunciaci�n.', 3.80, GETDATE(), 3, 4),
('Muy buena tutor�a.', 4.90, GETDATE(), 4, 4),
('Contenido interesante.', 4.70, GETDATE(), 5, 3);
GO