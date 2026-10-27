using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SVR.Aplicacion;

public interface IServicioReportes
{
    void RegistrarVentaExitosa(string transaccionId, string productoNombre, int cantidad, decimal subtotalOriginal, decimal totalConPromocion, SVR.Dominio.TipoPago tipoPago);
    Task GenerarReporteCierreAsync(string rutaReporte, CancellationToken cancellationToken = default);
    
    // Métodos para la HU-08 (Reportes en CSV)
    Task GenerarReporteTopVentasCsvAsync(string rutaCsv, IEnumerable<(string Producto, int Cantidad)> topVentas, CancellationToken cancellationToken = default);
    Task GenerarReportePedidoSugeridoCsvAsync(string rutaCsv, IEnumerable<(string Ean, string Producto, int Sugerido)> pedidoSugerido, CancellationToken cancellationToken = default);
}