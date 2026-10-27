using System.Runtime.CompilerServices;
using SVR.Dominio;
using SVR.Aplicacion;

namespace SVR.Infraestructura;

public class LectorPromociones : ILectorPromociones
{
    public async IAsyncEnumerable<Promocion> LeerPromocionesAsync(
        string rutaArchivo, 
        Action<string, string> onRechazada, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(rutaArchivo)) yield break;

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

            if (partes.Length < 6)
            {
                onRechazada(linea, $"Línea {numeroLinea}: Formato de promoción inválido.");
                continue;
            }

            string ean = partes[0].Trim();
            if (!Enum.TryParse<TipoPromocion>(partes[1].Trim(), true, out var tipo))
            {
                tipo = TipoPromocion.Porcentaje;
            }

            if (!decimal.TryParse(partes[2].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valor))
                valor = 0;

            if (!int.TryParse(partes[3].Trim(), out int cantReq)) cantReq = 1;

            if (!DateTime.TryParse(partes[4].Trim(), out DateTime fInicio)) fInicio = DateTime.MinValue;
            if (!DateTime.TryParse(partes[5].Trim(), out DateTime fFin)) fFin = DateTime.MaxValue;

            Promocion promo;
            try
            {
                promo = new Promocion(ean, tipo, valor, cantReq, fInicio, fFin);
            }
            catch (Exception ex)
            {
                onRechazada(linea, $"Línea {numeroLinea}: {ex.Message}");
                continue;
            }

            yield return promo;
        }
    }
}