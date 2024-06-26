namespace StretchScheduler.Models
{
    public class Client
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public int Balance { get; set; } = 0;
        public required Guid AdminId { get; set; }
    }
}