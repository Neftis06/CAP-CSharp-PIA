using System.Runtime.CompilerServices;
using SVR.Dominio;
using SVR.Aplicacion;

namespace SVR.Infraestructura;

public class LectorCsv : ILectorCatalogo
{
    public async IAsyncEnumerable<Producto> LeerCatalogoAsync(
        string rutaArchivo, 
        Action<string, string> onLineaRechazada, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(rutaArchivo))
        {
            Console.WriteLine($"[DEBUG] El archivo no existe en la ruta: {rutaArchivo}");
            yield break;
        }

        using var reader = new StreamReader(rutaArchivo);
        string? header = await reader.ReadLineAsync(cancellationToken);
        Console.WriteLine($"[DEBUG] Cabecera leída: '{header}'");
        
        int numeroLinea = 1;

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            numeroLinea++;
            string? linea = await reader.ReadLineAsync(cancellationToken);
            
            if (string.IsNullOrWhiteSpace(linea)) continue;

            // Soporta coma o punto y coma
            var separador = linea.Contains(';') ? ';' : ',';
            var partes = linea.Split(separador);

            // Tu estructura tiene 6 columnas: ean, descripcion, categoria, precio, punto_reorden, stock
            if (partes.Length < 6)
            {
                Console.WriteLine($"[RECHAZADO MÓDULO] Línea {numeroLinea} (partes: {partes.Length}): {linea}");
                onLineaRechazada(linea, $"Línea {numeroLinea}: Formato CSV inválido (esperaba 6 columnas, encontró {partes.Length}).");
                continue;
            }

            string ean = partes[0].Trim();
            string nombre = partes[1].Trim();
            // partes[2] es la categoría (se omite en la entidad Producto actual)

            if (string.IsNullOrEmpty(ean))
            {
                onLineaRechazada(linea, $"Línea {numeroLinea}: EAN vacío.");
                continue;
            }

            // El precio está en el índice 3
            if (!decimal.TryParse(partes[3].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal precio) || precio <= 0)
            {
                Console.WriteLine($"[RECHAZADO MÓDULO] Línea {numeroLinea}: No pudo parsear precio '{partes[3]}'");
                onLineaRechazada(linea, $"Línea {numeroLinea}: Precio inválido ('{partes[3]}').");
                continue;
            }

            // El punto de reorden está en el índice 4 y el stock en el índice 5
            if (!int.TryParse(partes[4].Trim(), out int puntoReorden)) puntoReorden = 0;
            if (!int.TryParse(partes[5].Trim(), out int stock)) stock = 0;

            Producto producto;
            try
            {
                producto = new Producto(ean, nombre, precio, stock, puntoReorden);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RECHAZADO MÓDULO] Línea {numeroLinea} excepción dominio: {ex.Message}");
                onLineaRechazada(linea, $"Línea {numeroLinea}: {ex.Message}");
                continue;
            }

            yield return producto;
        }
    }
}