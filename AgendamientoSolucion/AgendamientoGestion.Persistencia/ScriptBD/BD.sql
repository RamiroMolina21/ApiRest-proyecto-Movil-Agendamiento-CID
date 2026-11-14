-- =====================================================================
-- TutorPro (Agendamiento) - Esquema COMPLETO de la base de datos
-- Actualizado: 2026-11-13
--
-- Crea desde cero el esquema actual (equivalente a la BD de desarrollo
-- proyectoagendamientov4 tras los scripts 2026-11-11 y 2026-11-12).
--
-- USO:
--   * Ejecutar CONECTADO A LA BASE DE DATOS DESTINO, ya creada y vacía.
--     No contiene CREATE DATABASE ni USE, por lo que funciona igual en
--     SQL Server local y en Azure SQL Database.
--   * Intercalación recomendada de la base: Modern_Spanish_CI_AS.
--     En local:  CREATE DATABASE proyectoagendamientov4 COLLATE Modern_Spanish_CI_AS;
--     En Azure:  elegir esa intercalación al crear la base de datos.
--
-- DATOS: solo inserta los 5 roles (los ids los usa el código).
-- No crea usuarios: el primer Administrador debe insertarse con un hash
-- BCrypt válido (o migrar los datos de una base existente).
--
-- Los scripts 2026-11-11_* y 2026-11-12_* de esta carpeta sirven para
-- ACTUALIZAR una base creada con la versión anterior de este archivo;
-- no hace falta ejecutarlos sobre una base creada con este script.
-- =====================================================================

SET XACT_ABORT ON;
GO

-- =========================================
-- TABLA Rol
-- =========================================
CREATE TABLE Rol (
    idRol INT IDENTITY(1,1) NOT NULL,
    tipoRol VARCHAR(45) NOT NULL,
    CONSTRAINT PK_Rol PRIMARY KEY (idRol)
);
GO

