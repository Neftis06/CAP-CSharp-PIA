using SVR.Aplicacion;

namespace SVR.Infraestructura;

public sealed class CedisSimulado : ICedisServicio
{
    private static readonly Dictionary<string, (int Existencia, int DemoraMs)> Datos = new()
    {
        ["7501055300075"] = (1240, 400),   
        ["7501000111206"] = (380, 700),    
        ["7501234500011"] = (960, 900),    
        ["7501030411111"] = (55, 4000),    
    };

    public async Task<int> ExistenciaAsync(string ean, CancellationToken ct)
    {
        var (existencia, demoraMs) = Datos.TryGetValue(ean, out var dato) ? dato : (0, 300);
        await Task.Delay(demoraMs, ct);
        return existencia;
    }
}