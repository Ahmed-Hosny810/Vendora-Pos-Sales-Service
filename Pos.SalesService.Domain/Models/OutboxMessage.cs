
namespace Pos.SalesService.Domain.Models
{
    public class OutboxMessage:SalesEntity
    {
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
