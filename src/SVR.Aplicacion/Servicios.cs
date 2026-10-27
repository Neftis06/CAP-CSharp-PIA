using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SVR.Dominio;

namespace SVR.Aplicacion;

public class ServicioCatalogo : IServicioCatalogo
{
    private readonly Dictionary<string, Producto> _catalogo = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _rechazosCatalogo = new();
    private readonly ILectorCatalogo _lectorCatalogo;
    
    public ServicioCatalogo(ILectorCatalogo lectorCatalogo)
    {
        _lectorCatalogo = lectorCatalogo ?? throw new ArgumentNullException(nameof(lectorCatalogo));
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
        _servicioCatalogo = servicioCatalogo ?? throw new ArgumentNullException(nameof(servicioCatalogo));
        _lectorVentas = lectorVentas ?? throw new ArgumentNullException(nameof(lectorVentas));
        _servicioPromociones = servicioPromociones ?? throw new ArgumentNullException(nameof(servicioPromociones));
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
                        continue; 
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

                // HU-04: Validación de Medio de Pago Dinámico
                try
                {
                    TipoPago tipoPagoReal = venta.TipoPago;

                    var pago = new Pago(venta.TransaccionId, tipoPagoReal, subtotalFinal, subtotalFinal);
                    Console.WriteLine($"[PAGO VALIDADO] TX: {venta.TransaccionId} | Método: {pago.Tipo} | Monto Cubierto: ${pago.MontoTotal:F2}");
                    
                    // HU-05: Registrar en el reporte de cierre de caja con el medio de pago real
                    _servicioReportes.RegistrarVentaExitosa(
                        venta.TransaccionId, 
                        producto.Nombre, 
                        venta.Cantidad, 
                        producto.Precio * venta.Cantidad, 
                        subtotalFinal, 
                        pago.Tipo
                    );
                }
                catch (Exception exPago)
                {
                    lineasRechazadas.Add($"[RECHAZADO PAGO] TX: {venta.TransaccionId} -> {exPago.Message}");
                    continue;
                }

                // Reducir stock del producto
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
        _lectorPromociones = lectorPromociones ?? throw new ArgumentNullException(nameof(lectorPromociones));
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

public class ServicioReabastecimiento : IServicioReabastecimiento
{
    private readonly List<DetalleReabastecimiento> _productosAReabastecer = new();

    public void EvaluarPuntoReorden(string ean, string productoNombre, int stockActual, int puntoReorden)
    {
        if (stockActual <= puntoReorden)
        {
            int sugerido = (puntoReorden * 2) - stockActual;
            if (sugerido <= 0) sugerido = 10;

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

public class DetalleReabastecimiento
{
    public string Ean { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int StockActual { get; set; }
    public int PuntoReorden { get; set; }
    public int CantidadSugerida { get; set; }
}