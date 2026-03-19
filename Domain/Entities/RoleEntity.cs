using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Entidade de Role (Perfil de Usuário)
    /// Representa os papéis/perfis disponíveis no sistema
    /// </summary>
    [Table("Roles")]
    public class RoleEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] 
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        // Relacionamento com Users (uma role pode ter vários usuários)
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
