namespace SVR.Aplicacion;

public class SolicitudReabastecimiento
{
    public string Ean { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int StockActual { get; set; }
    public int PuntoReorden { get; set; }
    public int CantidadSugerida { get; set; }
}

public class ServicioReabastecimiento : IServicioReabastecimiento
{
    private readonly List<SolicitudReabastecimiento> _solicitudesCedis = new();

    public void EvaluarPuntoReorden(string ean, string productoNombre, int stockActual, int puntoReorden)
    {
        // Regla: Si el stock actual es menor o igual al punto de reorden, se pide lote al CEDIS
        if (stockActual <= puntoReorden)
        {
            // Calculamos una cantidad sugerida de reabastecimiento (ej. el doble del punto de reorden o un lote base)
            int cantidadSugerida = puntoReorden > 0 ? puntoReorden * 2 : 10;

            _solicitudesCedis.Add(new SolicitudReabastecimiento
            {
                Ean = ean,
                ProductoNombre = productoNombre,
                StockActual = stockActual,
                PuntoReorden = puntoReorden,
                CantidadSugerida = cantidadSugerida
            });

            Console.WriteLine($"[ALERTA CEDIS] Producto '{productoNombre}' (EAN: {ean}) en stock crítico ({stockActual} <= {puntoReorden}). Orden generada por {cantidadSugerida} unidades.");
        }
    }

    public async Task GenerarReporteReabastecimientoAsync(string rutaReporte, CancellationToken cancellationToken = default)
    {
        var lineasReporte = new List<string>
        {
            "==================================================",
            $"    REPORTE DE REABASTECIMIENTO CEDIS - SVR       ",
            $"    Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}     ",
            "==================================================",
            "EAN\t\tPRODUCTO\t\tSTOCK\tREORDEN\tSUGERIDO",
            "--------------------------------------------------"
        };

        foreach (var s in _solicitudesCedis)
        {
            lineasReporte.Add($"{s.Ean}\t{s.ProductoNombre}\t{s.StockActual}\t{s.PuntoReorden}\t{s.CantidadSugerida}");
        }

        lineasReporte.Add("--------------------------------------------------");
        lineasReporte.Add($"TOTAL DE PRODUCTOS A REABASTECER: {_solicitudesCedis.Count}");
        lineasReporte.Add("==================================================");

        var dir = Path.GetDirectoryName(rutaReporte);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllLinesAsync(rutaReporte, lineasReporte, cancellationToken);
        Console.WriteLine($"\n[REPORTE HU-06] Solicitudes de CEDIS generadas exitosamente en: {rutaReporte}");
    }
}