# Especificaciones del Proyecto — Sistema de Gestión de Concesionaria

## 1. Resumen

Sistema web para la gestión de una concesionaria de automóviles (0km y usados). Permite cargar vehículos, vendedores, clientes y ventas, con cálculo automático de comisiones según reglas de negocio configurables. Incluye dashboard de estadísticas y auditoría de acciones.

## 2. Stack tecnológico

- **Framework:** ASP.NET Core MVC — .NET 10
- **Base de datos:** SQL Server
- **ORM:** Entity Framework Core (Code First)
- **Metodología de desarrollo:** Spec-Driven Development (OpenCode)

## 3. Roles de usuario

Un único tipo de entidad `Usuario` con un campo `Rol`:

| Rol | Permisos |
|---|---|
| **Admin** (administrador/gerente/dueño) | CRUD de vehículos, CRUD de vendedores, ver dashboard y estadísticas completas, anular ventas, editar configuración de comisiones y recargos |
| **Vendedor** | Cargar ventas, cargar/editar clientes, ver sus propias ventas y comisiones |

> No se distingue entre "gerente" y "administrador" como entidades separadas: ambos títulos mapean al mismo rol `Admin`, dado que no existe diferencia de permisos entre ellos.

## 4. Entidades y propiedades

### 4.1 Usuario
| Campo | Tipo | Notas |
|---|---|---|
| Id | int/Guid | PK |
| Nombre | string | |
| Email | string | único, usado para login |
| PasswordHash | string | |
| Telefono | string | |
| Rol | enum (Admin, Vendedor) | |
| FechaAlta | DateTime | usada para calcular antigüedad automáticamente |
| Activo | bool | baja lógica — nunca borrar físicamente (rompería historial de ventas) |

### 4.2 Vehiculo
| Campo | Tipo | Notas |
|---|---|---|
| Id | int/Guid | PK |
| Marca | string | |
| Modelo | string | |
| Anio | int | año/modelo |
| Tipo | enum (Sedan, CuatroPuertas, Cuatro4x4, Deportivo) | define comisión base |
| EsUsado | bool | true = usado, false = 0km |
| Patente | string | nullable si es 0km sin patentar aún |
| Color | string | |
| Kilometraje | int | nullable/0 si es 0km |
| PrecioBase | decimal | |
| Estado | enum (Disponible, Reservado, Vendido) | |
| FechaIngreso | DateTime | fecha de ingreso al stock |

### 4.3 Cliente
| Campo | Tipo | Notas |
|---|---|---|
| Id | int/Guid | PK |
| Nombre | string | |
| DNI | string | único |
| Telefono | string | |
| Email | string | |
| Direccion | string | opcional |
| FechaAlta | DateTime | |

### 4.4 Venta
| Campo | Tipo | Notas |
|---|---|---|
| Id | int/Guid | PK |
| VehiculoId | FK → Vehiculo | |
| ClienteId | FK → Cliente | |
| VendedorId | FK → Usuario | |
| FechaVenta | DateTime | |
| MetodoPago | enum (Efectivo, Tarjeta, FinanciacionPropia) | fijo, no configurable |
| CantidadCuotas | int | 1 si es contado |
| PrecioBase | decimal | precio del vehículo sin recargo |
| PrecioFinal | decimal | precio base + recargo por cuotas |
| PorcentajeComisionAplicado | decimal | snapshot del % usado (ver §5) |
| ComisionCalculada | decimal | monto final que cobra el vendedor |
| Estado | enum (Activa, Anulada) | |
| Observaciones | string | opcional |
| MotivoAnulacion | string | nullable, obligatorio si Estado = Anulada |
| FechaAnulacion | DateTime | nullable |
| AnuladoPorUsuarioId | FK → Usuario | nullable — quién anuló |

### 4.5 ComisionPorTipoVehiculo (configurable, editable por Admin)
| Campo | Tipo |
|---|---|
| Tipo (enum Vehiculo.Tipo) | PK |
| PorcentajeBase | decimal |

### 4.6 ComisionPorAntiguedad (configurable, editable por Admin)
| Campo | Tipo |
|---|---|
| Id | PK |
| MesesMin | int |
| MesesMax | int (nullable = sin tope) |
| PorcentajeAdicional | decimal |

### 4.7 RecargoPorCuotas (configurable, editable por Admin)
| Campo | Tipo |
|---|---|
| Id | PK |
| CuotasMin | int |
| CuotasMax | int (nullable = sin tope) |
| PorcentajeRecargo | decimal |

### 4.8 RegistroAuditoria
| Campo | Tipo | Notas |
|---|---|---|
| Id | PK | |
| UsuarioId | FK → Usuario | quién ejecutó la acción |
| Accion | string | ej: "CargoVenta", "AnuloVenta", "CargoVehiculo", "EditoVehiculo" |
| EntidadAfectada | string | nombre de la entidad (ej: "Venta") |
| EntidadId | int/Guid | id del registro afectado |
| Fecha | DateTime | |
| DetalleJson | string | nullable, snapshot opcional de los datos relevantes |

## 5. Reglas de negocio — Cálculo de comisión

**Fórmula:**
```
% Comisión = ComisionPorTipoVehiculo[vehiculo.Tipo].PorcentajeBase
           + ComisionPorAntiguedad[antigüedad del vendedor].PorcentajeAdicional

ComisionCalculada = Venta.PrecioBase × (% Comisión / 100)
```

