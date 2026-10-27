using SVR.Dominio;

namespace SVR.Aplicacion;

public interface IServicioReportes
{
    void RegistrarVentaExitosa(string transaccionId, string productoNombre, int cantidad, decimal subtotalOriginal, decimal totalConPromocion, TipoPago tipoPago);
    Task GenerarReporteCierreAsync(string rutaReporte, CancellationToken cancellationToken = default);
}