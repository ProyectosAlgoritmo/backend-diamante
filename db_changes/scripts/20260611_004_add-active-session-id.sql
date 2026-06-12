-- ============================================================
-- Script  : 20260611_004_add-active-session-id.sql
-- Fecha   : 2026-06-11
-- Rama    : fix/user-roles
-- ------------------------------------------------------------
-- Qué hace:
--   Agrega columna ActiveSessionId (NVARCHAR 32) a security.Users
--   para soportar sesión única por usuario. Cada login genera
--   un nuevo GUID; el middleware verifica que el claim "sid"
--   del JWT coincida con el valor en BD.
-- Idempotente: sí.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('security.Users') AND name = 'ActiveSessionId'
)
BEGIN
    ALTER TABLE security.Users ADD ActiveSessionId NVARCHAR(32) NULL;
    PRINT 'Columna ActiveSessionId agregada a security.Users.';
END
ELSE
    PRINT 'Columna ActiveSessionId ya existe.';
