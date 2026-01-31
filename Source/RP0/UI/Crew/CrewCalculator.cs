using System;
using System.Collections.Generic;
using System.Linq;

namespace RP0.UI.Crew
{
    /// <summary>
    /// Handles all crew and training calculations and data aggregation
    /// </summary>
    public class CrewCalculator
    {
        private static CrewCalculator _instance;
        public static CrewCalculator Instance => _instance ?? (_instance = new CrewCalculator());

        private CrewCalculator()
        {
        }

        /// <summary>
        /// Generate a complete crew snapshot for the current game state
        /// </summary>
        public CrewSnapshot GenerateSnapshot()
        {
            var snapshot = new CrewSnapshot
            {
                Timestamp = Planetarium.GetUniversalTime()
            };

            if (HighLogic.CurrentGame?.CrewRoster == null)
                return snapshot;

            // Count astronauts by status
            foreach (ProtoCrewMember pcm in HighLogic.CurrentGame.CrewRoster.Crew)
            {
                if (pcm.type != ProtoCrewMember.KerbalType.Crew)
                    continue;

                snapshot.TotalAstronauts++;

                var crewInfo = GenerateCrewMemberInfo(pcm);
                snapshot.CrewMembers.Add(crewInfo);

                switch (pcm.rosterStatus)
                {
                    case ProtoCrewMember.RosterStatus.Available:
                        if (pcm.inactive)
                            snapshot.InactiveAstronauts++;
                        else if (crewInfo.IsInTraining)
                            snapshot.InTrainingAstronauts++;
                        else
                            snapshot.AvailableAstronauts++;
                        break;
                    case ProtoCrewMember.RosterStatus.Assigned:
                        snapshot.AssignedAstronauts++;
                        break;
                }
            }

            // Get training courses
            if (RP0.Crew.CrewHandler.Instance != null)
            {
                foreach (var course in RP0.Crew.CrewHandler.Instance.TrainingCourses)
                {
                    snapshot.ActiveCourses.Add(GenerateTrainingCourseInfo(course, true));
                }

                foreach (var template in RP0.Crew.CrewHandler.Instance.TrainingTemplates)
                {
                    if (!template.isTemporary)
                    {
                        snapshot.AvailableCourses.Add(GenerateTrainingCourseInfo(template));
                    }
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Generate detailed info for a crew member
        /// </summary>
        private CrewMemberInfo GenerateCrewMemberInfo(ProtoCrewMember pcm)
        {
            var info = new CrewMemberInfo
            {
                PCM = pcm,
                Name = pcm.displayName,
                Status = GetCrewStatus(pcm),
                RetireTime = RP0.Crew.CrewHandler.Instance?.GetRetireTime(pcm.name) ?? 0,
                InactiveUntil = pcm.inactiveTimeEnd,
                TrainingFinishTime = RP0.Crew.CrewHandler.Instance?.GetTrainingFinishTime(pcm) ?? -1,
                FlightCount = pcm.careerLog?.Entries?.Count(e => e.type == "Flight" || e.type == "Orbit") ?? 0
            };

            // Get current training
            if (info.TrainingFinishTime > 0)
            {
                var course = RP0.Crew.CrewHandler.Instance?.TrainingCourses.FirstOrDefault(c => c.Students.Contains(pcm));
                if (course != null)
                {
                    info.CurrentTraining = course.GetItemName();
                }
            }

            // Parse training from flight log
            if (pcm.careerLog != null)
            {
                foreach (var entry in pcm.careerLog.Entries)
                {
                    if (entry.type == RP0.Crew.CrewHandler.TrainingType_Proficiency)
                    {
                        info.Proficiencies.Add(entry.target);
                    }
                    else if (entry.type == RP0.Crew.CrewHandler.TrainingType_Mission)
                    {
                        // Check if expired
                        double expiration = GetMissionTrainingExpiration(pcm.name, entry);
                        if (expiration > Planetarium.GetUniversalTime())
                        {
                            info.MissionTrainings.Add(entry.target);
                        }
                    }
                }
            }

            return info;
        }

        /// <summary>
        /// Get mission training expiration time
        /// </summary>
        private double GetMissionTrainingExpiration(string pcmName, FlightLog.Entry entry)
        {
            if (RP0.Crew.CrewHandler.Instance == null)
                return 0;

            // Use reflection to access private _expireTimes field
            var expireTimesField = typeof(RP0.Crew.CrewHandler).GetField("_expireTimes", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (expireTimesField != null)
            {
                var expireTimes = expireTimesField.GetValue(RP0.Crew.CrewHandler.Instance) as ROUtils.DataTypes.PersistentList<RP0.Crew.TrainingExpiration>;
                if (expireTimes != null)
                {
                    foreach (var exp in expireTimes)
                    {
                        if (exp.pcmName == pcmName && exp.Compare(entry))
                        {
                            return exp.expiration;
                        }
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Get human-readable crew status
        /// </summary>
        private string GetCrewStatus(ProtoCrewMember pcm)
        {
            if (pcm.inactive)
                return "Inactive";
            
            if (RP0.Crew.CrewHandler.Instance?.GetTrainingFinishTime(pcm) > 0)
                return "Training";

            return pcm.rosterStatus switch
            {
                ProtoCrewMember.RosterStatus.Available => "Available",
                ProtoCrewMember.RosterStatus.Assigned => "Assigned",
                ProtoCrewMember.RosterStatus.Dead => "Dead",
                ProtoCrewMember.RosterStatus.Missing => "Missing",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Generate training course info from active course
        /// </summary>
        private TrainingCourseInfo GenerateTrainingCourseInfo(RP0.Crew.TrainingCourse course, bool isActive)
        {
            var info = new TrainingCourseInfo
            {
                CourseName = course.GetItemName(),
                CourseType = course.Type.ToString(),
                Duration = course.GetTimeLeft() / (1.0 - course.GetFractionComplete()),
                TimeRemaining = course.GetTimeLeft(),
                CompletionTime = Planetarium.GetUniversalTime() + course.GetTimeLeft(),
                SeatsFilled = course.Students.Count,
                SeatsTotal = course.SeatMax,
                Cost = 0, // Training courses don't have a direct cost
                IsActive = isActive
            };

            foreach (var student in course.Students)
            {
                info.Students.Add(student.displayName);
            }

            foreach (var part in course.PartsCovered)
            {
                info.PartsCovered.Add(part.title);
            }

            info.Description = GenerateCourseDescription(course);

            return info;
        }

        /// <summary>
        /// Generate training course info from template
        /// </summary>
        private TrainingCourseInfo GenerateTrainingCourseInfo(RP0.Crew.TrainingTemplate template)
        {
            var info = new TrainingCourseInfo
            {
                CourseName = template.name,
                CourseType = template.type.ToString(),
                Duration = template.time,
                TimeRemaining = template.time,
                SeatsFilled = 0,
                SeatsTotal = template.seatMax,
                IsActive = false
            };

            if (template.partsCovered != null)
            {
                foreach (var part in template.partsCovered)
                {
                    info.PartsCovered.Add(part.title);
                }
            }

            info.Description = template.PartsTooltip ?? "Training course";

            return info;
        }

        /// <summary>
        /// Generate description for a training course
        /// </summary>
        private string GenerateCourseDescription(RP0.Crew.TrainingCourse course)
        {
            var parts = string.Join(", ", course.PartsCovered.Select(p => p.title));
            return $"Training for: {parts}";
        }

        /// <summary>
        /// Format time duration with appropriate precision
        /// </summary>
        public static string FormatDuration(double seconds)
        {
            return KSPUtil.PrintDateDeltaCompact(seconds, true, false);
        }
    }
}
