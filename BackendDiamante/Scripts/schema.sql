-- ============================================================
--  Limpieza Inteligente / Diamante
--  Schema completo de base de datos
--  Motor: SQL Server (SQLEXPRESS local / SQL Server en produccion)
--  Ejecutar una sola vez en la instancia destino.
-- ============================================================

USE LimpiezaInteligente_Dev;   -- Cambia el nombre si es necesario
GO

-- ──────────────────────────────────────────────────────────────
--  1. BUSINESS SCHEMA
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

PRINT '🏁 Schema business aplicado correctamente.';
GO

-- ──────────────────────────────────────────────────────────────
--  3. SCHEMA [security]
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'security')
BEGIN
    EXEC('CREATE SCHEMA security');
    PRINT '✅ Schema [security] creado.';
END
ELSE
    PRINT '⚠️  Schema [security] ya existe, se omite.';
GO

-- ──────────────────────────────────────────────────────────────
--  4. TABLAS security.*
--     / Permissions / RolePermissions
-- ──────────────────────────────────────────────────────────────

-- 8.1 Modules
IF OBJECT_ID(N'security.Modules', N'U') IS NULL
BEGIN
    CREATE TABLE security.Modules (
        Id        INT           NOT NULL IDENTITY(1,1),
        Name      NVARCHAR(100) NOT NULL,
        Code      NVARCHAR(50)  NOT NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_Modules_IsActive  DEFAULT (1),
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_Modules_CreatedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_Modules      PRIMARY KEY (Id),
        CONSTRAINT UQ_Modules_Code UNIQUE (Code)
    );
    PRINT '✅ Tabla security.Modules creada.';
END
ELSE
    PRINT '⚠️  Tabla security.Modules ya existe, se omite.';
GO

-- 8.2 Submodules
IF OBJECT_ID(N'security.Submodules', N'U') IS NULL
BEGIN
    CREATE TABLE security.Submodules (
        Id        INT           NOT NULL IDENTITY(1,1),
        Name      NVARCHAR(100) NOT NULL,
        Code      NVARCHAR(100) NOT NULL,
        ModuleId  INT           NOT NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_Submodules_IsActive  DEFAULT (1),
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_Submodules_CreatedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_Submodules      PRIMARY KEY (Id),
        CONSTRAINT UQ_Submodules_Code UNIQUE (Code),
        CONSTRAINT FK_Submodules_Modules
            FOREIGN KEY (ModuleId) REFERENCES security.Modules (Id)
    );
    PRINT '✅ Tabla security.Submodules creada.';
END
ELSE
    PRINT '⚠️  Tabla security.Submodules ya existe, se omite.';
GO

-- 8.3 Permissions
IF OBJECT_ID(N'security.Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE security.Permissions (
        Id          INT           NOT NULL IDENTITY(1,1),
        Name        NVARCHAR(50)  NOT NULL,
        Code        NVARCHAR(200) NOT NULL,
        SubmoduleId INT           NOT NULL,
        CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Permissions_CreatedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_Permissions      PRIMARY KEY (Id),
        CONSTRAINT UQ_Permissions_Code UNIQUE (Code),
        CONSTRAINT FK_Permissions_Submodules
            FOREIGN KEY (SubmoduleId) REFERENCES security.Submodules (Id)
    );
    PRINT '✅ Tabla security.Permissions creada.';
END
ELSE
    PRINT '⚠️  Tabla security.Permissions ya existe, se omite.';
GO

-- 8.4 Roles
IF OBJECT_ID(N'security.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE security.Roles (
        Id          INT            NOT NULL IDENTITY(1,1),
        Name        NVARCHAR(100)  NOT NULL,
        Description NVARCHAR(500)      NULL,
        IsActive    BIT            NOT NULL CONSTRAINT DF_sec_Roles_IsActive  DEFAULT (1),
        CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_sec_Roles_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt   DATETIME2          NULL,
        DeletedAt   DATETIME               NULL,

        CONSTRAINT PK_sec_Roles PRIMARY KEY (Id)
    );
    PRINT '✅ Tabla security.Roles creada.';
