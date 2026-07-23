# UsaAutoPartes API

Backend de un **ERP para importadora de autopartes**. Gestiona inventario, importaciones, ventas, caja, créditos, préstamos y usuarios con roles diferenciados. 

---

## Tabla de contenidos

- [Características](#características)
- [Stack tecnológico](#stack-tecnológico)
- [Arquitectura](#arquitectura)
- [Requisitos previos](#requisitos-previos)
- [Instalación y ejecución](#instalación-y-ejecución)
- [Configuración](#configuración)
- [Base de datos y migraciones](#base-de-datos-y-migraciones)
- [Autenticación y roles](#autenticación-y-roles)
- [API REST](#api-rest)
- [GraphQL](#graphql)
- [SignalR](#signalr)
- [Documentación de la API](#documentación-de-la-api)
- [Despliegue](#despliegue)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Documentación adicional](#documentación-adicional)

---

## Características

| Módulo | Descripción |
|--------|-------------|
| **Productos** | CRUD, cambio de precios, historial, búsqueda, exportación Excel, importación masiva |
| **Kits** | Productos compuestos por piezas; conversión kit ↔ regular; venta parcial de piezas |
| **Importaciones** | Registro de importaciones con detalle, proveedores y costos (flete, aduana, transporte) |
| **Ventas** | Órdenes de venta con flujo completo (almacén → caja), venta rápida y reserva de stock |
| **Caja** | Apertura, movimientos y cierre de caja |
| **Créditos** | Ventas a crédito con pagos parciales |
| **Préstamos** | Préstamo y devolución de productos a clientes |
| **Clientes y marcas** | Gestión de clientes y marcas de productos |
| **Descuentos** | Descuentos configurables con activación/desactivación |
| **Tipo de cambio** | Registro de tipos de cambio |
| **Ajustes de stock** | Ajustes manuales en productos y piezas de kit |
| **Usuarios** | Registro, bloqueo por horario, comisiones y gestión de acceso |
| **Facturas (IA)** | Extracción de productos desde archivos Excel/PDF usando Claude API |

---

## Stack tecnológico

- **.NET 9** — ASP.NET Core Web API
- **PostgreSQL** — Base de datos (Npgsql + Entity Framework Core 9)
- **ASP.NET Core Identity** — Gestión de usuarios y roles
- **JWT** — Autenticación con tokens en cookies HTTP-only
- **HotChocolate 15** — API GraphQL con filtros, ordenamiento y paginación
- **Scalar** — Documentación interactiva de la API REST (OpenAPI)
- **SignalR** — Notificaciones en tiempo real de ventas
- **Mapster** — Mapeo de objetos
- **EPPlus** — Exportación/importación Excel
- **Anthropic (Claude)** — Extracción inteligente de facturas

---

## Arquitectura

El proyecto sigue **Clean Architecture** con 4 capas y dependencias que apuntan hacia el dominio:

```
UsaAutoPartes.Api.sln
├── UsaAutoPartes.Api             → Presentación (controllers, GraphQL, middleware)
├── UsaAutoPartes.Application     → Lógica de negocio (DTOs, interfaces, excepciones)
├── UsaAutoPartes.Domain          → Entidades y enums del dominio
└── UsaAutoPartes.Infrastructure  → EF Core, repositorios, servicios externos
```

```
[ Api ]
   ↓
[ Application ]
   ↓
[ Domain ] ← [ Infrastructure ]
```

Flujo típico de una petición:

```
Cliente HTTP → Controller → IRepositorio → Repositorio → DbContext → Entidad → DTO/Type → Respuesta
```

Para más detalle sobre cada capa y sus reglas, consulta [`arquitectura.md`](./arquitectura.md).

---
 

## Instalación y ejecución

```bash
# Clonar el repositorio
git clone https://github.com/JaimeGLT/backend-importadora.git
cd backend-importadora

# Restaurar dependencias
dotnet restore UsaAutoPartes.Api/UsaAutoPartes.Api.sln

# Compilar
dotnet build UsaAutoPartes.Api/UsaAutoPartes.Api.sln

# Ejecutar la API
dotnet run --project UsaAutoPartes.Api/UsaAutoPartes.Api/UsaAutoPartes.Api.csproj
```

La API arranca por defecto en:

| Entorno | URL |
|---------|-----|
| HTTP | `http://localhost:5120` |
| HTTPS | `https://localhost:7134` |

---

## Configuración

Copia y configura `appsettings.json` en `UsaAutoPartes.Api/UsaAutoPartes.Api/`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ConexionDataBase": {
    "CadenaConexion": "Host=TU_HOST;Database=TU_DB;Username=TU_USUARIO;Password=TU_PASSWORD;SSL Mode=Require"
  },
  "JwtOptions": {
    "Issuer": "usa.auto.partes.Com",
    "Experice": 30,
    "Audience": "usa.auto.partes.Api",
    "SecretKey": "TU_CLAVE_SECRETA_LARGA_Y_SEGURA"
  },
  "IA": {
    "ClaudeApiKey": "TU_API_KEY_DE_ANTHROPIC"
  },
  "Cors": {
    "Origins": [
      "http://localhost:5173",
      "http://localhost:5174"
    ]
  }
}
```

> **Importante:** No subas credenciales reales al repositorio. Usa `appsettings.Development.json` (ignorado por git) o variables de entorno en producción. Rota cualquier secreto que haya quedado expuesto en el historial de git.

| Sección | Descripción |
|---------|-------------|
| `ConexionDataBase:CadenaConexion` | Cadena de conexión PostgreSQL |
| `JwtOptions` | Emisor, audiencia, expiración (minutos) y clave secreta del JWT |
| `IA:ClaudeApiKey` | API key de Anthropic para extracción de facturas |
| `Cors:Origins` | Orígenes permitidos del frontend (con `AllowCredentials`) |

---

## Base de datos y migraciones

Aplicar migraciones existentes:

```bash
dotnet ef database update \
  --project UsaAutoPartes.Api/UsaAutoPartes.Infrastructure \
  --startup-project UsaAutoPartes.Api/UsaAutoPartes.Api
```

Crear una nueva migración:

```bash
dotnet ef migrations add NombreDescriptivo \
  --project UsaAutoPartes.Api/UsaAutoPartes.Infrastructure \
  --startup-project UsaAutoPartes.Api/UsaAutoPartes.Api
```

Al iniciar la aplicación se crean automáticamente los roles: **Admin**, **Cajero**, **Almacenero** y **Operador**.

---

## Autenticación y roles

La autenticación usa **JWT almacenado en cookies HTTP-only**:

| Cookie | Uso |
|--------|-----|
| `access` | Token de acceso |
| `accessreload` | Refresh token (ruta `/api/Auth/refresh`) |

### Endpoints de autenticación

| Método | Ruta | Descripción | Rol requerido |
|--------|------|-------------|---------------|
| `POST` | `/api/Auth/login` | Iniciar sesión | Público |
| `POST` | `/api/Auth/refresh` | Renovar token | Público (cookie) |
| `POST` | `/api/Auth/logout` | Cerrar sesión | Autenticado |
| `POST` | `/api/Auth` | Registrar usuario | Admin |

### Roles del sistema

| Rol | Descripción |
|-----|-------------|
| **Admin** | Acceso total: importaciones, proveedores, usuarios, configuración |
| **Cajero** | Operaciones de caja y ventas |
| **Almacenero** | Preparación de pedidos e inventario |
| **Operador** | Operaciones generales |

Un middleware (`UsuarioBloqueadoMiddleware`) bloquea el acceso a usuarios desactivados o fuera de su horario permitido.

---

## API REST

Todos los controladores están bajo el prefijo `/api/`.

| Controlador | Ruta base | Funcionalidad principal |
|-------------|-----------|-------------------------|
| `AuthController` | `/api/Auth` | Login, registro, refresh, logout |
| `ProductoController` | `/api/Producto` | CRUD, kits, importación Excel, búsqueda, exportación |
| `ProveedorController` | `/api/Proveedor` | CRUD de proveedores |
| `OrdenVentaController` | `/api/OrdenVenta` | Flujo completo de ventas |
| `ClienteController` | `/api/Cliente` | CRUD de clientes |
| `CajaController` | `/api/Caja` | Apertura, movimientos y cierre |
| `CreditoController` | `/api/Credito` | Créditos y pagos |
| `PrestamoController` | `/api/Prestamo` | Préstamos y devoluciones |
| `DescuentoController` | `/api/Descuento` | CRUD y estado de descuentos |
| `MarcaController` | `/api/Marca` | CRUD de marcas |
| `TipoCambioController` | `/api/TipoCambio` | Registro de tipo de cambio |
| `MargenGananciaController` | `/api/MargenGanancia` | Configuración de márgenes |
| `ConfigVentaController` | `/api/ConfigVenta` | Configuración de ventas |
| `AjusteStockController` | `/api/AjusteStock` | Ajustes de inventario |
| `UsuarioController` | `/api/Usuario` | Gestión de usuarios y horarios |
| `MiCuentaController` | `/api/MiCuenta` | Perfil y cambio de contraseña |
| `FacturaController` | `/api/Factura` | Extracción de productos desde Excel/PDF |

---

## GraphQL

Endpoint: **`/graphql`** (Banana Cake Pop / IDE integrado de HotChocolate)

Consultas disponibles:

- `productos`, `proveedores`, `importaciones`
- `ordenesVenta`, `clientes`, `descuentos`
- `prestamos`, `cajas`, `creditos`
- `tipoCambio`, `marcas`, `margenGanancia`, `configVenta`
- `ajusteStock`, `me` (usuario autenticado)

Soporta **filtrado**, **ordenamiento** y **paginación** en las consultas.

---

## SignalR

Hub de ventas en tiempo real:

```
/hubs/ventas
```

Métodos disponibles:
- `UnirseAGrupo(grupo)` — Suscribirse a un grupo de notificaciones
- `AbandonarGrupo(grupo)` — Salir de un grupo

Requiere autenticación JWT.

---

## Documentación de la API

Con la aplicación en ejecución:

| Recurso | URL |
|---------|-----|
| **Scalar (UI interactiva)** | `https://localhost:7134/scalar/v1` |
| **OpenAPI JSON** | `https://localhost:7134/openapi/v1.json` |
| **GraphQL IDE** | `https://localhost:7134/graphql` |

---

 
---

## Estructura del repositorio

```
backend-importadora/
├── .github/
│   └── workflows/              # CI/CD hacia Azure
├── UsaAutoPartes.Api/
│   ├── UsaAutoPartes.Api/      # Capa de presentación
│   │   ├── Controllers/
│   │   ├── Hubs/
│   │   ├── Middleware/
│   │   ├── Schema/             # GraphQL queries y types
│   │   └── Program.cs
│   ├── UsaAutoPartes.Application/
│   ├── UsaAutoPartes.Domain/
│   ├── UsaAutoPartes.Infrastructure/
│   │   ├── Data/
│   │   ├── Migrations/
│   │   └── Servicios/
│   └── UsaAutoPartes.Api.sln
 
```

 

---

## Licencia

Proyecto privado. Todos los derechos reservados.
