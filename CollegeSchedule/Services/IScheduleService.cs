using CollegeSchedule.DTO;

namespace CollegeSchedule.Services
{
    public interface IScheduleService
    {
        Task<List<ScheduleByDateDTO>> GetScheduleForGroup(string groupName, DateTime startDate, DateTime endDate);
    }
}
