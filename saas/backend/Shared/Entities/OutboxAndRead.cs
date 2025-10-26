using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.Entities
{
    public class OutboxMessage
    {
       [Key] public Guid OutboxId { get; set; } = Guid.NewGuid();
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        [MaxLength(100)] public string EventType { get; set; } = default!;
        public string Payload { get; set; } = default!;
        public Guid OrganizationId { get; set; }
        public bool Dispatched { get; set; } = false;
        public int DispatchAttempts { get; set; } = 0;
    }
    
    public class InventoryRead
    {
        [Key] public Guid InventoryReadId { get; set; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; }
        public Guid ProjectId { get; set; }
        [MaxLength(200)] public string ProductName { get; set; } = default!;
        public int StockLevel { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
