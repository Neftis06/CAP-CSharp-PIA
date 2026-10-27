using SVR.Dominio;

namespace SVR.Aplicacion;

public interface ILectorCatalogo
{
    IAsyncEnumerable<Producto> LeerCatalogoAsync(string rutaArchivo, Action<string, string> onLineaRechazada, CancellationToken cancellationToken = default);
}

public interface IServicioCatalogo
{
    Task CargarCatalogoAsync(string rutaCsv, CancellationToken cancellationToken = default);
    Producto ObtenerPorEan(string ean);
    IReadOnlyDictionary<string, Producto> ObtenerTodos();
}

public interface ILectorVentas
{
    IAsyncEnumerable<Venta> LeerVentasAsync(string rutaArchivo, Action<string, string> onRechazada, CancellationToken cancellationToken = default);
}

public interface IServicioVentas
{
    Task ProcesarVentasAsync(string rutaVentas, string rutaRechazos, CancellationToken cancellationToken = default);
}

public interface ILectorPromociones
{
    IAsyncEnumerable<Promocion> LeerPromocionesAsync(string rutaArchivo, Action<string, string> onRechazada, CancellationToken cancellationToken = default);
}

public interface IServicioPromociones
{
    Task CargarPromocionesAsync(string rutaCsv, CancellationToken cancellationToken = default);
    Promocion? ObtenerPromocionActiva(string ean, DateTime fecha);
}

public interface ICedisServicio
{
    Task SolicitarReabastecimientoAsync(string ean, int cantidad, CancellationToken cancellationToken = default);
}