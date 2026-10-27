using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SVR.Dominio;

namespace SVR.Aplicacion;

public class DetalleReporteVenta
{
    public string TransaccionId { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal TotalCobrado { get; set; }
    public TipoPago TipoPago { get; set; }
}

public class ServicioReportes : IServicioReportes
{
    private readonly List<DetalleReporteVenta> _ventasDelDia = new();

    public void RegistrarVentaExitosa(string transaccionId, string productoNombre, int cantidad, decimal subtotalOriginal, decimal totalConPromocion, TipoPago tipoPago)
    {
        _ventasDelDia.Add(new DetalleReporteVenta
        {
            TransaccionId = transaccionId,
            ProductoNombre = productoNombre,
            Cantidad = cantidad,
            TotalCobrado = totalConPromocion,
            TipoPago = tipoPago
        });
    }

    public async Task GenerarReporteCierreAsync(string rutaReporte, CancellationToken cancellationToken = default)
    {
        var lineasReporte = new List<string>
        {
            "=========================================",
            $"    REPORTE DE CIERRE DE CAJA - SVR      ",
            $"    Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}  ",
            "=========================================",
            "TX ID\t\tPRODUCTO\tCANT\tTOTAL\tPAGO",
            "-----------------------------------------"
        };

        decimal totalVentasDia = 0;

        foreach (var v in _ventasDelDia)
        {
            lineasReporte.Add($"{v.TransaccionId}\t{v.ProductoNombre}\t{v.Cantidad}\t${v.TotalCobrado:F2}\t{v.TipoPago}");
            totalVentasDia += v.TotalCobrado;
        }

        lineasReporte.Add("-----------------------------------------");
        lineasReporte.Add($"TOTAL DE TRANSACCIONES: {_ventasDelDia.Count}");
        lineasReporte.Add($"INGRESO TOTAL ACUMULADO: ${totalVentasDia:F2}");
        lineasReporte.Add("=========================================");

        var dir = Path.GetDirectoryName(rutaReporte);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllLinesAsync(rutaReporte, lineasReporte, cancellationToken);
        Console.WriteLine($"\n[REPORTE HU-05] Cierre de caja generado exitosamente en: {rutaReporte}");
    }

    public async Task GenerarReporteTopVentasCsvAsync(string rutaCsv, IEnumerable<(string Producto, int Cantidad)> topVentas, CancellationToken cancellationToken = default)
    {
        var lineas = new List<string> { "producto,cantidad_vendida" };
        foreach (var item in topVentas)
        {
            lineas.Add($"\"{item.Producto}\",{item.Cantidad}");
        }

        var dir = Path.GetDirectoryName(rutaCsv);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllLinesAsync(rutaCsv, lineas, cancellationToken);
        Console.WriteLine($"[REPORTE HU-08] Top de ventas en CSV generado en: {rutaCsv}");
    }

    public async Task GenerarReportePedidoSugeridoCsvAsync(string rutaCsv, IEnumerable<(string Ean, string Producto, int Sugerido)> pedidoSugerido, CancellationToken cancellationToken = default)
    {
        var lineas = new List<string> { "ean,producto,cantidad_sugerida" };
        foreach (var item in pedidoSugerido)
        {
            lineas.Add($"{item.Ean},\"{item.Producto}\",{item.Sugerido}");
        }

        var dir = Path.GetDirectoryName(rutaCsv);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllLinesAsync(rutaCsv, lineas, cancellationToken);
        Console.WriteLine($"[REPORTE HU-08] Pedido sugerido CEDIS en CSV generado en: {rutaCsv}");
    }
}