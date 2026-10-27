public class DetalleReabastecimiento
{
    public string Ean { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int StockActual { get; set; }
    public int PuntoReorden { get; set; }
    public int CantidadSugerida { get; set; }
}

public interface IServicioReabastecimiento
{
    void EvaluarPuntoReorden(string ean, string productoNombre, int stockActual, int puntoReorden);
    Task GenerarReporteReabastecimientoAsync(string rutaReporte, CancellationToken cancellationToken = default);
}

public class ServicioReabastecimiento : IServicioReabastecimiento
{
    private readonly List<DetalleReabastecimiento> _productosAReabastecer = new();

    public void EvaluarPuntoReorden(string ean, string productoNombre, int stockActual, int puntoReorden)
    {
        // Si el stock actual es menor o igual al punto de reorden, se añade a la lista de pedido sugerido
        if (stockActual <= puntoReorden)
        {
            // Calculamos una cantidad sugerida de reabastecimiento (ej. la diferencia para duplicar el punto de reorden o un lote base)
            int sugerido = (puntoReorden * 2) - stockActual; 
            if (sugerido <= 0) sugerido = 10; // Mínimo de respaldo

            _productosAReabastecer.Add(new DetalleReabastecimiento
            {
                Ean = ean,
                ProductoNombre = productoNombre,
                StockActual = stockActual,
                PuntoReorden = puntoReorden,
                CantidadSugerida = sugerido
            });
        }
    }

    public async Task GenerarReporteReabastecimientoAsync(string rutaReporte, CancellationToken cancellationToken = default)
    {
        var lineasReporte = new List<string>
        {
            "==================================================",
            $"    REPORTE DE REABASTECIMIENTO CEDIS - SVR      ",
            $"    Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}      ",
            "==================================================",
            "EAN\t\tPRODUCTO\tSTOCK\tREORD\tSUGERIDO",
            "--------------------------------------------------"
        };

        foreach (var p in _productosAReabastecer)
        {
            lineasReporte.Add($"{p.Ean}\t{p.ProductoNombre}\t{p.StockActual}\t{p.PuntoReorden}\t{p.CantidadSugerida}");
        }

        lineasReporte.Add("--------------------------------------------------");
        lineasReporte.Add($"TOTAL DE PRODUCTOS A REABASTECER: {_productosAReabastecer.Count}");
        lineasReporte.Add("==================================================");

        var dir = Path.GetDirectoryName(rutaReporte);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllLinesAsync(rutaReporte, lineasReporte, cancellationToken);
        Console.WriteLine($"[REPORTE HU-06] Reporte de reabastecimiento CEDIS generado en: {rutaReporte}");
    }
}