namespace SVR.Dominio;

public enum TipoPromocion
{
    Porcentaje,
    DosPorUno,     // 2x1
    TresPorDos,    // 3x2
    Volumen        // Precio especial por X cantidad
}

public class Promocion
{
    public string Ean { get; }
    public TipoPromocion Tipo { get; }
    public decimal Valor { get; }         // Porcentaje de descuento o precio especial de volumen
    public int CantidadRequerida { get; } // Ej: 2 para 2x1, 3 para 3x2 o volumen
    public DateTime FechaInicio { get; }
    public DateTime FechaFin { get; }

    public Promocion(string ean, TipoPromocion tipo, decimal valor, int cantidadRequerida, DateTime fechaInicio, DateTime fechaFin)
    {
        if (string.IsNullOrWhiteSpace(ean))
            throw new ArgumentException("El EAN no puede estar vacío.", nameof(ean));

        Ean = ean.Trim();
        Tipo = tipo;
        Valor = valor;
        CantidadRequerida = cantidadRequerida;
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
    }

    public bool EstaVigente(DateTime fecha)
    {
        return fecha.Date >= FechaInicio.Date && fecha.Date <= FechaFin.Date;
    }

    public decimal CalcularSubtotalConPromocion(int cantidadComprada, decimal precioUnitario)
    {
        if (cantidadComprada <= 0) return 0;

        switch (Tipo)
        {
            case TipoPromocion.Porcentaje:
                // Valor representa el porcentaje de descuento (ej: 10 para 10%)
                decimal factorDescuento = (100 - Valor) / 100m;
                return Math.Round(cantidadComprada * precioUnitario * factorDescuento, 2);

            case TipoPromocion.DosPorUno:
                // Cada 2 piezas, pagas 1
                int grupos2x1 = cantidadComprada / 2;
                int sobrantes2x1 = cantidadComprada % 2;
                return (grupos2x1 * precioUnitario) + (sobrantes2x1 * precioUnitario);

            case TipoPromocion.TresPorDos:
                // Cada 3 piezas, pagas 2
                int grupos3x2 = cantidadComprada / 3;
                int sobrantes3x2 = cantidadComprada % 3;
                return (grupos3x2 * 2 * precioUnitario) + (sobrantes3x2 * precioUnitario);

            case TipoPromocion.Volumen:
                // Si la cantidad alcanza la requerida, aplica el precio unitario especial de 'Valor'
                if (cantidadComprada >= CantidadRequerida)
                {
                    return Math.Round(cantidadComprada * Valor, 2);
                }
                return Math.Round(cantidadComprada * precioUnitario, 2);

            default:
                return Math.Round(cantidadComprada * precioUnitario, 2);
        }
    }
}