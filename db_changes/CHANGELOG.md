# Changelog de Base de Datos

Registro cronológico de todos los cambios estructurales aplicados a la BD.
El cambio más reciente va **al inicio** del archivo.

Formato de cada entrada:

```
## [YYYY-MM-DD] Título del cambio
**Script:** nombre-del-archivo.sql
**Autor:** Nombre
**Rama:** nombre-de-la-rama
**Motivación:** Por qué fue necesario este cambio.
**Impacto:** Qué tablas/columnas/índices afecta.
```

---

## [2026-06-11] Crear tablas Certificates y UserCertificates

**Script:** [`20260611_002_add-certificates-tables.sql`](scripts/20260611_002_add-certificates-tables.sql)
**Autor:** Juan Sandoval
**Rama:** `fix/user-roles`

**Motivación:**
Los certificados de los usuarios estaban almacenados como un array JSON en la columna `security.Users.Certificates` (NVARCHAR(MAX)), lo que impedía gestionar el catálogo de certificados de forma independiente y hacía imposibles las búsquedas o reportes por certificado. Se requería además poder crear nuevos certificados desde el formulario de usuario.

**Impacto:**
- Tabla nueva: `security.Certificates` — catálogo de certificados/cursos disponibles.
- Tabla nueva: `security.UserCertificates` — relación muchos-a-muchos entre usuarios y certificados.
- La columna `security.Users.Certificates` (JSON) queda obsoleta; el seeder la migra al nuevo esquema y la limpia.

---

## [2026-06-11] Agregar índice en security.Roles (Name)

**Script:** [`20260611_001_add-index-roles-name.sql`](scripts/20260611_001_add-index-roles-name.sql)
**Autor:** Juan Sandoval
**Rama:** `dev`

**Motivación:**
El middleware `PermissionAuthorizationMiddleware` consultaba `security.Roles` filtrando por la columna `Name` en cada request protegido, sin índice. Esto generaba full table scans frecuentes que, combinados con la ausencia de caché, causaban timeouts (`TaskCanceledException`) en los endpoints de Centros de Costo y Asignación de Personal, especialmente después de que el seeder `SecurityRolesSeed` pobló la tabla con varios roles y permisos.

**Solución aplicada (código):**
Además del índice, se agregó `IMemoryCache` al middleware para cachear el rol del usuario y los permisos del rol por 5 minutos, eliminando las queries a BD en requests subsiguientes.

**Impacto:**
- Tabla: `security.Roles`
- Columna indexada: `Name`
- Nombre del índice: `IX_Roles_Name`
- No requiere migración de datos, solo creación del índice.
