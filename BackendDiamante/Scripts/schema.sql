-- ============================================================
--  Limpieza Inteligente / Diamante
--  Schema completo de base de datos
--  Motor: SQL Server (SQLEXPRESS local / SQL Server en producción)
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

    PRINT '✅ Tabla Roles creada.';
END
ELSE
    PRINT '⚠️  Tabla Roles ya existe, se omite.';
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

    -- Email único
    CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users (Email);

    PRINT '✅ Tabla Users creada.';
END
ELSE
    PRINT '⚠️  Tabla Users ya existe, se omite.';
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

    -- Token único
    CREATE UNIQUE INDEX IX_RefreshTokens_Token  ON dbo.RefreshTokens (Token);
    CREATE        INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId);

    PRINT '✅ Tabla RefreshTokens creada.';
END
ELSE
    PRINT '⚠️  Tabla RefreshTokens ya existe, se omite.';
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
--  5. SEED — usuarios de prueba
--     Los hashes son BCrypt work factor 12.
--     admin@diamante.co      → Admin123!
--     supervisor@diamante.co → Super123!
--     cliente@diamante.co    → Cliente123!
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    INSERT INTO dbo.Users (Email, Name, PasswordHash, Role, IsActive, CreatedAt)
    VALUES
        ('admin@diamante.co',
         'Administrador Diamante',
         '$2a$12$GkMFMFAMJRZEb.4wlPNpouSHa5VGlFCNoI7fT.ZFLjr2qMFO2Udma',
         'admin', 1, GETUTCDATE()),

        ('supervisor@diamante.co',
         'Supervisor Diamante',
         '$2a$12$JGj1y8VG7fANVMkjZB1Hn.WzTBlz1blQVgOkrNfKM8uqFNq.lpbGy',
         'supervisor', 1, GETUTCDATE()),

        ('cliente@diamante.co',
         'Cliente Diamante',
         '$2a$12$y56rT/F3EV2eYmrVVDqvhuEIWJZz.0WC0VZ/Yv6OGmXVXVpT9Xn8S',
         'cliente', 1, GETUTCDATE());

    PRINT '✅ Usuarios de prueba insertados.';
END
ELSE
    PRINT '⚠️  Ya existen usuarios, seed omitido.';
GO

PRINT '🏁 Schema aplicado correctamente.';
GO
