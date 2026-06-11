# db_changes

Carpeta de control de cambios a la base de datos del proyecto Diamante.

El objetivo es que cualquier miembro del equipo pueda entender **qué cambió en la BD, por qué y cómo aplicarlo**, sin depender únicamente de las migraciones automáticas de EF Core.

---

## Estructura

```
db_changes/
├── README.md        — este archivo
├── CHANGELOG.md     — bitácora cronológica de todos los cambios
└── scripts/         — scripts SQL listos para ejecutar
    └── YYYYMMDD_NNN_descripcion.sql
```

---

## Convención de nombres para scripts

```
YYYYMMDD_NNN_descripcion-corta.sql
```

| Parte | Descripción |
|---|---|
| `YYYYMMDD` | Fecha del cambio (ej. `20260611`) |
| `NNN` | Número secuencial del día, empieza en `001` |
| `descripcion-corta` | Qué hace el script, en kebab-case |

**Ejemplo:** `20260611_001_add-index-roles-name.sql`

---

## Cómo agregar un cambio

1. Crea el script `.sql` en `scripts/` siguiendo la convención de nombres.
2. Agrega una entrada al inicio de `CHANGELOG.md` con fecha, descripción, motivación y referencia al archivo.
3. Haz commit junto con los cambios de código que lo originaron.

---

## Cómo aplicar un script

Conectarte a la BD y ejecutar el archivo en orden cronológico (por nombre).
Cada script debe ser **idempotente** cuando sea posible — es decir, ejecutarlo dos veces no debe romper nada.

```sql
-- Ejemplo de script idempotente para un índice
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_Name')
    CREATE INDEX IX_Roles_Name ON security.Roles (Name);
```

---

## Reglas del equipo

- **No modificar scripts ya aplicados en producción.** Si hay un error, crear un nuevo script que lo corrija.
- **Documentar siempre el "por qué"**, no solo el "qué". El código dice qué hace; la bitácora dice por qué fue necesario.
- **Un script por cambio lógico.** No acumular varios cambios no relacionados en un solo archivo.
