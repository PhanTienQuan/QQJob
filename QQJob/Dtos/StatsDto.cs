namespace QQJob.Dtos
{
    public class StatsDto
    {
        public string Month { get; set; }
        public int Posts { get; set; }
        public int Users { get; set; }
        public double? PostGrowth { get; set; }
        public double? UserGrowth { get; set; }
    }
}
