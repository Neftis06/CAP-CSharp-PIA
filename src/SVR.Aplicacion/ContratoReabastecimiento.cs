namespace SVR.Aplicacion;

public interface IServicioReabastecimiento
{
    void EvaluarPuntoReorden(string ean, string productoNombre, int stockActual, int puntoReorden);
    Task GenerarReporteReabastecimientoAsync(string rutaReporte, CancellationToken cancellationToken = default);
}