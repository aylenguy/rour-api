# UrbanStore — Backend API

API REST del e-commerce **UrbanStore**, construida con ASP.NET Core 8 y Entity Framework Core con PostgreSQL.

---

## 🛠 Stack

| Tecnología | Uso |
|---|---|
| [ASP.NET Core 8](https://dotnet.microsoft.com/) | Framework web |
| Entity Framework Core | ORM para acceso a datos |
| PostgreSQL | Base de datos relacional |
| JWT (JSON Web Tokens) | Autenticación del panel admin |
| Data Annotations | Validación de entrada |

---

## 📁 Estructura del proyecto

```
UrbanStore.API/
├── Controllers/
│   ├── AuthController.cs       # Login y generación de JWT
│   ├── OrdersController.cs     # CRUD de órdenes + validaciones
│   └── ProductsController.cs   # CRUD de productos
├── Data/
│   └── AppDbContext.cs         # Contexto de EF Core
├── Models/
│   ├── AdminUser.cs            # Modelo de usuario admin
│   ├── Order.cs                # Modelo de orden
│   ├── OrderItem.cs            # Modelo de ítem de orden
│   └── Product.cs              # Modelo de producto
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

---

## ⚙️ Variables de entorno / configuración

En `appsettings.Development.json` configurá la cadena de conexión y el secreto JWT:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=urbanstore;Username=postgres;Password=tu_password"
  },
  "Jwt": {
    "Key": "tu_clave_secreta_larga_y_segura",
    "Issuer": "UrbanStore",
    "Audience": "UrbanStore"
  }
}
```

> En producción usá variables de entorno del servidor en lugar de appsettings. Nunca expongas la `Jwt:Key` en el repositorio.

---

## 🚀 Instalación y ejecución

### Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/) corriendo localmente

```bash
# 1. Clonar el repositorio
git clone https://github.com/tu-usuario/urbanstore-backend.git
cd urbanstore-backend

# 2. Restaurar dependencias
dotnet restore

# 3. Configurar la cadena de conexión y JWT
# Editá appsettings.Development.json con tus valores

# 4. Aplicar migraciones y crear la base de datos
dotnet ef database update

# 5. Correr la API
dotnet run
```

La API estará disponible en `http://localhost:5000`.  
Swagger UI en `http://localhost:5000/swagger` (solo en desarrollo).

---

## 📡 Endpoints

### Autenticación — `/api/Auth`

| Método | Ruta | Descripción | Auth |
|---|---|---|---|
| `POST` | `/api/Auth/login` | Login de admin, devuelve JWT | No |

#### Body — `POST /api/Auth/login`

```json
{
  "email": "admin@urbanstore.com",
  "password": "tu_password"
}
```

#### Respuesta exitosa

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

---

### Órdenes — `/api/Orders`

| Método | Ruta | Descripción | Auth |
|---|---|---|---|
| `POST` | `/api/Orders` | Crear una nueva orden | No |
| `GET` | `/api/Orders` | Listar todas las órdenes | JWT |
| `GET` | `/api/Orders/{id}` | Obtener orden por ID | No |
| `PATCH` | `/api/Orders/{id}/status` | Actualizar estado de una orden | JWT |
| `DELETE` | `/api/Orders/{id}` | Eliminar una orden | JWT |

#### Body — `POST /api/Orders`

```json
{
  "customerName": "Juan García",
  "customerEmail": "juan@email.com",
  "address": "San Martín 1234",
  "city": "Rosario",
  "province": "Santa Fe",
  "zip": "2000",
  "paymentMethod": "mp",
  "items": [
    {
      "productId": 1,
      "quantity": 2,
      "size": "M",
      "color": "Negro"
    }
  ]
}
```

> **Importante:** el campo `total` no se acepta desde el cliente. El backend lo recalcula consultando los precios reales de la base de datos.

#### Valores válidos para `paymentMethod`
`"mp"` · `"card"` · `"transfer"`

#### Valores válidos para `status` (PATCH)
`"pending"` · `"confirmed"` · `"shipped"` · `"delivered"` · `"cancelled"`

---

### Productos — `/api/Products`

| Método | Ruta | Descripción | Auth |
|---|---|---|---|
| `GET` | `/api/Products` | Listar todos los productos | No |
| `GET` | `/api/Products/{id}` | Obtener producto por ID | No |
| `POST` | `/api/Products` | Crear producto | JWT |
| `PUT` | `/api/Products/{id}` | Actualizar producto | JWT |
| `DELETE` | `/api/Products/{id}` | Eliminar producto | JWT |

---

## 🔐 Autenticación JWT

Los endpoints protegidos requieren el token en el header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

El token se obtiene haciendo `POST /api/Auth/login` con las credenciales del admin.

---

## ✅ Validaciones

El endpoint `POST /api/Orders` aplica las siguientes validaciones:

| Campo | Regla |
|---|---|
| `customerName` | Obligatorio, 3–100 caracteres |
| `customerEmail` | Obligatorio, formato email válido |
| `address` | Obligatorio, 5–200 caracteres |
| `city` | Obligatorio, 2–100 caracteres |
| `province` | Obligatorio, 2–100 caracteres |
| `zip` | Opcional, máximo 8 caracteres |
| `paymentMethod` | Obligatorio, lista blanca de valores |
| `items` | Al menos 1 ítem |
| `items[].productId` | Debe existir en la base de datos |
| `items[].quantity` | Entre 1 y 100 |
| Total | Recalculado en el servidor (precio real × cantidad) |

---

## 🗄 Migraciones

```bash
# Crear una nueva migración
dotnet ef migrations add NombreDeLaMigracion

# Aplicar migraciones pendientes
dotnet ef database update

# Revertir la última migración
dotnet ef migrations remove
```

---

## 📝 Notas

- El backend **nunca confía en el total enviado por el cliente**. Siempre recalcula el precio desde la base de datos.
- El costo de envío también se calcula en el servidor: gratis para órdenes ≥ $50.000, caso contrario $2.500.
- Los campos `Status` y `CreatedAt` de la orden son asignados por el servidor y no pueden ser enviados por el cliente.
- La `Jwt:Key` debe tener al menos 32 caracteres para ser segura.
