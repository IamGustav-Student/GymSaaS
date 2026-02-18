namespace GymSaaS.Application.Membresias.Queries.GetTiposMembresia
{
    public class TipoMembresiaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int DuracionDias { get; set; }
        public int? CantidadClases { get; set; }

        // Nuevos Campos para la Vista
        public bool AccesoLunes { get; set; }
        public bool AccesoMartes { get; set; }
        public bool AccesoMiercoles { get; set; }
        public bool AccesoJueves { get; set; }
        public bool AccesoViernes { get; set; }
        public bool AccesoSabado { get; set; }
        public bool AccesoDomingo { get; set; }
        public bool IsDeleted { get; internal set; }
    }
}