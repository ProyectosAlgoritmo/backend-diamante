# AI_CONTEXT_BACKEND

## Objetivo

Este documento define las reglas arquitectónicas, convenciones de código y estándares que toda IA generativa (Cursor, Claude Code, Windsurf, GitHub Copilot, ChatGPT, Cline, Roo Code, etc.) debe seguir al generar, modificar o refactorizar código dentro de este proyecto.

La prioridad es mantener consistencia, escalabilidad, seguridad y alineación con la arquitectura existente.

---

# Stack Tecnológico

```text
.NET 8
ASP.NET Core Web API
Entity Framework Core
SQL Server
JWT Authentication
OAuth (Google / Microsoft)
Swagger
BCrypt
```

---

# Arquitectura del Proyecto

Seguir siempre la siguiente estructura:

```text
Controller
    ↓
Logic Interface
    ↓
Logic Implementation
    ↓
ApplicationDbContext
    ↓
Database
```

---

# Regla Fundamental

Este proyecto NO utiliza Services.

Toda la lógica de negocio debe vivir dentro de:

```text
Logic/
```

Ejemplos:

```text
UsersLogic
RolesLogic
AuthLogic
CostCentersLogic
InventoryLogic
```

Nunca crear carpetas o clases llamadas:

```text
Services
UserService
RoleService
InventoryService
```

Utilizar siempre la capa Logic.

---

# Responsabilidad de Cada Capa

## Controllers

Responsables únicamente de:

* Recibir requests HTTP.
* Validar parámetros básicos.
* Ejecutar lógica mediante Logic.
* Retornar respuestas.

No colocar lógica de negocio dentro de Controllers.

---

## Logic

Responsable de:

* Reglas de negocio.
* Consultas a base de datos.
* Validaciones de negocio.
* Transformación de datos.
* Operaciones CRUD.

Toda lógica debe vivir aquí.

---

## Entities

Representan tablas de base de datos.

Ubicación:

```text
Models/Entities
```

Convenciones:

```text
PascalCase
Singular
```

Ejemplos:

```csharp
User
Role
Permission
Company
CostCenter
Operator
```

---

## DTOs

Nunca exponer entidades directamente al cliente.

Siempre utilizar DTOs.

Ubicación:

```text
Models/DTOs
```

---

# Convenciones de Nombres

## Requests

Formato:

```text
Create{Entidad}Request
Update{Entidad}Request
```

Ejemplos:

```csharp
CreateUserRequest
UpdateUserRequest
CreateCompanyRequest
```

---

## Responses

Formato:

```text
{Entidad}Response
```

Ejemplos:

```csharp
UserResponse
RoleResponse
CompanyResponse
```

---

## Interfaces

Formato:

```text
I{Nombre}Logic
```

Ejemplos:

```csharp
IUsersLogic
IRolesLogic
IInventoryLogic
```

---

## Implementaciones

Formato:

```text
{Nombre}Logic
```

Ejemplos:

```csharp
UsersLogic
RolesLogic
InventoryLogic
```

---

## Controllers

Formato:

```text
{Nombre}Controller
```

Ejemplos:

```csharp
UsersController
RolesController
InventoryController
```

---

# Métodos Asíncronos

Todos los métodos asíncronos deben terminar con:

```csharp
Async
```

Ejemplos:

```csharp
GetAllAsync()
GetByIdAsync()
CreateAsync()
UpdateAsync()
DeleteAsync()
```

---

# Entity Framework

Utilizar siempre Entity Framework Core.

Buenas prácticas obligatorias:

* Utilizar consultas asíncronas.
* Utilizar LINQ.
* Utilizar AsNoTracking() en consultas de solo lectura.
* Evitar consultas dentro de ciclos.
* Utilizar Include() únicamente cuando sea necesario.
* Evitar cargar relaciones que no serán utilizadas.

---

# Soft Delete

Las entidades que soportan eliminación lógica utilizan:

```csharp
DeletedAt
IsActive
```

Al eliminar:

```csharp
entity.DeletedAt = DateTime.UtcNow;
entity.IsActive = false;
```

Nunca realizar borrado físico cuando la entidad soporte Soft Delete.

Todas las consultas deben excluir registros con:

```csharp
DeletedAt != null
```

---

# Sistema de Permisos

