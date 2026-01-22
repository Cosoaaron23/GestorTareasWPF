using System;

namespace GestorTareasWPF
{
    public class TareaModel
    {
        public string Id { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string Estado { get; set; } // "Pendiente", "Completado", etc.

        // NUEVOS CAMPOS PARA EL INFORME
        public string Prioridad { get; set; } = "Media"; // Baja, Media, Alta
        public string Responsable { get; set; } = "Usuario";
    }
}