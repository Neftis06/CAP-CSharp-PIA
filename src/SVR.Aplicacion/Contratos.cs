using SVR.Dominio;

namespace SVR.Aplicacion;

public interface ILectorCatalogo
{
    IAsyncEnumerable<Producto> LeerCatalogoAsync(string rutaArchivo, Action<string, string> onLineaRechazada, CancellationToken cancellationToken = default);
}

public interface ILectorVentas
{
    IAsyncEnumerable<Venta> LeerVentasAsync(string rutaArchivo, Action<string, string> onVentaRechazada, CancellationToken cancellationToken = default);
}

public interface IServicioCatalogo
{
    Task CargarCatalogoAsync(string rutaCsv, CancellationToken cancellationToken = default);
    Producto ObtenerPorEan(string ean);
    IReadOnlyDictionary<string, Producto> ObtenerTodos();
}

public interface IServicioVentas
{
    Task ProcesarVentasAsync(string rutaVentasCsv, string rutaRechazosLog, CancellationToken cancellationToken = default);
}

public interface ICedisServicio
{
    Task<int> ExistenciaAsync(string ean, CancellationToken ct);
}