using System;
using System.ComponentModel.DataAnnotations;

namespace SaigonBite.Domain.Entities
{
    public class SystemSetting
    {
        public int Id { get; set; }

        public bool IsHeavyRainOn { get; set; }

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