END
ELSE
    PRINT '⚠️  Tabla security.Roles ya existe, se omite.';
GO

-- 8.5 RolePermissions
IF OBJECT_ID(N'security.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE security.RolePermissions (
        Id           INT       NOT NULL IDENTITY(1,1),
        RoleId       INT       NOT NULL,
        PermissionId INT       NOT NULL,
        GrantedAt    DATETIME2 NOT NULL CONSTRAINT DF_RolePermissions_GrantedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_RolePermissions               PRIMARY KEY (Id),
        CONSTRAINT UQ_RolePermissions_RolePermission UNIQUE (RoleId, PermissionId),
        CONSTRAINT FK_RolePermissions_Roles
            FOREIGN KEY (RoleId)       REFERENCES security.Roles (Id),
        CONSTRAINT FK_RolePermissions_Permissions
            FOREIGN KEY (PermissionId) REFERENCES security.Permissions (Id)
    );
    PRINT '✅ Tabla security.RolePermissions creada.';
END
ELSE
    PRINT '⚠️  Tabla security.RolePermissions ya existe, se omite.';
GO

-- 8.6 Users
IF OBJECT_ID(N'security.Users', N'U') IS NULL
BEGIN
    CREATE TABLE security.Users (
        Id           INT            NOT NULL IDENTITY(1,1),
        Email        NVARCHAR(200)  NOT NULL,
        Name         NVARCHAR(150)  NOT NULL,
        PasswordHash NVARCHAR(MAX)  NOT NULL,
        Role         NVARCHAR(50)   NOT NULL,
        IsActive     BIT            NOT NULL CONSTRAINT DF_sec_Users_IsActive  DEFAULT (1),
        CreatedAt    DATETIME2      NOT NULL CONSTRAINT DF_sec_Users_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt    DATETIME2          NULL,
        LastLoginAt  DATETIME2          NULL,
        FirstName    NVARCHAR(100)      NULL,
        LastName     NVARCHAR(100)      NULL,
        Username     NVARCHAR(50)       NULL,
        Phone        NVARCHAR(30)       NULL,
        DocumentId   NVARCHAR(30)       NULL,
        Status       NVARCHAR(20)   NOT NULL CONSTRAINT DF_sec_Users_Status DEFAULT ('Activo'),
        Certificates NVARCHAR(MAX)      NULL,

        CONSTRAINT PK_sec_Users PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX IX_sec_Users_Email       ON security.Users (Email);
    CREATE UNIQUE INDEX IX_sec_Users_Username    ON security.Users (Username)   WHERE Username   IS NOT NULL;
    CREATE UNIQUE INDEX IX_sec_Users_DocumentId  ON security.Users (DocumentId) WHERE DocumentId IS NOT NULL;

    PRINT '✅ Tabla security.Users creada.';
END
ELSE
    PRINT '⚠️  Tabla security.Users ya existe, se omite.';
GO

-- 8.7 Notifications
IF OBJECT_ID(N'security.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE security.Notifications (
        Id              INT             NOT NULL IDENTITY(1,1),
        Title           NVARCHAR(200)   NOT NULL,
        Message         NVARCHAR(1000)  NOT NULL,
        Type            NVARCHAR(50)    NOT NULL,
        Severity        NVARCHAR(20)    NOT NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_sec_Notifications_CreatedAt DEFAULT (GETUTCDATE()),
        CreatedByUserId INT                 NULL,

        CONSTRAINT PK_sec_Notifications PRIMARY KEY (Id),
        CONSTRAINT FK_Notifications_Users
            FOREIGN KEY (CreatedByUserId) REFERENCES security.Users (Id)
    );
    PRINT '✅ Tabla security.Notifications creada.';
END
ELSE
    PRINT '⚠️  Tabla security.Notifications ya existe, se omite.';
GO

-- 8.8 NotificationUsers
IF OBJECT_ID(N'security.NotificationUsers', N'U') IS NULL
BEGIN
    CREATE TABLE security.NotificationUsers (
        Id             INT       NOT NULL IDENTITY(1,1),
        NotificationId INT       NOT NULL,
        UserId         INT       NOT NULL,
        IsRead         BIT       NOT NULL CONSTRAINT DF_sec_NotificationUsers_IsRead DEFAULT (0),
        ReadAt         DATETIME2     NULL,

        CONSTRAINT PK_sec_NotificationUsers      PRIMARY KEY (Id),
        CONSTRAINT FK_NotificationUsers_Notifications
            FOREIGN KEY (NotificationId) REFERENCES security.Notifications (Id),
        CONSTRAINT FK_NotificationUsers_Users
            FOREIGN KEY (UserId) REFERENCES security.Users (Id)
    );
    PRINT '✅ Tabla security.NotificationUsers creada.';
END
ELSE
    PRINT '⚠️  Tabla security.NotificationUsers ya existe, se omite.';
GO

-- 8.9 RefreshTokens
IF OBJECT_ID(N'security.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE security.RefreshTokens (
        Id          INT            NOT NULL IDENTITY(1,1),
        Token       NVARCHAR(200)  NOT NULL,
        ExpiresAt   DATETIME2      NOT NULL,
        IsRevoked   BIT            NOT NULL CONSTRAINT DF_sec_RefreshTokens_IsRevoked DEFAULT (0),
        CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_sec_RefreshTokens_CreatedAt DEFAULT (GETUTCDATE()),
        CreatedByIp NVARCHAR(MAX)      NULL,
        RevokedAt   DATETIME2          NULL,
        RevokedByIp NVARCHAR(MAX)      NULL,
        UserId      INT            NOT NULL,

        CONSTRAINT PK_sec_RefreshTokens PRIMARY KEY (Id),
        CONSTRAINT FK_sec_RefreshTokens_Users
            FOREIGN KEY (UserId) REFERENCES security.Users (Id) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_sec_RefreshTokens_Token  ON security.RefreshTokens (Token);
    CREATE        INDEX IX_sec_RefreshTokens_UserId ON security.RefreshTokens (UserId);

    PRINT '✅ Tabla security.RefreshTokens creada.';
END
ELSE
    PRINT '⚠️  Tabla security.RefreshTokens ya existe, se omite.';
GO

-- 8.10 PasswordResetTokens
IF OBJECT_ID(N'security.PasswordResetTokens', N'U') IS NULL
BEGIN
    CREATE TABLE security.PasswordResetTokens (
        Id        INT            NOT NULL IDENTITY(1,1),
        Token     NVARCHAR(200)  NOT NULL,
        ExpiresAt DATETIME2      NOT NULL,
        IsUsed    BIT            NOT NULL CONSTRAINT DF_sec_PRT_IsUsed    DEFAULT (0),
        CreatedAt DATETIME2      NOT NULL CONSTRAINT DF_sec_PRT_CreatedAt DEFAULT (GETUTCDATE()),
        UsedAt    DATETIME2          NULL,
        UserId    INT            NOT NULL,

        CONSTRAINT PK_sec_PasswordResetTokens PRIMARY KEY (Id),
        CONSTRAINT FK_sec_PasswordResetTokens_Users
            FOREIGN KEY (UserId) REFERENCES security.Users (Id) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_sec_PRT_Token  ON security.PasswordResetTokens (Token);
    CREATE        INDEX IX_sec_PRT_UserId ON security.PasswordResetTokens (UserId);

    PRINT '✅ Tabla security.PasswordResetTokens creada.';
END
ELSE
    PRINT '⚠️  Tabla security.PasswordResetTokens ya existe, se omite.';
GO

-- ──────────────────────────────────────────────────────────────
--  5. MIGRACION — copiar datos de dbo.* a security.*
--     y eliminar las tablas dbo obsoletas
--     (solo aplica si se viene de una BD anterior con schema dbo)
-- ──────────────────────────────────────────────────────────────

-- 9.1 Modules
IF OBJECT_ID(N'dbo.Modules', N'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT security.Modules ON;

    INSERT INTO security.Modules (Id, Name, Code, IsActive, CreatedAt)
    SELECT src.Id, src.Name, src.Code, src.IsActive, src.CreatedAt
    FROM dbo.Modules AS src
    WHERE NOT EXISTS (
        SELECT 1 FROM security.Modules WHERE Code = src.Code
    );

    SET IDENTITY_INSERT security.Modules OFF;
    PRINT '✅ Datos de dbo.Modules migrados a security.Modules.';
END
GO

-- 9.2 Submodules
IF OBJECT_ID(N'dbo.Submodules', N'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT security.Submodules ON;

    INSERT INTO security.Submodules (Id, Name, Code, ModuleId, IsActive, CreatedAt)
    SELECT src.Id, src.Name, src.Code, src.ModuleId, src.IsActive, src.CreatedAt
    FROM dbo.Submodules AS src
    WHERE NOT EXISTS (
        SELECT 1 FROM security.Submodules WHERE Code = src.Code
    );

    SET IDENTITY_INSERT security.Submodules OFF;
    PRINT '✅ Datos de dbo.Submodules migrados a security.Submodules.';
END
GO

-- 9.3 Permissions
IF OBJECT_ID(N'dbo.Permissions', N'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT security.Permissions ON;

    INSERT INTO security.Permissions (Id, Name, Code, SubmoduleId, CreatedAt)
    SELECT src.Id, src.Name, src.Code, src.SubmoduleId, src.CreatedAt
    FROM dbo.Permissions AS src
    WHERE NOT EXISTS (
        SELECT 1 FROM security.Permissions WHERE Code = src.Code
    );

    SET IDENTITY_INSERT security.Permissions OFF;
    PRINT '✅ Datos de dbo.Permissions migrados a security.Permissions.';
END
GO

-- 9.4 Roles
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT security.Roles ON;

    INSERT INTO security.Roles (Id, Name, Description, IsActive, CreatedAt, UpdatedAt, DeletedAt)
    SELECT src.Id, src.Name, src.Description, src.IsActive, src.CreatedAt, src.UpdatedAt, src.DeletedAt
    FROM dbo.Roles AS src
    WHERE NOT EXISTS (
        SELECT 1 FROM security.Roles WHERE Name = src.Name
    );

    SET IDENTITY_INSERT security.Roles OFF;
    PRINT '✅ Datos de dbo.Roles migrados a security.Roles.';
END
GO

-- 9.5 RolePermissions
IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT security.RolePermissions ON;

    INSERT INTO security.RolePermissions (Id, RoleId, PermissionId, GrantedAt)
    SELECT src.Id, src.RoleId, src.PermissionId, src.GrantedAt
    FROM dbo.RolePermissions AS src
    WHERE NOT EXISTS (
        SELECT 1 FROM security.RolePermissions
        WHERE RoleId = src.RoleId AND PermissionId = src.PermissionId
    );

    SET IDENTITY_INSERT security.RolePermissions OFF;
    PRINT '✅ Datos de dbo.RolePermissions migrados a security.RolePermissions.';
END
GO

-- 9.6 Users
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT security.Users ON;

    INSERT INTO security.Users (
        Id, Email, Name, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt,
        LastLoginAt, FirstName, LastName, Username, Phone, DocumentId, Status, Certificates
    )
    SELECT
        src.Id, src.Email, src.Name, src.PasswordHash, src.Role, src.IsActive, src.CreatedAt,
        src.UpdatedAt, src.LastLoginAt, src.FirstName, src.LastName, src.Username,
        src.Phone, src.DocumentId, ISNULL(src.Status, 'Activo'), src.Certificates
    FROM dbo.Users AS src
    WHERE NOT EXISTS (
        SELECT 1 FROM security.Users WHERE Email = src.Email
    );

    SET IDENTITY_INSERT security.Users OFF;
    PRINT '✅ Datos de dbo.Users migrados a security.Users.';
END
GO

-- 9.6 RefreshTokens
IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT security.RefreshTokens ON;

    INSERT INTO security.RefreshTokens (Id, Token, ExpiresAt, IsRevoked, CreatedAt, CreatedByIp, RevokedAt, RevokedByIp, UserId)
    SELECT src.Id, src.Token, src.ExpiresAt, src.IsRevoked, src.CreatedAt, src.CreatedByIp, src.RevokedAt, src.RevokedByIp, src.UserId
    FROM dbo.RefreshTokens AS src
    WHERE NOT EXISTS (
        SELECT 1 FROM security.RefreshTokens WHERE Token = src.Token
    );

    SET IDENTITY_INSERT security.RefreshTokens OFF;
    PRINT '✅ Datos de dbo.RefreshTokens migrados a security.RefreshTokens.';
END
GO

-- 9.7 PasswordResetTokens
IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT security.PasswordResetTokens ON;

    INSERT INTO security.PasswordResetTokens (Id, Token, ExpiresAt, IsUsed, CreatedAt, UsedAt, UserId)
    SELECT src.Id, src.Token, src.ExpiresAt, src.IsUsed, src.CreatedAt, src.UsedAt, src.UserId
    FROM dbo.PasswordResetTokens AS src
    WHERE NOT EXISTS (
        SELECT 1 FROM security.PasswordResetTokens WHERE Token = src.Token
    );

    SET IDENTITY_INSERT security.PasswordResetTokens OFF;
    PRINT '✅ Datos de dbo.PasswordResetTokens migrados a security.PasswordResetTokens.';
END
GO

-- 9.9 Eliminar tablas dbo obsoletas (orden inverso de FKs)
IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.RolePermissions;
    PRINT '🗑️  Tabla dbo.RolePermissions eliminada.';
END
GO

IF OBJECT_ID(N'dbo.Permissions', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Permissions;
    PRINT '🗑️  Tabla dbo.Permissions eliminada.';
END
GO

IF OBJECT_ID(N'dbo.Submodules', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Submodules;
    PRINT '🗑️  Tabla dbo.Submodules eliminada.';
END
GO

IF OBJECT_ID(N'dbo.Modules', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Modules;
    PRINT '🗑️  Tabla dbo.Modules eliminada.';
END
GO

IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Roles;
    PRINT '🗑️  Tabla dbo.Roles eliminada.';
END
GO

IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.PasswordResetTokens;
    PRINT '🗑️  Tabla dbo.PasswordResetTokens eliminada.';
END
GO

IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.RefreshTokens;
    PRINT '🗑️  Tabla dbo.RefreshTokens eliminada.';
END
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Users;
    PRINT '🗑️  Tabla dbo.Users eliminada.';
END
GO

-- ──────────────────────────────────────────────────────────────
--  6. SEED — Usuarios, Modulos, Submodulos y Permisos
-- ──────────────────────────────────────────────────────────────
--  Hash BCrypt work factor 12. Contrasena: X@132204513375aj

-- Administrador
IF NOT EXISTS (SELECT 1 FROM security.Users WHERE Email = 'limpieza.inteligente@diamante.net.co')
    INSERT INTO security.Users (Email, Name, PasswordHash, Role, IsActive, CreatedAt)
    VALUES ('limpieza.inteligente@diamante.net.co', 'Administrador Diamante',
            '$2b$12$YwAmJZBze3x2gmktdAfozeeBlBqOaRXXDQGIjNKMSLwrLyCIP/n/q', 'admin', 1, GETUTCDATE());
GO

-- Supervisor
IF NOT EXISTS (SELECT 1 FROM security.Users WHERE Email = 'supervisor.prueba@diamante.net.co')
    INSERT INTO security.Users (Email, Name, PasswordHash, Role, IsActive, CreatedAt)
    VALUES ('supervisor.prueba@diamante.net.co', 'Supervisor Diamante',
            '$2b$12$YwAmJZBze3x2gmktdAfozeeBlBqOaRXXDQGIjNKMSLwrLyCIP/n/q', 'supervisor', 1, GETUTCDATE());
GO

-- Cliente
IF NOT EXISTS (SELECT 1 FROM security.Users WHERE Email = 'cliente.prueba@diamante.net.co')
    INSERT INTO security.Users (Email, Name, PasswordHash, Role, IsActive, CreatedAt)
    VALUES ('cliente.prueba@diamante.net.co', 'Cliente Diamante',
            '$2b$12$YwAmJZBze3x2gmktdAfozeeBlBqOaRXXDQGIjNKMSLwrLyCIP/n/q', 'cliente', 1, GETUTCDATE());
GO

-- Modulo: SECURITY
IF NOT EXISTS (SELECT 1 FROM security.Modules WHERE Code = 'SECURITY')
    INSERT INTO security.Modules (Name, Code) VALUES ('Seguridad', 'SECURITY');
GO

-- Modulo: OPERATIONAL_CONTROL
IF NOT EXISTS (SELECT 1 FROM security.Modules WHERE Code = 'OPERATIONAL_CONTROL')
    INSERT INTO security.Modules (Name, Code) VALUES ('Control Operacional', 'OPERATIONAL_CONTROL');
GO

-- Submodulos bajo SECURITY
IF NOT EXISTS (SELECT 1 FROM security.Submodules WHERE Code = 'SECURITY.USERS')
    INSERT INTO security.Submodules (Name, Code, ModuleId)
    SELECT 'Usuarios', 'SECURITY.USERS', Id FROM security.Modules WHERE Code = 'SECURITY';
GO

IF NOT EXISTS (SELECT 1 FROM security.Submodules WHERE Code = 'SECURITY.ROLES')
    INSERT INTO security.Submodules (Name, Code, ModuleId)
    SELECT 'Roles', 'SECURITY.ROLES', Id FROM security.Modules WHERE Code = 'SECURITY';
GO

IF NOT EXISTS (SELECT 1 FROM security.Submodules WHERE Code = 'SECURITY.SETTINGS')
    INSERT INTO security.Submodules (Name, Code, ModuleId)
    SELECT 'Configuracion', 'SECURITY.SETTINGS', Id FROM security.Modules WHERE Code = 'SECURITY';
GO

-- Submodulos bajo OPERATIONAL_CONTROL
IF NOT EXISTS (SELECT 1 FROM security.Submodules WHERE Code = 'OPERATIONAL_CONTROL.COMPANIES')
    INSERT INTO security.Submodules (Name, Code, ModuleId)
    SELECT 'Empresas', 'OPERATIONAL_CONTROL.COMPANIES', Id FROM security.Modules WHERE Code = 'OPERATIONAL_CONTROL';
GO

IF NOT EXISTS (SELECT 1 FROM security.Submodules WHERE Code = 'OPERATIONAL_CONTROL.COST_CENTERS')
    INSERT INTO security.Submodules (Name, Code, ModuleId)
    SELECT 'Centros de Costo', 'OPERATIONAL_CONTROL.COST_CENTERS', Id FROM security.Modules WHERE Code = 'OPERATIONAL_CONTROL';
GO

IF NOT EXISTS (SELECT 1 FROM security.Submodules WHERE Code = 'OPERATIONAL_CONTROL.STAFF_ASSIGNMENT')
    INSERT INTO security.Submodules (Name, Code, ModuleId)
    SELECT 'Asignacion de Personal', 'OPERATIONAL_CONTROL.STAFF_ASSIGNMENT', Id FROM security.Modules WHERE Code = 'OPERATIONAL_CONTROL';
GO

-- Permisos VIEW
IF NOT EXISTS (SELECT 1 FROM security.Permissions WHERE Code = 'SECURITY.USERS.VIEW')
    INSERT INTO security.Permissions (Name, Code, SubmoduleId)
    SELECT 'Ver', 'SECURITY.USERS.VIEW', Id FROM security.Submodules WHERE Code = 'SECURITY.USERS';
GO

IF NOT EXISTS (SELECT 1 FROM security.Permissions WHERE Code = 'SECURITY.ROLES.VIEW')
    INSERT INTO security.Permissions (Name, Code, SubmoduleId)
    SELECT 'Ver', 'SECURITY.ROLES.VIEW', Id FROM security.Submodules WHERE Code = 'SECURITY.ROLES';
GO

IF NOT EXISTS (SELECT 1 FROM security.Permissions WHERE Code = 'SECURITY.SETTINGS.VIEW')
    INSERT INTO security.Permissions (Name, Code, SubmoduleId)
    SELECT 'Ver', 'SECURITY.SETTINGS.VIEW', Id FROM security.Submodules WHERE Code = 'SECURITY.SETTINGS';
GO

IF NOT EXISTS (SELECT 1 FROM security.Permissions WHERE Code = 'OPERATIONAL_CONTROL.COMPANIES.VIEW')
    INSERT INTO security.Permissions (Name, Code, SubmoduleId)
    SELECT 'Ver', 'OPERATIONAL_CONTROL.COMPANIES.VIEW', Id FROM security.Submodules WHERE Code = 'OPERATIONAL_CONTROL.COMPANIES';
GO

IF NOT EXISTS (SELECT 1 FROM security.Permissions WHERE Code = 'OPERATIONAL_CONTROL.COMPANIES.IMPORT')
    INSERT INTO security.Permissions (Name, Code, SubmoduleId)
    SELECT 'Importar', 'OPERATIONAL_CONTROL.COMPANIES.IMPORT', Id FROM security.Submodules WHERE Code = 'OPERATIONAL_CONTROL.COMPANIES';
GO

IF NOT EXISTS (SELECT 1 FROM security.Permissions WHERE Code = 'OPERATIONAL_CONTROL.COMPANIES.ASSIGN')
    INSERT INTO security.Permissions (Name, Code, SubmoduleId)
    SELECT 'Asignar personal', 'OPERATIONAL_CONTROL.COMPANIES.ASSIGN', Id FROM security.Submodules WHERE Code = 'OPERATIONAL_CONTROL.COMPANIES';
GO

IF NOT EXISTS (SELECT 1 FROM security.Permissions WHERE Code = 'OPERATIONAL_CONTROL.COMPANIES.EDIT')
    INSERT INTO security.Permissions (Name, Code, SubmoduleId)
    SELECT 'Editar', 'OPERATIONAL_CONTROL.COMPANIES.EDIT', Id FROM security.Submodules WHERE Code = 'OPERATIONAL_CONTROL.COMPANIES';
GO

IF NOT EXISTS (SELECT 1 FROM security.Permissions WHERE Code = 'OPERATIONAL_CONTROL.COMPANIES.DELETE')
    INSERT INTO security.Permissions (Name, Code, SubmoduleId)
    SELECT 'Eliminar', 'OPERATIONAL_CONTROL.COMPANIES.DELETE', Id FROM security.Submodules WHERE Code = 'OPERATIONAL_CONTROL.COMPANIES';
GO

IF NOT EXISTS (SELECT 1 FROM security.Permissions WHERE Code = 'OPERATIONAL_CONTROL.COST_CENTERS.VIEW')
    INSERT INTO security.Permissions (Name, Code, SubmoduleId)
    SELECT 'Ver', 'OPERATIONAL_CONTROL.COST_CENTERS.VIEW', Id FROM security.Submodules WHERE Code = 'OPERATIONAL_CONTROL.COST_CENTERS';
GO

IF NOT EXISTS (SELECT 1 FROM security.Permissions WHERE Code = 'OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.VIEW')
    INSERT INTO security.Permissions (Name, Code, SubmoduleId)
    SELECT 'Ver', 'OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.VIEW', Id FROM security.Submodules WHERE Code = 'OPERATIONAL_CONTROL.STAFF_ASSIGNMENT';
GO

PRINT '✅ Seed completo aplicado.';
GO