La plataforma utiliza permisos jerárquicos.

Formato:

```text
MODULO.SUBMODULO.ACCION
```

Ejemplos:

```text
SECURITY.ROLES.VIEW
SECURITY.ROLES.CREATE

OPERATIONAL_CONTROL.COST_CENTERS.VIEW
OPERATIONAL_CONTROL.COST_CENTERS.CREATE

OPERATIONAL_CONTROL.STAFF_ASSIGNMENT.EDIT
```

---

# Protección de Endpoints

Siempre que aplique, proteger endpoints mediante:

```csharp
[RequirePermission("MODULO.SUBMODULO.ACCION")]
```

Ejemplo:

```csharp
[RequirePermission("OPERATIONAL_CONTROL.COST_CENTERS.CREATE")]
```

No omitir permisos en funcionalidades sensibles.

---

# Autenticación

El sistema utiliza:

```text
JWT
Refresh Token
Google OAuth
Microsoft OAuth
```

Los endpoints protegidos deben utilizar:

```csharp
[Authorize]
```

cuando corresponda.

---

# Inyección de Dependencias

Toda nueva lógica debe registrarse en:

```csharp
Program.cs
```

Ejemplo:

```csharp
builder.Services.AddScoped<IInventoryLogic, InventoryLogic>();
```

Nunca crear lógica sin registrarla en DI.

---

# Formato de Respuestas

Los Controllers que heredan de BaseController deben utilizar:

## Éxito

```json
{
  "success": true,
  "message": "Operación exitosa",
  "data": {}
}
```

## Error

```json
{
  "success": false,
  "message": "Descripción del error"
}
```

Mantener consistencia con el formato existente.

---

# Creación de Nuevos Módulos

Al crear un nuevo módulo seguir este orden:

```text
1. Entity
2. DTOs
3. DbContext
4. Interface Logic
5. Logic
6. Dependency Injection
7. Controller
8. Migration
```

No saltar pasos.

---

# Seguridad

Reglas obligatorias:

* Nunca almacenar contraseñas en texto plano.
* Utilizar BCrypt para hashing.
* Nunca exponer secretos.
* Nunca exponer entidades completas.
* No retornar información sensible.
* No retornar stack traces en producción.
* Mantener appsettings.json fuera del repositorio.
* Utilizar appsettings.Template.json como referencia.

---

# Git Workflow

Cada funcionalidad debe desarrollarse en una rama independiente.

Formato:

```bash
section/<modulo>
```

Ejemplos:

```bash
section/users
section/roles
section/business
section/inventory
```

---

# Conventional Commits

## Nuevas funcionalidades

```bash
feat:
```

Ejemplos:

```bash
feat: add inventory module
feat: create companies endpoints
feat: implement stock management
```

---

## Correcciones

```bash
fix:
```

Ejemplos:

```bash
fix: update user validation
fix: resolve authentication issue
fix: correct role permissions mapping
```

---

# Reglas de Generación de Código

Cuando generes código para este proyecto:

* Mantén la arquitectura existente.
* Reutiliza patrones ya implementados.
* Sigue las convenciones de nombres existentes.
* Utiliza DTOs para requests y responses.
* No retornes entidades EF Core directamente.
* Mantén Controllers ligeros.
* Coloca toda lógica de negocio en Logic.
* Utiliza métodos Async.
* Registra dependencias en Program.cs.
* Aplica Soft Delete cuando corresponda.
* Protege endpoints con permisos cuando aplique.
* Mantén el formato estándar de respuestas.
* Utiliza Entity Framework Core como mecanismo principal de acceso a datos.

---

# Prácticas Prohibidas

No hacer:

* Lógica de negocio dentro de Controllers.
* Services paralelos a Logic.
* Consultas SQL embebidas innecesarias.
* Retornar entidades directamente.
* Borrar físicamente registros con Soft Delete.
* Ignorar permisos.
* Hardcodear secretos.
* Crear estructuras arquitectónicas distintas a las existentes.
* Hacer push directo a dev o main.

---

# Módulos de Referencia

Los módulos de:

```text
Users
Roles
CostCenters
Auth
```

son la referencia oficial para cualquier nueva implementación.

Antes de crear nuevas funcionalidades, analizar estos módulos y replicar los mismos patrones arquitectónicos.
