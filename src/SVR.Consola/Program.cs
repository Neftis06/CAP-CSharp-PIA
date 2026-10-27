using SVR.Aplicacion;
using SVR.Infraestructura;

namespace SVR.Consola;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== SVR - Sistema de Venta y Reabastecimiento ===");

        ILectorCatalogo lectorCatalogo = new LectorCsv();
        ILectorVentas lectorVentas = new LectorVentas();
        IServicioCatalogo servicioCatalogo = new ServicioCatalogo(lectorCatalogo);
        IServicioVentas servicioVentas = new ServicioVentas(servicioCatalogo, lectorVentas);

        // Búsqueda inteligente de la carpeta 'datos' para asegurar que encuentre los CSVs
        string directorioActual = AppContext.BaseDirectory;
        string rutaDatos = Path.Combine(directorioActual, "datos");

        // Si no existe en la salida de compilación, busca retrocediendo hacia la raíz del proyecto fuente
        if (!Directory.Exists(rutaDatos))
        {
            var dir = new DirectoryInfo(directorioActual);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src", "SVR.Consola")))
            {
                dir = dir.Parent;
            }
            if (dir != null)
            {
                rutaDatos = Path.Combine(dir.FullName, "src", "SVR.Consola", "datos");
            }
        }

        string rutaCatalogo = Path.Combine(rutaDatos, "catalogo.csv");
        string rutaVentas = Path.Combine(rutaDatos, "ventas_0457.csv");
        string rutaRechazos = Path.Combine(rutaDatos, "rechazos.log");

        Console.WriteLine($"[INFO] Ruta de datos utilizada: {rutaDatos}");

        try
        {
            Console.WriteLine("\nCargando catálogo...");
            await servicioCatalogo.CargarCatalogoAsync(rutaCatalogo);
            Console.WriteLine($"[ÉXITO] Catálogo cargado. Total de productos válidos: {servicioCatalogo.ObtenerTodos().Count}");

            Console.WriteLine("\nProcesando ventas y generando bitácora de rechazos...");
            await servicioVentas.ProcesarVentasAsync(rutaVentas, rutaRechazos);
            Console.WriteLine($"[ÉXITO] Procesamiento de ventas finalizado. Archivo de rechazos generado en: {rutaRechazos}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR CRÍTICO] {ex.Message}");
        }
    }
}