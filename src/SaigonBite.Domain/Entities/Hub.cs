using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SaigonBite.Domain.Entities
{
    public class Hub
    {
        public Hub()
        {
            HubBrands = new List<HubBrand>();
            StaffProfiles = new List<StaffProfile>();
        }

        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; }

        [Required]
        [MaxLength(250)]
        public string Address { get; set; }

        [Required]
        [MaxLength(80)]
        public string District { get; set; }

        public int MaxCapacity { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        /// <summary>UC03 (FR14.3): optimistic concurrency token for hub edits.</summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }

        public virtual ICollection<HubBrand> HubBrands { get; set; }
        public virtual ICollection<StaffProfile> StaffProfiles { get; set; }
    }
}
