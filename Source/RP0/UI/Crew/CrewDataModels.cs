using System;
using System.Collections.Generic;

namespace RP0.UI.Crew
{
    /// <summary>
    /// Represents a complete crew snapshot
    /// </summary>
    public class CrewSnapshot
    {
        public double Timestamp { get; set; }
        public int TotalAstronauts { get; set; }
        public int AvailableAstronauts { get; set; }
        public int AssignedAstronauts { get; set; }
        public int InactiveAstronauts { get; set; }
        public int InTrainingAstronauts { get; set; }
        public List<CrewMemberInfo> CrewMembers { get; set; }
        public List<TrainingCourseInfo> ActiveCourses { get; set; }
        public List<TrainingCourseInfo> AvailableCourses { get; set; }

        public CrewSnapshot()
        {
            Timestamp = Planetarium.GetUniversalTime();
            CrewMembers = new List<CrewMemberInfo>();
            ActiveCourses = new List<TrainingCourseInfo>();
            AvailableCourses = new List<TrainingCourseInfo>();
        }
    }

    /// <summary>
    /// Information about a single crew member
    /// </summary>
    public class CrewMemberInfo
    {
        public ProtoCrewMember PCM { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public double RetireTime { get; set; }
        public double InactiveUntil { get; set; }
        public double TrainingFinishTime { get; set; }
        public string CurrentTraining { get; set; }
        public List<string> Proficiencies { get; set; }
        public List<string> MissionTrainings { get; set; }
        public int FlightCount { get; set; }

        public CrewMemberInfo()
        {
            Proficiencies = new List<string>();
            MissionTrainings = new List<string>();
        }

        public bool IsAvailable => Status == "Available";
        public bool IsAssigned => Status == "Assigned";
        public bool IsInactive => Status == "Inactive";
        public bool IsInTraining => !string.IsNullOrEmpty(CurrentTraining);
    }

    /// <summary>
    /// Information about a training course
    /// </summary>
    public class TrainingCourseInfo
    {
        public string CourseName { get; set; }
        public string CourseType { get; set; }
        public double Duration { get; set; }
        public double TimeRemaining { get; set; }
        public double CompletionTime { get; set; }
        public List<string> Students { get; set; }
        public int SeatsFilled { get; set; }
        public int SeatsTotal { get; set; }
        public double Cost { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public List<string> PartsCovered { get; set; }

        public TrainingCourseInfo()
        {
            Students = new List<string>();
            PartsCovered = new List<string>();
        }

        public double Progress => Duration > 0 ? Math.Min(1.0, (Duration - TimeRemaining) / Duration) : 0;
        public bool HasSeatsAvailable => SeatsFilled < SeatsTotal;
    }
}
