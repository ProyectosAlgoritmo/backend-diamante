-- ============================================================
-- Script  : 20260611_002_add-certificates-tables.sql
-- Fecha   : 2026-06-11
-- Autor   : Juan Sandoval
-- Rama    : fix/user-roles
-- ------------------------------------------------------------
-- Qué hace:
--   Crea las tablas security.Certificates y
--   security.UserCertificates para almacenar el catálogo de
--   certificados/cursos y la relación muchos-a-muchos con
--   los usuarios.
--
-- Por qué:
--   Los certificados estaban almacenados como JSON en la columna
--   Users.Certificates (NVARCHAR(MAX)), lo que impedía gestionar
--   el catálogo de forma independiente y hacía imposibles las
--   búsquedas o reportes por certificado.
--
-- Idempotente: sí — no falla si las tablas ya existen.
-- ============================================================

IF NOT EXISTS (
    SELECT 1
    FROM   sys.tables  t
    JOIN   sys.schemas s ON t.schema_id = s.schema_id
    WHERE  s.name = 'security' AND t.name = 'Certificates'
)
BEGIN
    CREATE TABLE security.Certificates (
        Id        INT IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(200)     NOT NULL,
        IsActive  BIT               NOT NULL CONSTRAINT DF_Certificates_IsActive  DEFAULT 1,
        CreatedAt DATETIME2         NOT NULL CONSTRAINT DF_Certificates_CreatedAt DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2         NULL,
        CONSTRAINT PK_Certificates      PRIMARY KEY (Id),
        CONSTRAINT UQ_Certificates_Name UNIQUE      (Name)
    );

    PRINT 'Tabla security.Certificates creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla security.Certificates ya existe, no se realizó ningún cambio.';
END

IF NOT EXISTS (
    SELECT 1
    FROM   sys.tables  t
    JOIN   sys.schemas s ON t.schema_id = s.schema_id
    WHERE  s.name = 'security' AND t.name = 'UserCertificates'
)
BEGIN
    CREATE TABLE security.UserCertificates (
        UserId        INT       NOT NULL,
        CertificateId INT       NOT NULL,
        AssignedAt    DATETIME2 NOT NULL CONSTRAINT DF_UserCertificates_AssignedAt DEFAULT GETUTCDATE(),
        CONSTRAINT PK_UserCertificates       PRIMARY KEY (UserId, CertificateId),
        CONSTRAINT FK_UserCertificates_Users FOREIGN KEY (UserId)
            REFERENCES security.Users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserCertificates_Certs FOREIGN KEY (CertificateId)
            REFERENCES security.Certificates(Id) ON DELETE CASCADE
    );

    PRINT 'Tabla security.UserCertificates creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla security.UserCertificates ya existe, no se realizó ningún cambio.';
END
