using SVR.Aplicacion;
using SVR.Dominio;

namespace SVR.Infraestructura;

public class CedisSimulado : ICedisServicio
{
    private readonly string _rutaLogCedis;

    public CedisSimulado(string rutaLogCedis = "cedis_pedidos.log")
    {
        _rutaLogCedis = rutaLogCedis;
    }

    public async Task SolicitarReabastecimientoAsync(string ean, int cantidad, CancellationToken cancellationToken = default)
    {
        string mensaje = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] PEDIDO CEDIS -> EAN: {ean} | Cantidad Solicitada: {cantidad}";
        
        Console.WriteLine($"[CEDIS] ¡Alerta de reorden cruzada! {mensaje}");

        // Opcional: Guardar una bitácora física de los pedidos enviados al CEDIS
        try
        {
            var directorio = Path.GetDirectoryName(_rutaLogCedis);
            if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio);
            }
            
            await File.AppendAllTextAsync(_rutaLogCedis, mensaje + Environment.NewLine, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR CEDIS] No se pudo escribir en el log de pedidos: {ex.Message}");
        }
    }
}