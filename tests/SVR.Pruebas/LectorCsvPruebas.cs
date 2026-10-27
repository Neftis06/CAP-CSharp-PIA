using SVR.Infraestructura;
using SVR.Dominio;
using Xunit;

namespace SVR.Pruebas;

public class LectorCsvPruebas
{
    [Fact]
    public async Task LeerCatalogoAsync_DebeRechazarLineasConEanVacioPrecioInvalido()
    {
        // Arrange
        string rutaTemporal = Path.GetTempFileName();
        var lineas = new[]
        {
            "EAN,Nombre,Precio,Stock,PuntoReorden",
            ",ProductoSinEan,10.00,5,2",          // Línea inválida por EAN vacío
            "7501000111206,Galletas,-5.00,10,3",    // Línea inválida por precio negativo
            "7501055300075,Refresco,15.50,20,5"     // Línea válida
        };
        await File.WriteAllLinesAsync(rutaTemporal, lineas);

        var lector = new LectorCsv();
        var rechazados = new List<string>();

        // Act
        var productos = new List<Producto>();
        await foreach (var producto in lector.LeerCatalogoAsync(rutaTemporal, (linea, motivo) => rechazados.Add(motivo)))
        {
            productos.Add(producto);
        }

        // Cleanup
        if (File.Exists(rutaTemporal)) File.Delete(rutaTemporal);

        // Assert
        Assert.Single(productos); // Solo debe pasar el refreso
        Assert.Equal("7501055300075", productos[0].Ean);
        Assert.Equal(2, rechazados.Count); // Debe haber rechazado el EAN vacío y el precio negativo
    }
}