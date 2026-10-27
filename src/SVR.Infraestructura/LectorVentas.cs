using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
            
            // Validamos que existan al menos 5 columnas (TxId, Ean, Cantidad, Fecha, TipoPago)
            if (partes.Length < 5)
            {
                onVentaRechazada(linea, $"Línea {numeroLinea}: Formato de venta inválido o faltan columnas.");
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

            // Parseo dinámico del Medio de Pago (Efectivo, Tarjeta, Vales)
            string textoPago = partes[4].Trim();
            if (!Enum.TryParse<TipoPago>(textoPago, true, out TipoPago tipoPago))
            {
                onVentaRechazada(linea, $"Línea {numeroLinea}: Tipo de pago inválido ('{textoPago}').");
                continue;
            }

            Venta venta;
            try
            {
                // Instanciamos la venta pasándole los 5 parámetros requeridos
                venta = new Venta(txId, ean, cantidad, fecha, tipoPago);
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