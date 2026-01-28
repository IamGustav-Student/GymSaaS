namespace GymSaaS.Application.Membresias.Queries.GetTiposMembresia
{
    public class TipoMembresiaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int DuracionDias { get; set; }
        public int? CantidadClases { get; set; } // Null = Ilimitado
    }
}