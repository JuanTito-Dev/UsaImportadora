using UsaAutoPartes.Domain.Entities.BasesEntidades;

namespace UsaAutoPartes.Domain.Entities
{
    public class PiezaKit : BaseEntity
    {
        public int Id_Producto { get; set; }

        public string CodigoUniversal { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public int CantidadPorKit { get; set; } = 1;

        public int StockActual { get; set; }

        public int StockReservado { get; set; } = 0;

        public Producto? Producto { get; set; }

        public PiezaKit() { }

        public PiezaKit(string codigoUniversal, string nombre, int cantidadPorKit)
        {
            CodigoUniversal = codigoUniversal;
            Nombre = nombre;
            CantidadPorKit = cantidadPorKit;
        }

        public void ActualizarCodigo()
        {
            CodigoUniversal = $"P-{CodigoUniversal}-{Id}";
        }

        public void ActualizarDatos(string? codigoBase, string? nombre, int? cantidadPorKit)
        {
            if (!string.IsNullOrWhiteSpace(codigoBase))
                CodigoUniversal = $"P-{codigoBase}-{Id}";
            if (!string.IsNullOrWhiteSpace(nombre))
                Nombre = nombre;
            if (cantidadPorKit.HasValue)
                CantidadPorKit = cantidadPorKit.Value;
        }

        public void EstablecerStockInicial(int stockKit)
        {
            StockActual = stockKit * CantidadPorKit;
        }

        public void Reservar(int cantidad) => StockReservado += cantidad;

        public void LiberarReserva(int cantidad) => StockReservado = Math.Max(0, StockReservado - cantidad);

        public void AgregarStock(int cantidad)
        {
            StockActual += cantidad;
        }

        public void DescontarStock(int cantidad)
        {
            StockActual -= cantidad;
        }
    }
}
