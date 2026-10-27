namespace SVR.Dominio;

public class Producto
{
    public string Ean { get; }
    public string Nombre { get; }
    public string Categoria { get; } // <--- ASEGÚRATE DE TENER ESTA PROPIEDAD
    public decimal Precio { get; private set; }
    public int Stock { get; private set; }
    public int PuntoReorden { get; }

    public Producto(string ean, string nombre, string categoria, decimal precio, int stock, int puntoReorden)
    {
        if (string.IsNullOrWhiteSpace(ean)) throw new ArgumentException("El EAN no puede estar vacío.", nameof(ean));
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre no puede estar vacío.", nameof(nombre));
        if (string.IsNullOrWhiteSpace(categoria)) throw new ArgumentException("La categoría no puede estar vacía.", nameof(categoria));
        if (precio <= 0) throw new ArgumentException("El precio debe ser mayor a cero.", nameof(precio));
        if (stock < 0) throw new ArgumentException("El stock no puede ser negativo.", nameof(stock));

        Ean = ean.Trim();
        Nombre = nombre.Trim();
        Categoria = categoria.Trim();
        Precio = precio;
        Stock = stock;
        PuntoReorden = puntoReorden;
    }

    public void ReducirStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a reducir debe ser mayor a cero.", nameof(cantidad));
        
        if (Stock < cantidad)
            throw new InvalidOperationException($"Stock insuficiente para reducir {cantidad} unidades. Stock actual: {Stock}");

        Stock -= cantidad;
    }
}