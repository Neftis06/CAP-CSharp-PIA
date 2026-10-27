namespace SVR.Dominio;

public class ProductoNoEncontradoException : Exception
{
    public string Ean { get; }

    public ProductoNoEncontradoException(string ean) 
        : base($"El producto con EAN '{ean}' no fue encontrado en el catálogo.")
    {
        Ean = ean;
    }
}