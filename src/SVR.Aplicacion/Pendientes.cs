using SVR.Dominio;

namespace SVR.Aplicacion;

public class ServicioCatalogo : IServicioCatalogo
{
    private readonly Dictionary<string, Producto> _catalogo = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _rechazosCatalogo = new();
    private readonly ILectorCatalogo _lectorCatalogo;

    public ServicioCatalogo(ILectorCatalogo lectorCatalogo)
    {
        _lectorCatalogo = lectorCatalogo;
    }

    public async Task CargarCatalogoAsync(string rutaCsv, CancellationToken cancellationToken = default)
    {
        _catalogo.Clear();
        _rechazosCatalogo.Clear();

        await foreach (var producto in _lectorCatalogo.LeerCatalogoAsync(
            rutaCsv, 
            (linea, motivo) => _rechazosCatalogo.Add(motivo), 
            cancellationToken))
        {
            _catalogo[producto.Ean] = producto;
        }
    }

    public Producto ObtenerPorEan(string ean)
    {
        if (_catalogo.TryGetValue(ean, out var producto))
        {
            return producto;
        }
        throw new ProductoNoEncontradoException(ean);
    }

    public IReadOnlyDictionary<string, Producto> ObtenerTodos() => _catalogo;
}

public class ServicioVentas : IServicioVentas
{
    private readonly IServicioCatalogo _servicioCatalogo;
    private readonly ILectorVentas _lectorVentas;

    public ServicioVentas(IServicioCatalogo servicioCatalogo, ILectorVentas lectorVentas)
    {
        _servicioCatalogo = servicioCatalogo;
        _lectorVentas = lectorVentas;
    }

    public async Task ProcesarVentasAsync(string rutaVentasCsv, string rutaRechazosLog, CancellationToken cancellationToken = default)
    {
        var lineasRechazadas = new List<string>();

        await foreach (var venta in _lectorVentas.LeerVentasAsync(
            rutaVentasCsv, 
            (linea, motivo) => lineasRechazadas.Add($"[RECHAZADO] {linea} -> Motivo: {motivo}"), 
            cancellationToken))
        {
            try
            {
                var producto = _servicioCatalogo.ObtenerPorEan(venta.Ean);
                
                if (producto.Stock < venta.Cantidad)
                {
                    lineasRechazadas.Add($"[RECHAZADO] TX: {venta.TransaccionId} -> Stock insuficiente (Disponible: {producto.Stock}, Solicitado: {venta.Cantidad})");
                    continue;
                }
            }
            catch (ProductoNoEncontradoException)
            {
                lineasRechazadas.Add($"[RECHAZADO] TX: {venta.TransaccionId} -> Producto con EAN '{venta.Ean}' no existe en catálogo.");
            }
            catch (Exception ex)
            {
                lineasRechazadas.Add($"[RECHAZADO] TX: {venta.TransaccionId} -> Error: {ex.Message}");
            }
        }

        if (lineasRechazadas.Count > 0)
        {
            await File.WriteAllLinesAsync(rutaRechazosLog, lineasRechazadas, cancellationToken);
        }
    }
}