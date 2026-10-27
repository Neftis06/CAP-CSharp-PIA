namespace SVR.Infraestructura;

// HU-06 · TODO: InventarioEnMemoria : IInventarioRepo
//   - Se llena con el stock de catalogo.csv (Dictionary<string, int> por EAN).
//   - Descontar lanza StockInsuficienteException si no alcanza; el stock nunca queda negativo.
//   - Módulos 3, 5 y 8 (es la implementación "de producción" de este proyecto).

// HU-08 · TODO: CorteCajaExportador : IDisposable
//   - Escribe el corte de caja a CSV con un StreamWriter privado.
//   - Se usa con using para que el archivo siempre se cierre.
//   - Módulos 7 y 8.
