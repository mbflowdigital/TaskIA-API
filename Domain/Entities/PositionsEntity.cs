using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PositionsEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string PositionName { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
