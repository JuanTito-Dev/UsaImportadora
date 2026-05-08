namespace UsaAutoPartes.Application.Dtos.CajaDtos
{
    public class DtoCajaResumen
    {
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal IngresoEfectivo { get; set; }
        public decimal IngresoQR { get; set; }
        public decimal IngresoTarjeta { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal EfectivoEsperado { get; set; }
        public decimal MontoContado { get; set; }
        public string? Justificacion { get; set; }
    }
}
