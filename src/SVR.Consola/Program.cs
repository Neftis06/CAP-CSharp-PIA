using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        string rutaReporteCierre = Path.Combine(rutaDatos, "cierre_caja.log");
        string rutaReporteCedis = Path.Combine(rutaDatos, "reabastecimiento_cedis.log");

        // 2. Instanciación de Infraestructura y Servicios de Aplicación
        ILectorCatalogo lectorCatalogo = new LectorCsv();
        IServicioCatalogo servicioCatalogo = new ServicioCatalogo(lectorCatalogo);

        ILectorPromociones lectorPromociones = new LectorPromociones();
        IServicioPromociones servicioPromociones = new ServicioPromociones(lectorPromociones);

        ILectorVentas lectorVentas = new LectorVentas();
        
        // Nuevos servicios para HU-05 y HU-06
        IServicioReportes servicioReportes = new ServicioReportes();
        IServicioReabastecimiento servicioReabastecimiento = new ServicioReabastecimiento();

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

        // 5. Procesar las Ventas (Aplica HU-03, HU-04, HU-05 restricción de horario, HU-05 reporte y HU-06 reabastecimiento)
        Console.WriteLine("Procesando ventas y generando bitácora de rechazos...");
        await servicioVentas.ProcesarVentasAsync(rutaVentas, rutaRechazos);
        Console.WriteLine($"[ÉXITO] Procesamiento de ventas finalizado. Archivo de rechazos en: {rutaRechazos}\n");

        // 6. Generar Reportes Finales (HU-05 y HU-06)
        await servicioReportes.GenerarReporteCierreAsync(rutaReporteCierre);
        await servicioReabastecimiento.GenerarReporteReabastecimientoAsync(rutaReporteCedis);

        Console.WriteLine("\n=== Fin del Proceso SVR ===");
    }
}