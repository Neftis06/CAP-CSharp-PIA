namespace SVR.Dominio;

public enum TipoPago
{
    Efectivo,
    Tarjeta,
    Vales
}

public class Pago
{
    public string TransaccionId { get; }
    public TipoPago Tipo { get; }
    public decimal MontoPagado { get; }
    public decimal MontoTotal { get; }

    public Pago(string transaccionId, TipoPago tipo, decimal montoPagado, decimal montoTotal)
    {
        if (string.IsNullOrWhiteSpace(transaccionId)) throw new ArgumentException("El ID de transacción no puede estar vacío.", nameof(transaccionId));
        if (montoPagado <= 0) throw new ArgumentException("El monto pagado debe ser mayor a cero.", nameof(montoPagado));
        if (montoTotal <= 0) throw new ArgumentException("El monto total debe ser mayor a cero.", nameof(montoTotal));

        if (tipo == TipoPago.Efectivo && montoPagado < montoTotal)
        {
            throw new InvalidOperationException("El monto pagado en efectivo es menor al total de la venta.");
        }

        TransaccionId = transaccionId.Trim();
        Tipo = tipo;
        MontoPagado = montoPagado;
        MontoTotal = montoTotal;
    }
}