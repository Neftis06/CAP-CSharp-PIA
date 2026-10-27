# SVR · Proyecto base del integrador

Sistema de Venta y Reabastecimiento para la tienda 0457 de una cadena de tiendas de conveniencia.
Este proyecto base ya trae la estructura, los datos, la lectura de los CSV y el CEDIS simulado,
para que el día 2 dediques el tiempo a la lógica de negocio.

- **.NET 8** y **C# 12** (`Directory.Build.props`); `global.json` pide un SDK 8.0.
- Compila y corre desde el inicio: muestra las filas leídas y las historias pendientes.

## Cómo correr

```bash
dotnet build
dotnet run --project src/SVR.Consola
dotnet test
```

## Lo que ya funciona

| Pieza | Dónde | Qué hace |
|---|---|---|
| `LectorCsv` | `src/SVR.Infraestructura` | Lee un CSV en streaming y regresa cada fila con su número de línea y sus campos como texto. No convierte ni valida |
| `CedisSimulado` | `src/SVR.Infraestructura` | Simula el CEDIS con demoras reales; el yogurt tarda 4 s, así que con un timeout de 3 s su consulta debe cancelarse |
| `ICedisServicio`, `IInventarioRepo` | `src/SVR.Aplicacion/Contratos.cs` | Contratos para depender de abstracciones (DIP) |
| `Reglas` | `src/SVR.Dominio/Reglas.cs` | Tasa de IVA, horario de alcohol y máximo en vales, sin números mágicos |
| Datos | `datos/` | Catálogo, promociones y ventas del día; se copian junto al ejecutable |
| Prueba de ejemplo | `tests/SVR.Pruebas` | Prueba de `LectorCsv`, como plantilla para tus pruebas |

## Lo que construyes

Cada archivo `Pendientes.cs` describe qué va en esa capa. En `Program.cs`, reemplaza cada `Pendiente(...)` por tu código.

| Avance | Hora | Historias | Dónde va |
|---|---|---|---|
| 1 | 9:35–10:15 | HU-01 catálogo con validaciones · HU-02 ventas y `rechazos.log` | Dominio (Producto, LineaVenta, excepciones) |
| 2 | 10:15–11:15 | HU-03 promociones · HU-04 medios de pago · HU-05 reglas de venta · HU-06 inventario | Dominio, Aplicación (ServicioVenta), Infraestructura (InventarioEnMemoria) |
| 3 | 11:15–12:15 | HU-07 CEDIS en paralelo con timeout · HU-08 pedido sugerido y corte de caja | Aplicación (ServicioReabasto), Infraestructura (CorteCajaExportador) |

Regla de dependencias: Consola → Infraestructura → Aplicación → Dominio. El Dominio no depende de nadie.

## Los datos

- **catalogo.csv:** EAN, descripción, categoría, precio (con IVA y punto decimal), punto de reorden y stock.
- **promociones.csv:** `NXM 2;1` = lleva 2, paga 1 · `PORCENTAJE 15` = 15 % de descuento · `VOLUMEN 6;18.00` = desde 6 piezas, cada una a $18.00.
- **ventas_0457.csv:** tiene errores a propósito. El sistema debe rechazarlos con número de línea y motivo, y seguir:
  - línea 4: EAN inexistente → `ProductoNoEncontradoException`
  - línea 5: `piezas = abc` → error de formato
  - línea 6: alcohol a las 23:30 → `VentaRestringidaException`

Agrega 50 a 100 líneas de venta propias para que el reporte y el pedido sugerido tengan sentido.
