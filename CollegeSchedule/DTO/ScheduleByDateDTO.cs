namespace CollegeSchedule.DTO
{
    public class ScheduleByDateDTO
    {
        public DateTime LessonDate { get; set; }
        public string Weekday { get; set; } = null!;
        public List<LessonDTO> Lessons { get; set; } = new();
    }
}
