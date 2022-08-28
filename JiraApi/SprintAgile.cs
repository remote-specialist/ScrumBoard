namespace JiraApi
{
    public class SprintAgile
    {
        public int Id { get; set; }

        public string Self { get; set; }

        public string State { get; set; }

        public string Name { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public int OriginBoardId { get; set; }

        public string Goal { get; set; }

        public DateTime GetStart() => DateTime.Parse(StartDate);

        public DateTime GetEnd() => DateTime.Parse(EndDate);
    }
}
