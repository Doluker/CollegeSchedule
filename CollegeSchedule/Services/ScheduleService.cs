using CollegeSchedule.Data;
using CollegeSchedule.DTO;
using CollegeSchedule.Models;
using Microsoft.EntityFrameworkCore;

namespace CollegeSchedule.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly AppDbContext _db;
        public ScheduleService(AppDbContext db)
        {
            _db = db;
        }
        public async Task<List<ScheduleByDateDTO>> GetScheduleForGroup(string groupName, DateTime startDate, DateTime endDate)
        {
            ValidateDates(startDate, endDate);
            var group = await GetGroupByName(groupName);
            var schedules = await LoadSchedules(group.GroupId, startDate, endDate);
            return BuildScheduleDTO(startDate,endDate,schedules);
        }
        private static void ValidateDates(DateTime start, DateTime end)
        {
            if (start > end)
                throw new ArgumentOutOfRangeException(nameof(start), "Дата начала больше даты окончания.");
        }
        private async Task<StudentGroup> GetGroupByName(string groupName)
        {
            var group = await _db.StudentGroups.FirstOrDefaultAsync(g => g.GroupName == groupName);
            if (group == null)
                throw new KeyNotFoundException($"Группа {groupName} не найдена.");
            return group;
        }
        private async Task<List<Schedule>> LoadSchedules(int groupId, DateTime start, DateTime end)
        {
            return await _db.Schedules
            .Where(s => s.GroupId == groupId &&
            s.LessonDate >= start &&
            s.LessonDate <= end)
            .Include(s => s.Weekday)
            .Include(s => s.LessonTime)
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
            .Include(s => s.Classroom)
            .ThenInclude(c => c.Building)
            .OrderBy(s => s.LessonDate)
            .ThenBy(s => s.LessonTime.LessonNumber)
            .ThenBy(s => s.GroupPart)
            .ToListAsync();
        }
    private static List<ScheduleByDateDTO> BuildScheduleDTO(DateTime startDate, DateTime endDate, List<Schedule> schedules)
        {
            var scheduleByDate = GroupSchedulesByDate(schedules);
            var result = new List<ScheduleByDateDTO>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Sunday)
                    continue;
                if (!scheduleByDate.TryGetValue(date, out var daySchedules))
                {
                    result.Add(BuildEmptyDayDTO(date));
                }
                else
                {
                    result.Add(BuildDayDTO(daySchedules));
                }
            }
            return result;
        }
        private static LessonDTO BuildLessonDTO(IGrouping<dynamic, Schedule> lessonGroup)
        {
            var lessonDto = new LessonDTO
            {
                LessonNumber = lessonGroup.Key.LessonNumber,
                Time = $"{lessonGroup.Key.TimeStart:HH\\:mm}-{lessonGroup.Key.TimeEnd:HH\\:mm}",
                GroupParts = new Dictionary<LessonGroupPart, LessonPartDTO?>()
            };
            foreach (var part in lessonGroup)
            {
                lessonDto.GroupParts[part.GroupPart] = new LessonPartDTO
                {
                    Subject = part.Subject.Name,
                    Teacher = $"{part.Teacher.LastName} {part.Teacher.FirstName} { part.Teacher.MiddleName }",
                    TeacherPosition = part.Teacher.Position,
                    Classroom = part.Classroom.RoomNumber,
                    Building = part.Classroom.Building.Name,
                    Address = part.Classroom.Building.Address
                };
            }
            if (!lessonDto.GroupParts.ContainsKey(LessonGroupPart.FULL))
                lessonDto.GroupParts.TryAdd(LessonGroupPart.FULL, null);
            return lessonDto;
        }
        private static Dictionary<DateTime, List<Schedule>> GroupSchedulesByDate(List<Schedule> schedules)
        {
            return schedules
            .GroupBy(s => s.LessonDate)
            .ToDictionary(g => g.Key, g => g.ToList());
        }
        private static ScheduleByDateDTO BuildDayDTO(List<Schedule> daySchedules)
        {
            var lessons = daySchedules
            .GroupBy(s => new {
                s.LessonTime.LessonNumber,
                s.LessonTime.TimeStart,
                s.LessonTime.TimeEnd
            })
            .Select(BuildLessonDTO)
            .ToList();
            return new ScheduleByDateDTO
            {
                LessonDate = daySchedules.First().LessonDate,
                Weekday = daySchedules.First().Weekday.Name,
                Lessons = lessons
            };
        }
        private static ScheduleByDateDTO BuildEmptyDayDTO(DateTime date)
        {
            return new ScheduleByDateDTO
            {
                LessonDate = date,
                Weekday = date.DayOfWeek.ToString(),
                Lessons = new List<LessonDTO>()
            };
        }
        public Task<List<string>> GetGroups() 
        {
            var groups = _db.StudentGroups.Select(g => g.GroupName).ToListAsync();
            return groups;
        }
    }
}
