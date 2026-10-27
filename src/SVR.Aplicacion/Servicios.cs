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
    private readonly IServicioPromociones _servicioPromociones;
    private readonly IServicioReportes _servicioReportes;
    private readonly IServicioReabastecimiento _servicioReabastecimiento;

    public ServicioVentas(
        IServicioCatalogo servicioCatalogo,
        ILectorVentas lectorVentas,
        IServicioPromociones servicioPromociones,
        IServicioReportes servicioReportes,
        IServicioReabastecimiento servicioReabastecimiento)
    {
        _servicioCatalogo = servicioCatalogo;
        _lectorVentas = lectorVentas;
        _servicioPromociones = servicioPromociones;
        _servicioReportes = servicioReportes ?? throw new ArgumentNullException(nameof(servicioReportes));
        _servicioReabastecimiento = servicioReabastecimiento ?? throw new ArgumentNullException(nameof(servicioReabastecimiento));
    }

    public async Task ProcesarVentasAsync(string rutaVentasCsv, string rutaRechazosLog, CancellationToken cancellationToken = default)
    {
        var lineasRechazadas = new List<string>();

        var dirRechazos = Path.GetDirectoryName(rutaRechazosLog);
        if (!string.IsNullOrEmpty(dirRechazos) && !Directory.Exists(dirRechazos))
        {
            Directory.CreateDirectory(dirRechazos);
        }

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

                // -------------------------------------------------------------
                // RESTRICCIÓN HU-05: Validación de horario para alcohol / bebidas
                // -------------------------------------------------------------
                bool esAlcohol = producto.Categoria.Equals("Alcohol", StringComparison.OrdinalIgnoreCase) ||
                                 producto.Categoria.Equals("Bebidas Alcohólicas", StringComparison.OrdinalIgnoreCase);

                if (esAlcohol)
                {
                    TimeSpan horaVenta = venta.Fecha.TimeOfDay;
                    TimeSpan inicioPermitido = new TimeSpan(11, 0, 0); // 11:00 AM
                    TimeSpan finPermitido = new TimeSpan(22, 0, 0);    // 10:00 PM (22:00 hrs)

                    if (horaVenta < inicioPermitido || horaVenta > finPermitido)
                    {
                        lineasRechazadas.Add($"[RECHAZADO] TX: {venta.TransaccionId} -> Venta de alcohol fuera de horario permitido ({venta.Fecha:HH:mm}).");
                        continue; // Rechaza la venta y la manda al log de rechazos
                    }
                }

                // HU-03: Evaluar y aplicar Promociones Vigentes
                decimal subtotalFinal = producto.Precio * venta.Cantidad;
                var promocionActiva = _servicioPromociones.ObtenerPromocionActiva(venta.Ean, venta.Fecha);

                if (promocionActiva != null)
                {
                    subtotalFinal = promocionActiva.CalcularSubtotalConPromocion(venta.Cantidad, producto.Precio);
                    Console.WriteLine($"[PROMOCIÓN APLICADA] TX: {venta.TransaccionId} | {promocionActiva.Tipo} en '{producto.Nombre}' -> Total con promo: ${subtotalFinal:F2}");
                }
                else
                {
                    Console.WriteLine($"[VENTA REGULAR] TX: {venta.TransaccionId} | '{producto.Nombre}' x{venta.Cantidad} -> Subtotal: ${subtotalFinal:F2}");
                }

                // HU-04: Validación de Medio de Pago
                try
                {
                    var pago = new Pago(venta.TransaccionId, TipoPago.Efectivo, subtotalFinal, subtotalFinal);
                    Console.WriteLine($"[PAGO VALIDADO] TX: {venta.TransaccionId} | Método: {pago.Tipo} | Monto Cubierto: ${pago.MontoTotal:F2}");
                    // HU-05: Registrar en el reporte de cierre
                    _servicioReportes.RegistrarVentaExitosa(venta.TransaccionId, producto.Nombre, venta.Cantidad, producto.Precio * venta.Cantidad, subtotalFinal, pago.Tipo);
                }
                catch (Exception exPago)
                {
                    lineasRechazadas.Add($"[RECHAZADO PAGO] TX: {venta.TransaccionId} -> {exPago.Message}");
                    continue;
                }

                producto.ReducirStock(venta.Cantidad);

                // HU-06: Evaluar reabastecimiento con CEDIS después de la venta
                _servicioReabastecimiento.EvaluarPuntoReorden(producto.Ean, producto.Nombre, producto.Stock, producto.PuntoReorden);
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

        Console.WriteLine($"[ÉXITO] Procesamiento de ventas finalizado. Archivo de rechazos generado en: {rutaRechazosLog}");
    }
}

public class ServicioPromociones : IServicioPromociones
{
    private readonly List<Promocion> _promociones = new();
    private readonly ILectorPromociones _lectorPromociones;

    public ServicioPromociones(ILectorPromociones lectorPromociones)
    {
        _lectorPromociones = lectorPromociones;
    }

    public async Task CargarPromocionesAsync(string rutaCsv, CancellationToken cancellationToken = default)
    {
        _promociones.Clear();

        await foreach (var promo in _lectorPromociones.LeerPromocionesAsync(
            rutaCsv,
            (linea, motivo) => { },
            cancellationToken))
        {
            _promociones.Add(promo);
        }
    }

    public Promocion? ObtenerPromocionActiva(string ean, DateTime fecha)
    {
        return _promociones.FirstOrDefault(p => p.Ean.Equals(ean, StringComparison.OrdinalIgnoreCase) && p.EstaVigente(fecha));
    }
}