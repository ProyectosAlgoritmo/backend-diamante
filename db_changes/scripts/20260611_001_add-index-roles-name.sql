-- ============================================================
-- Script  : 20260611_001_add-index-roles-name.sql
-- Fecha   : 2026-06-11
-- Autor   : Juan Sandoval
-- Rama    : dev
-- ------------------------------------------------------------
-- Qué hace:
--   Crea un índice no único sobre la columna Name de la tabla
--   security.Roles para acelerar las búsquedas por nombre de
--   rol que realiza PermissionAuthorizationMiddleware en cada
--   request protegido.
--
-- Por qué:
--   Sin este índice, EF Core generaba un full table scan en
--   cada consulta WHERE Name = @roleName, causando timeouts
--   en endpoints de Centros de Costo y Asignación de Personal.
--
-- Idempotente: sí — no falla si el índice ya existe.
-- ============================================================

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.objects o ON i.object_id = o.object_id
    INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
    WHERE s.name = 'security'
      AND o.name = 'Roles'
      AND i.name = 'IX_Roles_Name'
)
BEGIN
    CREATE INDEX IX_Roles_Name
        ON security.Roles (Name);

    PRINT 'Índice IX_Roles_Name creado correctamente.';
END
ELSE
BEGIN
    PRINT 'El índice IX_Roles_Name ya existe, no se realizó ningún cambio.';
END
