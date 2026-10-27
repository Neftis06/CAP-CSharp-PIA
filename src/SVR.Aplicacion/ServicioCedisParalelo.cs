namespace SVR.Aplicacion;

public interface IServicioCedisParalelo
{
    Task<Dictionary<string, int>> ConsultarExistenciasCedisAsync(IEnumerable<string> listaEans);
}

public class ServicioCedisParalelo : IServicioCedisParalelo
{
    public async Task<Dictionary<string, int>> ConsultarExistenciasCedisAsync(IEnumerable<string> listaEans)
    {
        var resultados = new Dictionary<string, int>();
        
        // Timeout estricto de 3 segundos como pide la HU-07
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        try
        {
            // Creamos una tarea de consulta simulada en paralelo para cada EAN
            var tareas = listaEans.Select(async ean =>
            {
                // Simulamos latencia de red hacia el CEDIS
                await Task.Delay(200, cts.Token);
                
                // Simulación de stock disponible en el CEDIS (ej. un valor aleatorio o base)
                int stockCedis = new Random().Next(50, 500); 
                return (ean, stockCedis);
            });

            // Ejecutamos todas en paralelo controladas por el token de timeout
            var cedisRespuesta = await Task.WhenAll(tareas);

            foreach (var item in cedisRespuesta)
            {
                resultados[item.ean] = item.stockCedis;
            }

            Console.WriteLine("[ÉXITO HU-07] Consulta de existencias en el CEDIS completada en paralelo (dentro del timeout de 3s).");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[ADVERTENCIA HU-07] La consulta al CEDIS excedió el tiempo límite de 3 segundos (Timeout).");
        }

        return resultados;
    }
}