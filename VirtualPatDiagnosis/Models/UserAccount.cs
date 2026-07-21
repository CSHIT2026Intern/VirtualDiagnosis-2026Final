using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace VirtualPatDiagnosis.Models
{
    public class UserAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Password { get; set; } = null!;

        [StringLength(100)]
        public string DisplayName { get; set; } = null!;

        [Required]
        [StringLength(20)]
        [RegularExpression("admin|teacher|student", ErrorMessage = "Role must be admin, teacher, or student")]
        public string Role { get; set; } = null!;
    }
}