- La comisión se calcula **sobre el `PrecioBase` del vehículo**, no sobre el `PrecioFinal` con recargo de cuotas. El recargo por financiación compensa el costo/riesgo financiero, no es venta generada por el vendedor.
- La antigüedad del vendedor se calcula automáticamente como `(FechaVenta - Usuario.FechaAlta)` en meses, al momento de cada venta (no es un valor fijo del vendedor: crece con el tiempo).
- El `PorcentajeComisionAplicado` y `ComisionCalculada` se guardan como snapshot en la tabla `Venta` en el momento de la carga. Si luego se edita la tabla `ComisionPorTipoVehiculo` o `ComisionPorAntiguedad`, las ventas ya cargadas **no se recalculan**.
- **Valores base sugeridos** (editables por el Admin desde el sistema):

  | Tipo de vehículo | % Base |
  |---|---|
  | Sedán | 3% |
  | 4 Puertas | 3.5% |
  | 4x4 | 5% |
  | Deportivo | 4% |

  | Antigüedad vendedor | % Adicional |
  |---|---|
  | 0–6 meses | +0% |
  | 6–12 meses | +0.5% |
  | 1–3 años | +1% |
  | +3 años | +1.5% |

## 6. Reglas de negocio — Cálculo de precio final (recargo por cuotas)

**Fórmula:**
```
PrecioFinal = PrecioBase × (1 + RecargoPorCuotas[cantidad de cuotas].PorcentajeRecargo / 100)
```

- Cálculo por **tabla de tramos fija**, no interés compuesto.
- **Valores sugeridos** (editables por el Admin):

  | Cuotas | Recargo |
  |---|---|
  | 1 (contado) | 0% |
  | 2–3 | 5% |
  | 4–6 | 10% |
  | 7–12 | 18% |

- Métodos de pago (`Efectivo`, `Tarjeta`, `FinanciacionPropia`) son un enum fijo en código, **no** configurable desde el sistema.

## 7. Flujos principales

### 7.1 Carga de venta (rol Vendedor)
1. El vendedor selecciona un vehículo con Estado = `Disponible`.
2. Carga o selecciona un cliente existente.
3. Ingresa método de pago y cantidad de cuotas.
4. El sistema calcula automáticamente `PrecioFinal` (regla §6) y `ComisionCalculada` (regla §5) **al guardar**, sin paso de confirmación intermedio.
5. Al confirmar la venta:
   - `Venta.Estado` = `Activa`.
   - `Vehiculo.Estado` pasa a `Vendido`.
   - Se genera un `RegistroAuditoria` con acción `CargoVenta`.

### 7.2 Anulación de venta (solo rol Admin)
1. Solo un usuario con rol `Admin` puede anular una venta. El vendedor que la cargó **no puede** anularla ni editarla.
2. Al anular, es obligatorio completar `MotivoAnulacion`.
3. `Venta.Estado` pasa a `Anulada`, se registra `FechaAnulacion` y `AnuladoPorUsuarioId`.
4. **El vehículo NO vuelve automáticamente a `Disponible`** — el Admin debe cambiar el estado del vehículo manualmente si corresponde (puede haber sido anulada por otro motivo que no libera el auto, ej. error de carga de datos vs. venta caída).
5. Se genera un `RegistroAuditoria` con acción `AnuloVenta`.

### 7.3 Dashboard y estadísticas (solo rol Admin)
- Filtro obligatorio por **rango de fechas**.
- Estadísticas por vendedor: cantidad de ventas, monto total vendido, comisiones acumuladas (en el período filtrado).
- Estadísticas por vehículo/tipo: cuáles se venden más, tiempo promedio en stock (`FechaIngreso` → `FechaVenta`).
- Estadísticas por cliente: histórico de compras.
- Acceso al `RegistroAuditoria` filtrable por usuario, acción y fecha.

## 8. Reglas de integridad y permisos — resumen

- Ningún registro de `Usuario`, `Vehiculo` o `Cliente` con ventas asociadas se borra físicamente; solo baja lógica (`Activo = false` en Usuario; estado en Vehiculo).
- Un vehículo con Estado ≠ `Disponible` no debe poder seleccionarse para una nueva venta.
- Toda acción de creación/edición/anulación relevante genera un registro en `RegistroAuditoria`.
- Las tablas `ComisionPorTipoVehiculo`, `ComisionPorAntiguedad` y `RecargoPorCuotas` son editables únicamente por rol `Admin`.

## 9. Fuera de alcance (explícitamente excluido)

- Los clientes finales no tienen acceso al sistema ni cargan sus propios datos — toda carga la hace el vendedor.
- No hay entidad `DetalleVenta`: cada venta corresponde a un único vehículo (no se contemplan ventas multi-ítem como accesorios o seguros dentro de la misma transacción).
- No se implementa cálculo de interés compuesto para financiación — solo tabla de tramos fija.
- Métodos de pago no son configurables por el usuario del sistema.

## 10. Preguntas abiertas / a definir durante el desarrollo

- Formato exacto de `DetalleJson` en auditoría (qué campos snapshot conviene guardar por tipo de acción).
- Paginación y exportación (CSV/Excel) del dashboard — no especificado aún.
- Validación de unicidad de `Patente` a nivel de base de datos (constraint) — se recomienda, a confirmar.
