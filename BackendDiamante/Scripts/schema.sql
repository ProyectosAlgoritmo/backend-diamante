-- ============================================================
--  Limpieza Inteligente / Diamante
--  Schema completo de base de datos
--  Motor: SQL Server (SQLEXPRESS local / SQL Server en produccion)
--  Ejecutar una sola vez en la instancia destino.
-- ============================================================

USE LimpiezaInteligente_Dev;   -- Cambia el nombre si es necesario
GO

-- ──────────────────────────────────────────────────────────────
--  1. ROLES
-- ──────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        Id          INT            NOT NULL IDENTITY(1,1),
        Name        NVARCHAR(100)  NOT NULL,
        Description NVARCHAR(500)      NULL,
        IsActive    BIT            NOT NULL CONSTRAINT DF_Roles_IsActive    DEFAULT (1),
        CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_Roles_CreatedAt   DEFAULT (GETUTCDATE()),
        UpdatedAt   DATETIME2          NULL,

        CONSTRAINT PK_Roles PRIMARY KEY (Id)
    );

    PRINT 'Tabla Roles creada.';
END
ELSE
    PRINT 'Tabla Roles ya existe, se omite.';
GO

-- ──────────────────────────────────────────────────────────────
--  2. USERS
-- ──────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id           INT            NOT NULL IDENTITY(1,1),
        Email        NVARCHAR(200)  NOT NULL,
        Name         NVARCHAR(150)  NOT NULL,
        PasswordHash NVARCHAR(MAX)  NOT NULL,
        Role         NVARCHAR(50)   NOT NULL,
        IsActive     BIT            NOT NULL CONSTRAINT DF_Users_IsActive  DEFAULT (1),
        CreatedAt    DATETIME2      NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt    DATETIME2          NULL,
        LastLoginAt  DATETIME2          NULL,

        CONSTRAINT PK_Users PRIMARY KEY (Id)
    );

    -- Email unico
    CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users (Email);

    PRINT 'Tabla Users creada.';
END
ELSE
    PRINT 'Tabla Users ya existe, se omite.';
GO

-- ──────────────────────────────────────────────────────────────
--  3. REFRESH TOKENS
-- ──────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens (
        Id          INT            NOT NULL IDENTITY(1,1),
        Token       NVARCHAR(200)  NOT NULL,
        ExpiresAt   DATETIME2      NOT NULL,
        IsRevoked   BIT            NOT NULL CONSTRAINT DF_RefreshTokens_IsRevoked DEFAULT (0),
        CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT (GETUTCDATE()),
        CreatedByIp NVARCHAR(MAX)      NULL,
        RevokedAt   DATETIME2          NULL,
        RevokedByIp NVARCHAR(MAX)      NULL,
        UserId      INT            NOT NULL,

        CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id),

        CONSTRAINT FK_RefreshTokens_Users_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
            ON DELETE CASCADE
    );

    -- Token unico
    CREATE UNIQUE INDEX IX_RefreshTokens_Token  ON dbo.RefreshTokens (Token);
    CREATE        INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId);

    PRINT 'Tabla RefreshTokens creada.';
END
ELSE
    PRINT 'Tabla RefreshTokens ya existe, se omite.';
GO

-- ──────────────────────────────────────────────────────────────
--  4. PASSWORD RESET TOKENS
-- ──────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetTokens (
        Id          INT            NOT NULL IDENTITY(1,1),
        Token       NVARCHAR(200)  NOT NULL,
        ExpiresAt   DATETIME2      NOT NULL,
        IsUsed      BIT            NOT NULL CONSTRAINT DF_PRT_IsUsed    DEFAULT (0),
        CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_PRT_CreatedAt DEFAULT (GETUTCDATE()),
        UsedAt      DATETIME2          NULL,
        UserId      INT            NOT NULL,

        CONSTRAINT PK_PasswordResetTokens PRIMARY KEY (Id),

        CONSTRAINT FK_PasswordResetTokens_Users_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
            ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_PasswordResetTokens_Token  ON dbo.PasswordResetTokens (Token);
    CREATE        INDEX IX_PasswordResetTokens_UserId ON dbo.PasswordResetTokens (UserId);

    PRINT 'Tabla PasswordResetTokens creada.';
END
ELSE
    PRINT 'Tabla PasswordResetTokens ya existe, se omite.';
GO

-- ──────────────────────────────────────────────────────────────
--  5. SEED — usuarios de prueba @diamante.net.co
--     Hash BCrypt work factor 12.
--     Contrasena para los 3: X@132204513375aj
-- ──────────────────────────────────────────────────────────────

-- Administrador
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'limpieza.inteligente@diamante.net.co')
BEGIN
    INSERT INTO dbo.Users (Email, Name, PasswordHash, Role, IsActive, CreatedAt)
    VALUES (
        'limpieza.inteligente@diamante.net.co',
        'Administrador Diamante',
        '$2b$12$YwAmJZBze3x2gmktdAfozeeBlBqOaRXXDQGIjNKMSLwrLyCIP/n/q',
        'admin', 1, GETUTCDATE()
    );
    PRINT 'Usuario administrador insertado.';
END
GO

-- Supervisor
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'supervisor.prueba@diamante.net.co')
BEGIN
    INSERT INTO dbo.Users (Email, Name, PasswordHash, Role, IsActive, CreatedAt)
    VALUES (
        'supervisor.prueba@diamante.net.co',
        'Supervisor Diamante',
        '$2b$12$YwAmJZBze3x2gmktdAfozeeBlBqOaRXXDQGIjNKMSLwrLyCIP/n/q',
        'supervisor', 1, GETUTCDATE()
    );
    PRINT 'Usuario supervisor insertado.';
END
GO

-- Cliente
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'cliente.prueba@diamante.net.co')
BEGIN
    INSERT INTO dbo.Users (Email, Name, PasswordHash, Role, IsActive, CreatedAt)
    VALUES (
        'cliente.prueba@diamante.net.co',
        'Cliente Diamante',
        '$2b$12$YwAmJZBze3x2gmktdAfozeeBlBqOaRXXDQGIjNKMSLwrLyCIP/n/q',
        'cliente', 1, GETUTCDATE()
    );
    PRINT 'Usuario cliente insertado.';
END
GO

-- ──────────────────────────────────────────────────────────────
--  6. EXTENSION TABLA USERS — campos para modulo Usuarios
--     (ALTER TABLE seguro — solo agrega si la columna no existe)
-- ──────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'FirstName')
    ALTER TABLE dbo.Users ADD FirstName NVARCHAR(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'LastName')
    ALTER TABLE dbo.Users ADD LastName NVARCHAR(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'Username')
    ALTER TABLE dbo.Users ADD Username NVARCHAR(50) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'Phone')
    ALTER TABLE dbo.Users ADD Phone NVARCHAR(30) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'DocumentId')
    ALTER TABLE dbo.Users ADD DocumentId NVARCHAR(30) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'Status')
    ALTER TABLE dbo.Users ADD Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_Status DEFAULT ('Activo');
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'Certificates')
    ALTER TABLE dbo.Users ADD Certificates NVARCHAR(MAX) NULL;
GO

-- Indices unicos condicionales (Username y DocumentId)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE UNIQUE INDEX IX_Users_Username ON dbo.Users (Username) WHERE Username IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_DocumentId' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE UNIQUE INDEX IX_Users_DocumentId ON dbo.Users (DocumentId) WHERE DocumentId IS NOT NULL;
GO

PRINT 'Columnas extendidas de Users aplicadas.';
GO

PRINT 'Schema aplicado correctamente.';
GO
