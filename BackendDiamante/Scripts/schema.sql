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
--  5. BUSINESS SCHEMA
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'business')
BEGIN
    EXEC('CREATE SCHEMA business');
    PRINT '✅ Schema [business] creado.';
END
ELSE
    PRINT '⚠️  Schema [business] ya existe, se omite.';
GO

-- ─── 5.1 Companies ───────────────────────────────────────────
IF OBJECT_ID(N'business.Companies', N'U') IS NULL
BEGIN
    CREATE TABLE business.Companies (
        Id        INT           NOT NULL IDENTITY(1,1),
        Name      NVARCHAR(200) NOT NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_Companies_IsActive  DEFAULT (1),
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_Companies_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt DATETIME2         NULL,
        DeletedAt DATETIME2         NULL,

        CONSTRAINT PK_Companies PRIMARY KEY (Id)
    );
    PRINT '✅ Tabla business.Companies creada.';
END
ELSE
    PRINT '⚠️  Tabla business.Companies ya existe, se omite.';
GO

-- ─── 5.2 Sectors ─────────────────────────────────────────────
IF OBJECT_ID(N'business.Sectors', N'U') IS NULL
BEGIN
    CREATE TABLE business.Sectors (
        Id        INT           NOT NULL IDENTITY(1,1),
        Name      NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_Sectors_CreatedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_Sectors PRIMARY KEY (Id)
    );
    PRINT '✅ Tabla business.Sectors creada.';
END
ELSE
    PRINT '⚠️  Tabla business.Sectors ya existe, se omite.';
GO

-- ─── 5.3 Operators ───────────────────────────────────────────
IF OBJECT_ID(N'business.Operators', N'U') IS NULL
BEGIN
    CREATE TABLE business.Operators (
        Id        INT           NOT NULL IDENTITY(1,1),
        Name      NVARCHAR(200) NOT NULL,
        Role      NVARCHAR(100) NOT NULL,
        Shift     NVARCHAR(50)  NOT NULL,
        SectorId  INT               NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_Operators_IsActive  DEFAULT (1),
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_Operators_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt DATETIME2         NULL,

        CONSTRAINT PK_Operators   PRIMARY KEY (Id),
        CONSTRAINT FK_Operators_Sectors
            FOREIGN KEY (SectorId) REFERENCES business.Sectors (Id)
    );
    PRINT '✅ Tabla business.Operators creada.';
END
ELSE
    PRINT '⚠️  Tabla business.Operators ya existe, se omite.';
GO

-- ─── 5.4 CostCenters ─────────────────────────────────────────
IF OBJECT_ID(N'business.CostCenters', N'U') IS NULL
BEGIN
    CREATE TABLE business.CostCenters (
        Id        INT           NOT NULL IDENTITY(1,1),
        Code      NVARCHAR(50)  NOT NULL,
        Name      NVARCHAR(200) NOT NULL,
        Address   NVARCHAR(500)     NULL,
        Areas     INT           NOT NULL CONSTRAINT DF_CostCenters_Areas DEFAULT (0),
        CompanyId INT           NOT NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_CostCenters_IsActive  DEFAULT (1),
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_CostCenters_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt DATETIME2         NULL,
        DeletedAt DATETIME2         NULL,

        CONSTRAINT PK_CostCenters PRIMARY KEY (Id),
        CONSTRAINT UQ_CostCenters_Code UNIQUE (Code),
        CONSTRAINT FK_CostCenters_Companies
            FOREIGN KEY (CompanyId) REFERENCES business.Companies (Id)
    );
    PRINT '✅ Tabla business.CostCenters creada.';
END
ELSE
    PRINT '⚠️  Tabla business.CostCenters ya existe, se omite.';
GO

-- ─── 5.5 CostCenterOperators ─────────────────────────────────
IF OBJECT_ID(N'business.CostCenterOperators', N'U') IS NULL
BEGIN
    CREATE TABLE business.CostCenterOperators (
        Id           INT       NOT NULL IDENTITY(1,1),
        CostCenterId INT       NOT NULL,
        OperatorId   INT       NOT NULL,
        AssignedAt   DATETIME2 NOT NULL CONSTRAINT DF_CostCenterOperators_AssignedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_CostCenterOperators PRIMARY KEY (Id),
        CONSTRAINT UQ_CostCenterOperators  UNIQUE (CostCenterId, OperatorId),
        CONSTRAINT FK_CostCenterOperators_CostCenters
            FOREIGN KEY (CostCenterId) REFERENCES business.CostCenters (Id),
        CONSTRAINT FK_CostCenterOperators_Operators
            FOREIGN KEY (OperatorId)   REFERENCES business.Operators (Id)
    );
    PRINT '✅ Tabla business.CostCenterOperators creada.';
END
ELSE
    PRINT '⚠️  Tabla business.CostCenterOperators ya existe, se omite.';
GO

PRINT '🏁 Schema aplicado correctamente.';
GO
