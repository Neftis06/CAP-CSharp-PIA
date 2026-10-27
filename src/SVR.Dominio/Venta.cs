using System;

namespace SVR.Dominio;

public class Venta
{
    public string TransaccionId { get; }
    public string Ean { get; }
    public int Cantidad { get; }
    public DateTime Fecha { get; }
    public TipoPago TipoPago { get; }

    public Venta(string transaccionId, string ean, int cantidad, DateTime fecha, TipoPago tipoPago)
    {
        if (string.IsNullOrWhiteSpace(transaccionId)) throw new ArgumentException("El ID de transacción no puede estar vacío.", nameof(transaccionId));
        if (string.IsNullOrWhiteSpace(ean)) throw new ArgumentException("El EAN no puede estar vacío.", nameof(ean));
        if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor a cero.", nameof(cantidad));

        TransaccionId = transaccionId.Trim();
        Ean = ean.Trim();
        Cantidad = cantidad;
        Fecha = fecha;
        TipoPago = tipoPago;
    }
}