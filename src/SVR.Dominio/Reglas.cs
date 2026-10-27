namespace SVR.Dominio;

/// <summary>
/// Representa la entidad de negocio principal 'Producto' dentro del dominio.
/// </summary>
public class Producto
{
    public string Ean { get; }
    public string Nombre { get; }
    public decimal Precio { get; }
    public int Stock { get; }
    public int PuntoReorden { get; }

    public Producto(string ean, string nombre, decimal precio, int stock, int puntoReorden)
    {
        if (string.IsNullOrWhiteSpace(ean))
            throw new ArgumentException("El EAN no puede estar vacío.", nameof(ean));
        if (precio < 0)
            throw new ArgumentOutOfRangeException(nameof(precio), "El precio no puede ser negativo.");

        Ean = ean.Trim();
        Nombre = nombre?.Trim() ?? string.Empty;
        Precio = precio;
        Stock = stock;
        PuntoReorden = puntoReorden;
    }
}

/// <summary>
/// Representa una transacción de venta individual leída del archivo de transacciones.
/// </summary>
public class Venta
{
    public string TransaccionId { get; }
    public string Ean { get; }
    public int Cantidad { get; }
    public DateTime Fecha { get; }

    public Venta(string transaccionId, string ean, int cantidad, DateTime fecha)
    {
        if (string.IsNullOrWhiteSpace(transaccionId))
            throw new ArgumentException("El ID de transacción no puede estar vacío.", nameof(transaccionId));
        if (string.IsNullOrWhiteSpace(ean))
            throw new ArgumentException("El EAN no puede estar vacío.", nameof(ean));
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad a vender debe ser mayor a cero.");

        TransaccionId = transaccionId.Trim();
        Ean = ean.Trim();
        Cantidad = cantidad;
        Fecha = fecha;
    }
}

public class ProductoNoEncontradoException : Exception
{
    public string Ean { get; }
    public ProductoNoEncontradoException(string ean) : base($"El producto con EAN '{ean}' no fue encontrado en el catálogo.")
    {
        Ean = ean;
    }
}

public class VentaRestringidaException : Exception
{
    public VentaRestringidaException(string message) : base(message) { }
}

public class StockInsuficienteException : Exception
{
    public string Ean { get; }
    public int StockDisponible { get; }
    public int CantidadSolicitada { get; }

    public StockInsuficienteException(string ean, int stockDisponible, int cantidadSolicitada)
        : base($"Stock insuficiente para el producto EAN '{ean}'. Disponible: {stockDisponible}, Solicitado: {cantidadSolicitada}.")
    {
        Ean = ean;
        StockDisponible = stockDisponible;
        CantidadSolicitada = cantidadSolicitada;
    }
}