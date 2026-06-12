-- ============================================================
-- Script  : 20260611_003_soft-delete-users.sql
-- Fecha   : 2026-06-11
-- Rama    : fix/user-roles
-- ------------------------------------------------------------
-- Qué hace:
--   Agrega borrado lógico (soft-delete) a security.Users:
--   - Columna DeletedAt DATETIME2 NULL (null = activo, fecha = borrado)
--   - Elimina índice único sobre Email (el correo puede repetirse)
--   - Reemplaza índices únicos de DocumentId y Username para excluir
--     registros borrados lógicamente
--
-- Idempotente: sí — cada paso verifica existencia antes de actuar.
-- ============================================================

-- 1. Agregar columna DeletedAt
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('security.Users') AND name = 'DeletedAt'
)
BEGIN
    ALTER TABLE security.Users ADD DeletedAt DATETIME2 NULL;
    PRINT 'Columna DeletedAt agregada a security.Users.';
END
ELSE
    PRINT 'Columna DeletedAt ya existe.';

GO  -- batch boundary: los índices filtrados sobre DeletedAt necesitan que la columna ya exista

-- 2. Eliminar índice único sobre Email (el correo puede pertenecer a más de un usuario)
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('security.Users') AND name = 'IX_Users_Email'
)
BEGIN
    DROP INDEX IX_Users_Email ON security.Users;
    PRINT 'Índice IX_Users_Email (unique) eliminado.';
END

-- 3. Reemplazar índice único de DocumentId excluyendo borrados lógicos
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('security.Users') AND name = 'IX_Users_DocumentId'
)
BEGIN
    DROP INDEX IX_Users_DocumentId ON security.Users;
    PRINT 'Índice IX_Users_DocumentId eliminado.';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('security.Users') AND name = 'IX_Users_DocumentId_Active'
)
BEGIN
    CREATE UNIQUE INDEX IX_Users_DocumentId_Active ON security.Users (DocumentId)
    WHERE DocumentId IS NOT NULL AND DeletedAt IS NULL;
    PRINT 'Índice IX_Users_DocumentId_Active creado.';
END

-- 4. Reemplazar índice único de Username excluyendo borrados lógicos
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('security.Users') AND name = 'IX_Users_Username'
)
BEGIN
    DROP INDEX IX_Users_Username ON security.Users;
    PRINT 'Índice IX_Users_Username eliminado.';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('security.Users') AND name = 'IX_Users_Username_Active'
)
BEGIN
    CREATE UNIQUE INDEX IX_Users_Username_Active ON security.Users (Username)
    WHERE Username IS NOT NULL AND DeletedAt IS NULL;
    PRINT 'Índice IX_Users_Username_Active creado.';
END
