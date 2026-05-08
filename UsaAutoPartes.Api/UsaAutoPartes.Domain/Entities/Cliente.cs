using UsaAutoPartes.Domain.Entities.BasesEntidades;

namespace UsaAutoPartes.Domain.Entities
{
    public class Cliente : BaseEntity
    {
        public string Nombre { get; private set; } = string.Empty;
        public string Apellido { get; private set; } = string.Empty;
        public string Telefono { get; private set; } = string.Empty;

        public Cliente() { }

        public Cliente(string nombre, string apellido, string telefono)
        {
            Nombre = nombre;
            Apellido = apellido;
            Telefono = telefono;
        }

        public void Actualizar(string nombre, string apellido, string telefono)
        {
            Nombre = nombre;
            Apellido = apellido;
            Telefono = telefono;
        }
    }
}
