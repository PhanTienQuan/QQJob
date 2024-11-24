namespace QQJob.Models
{
    public class ViewJobHistory
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public AppUser User { get; set; }
        public int JobId { get; set; }
        public Job? Job { get; set; }
        public DateTime ViewOn { get; set; }
    }
}
