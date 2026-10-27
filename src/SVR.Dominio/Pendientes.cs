namespace SVR.Dominio;

// HU-01 · TODO: Producto
//   Ean, Descripcion, Categoria, Precio (decimal), PuntoReorden, Stock. Módulo 2.

// HU-02 · TODO: LineaVenta (record)
//   Folio, Caja, Hora (TimeOnly), Ean, Piezas, MedioPago. Módulo 7.

// HU-01 a HU-06 · TODO: excepciones de dominio
//   TiendaException (abstract) -> ProductoNoEncontradoException, StockInsuficienteException,
//   VentaRestringidaException. Con propiedades de contexto (Ean, Disponible...). Módulo 5.

// HU-03 · TODO: Promocion (abstract) con Descuento(precio, piezas)
//   -> LlevaNPagaM (NXM 2;1), PorcentajeDesc (PORCENTAJE 15), PrecioPorVolumen (VOLUMEN 6;18.00).
//   Una promoción nueva = una clase nueva; la caja no cambia. Módulo 8.

// HU-04 · TODO: Pago (abstract record) -> Efectivo, Tarjeta, Vales
//   Valida cada pago con switch expression y property patterns. Módulo 7.
