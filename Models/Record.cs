using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BioGamaEcuador.Models
{
    public class Record
    {
        [BindRequired] public int Id { get; set; }

        [BindRequired, Range(1, int.MaxValue)] public int SpeciesId { get; set; }
        public Species? Species { get; set; }

        [BindRequired, Range(1, int.MaxValue)] public int LocationId { get; set; }
        public Location? Location { get; set; }

        [BindRequired, Range(1, int.MaxValue)] public int ResearcherId { get; set; }
        public Researcher? Researcher { get; set; }

        [Required(ErrorMessage = "La fecha de descubrimiento es obligatoria.")]
        [BindRequired]
        public DateTime DiscoveryDate { get; set; }

        [Range(1, 99999, ErrorMessage = "La cantidad observada debe estar entre {1} y {2}.")]
        [BindRequired]
        public int ObservedQuantity { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones deben tener máximo {1} caracteres.")]
        public string Observations { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }
    }
}
