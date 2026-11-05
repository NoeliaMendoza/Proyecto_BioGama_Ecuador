using System.ComponentModel.DataAnnotations;

namespace BioGamaEcuador.Models;

public class NewPasswordInputModel
{
    [Required(ErrorMessage = "Este campo es obligatorio.")]
    [StringLength(100, ErrorMessage = "{0} debe tener entre {2} y {1} caracteres.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nueva contraseña")]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