-- =========================================
-- TABLA Usuario
-- =========================================
CREATE TABLE Usuario (
    idUsuario INT IDENTITY(1,1) NOT NULL,
    nombres VARCHAR(60) NOT NULL,
    apellidos VARCHAR(60) NOT NULL,
    correo VARCHAR(60) NOT NULL,
    contrasenaHash VARCHAR(100) NOT NULL,
    fechaRegistro DATETIME NOT NULL,
    Rol_idRol INT NOT NULL,
    activo BIT NOT NULL CONSTRAINT DF_Usuario_activo DEFAULT (1),
    CONSTRAINT PK_Usuario PRIMARY KEY (idUsuario),
    CONSTRAINT fk_Usuario_Rol1
        FOREIGN KEY (Rol_idRol) REFERENCES Rol(idRol)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO

-- =========================================
-- TABLA Notificacion (historial de correos enviados)
-- Se borra con el usuario (CASCADE).
-- =========================================
CREATE TABLE Notificacion (
    idNotificacion INT IDENTITY(1,1) NOT NULL,
    fecha DATETIME NOT NULL,
    asunto VARCHAR(45) NOT NULL,
    descripcion VARCHAR(250) NOT NULL,
    Usuario_idUsuario INT NOT NULL,
    CONSTRAINT PK_Notificacion PRIMARY KEY (idNotificacion),
    CONSTRAINT fk_Notificacion_Usuario1
        FOREIGN KEY (Usuario_idUsuario) REFERENCES Usuario(idUsuario)
        ON DELETE CASCADE ON UPDATE NO ACTION
);
GO

-- =========================================
-- TABLA Horario
-- El dueño puede quedar en NULL si se elimina el usuario (SET NULL).
-- =========================================
CREATE TABLE Horario (
    idHorario INT IDENTITY(1,1) NOT NULL,
    titulo VARCHAR(100) NOT NULL,
    fechaInicio DATE NOT NULL,
    fechaFin DATE NOT NULL,
    horaInicio DATETIME NOT NULL,
    horaFin DATETIME NOT NULL,
    cupos VARCHAR(60) NOT NULL,
    espacio VARCHAR(60) NOT NULL,
    estado VARCHAR(60) NOT NULL,
    Usuario_idUsuario INT NULL,
    CONSTRAINT PK_Horario PRIMARY KEY (idHorario),
    CONSTRAINT fk_Horario_Usuario1
        FOREIGN KEY (Usuario_idUsuario) REFERENCES Usuario(idUsuario)
        ON DELETE SET NULL ON UPDATE NO ACTION
);
GO

-- =========================================
-- TABLA Tutoria
-- El asignado (docente/tutor) puede quedar en NULL (SET NULL).
-- =========================================
CREATE TABLE Tutoria (
    idTutoria INT IDENTITY(1,1) NOT NULL,
    idioma VARCHAR(45) NOT NULL,
    nivel VARCHAR(45) NOT NULL,
    tema VARCHAR(45) NOT NULL,
    modalidad VARCHAR(45) NOT NULL,
    estado VARCHAR(45) NOT NULL,
    fechaHora DATETIME NOT NULL,
    Usuario_idUsuario INT NULL,
    Horario_idHorario INT NOT NULL,
    CONSTRAINT PK_Tutoria PRIMARY KEY (idTutoria),
    CONSTRAINT fk_Tutoria_Usuario1
        FOREIGN KEY (Usuario_idUsuario) REFERENCES Usuario(idUsuario)
        ON DELETE SET NULL ON UPDATE NO ACTION,
    CONSTRAINT fk_Tutoria_Horario1
        FOREIGN KEY (Horario_idHorario) REFERENCES Horario(idHorario)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO

-- =========================================
-- TABLA Informe
-- =========================================
CREATE TABLE Informe (
    idInforme INT IDENTITY(1,1) NOT NULL,
    descripcion VARCHAR(250) NOT NULL,
    fechaGeneracion DATETIME NOT NULL,
    Tutoria_idTutoria INT NOT NULL,
    Usuario_idUsuario INT NULL,
    CONSTRAINT PK_Informe PRIMARY KEY (idInforme),
    CONSTRAINT fk_Informe_Tutoria1
        FOREIGN KEY (Tutoria_idTutoria) REFERENCES Tutoria(idTutoria)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_Informe_Usuario1
        FOREIGN KEY (Usuario_idUsuario) REFERENCES Usuario(idUsuario)
        ON DELETE SET NULL ON UPDATE NO ACTION
);
GO

-- =========================================
-- TABLA Retroalimentacion
-- =========================================
CREATE TABLE Retroalimentacion (
    idRetroalimentacion INT IDENTITY(1,1) NOT NULL,
    comentario VARCHAR(300) NOT NULL,
    calificacion DECIMAL(3,2) NOT NULL,
    fecha DATETIME NOT NULL,
    Tutoria_idTutoria INT NOT NULL,
    Usuario_idUsuario INT NULL,
    CONSTRAINT PK_Retroalimentacion PRIMARY KEY (idRetroalimentacion),
    CONSTRAINT fk_Retroalimentacion_Tutoria1
        FOREIGN KEY (Tutoria_idTutoria) REFERENCES Tutoria(idTutoria)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_Retroalimentacion_Usuario1
        FOREIGN KEY (Usuario_idUsuario) REFERENCES Usuario(idUsuario)
        ON DELETE SET NULL ON UPDATE NO ACTION
);
GO

-- =========================================
-- TABLA TutoriaEstudiante (inscripciones N:M)
-- Se borra con el estudiante (CASCADE).
-- =========================================
CREATE TABLE TutoriaEstudiante (
    idTutoriaEstudiante INT IDENTITY(1,1) NOT NULL,
    Tutoria_idTutoria INT NOT NULL,
    Usuario_idUsuario INT NOT NULL,
    CONSTRAINT PK_TutoriaEstudiante PRIMARY KEY (idTutoriaEstudiante),
    CONSTRAINT fk_TutoriaEstudiante_Tutoria1
        FOREIGN KEY (Tutoria_idTutoria) REFERENCES Tutoria(idTutoria)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT fk_TutoriaEstudiante_Usuario1
        FOREIGN KEY (Usuario_idUsuario) REFERENCES Usuario(idUsuario)
        ON DELETE CASCADE ON UPDATE NO ACTION,
    CONSTRAINT uk_TutoriaEstudiante_Unique
        UNIQUE (Tutoria_idTutoria, Usuario_idUsuario)
);
GO

-- =========================================
-- TABLA EncuestaSatisfaccion
-- Datos del estudiante desnormalizados (sin FK a Usuario).
-- =========================================
CREATE TABLE EncuestaSatisfaccion (
    idEncuesta INT IDENTITY(1,1) NOT NULL,
    calificacion INT NOT NULL,
    comentario NVARCHAR(300) NULL,
    fechaRespuesta DATETIME2 NULL,
    tokenRespuesta NVARCHAR(100) NOT NULL,
    tokenUsado BIT NOT NULL CONSTRAINT DF_EncuestaSatisfaccion_tokenUsado DEFAULT (0),
    estudianteNombre NVARCHAR(100) NOT NULL,
    estudianteCorreo NVARCHAR(100) NOT NULL,
    Tutoria_idTutoria INT NOT NULL,
    CONSTRAINT PK_EncuestaSatisfaccion PRIMARY KEY (idEncuesta),
    CONSTRAINT FK_EncuestaSatisfaccion_Tutoria
        FOREIGN KEY (Tutoria_idTutoria) REFERENCES Tutoria(idTutoria)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO

CREATE UNIQUE INDEX IX_EncuestaSatisfaccion_tokenRespuesta
    ON EncuestaSatisfaccion (tokenRespuesta);
GO

-- =========================================
-- TABLA AsuntoAtendido ("Atendido" del dashboard)
-- =========================================
CREATE TABLE AsuntoAtendido (
    idAsuntoAtendido INT IDENTITY(1,1) NOT NULL,
    tipo VARCHAR(40) NOT NULL,
    clave INT NOT NULL,
    Usuario_idUsuario INT NULL,
    fecha DATETIME2 NOT NULL CONSTRAINT DF_AsuntoAtendido_fecha DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_AsuntoAtendido PRIMARY KEY (idAsuntoAtendido),
    CONSTRAINT FK_AsuntoAtendido_Usuario
        FOREIGN KEY (Usuario_idUsuario) REFERENCES Usuario(idUsuario)
        ON DELETE SET NULL ON UPDATE NO ACTION
);
GO

CREATE UNIQUE INDEX uq_AsuntoAtendido_tipo_clave
    ON AsuntoAtendido (tipo, clave);
GO

CREATE INDEX IX_AsuntoAtendido_Usuario
    ON AsuntoAtendido (Usuario_idUsuario);
GO

-- =========================================
-- DATOS INICIALES: roles (los ids 1..5 los usa el código)
-- =========================================
SET IDENTITY_INSERT Rol ON;
INSERT INTO Rol (idRol, tipoRol) VALUES
    (1, 'Administrador'),
    (2, 'Tutor'),
    (3, 'Docente'),
    (4, 'Estudiante'),
    (5, 'Auxiliar');
SET IDENTITY_INSERT Rol OFF;
GO
