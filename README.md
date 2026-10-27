# SVR - Sistema de Venta y Reabastecimiento

Sistema desarrollado en C# (.NET 8) bajo los principios de **Clean Architecture** (Arquitectura Limpia), diseñado para gestionar catálogos, procesar ventas con reglas de negocio estrictas, aplicar promociones, calcular puntos de reorden y comunicarse de forma concurrente con un Centro de Distribución (CEDIS), generando reportes analíticos tanto en texto como en formato CSV.

---

## 🏗️ Arquitectura del Sistema

El proyecto está estructurado en capas desacopladas para garantizar la mantenibilidad, escalabilidad y testabilidad:

1. **`SVR.Dominio`**: Contiene las entidades principales (`Producto`, `Promocion`, `TipoPago`, `Venta`), objetos de valor y excepciones de negocio (`TiendaException`).
2. **`SVR.Aplicacion`**: Aloja los casos de uso y servicios de lógica de negocio (`ServicioCatalogo`, `ServicioVentas`, `ServicioPromociones`, `ServicioReportes`, `ServicioReabastecimiento`, `ServicioCedisParalelo`).
3. **`SVR.Infraestructura`**: Gestiona los adaptadores de entrada/salida, lectura de archivos CSV (`LectorCsv`, `LectorPromociones`, `LectorVentas`) y persistencia de logs/reportes.
4. **`SVR.Consola`**: Capa de presentación (Entry Point) que coordina la ejecución de todo el flujo mediante la interfaz de comandos (`Program.cs`).
5. **`SVR.Pruebas`**: Capa de pruebas unitarias y de integración para validar las reglas de negocio.

---

## 📋 Historias de Usuario Implementadas (HU-01 a HU-08)

* **HU-01 y HU-02**: Carga masiva y validación de catálogos y promociones desde archivos CSV, generando bitácoras de rechazo ante errores de formato o precios inválidos.
* **HU-03 y HU-04**: Procesamiento de transacciones de venta con validación en tiempo real de existencias y aplicación automática de promociones (ej. *2x1* y *Descuento por Volumen*).
* **HU-05**: Validación de restricciones horarias para la venta de bebidas alcohólicas (permitido exclusivamente de 11:00 AM a 10:00 PM) y generación del **Cierre de Caja** (`cierre_caja.log`).
* **HU-06**: Disparador automático de órdenes de reabastecimiento hacia el CEDIS cuando el stock de un producto desciende por debajo o iguala su punto de reorden.
* **HU-07**: Consulta concurrente y en paralelo de existencias en el CEDIS utilizando programación asíncrona avanzada (`Task.WhenAll`, `CancellationTokenSource`) con un **timeout estricto de 3 segundos**.
* **HU-08**: Exportación de reportes gerenciales avanzados (Top de Ventas y Pedido Sugerido al CEDIS) estructurados en formato **CSV** para análisis financiero y logístico.

---

## 🚀 Requisitos Previos

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) instalado en tu equipo.

---

## ⚙️ Instrucciones de Compilación y Ejecución

1. Clona o ubícate en la carpeta raíz del proyecto (`SVR`).
2. Restaura las dependencias y compila la solución completa ejecutando en tu terminal:
   ```bash
   dotnet build