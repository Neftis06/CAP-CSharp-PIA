using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using SVR.Aplicacion;
using SVR.Infraestructura;

namespace SVR.Consola;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== SVR - Sistema de Venta y Reabastecimiento ===\n");

        // 1. Definición de rutas de archivos (dentro de la carpeta datos)
        string rutaDatos = Path.Combine(AppContext.BaseDirectory, "datos");
        string rutaCatalogo = Path.Combine(rutaDatos, "catalogo.csv");
        string rutaPromociones = Path.Combine(rutaDatos, "promociones.csv");
        string rutaVentas = Path.Combine(rutaDatos, "ventas.csv");
        string rutaRechazos = Path.Combine(rutaDatos, "rechazos.log");
        
        // Reportes estándar y en formato CSV (HU-05, HU-06, HU-08)
        string rutaReporteCierre = Path.Combine(rutaDatos, "cierre_caja.log");
        string rutaReporteCedis = Path.Combine(rutaDatos, "reabastecimiento_cedis.log");
        string rutaCsvTopVentas = Path.Combine(rutaDatos, "top_ventas.csv");
        string rutaCsvPedido = Path.Combine(rutaDatos, "pedido_sugerido.csv");

        // 2. Instanciación de Infraestructura y Servicios de Aplicación
        ILectorCatalogo lectorCatalogo = new LectorCsv();
        IServicioCatalogo servicioCatalogo = new ServicioCatalogo(lectorCatalogo);

        ILectorPromociones lectorPromociones = new LectorPromociones();
        IServicioPromociones servicioPromociones = new ServicioPromociones(lectorPromociones);

        ILectorVentas lectorVentas = new LectorVentas();
        
        IServicioReportes servicioReportes = new ServicioReportes();
        IServicioReabastecimiento servicioReabastecimiento = new ServicioReabastecimiento();
        IServicioCedisParalelo servicioCedis = new ServicioCedisParalelo();

        // Inyección del servicio de ventas completo con todas sus dependencias
        IServicioVentas servicioVentas = new ServicioVentas(
            servicioCatalogo, 
            lectorVentas, 
            servicioPromociones, 
            servicioReportes, 
            servicioReabastecimiento
        );

        // 3. Cargar el Catálogo
        Console.WriteLine("Cargando catálogo...");
        await servicioCatalogo.CargarCatalogoAsync(rutaCatalogo);
        Console.WriteLine("[ÉXITO] Catálogo cargado correctamente.\n");

        // 4. Cargar las Promociones
        Console.WriteLine("Cargando promociones...");
        await servicioPromociones.CargarPromocionesAsync(rutaPromociones);
        Console.WriteLine("[ÉXITO] Promociones cargadas.\n");

        // 5. Procesar las Ventas (Aplica reglas de negocio, horarios, pagos, rechazos y reabastecimiento)
        Console.WriteLine("Procesando ventas y generando bitácora de rechazos...");
        await servicioVentas.ProcesarVentasAsync(rutaVentas, rutaRechazos);
        Console.WriteLine($"[ÉXITO] Procesamiento de ventas finalizado. Archivo de rechazos en: {rutaRechazos}\n");

        // 6. HU-07: Consulta de existencias del CEDIS en paralelo con timeout de 3s
        Console.WriteLine("Consultando existencias del CEDIS en paralelo (HU-07)...");
        var eansAInspeccionar = new List<string> { "7501011122233", "7501044455566", "7501077788899" };
        var existenciasCedis = await servicioCedis.ConsultarExistenciasCedisAsync(eansAInspeccionar);
        Console.WriteLine();

        // 7. Generar Reportes Finales (Logs y Formato CSV - HU-05, HU-06, HU-08)
        await servicioReportes.GenerarReporteCierreAsync(rutaReporteCierre);
        await servicioReabastecimiento.GenerarReporteReabastecimientoAsync(rutaReporteCedis);

        // Exportación extra en CSV para gerencia (HU-08)
        var topVentasEjemplo = new List<(string, int)> { ("Refresco Cola 600ml", 120), ("Arroz Grano Largo 1kg", 85) };
        var pedidoEjemplo = new List<(string, string, int)> { ("7501011122233", "Cerveza Oscura 355ml", 40) };

        await servicioReportes.GenerarReporteTopVentasCsvAsync(rutaCsvTopVentas, topVentasEjemplo);
        await servicioReportes.GenerarReportePedidoSugeridoCsvAsync(rutaCsvPedido, pedidoEjemplo);

        Console.WriteLine("\n=== Fin del Proceso SVR ===");
    }
}