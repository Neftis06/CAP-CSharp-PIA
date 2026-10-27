using System.Runtime.CompilerServices;
using SVR.Dominio;
using SVR.Aplicacion;

namespace SVR.Infraestructura;

public class LectorVentas : ILectorVentas
{
    public async IAsyncEnumerable<Venta> LeerVentasAsync(
        string rutaArchivo, 
        Action<string, string> onVentaRechazada, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(rutaArchivo))
        {
            yield break;
        }

        using var reader = new StreamReader(rutaArchivo);
        string? header = await reader.ReadLineAsync(cancellationToken);
        int numeroLinea = 1;

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            numeroLinea++;
            string? linea = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(linea)) continue;

            var separador = linea.Contains(';') ? ';' : ',';
            var partes = linea.Split(separador);
            if (partes.Length < 4)
            {
                onVentaRechazada(linea, $"Línea {numeroLinea}: Formato de venta inválido.");
                continue;
            }

            string txId = partes[0].Trim();
            string ean = partes[1].Trim();

            if (!int.TryParse(partes[2], out int cantidad) || cantidad <= 0)
            {
                onVentaRechazada(linea, $"Línea {numeroLinea}: Cantidad inválida ('{partes[2]}').");
                continue;
            }

            if (!DateTime.TryParse(partes[3], out DateTime fecha))
            {
                fecha = DateTime.Now;
            }

            Venta venta;
            try
            {
                venta = new Venta(txId, ean, cantidad, fecha);
            }
            catch (Exception ex)
            {
                onVentaRechazada(linea, $"Línea {numeroLinea}: {ex.Message}");
                continue;
            }

            yield return venta;
        }
    }
}