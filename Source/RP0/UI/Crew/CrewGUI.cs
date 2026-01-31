using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RP0.UI.Budget;
using RP0.Crew;

namespace RP0.UI.Crew
{
    /// <summary>
    /// Modern unified Astronauts & Training UI
    /// </summary>
    public class CrewGUI : UIBase
    {
        private enum CrewView
        {
            Overview,
            StartTraining
        }

        private CrewSnapshot _currentSnapshot;
        private Vector2 _crewListScroll = Vector2.zero;
        private Vector2 _trainingListScroll = Vector2.zero;
        private Vector2 _courseSelectorScroll = Vector2.zero;
        private Vector2 _studentSelectorScroll = Vector2.zero;
        private CrewView _currentView = CrewView.Overview;
        private TrainingCourse _selectedCourse = null;
        private bool _showZeroDurationCourses = false;
        private bool _showLockedCourses = true;
        private readonly Dictionary<ProtoCrewMember, TrainingCourse> _activeMap = new Dictionary<ProtoCrewMember, TrainingCourse>();

        protected override void OnStart()
        {
            base.OnStart();
            SharedUIComponents.InitializeStyles();
            
            var evt = GameEvents.FindEvent<EventVoid>("OnKctRecalculateBuildRates");
            if (evt != null)
            {
                evt.Add(OnRecalcBuildRates);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            var evt = GameEvents.FindEvent<EventVoid>("OnKctRecalculateBuildRates");
            if (evt != null)
            {
                evt.Remove(OnRecalcBuildRates);
            }
        }

        private void OnRecalcBuildRates()
        {
            if (_selectedCourse != null)
                _selectedCourse.RecalculateBuildRate();
        }

        /// <summary>
        /// Refresh all crew calculations - called automatically on every render
        /// </summary>
        private void RefreshCrewData()
        {
            if (HighLogic.CurrentGame?.Mode != Game.Modes.CAREER)
                return;

            _currentSnapshot = CrewCalculator.Instance.GenerateSnapshot();
        }

        /// <summary>
        /// Main crew tab rendering
        /// </summary>
        public void RenderCrewTab()
        {
            if (HighLogic.CurrentGame?.CrewRoster == null)
            {
                GUILayout.Label("Crew system not available", BoldLabel);
                return;
            }

            // Always refresh data to keep it live
            RefreshCrewData();
            UpdateActiveCourseMap();

            if (_currentView == CrewView.Overview)
            {
                RenderOverviewView();
            }
            else
            {
                RenderStartTrainingView();
            }
        }

        /// <summary>
        /// Render the overview view (astronaut list + active training)
        /// </summary>
        private void RenderOverviewView()
        {
            // Top row: metrics on left, Start Training button on right
            GUILayout.BeginHorizontal();
            
            // Metrics
            RenderMetricsRow();
            
            GUILayout.FlexibleSpace();
            
            // Start Training button - match height of metric cards
            if (GUILayout.Button("Start Training", HighLogic.Skin.button, GUILayout.Width(120)))
            {
                _currentView = CrewView.StartTraining;
                _selectedCourse = null;
                TopWindow.RequestUIReset();
            }
            
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Vertical layout: Crew list on top, Training courses below
            RenderCrewList();
            
            GUILayout.Space(8);
            
            RenderTrainingSection();
        }

        /// <summary>
        /// Render the start training view (course selection + student selection)
        /// </summary>
        private void RenderStartTrainingView()
        {
            if (_selectedCourse == null)
            {
                // Course selector view - show Back button and toggle buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("← Back to Overview", HighLogic.Skin.button, GUILayout.Width(150)))
                {
                    _currentView = CrewView.Overview;
                    _selectedCourse = null;
                    TopWindow.RequestUIReset();
                }
                GUILayout.FlexibleSpace();
                
                string showAllText = _showZeroDurationCourses ? "Hide 0m" : "Show All";
                if (GUILayout.Button(showAllText, HighLogic.Skin.button, GUILayout.Width(80)))
                {
                    _showZeroDurationCourses = !_showZeroDurationCourses;
                }
                
                GUILayout.Space(5);
                
                string toggleText = _showLockedCourses ? "Hide Locked" : "Show Locked";
                if (GUILayout.Button(toggleText, HighLogic.Skin.button, GUILayout.Width(100)))
                {
                    _showLockedCourses = !_showLockedCourses;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                
                // Show course selector
                RenderCourseSelector();
            }
            else
            {
                // Student selector view - show Back, Training Name, Start Training
                GUILayout.BeginHorizontal();
                
                // Back button
                if (GUILayout.Button("← Back to Overview", HighLogic.Skin.button, GUILayout.Width(150)))
                {
                    _currentView = CrewView.Overview;
                    _selectedCourse = null;
                    TopWindow.RequestUIReset();
                }
                
                GUILayout.Space(10);
                
                // Training title
                var titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                GUILayout.Label(_selectedCourse.GetItemName(), titleStyle);
                
                GUILayout.FlexibleSpace();
                
                // Start Training button
                bool underMin = _selectedCourse.SeatMin > _selectedCourse.Students.Count;
                GUI.enabled = !underMin;
                if (GUILayout.Button("Start Training", HighLogic.Skin.button, GUILayout.Width(120), GUILayout.Height(30)))
                {
                    if (_selectedCourse.StartCourse())
                    {
                        RP0.Crew.CrewHandler.Instance.TrainingCourses.Add(_selectedCourse);
                        MaintenanceHandler.Instance.ScheduleMaintenanceUpdate();
                        _selectedCourse = null;
                        _currentView = CrewView.Overview;
                        TopWindow.RequestUIReset();
                    }
                }
                GUI.enabled = true;
                
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                
                // Show student selector
                RenderStudentSelector();
            }
        }

        /// <summary>
        /// Render top-level metrics cards in the style of Science UI
        /// </summary>
        private void RenderMetricsRow()
        {
            // Total Astronauts
            SharedUIComponents.RenderMetricCard(
                "Total Astronauts",
                $"{_currentSnapshot.TotalAstronauts}",
                SharedUIComponents.Colors.Accent,
                "All crew in roster"
            );

            GUILayout.Space(4);

            // Available Astronauts
            SharedUIComponents.RenderMetricCard(
                "Available",
                $"{_currentSnapshot.AvailableAstronauts}",
                SharedUIComponents.Colors.Positive,
                "Ready for assignment"
            );

            GUILayout.Space(4);

            // In Training
            SharedUIComponents.RenderMetricCard(
                "In Training",
                $"{_currentSnapshot.InTrainingAstronauts}",
                new Color(0.9f, 0.6f, 0.3f), // Orange
                "Cannot be assigned"
            );
        }

        /// <summary>
        /// Render crew list with filters and sorting
        /// </summary>
        private void RenderCrewList()
        {
            SharedUIComponents.BeginCard("Astronaut Corps");

            // Header row with proper styling - using manual positioning to match data rows
            Rect headerRowRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.Height(20));
            
            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = SharedUIComponents.Colors.TextSecondary }
            };
            
            float currentX = headerRowRect.x;
            var rHeaderStyle = new GUIStyle(headerStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(currentX, headerRowRect.y, 35, headerRowRect.height), "R", rHeaderStyle);
            currentX += 35;
            GUI.Label(new Rect(currentX, headerRowRect.y, 40, headerRowRect.height), "Lvl", headerStyle);
            currentX += 40;
            GUI.Label(new Rect(currentX, headerRowRect.y, 150, headerRowRect.height), "Name", headerStyle);
            currentX += 150;
            GUI.Label(new Rect(currentX, headerRowRect.y, 110, headerRowRect.height), "Training", headerStyle);
            currentX += 110;
            GUI.Label(new Rect(currentX, headerRowRect.y, 105, headerRowRect.height), "Completes", headerStyle);
            currentX += 105;
            GUI.Label(new Rect(currentX, headerRowRect.y, 105, headerRowRect.height), "NLT", headerStyle);
            currentX += 105;
            GUI.Label(new Rect(currentX, headerRowRect.y, 105, headerRowRect.height), "NET", headerStyle);

            // Crew list - reduced height from 300 to 200
            _crewListScroll = GUILayout.BeginScrollView(_crewListScroll, GUILayout.Height(200));

            // Show all crew members sorted by name
            var sortedCrew = _currentSnapshot.CrewMembers.OrderBy(c => c.Name).ToList();

            foreach (var crew in sortedCrew)
            {
                RenderCrewMemberRow(crew);
            }

            if (sortedCrew.Count == 0)
            {
                var emptyStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = SharedUIComponents.Colors.TextSecondary },
                    alignment = TextAnchor.MiddleCenter
                };
                GUILayout.Label("No astronauts", emptyStyle);
            }

            GUILayout.EndScrollView();

            SharedUIComponents.EndCard();
        }


        /// <summary>
        /// Render a single crew member row with hover effects and role-based coloring
        /// </summary>
        private void RenderCrewMemberRow(CrewMemberInfo crew)
        {
            // Calculate row rect for hover detection
            Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.Height(20));
            bool isHovered = rowRect.Contains(Event.current.mousePosition);

            // Build tooltip with all trainings
            string crewTooltip = BuildCrewTooltip(crew);

            // Determine role color (subtle)
            string role = crew.PCM?.trait ?? "Unknown";
            Color roleBaseColor;
            if (role.StartsWith("Pilot"))
                roleBaseColor = new Color(1f, 0.7f, 0.7f, 0.08f); // Light red tint
            else if (role.StartsWith("Scientist"))
                roleBaseColor = new Color(0.5f, 0.75f, 1f, 0.08f); // Blue tint
            else if (role.StartsWith("Engineer"))
                roleBaseColor = new Color(1f, 0.65f, 0.3f, 0.08f); // Orange tint
            else
                roleBaseColor = new Color(0.5f, 0.5f, 0.5f, 0.08f); // Gray tint

            // Draw role-based background (subtle)
            if (Event.current.type == EventType.Repaint)
            {
                Color bgColor = isHovered
                    ? new Color(roleBaseColor.r, roleBaseColor.g, roleBaseColor.b, roleBaseColor.a * 2.5f) // Intensify on hover
                    : roleBaseColor;
                var bgTex = SharedUIComponents.MakeTex(2, 2, bgColor);
                GUI.DrawTexture(rowRect, bgTex);
                
                // Draw very faint vertical lines between columns
                Color lineColor = new Color(1f, 1f, 1f, 0.05f);
                var lineTex = SharedUIComponents.MakeTex(1, 1, lineColor);
                float[] columnPositions = { 35, 75, 225, 335, 440, 545 };
                foreach (float xPos in columnPositions)
                {
                    GUI.DrawTexture(new Rect(rowRect.x + xPos, rowRect.y, 1, rowRect.height), lineTex);
                }
            }

            // Style with hover effect
            var cellStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = isHovered ? 13 : 12,
                normal = { textColor = isHovered ? Color.white : new Color(0.8f, 0.8f, 0.8f) }
            };

            float currentX = rowRect.x;

            // Role (trait) - single letter with color coding
            string roleShort = role.StartsWith("Engineer") ? "E" :
                              role.StartsWith("Pilot") ? "P" :
                              role.StartsWith("Scientist") ? "S" : "?";
            Color roleColor = role.StartsWith("Pilot") ? new Color(1f, 0.6f, 0.6f) : // Light red
                             role.StartsWith("Scientist") ? new Color(0.5f, 0.75f, 1f) : // Blue
                             role.StartsWith("Engineer") ? new Color(1f, 0.65f, 0.3f) : // Orange
                             Color.gray;
            
            var roleStyle = new GUIStyle(cellStyle)
            {
                normal = { textColor = isHovered ? Color.white : roleColor },
                alignment = TextAnchor.MiddleCenter
            };
            var roleContent = new GUIContent(roleShort, crewTooltip);
            GUI.Label(new Rect(currentX, rowRect.y, 35, rowRect.height), roleContent, roleStyle);
            currentX += 35;

            // Level
            int level = crew.PCM?.experienceLevel ?? 0;
            GUI.Label(new Rect(currentX, rowRect.y, 40, rowRect.height), new GUIContent(level.ToString(), crewTooltip), cellStyle);
            currentX += 40;

            // Name
            GUI.Label(new Rect(currentX, rowRect.y, 150, rowRect.height), new GUIContent(crew.Name, crewTooltip), cellStyle);
            currentX += 150;

            // Training (current training course name or "-")
            string trainingText = string.IsNullOrEmpty(crew.CurrentTraining) ? "-" : crew.CurrentTraining;
            GUI.Label(new Rect(currentX, rowRect.y, 110, rowRect.height), new GUIContent(trainingText, crewTooltip), cellStyle);
            currentX += 110;

            // Completes (training completion date - raw month/day/year format)
            string completesText = "-";
            if (crew.TrainingFinishTime > 0)
            {
                completesText = KSPUtil.PrintDate(crew.TrainingFinishTime, false);
            }
            var completesContent = new GUIContent(completesText, crewTooltip);
            GUI.Label(new Rect(currentX, rowRect.y, 105, rowRect.height), completesContent, cellStyle);
            currentX += 105;

            // Retires NLT (No Later Than - raw month/day/year format)
            string retireNLTText = "-";
            if (crew.InactiveUntil > 0)
            {
                retireNLTText = KSPUtil.PrintDate(crew.InactiveUntil, false);
            }
            var nltContent = new GUIContent(retireNLTText, crewTooltip);
            GUI.Label(new Rect(currentX, rowRect.y, 105, rowRect.height), nltContent, cellStyle);
            currentX += 105;

            // Retires NET (No Earlier Than - raw month/day/year format)
            string retireNETText = "N/A";
            if (crew.RetireTime > 0)
            {
                retireNETText = KSPUtil.PrintDate(crew.RetireTime, false);
            }
            var netContent = new GUIContent(retireNETText, crewTooltip);
            GUI.Label(new Rect(currentX, rowRect.y, 105, rowRect.height), netContent, cellStyle);
        }


        /// <summary>
        /// Build tooltip showing all trainings for a crew member
        /// </summary>
        private string BuildCrewTooltip(CrewMemberInfo crew)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"{crew.Name} ({crew.PCM?.trait ?? "Unknown"})");
            
            // Add proficiencies
            if (crew.Proficiencies.Count > 0)
            {
                sb.Append("\n\nProficiencies:");
                foreach (var prof in crew.Proficiencies)
                {
                    sb.Append($"\n  • {prof}");
                }
            }
            
            // Add mission trainings
            if (crew.MissionTrainings.Count > 0)
            {
                sb.Append("\n\nMission Trainings:");
                foreach (var mission in crew.MissionTrainings)
                {
                    sb.Append($"\n  • {mission}");
                }
            }
            
            // If no trainings
            if (crew.Proficiencies.Count == 0 && crew.MissionTrainings.Count == 0)
            {
                sb.Append("\n\nNo completed trainings");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Render training courses section
        /// </summary>
        private void RenderTrainingSection()
        {
            SharedUIComponents.BeginCard("Training Courses");

            if (_currentSnapshot.ActiveCourses.Count == 0)
            {
                var emptyStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = SharedUIComponents.Colors.TextSecondary },
                    alignment = TextAnchor.MiddleCenter
                };
                GUILayout.Label("No active training courses", emptyStyle);
            }
            else
            {
                _trainingListScroll = GUILayout.BeginScrollView(_trainingListScroll, GUILayout.Height(150));

                foreach (var course in _currentSnapshot.ActiveCourses)
                {
                    RenderTrainingCourse(course);
                }

                GUILayout.EndScrollView();
            }

            SharedUIComponents.EndCard();
        }

        /// <summary>
        /// Render a single training course
        /// </summary>
        private void RenderTrainingCourse(TrainingCourseInfo course)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // Course name and type
            GUILayout.BeginHorizontal();
            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            GUILayout.Label(course.CourseName, nameStyle);
            GUILayout.FlexibleSpace();
            
            var typeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = SharedUIComponents.Colors.TextSecondary }
            };
            GUILayout.Label($"[{course.CourseType}]", typeStyle);
            GUILayout.EndHorizontal();

            // Students
            if (course.Students.Count > 0)
            {
                var studentsStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
                GUILayout.Label($"Students: {string.Join(", ", course.Students)}", studentsStyle);
            }

            // Progress bar
            BudgetUIComponents.RenderProgressBar(
                (float)course.Progress,
                $"Completes: {KSPUtil.PrintDate(course.CompletionTime, false)}",
                new Color(0.9f, 0.6f, 0.3f)
            );

            GUILayout.EndVertical();
            GUILayout.Space(2);
        }

        /// <summary>
        /// Render course selector as compact table with single-line rows
        /// Shows ALL trainings for complete progression visibility
        /// </summary>
        private void RenderCourseSelector()
        {
            SharedUIComponents.BeginCard("");

            GUILayout.Space(4);

            // Header row with proper styling - using manual positioning to match data rows
            Rect headerRowRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.Height(20));
            
            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = SharedUIComponents.Colors.TextSecondary }
            };
            
            float currentX = headerRowRect.x;
            GUI.Label(new Rect(currentX, headerRowRect.y, 150, headerRowRect.height), "Part/Training", headerStyle);
            currentX += 150;
            GUI.Label(new Rect(currentX, headerRowRect.y, 100, headerRowRect.height), "Status", headerStyle);
            currentX += 100;
            GUI.Label(new Rect(currentX, headerRowRect.y, 80, headerRowRect.height), "Proficiency", headerStyle);
            currentX += 80;
            GUI.Label(new Rect(currentX, headerRowRect.y, 80, headerRowRect.height), "Mission", headerStyle);

            _courseSelectorScroll = GUILayout.BeginScrollView(_courseSelectorScroll, GUILayout.Height(400));

            int acLevel = KCTUtilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex);
            
            // Group courses by their base name (without "Proficiency: " or "Mission: " prefix)
            var proficiencyCourses = new Dictionary<string, TrainingTemplate>();
            var missionCourses = new Dictionary<string, TrainingTemplate>();
            
            foreach (TrainingTemplate course in RP0.Crew.CrewHandler.Instance.TrainingTemplates)
            {
                // Extract base name (remove "Proficiency: " or "Mission: " prefix)
                string baseName = course.name;
                if (course.type == TrainingTemplate.TrainingType.Proficiency && baseName.StartsWith("Proficiency: "))
                    baseName = baseName.Substring("Proficiency: ".Length);
                else if (course.type == TrainingTemplate.TrainingType.Mission && baseName.StartsWith("Mission: "))
                    baseName = baseName.Substring("Mission: ".Length);

                if (course.type == TrainingTemplate.TrainingType.Proficiency)
                    proficiencyCourses[baseName] = course;
                else if (course.type == TrainingTemplate.TrainingType.Mission)
                    missionCourses[baseName] = course;
            }

            // Create list of course pairs with status for sorting
            var coursePairs = new List<(string baseName, TrainingTemplate profCourse, TrainingTemplate missionCourse, int statusOrder)>();
            foreach (var kvp in proficiencyCourses)
            {
                string baseName = kvp.Key;
                TrainingTemplate profCourse = kvp.Value;
                TrainingTemplate missionCourse = missionCourses.ContainsKey(baseName) ? missionCourses[baseName] : null;
                
                // Determine status order: 0 = Ready, 1 = Researching, 2 = Locked
                int statusOrder = GetCourseStatusOrder(profCourse, missionCourse, acLevel);
                
                // Skip locked courses if toggle is off
                if (!_showLockedCourses && statusOrder == 2)
                    continue;
                
                // Skip 0m proficiency courses if toggle is off
                if (!_showZeroDurationCourses && profCourse.time <= 0)
                    continue;
                
                coursePairs.Add((baseName, profCourse, missionCourse, statusOrder));
            }

            // Sort by status first, then by name
            foreach (var pair in coursePairs.OrderBy(p => p.statusOrder).ThenBy(p => p.baseName))
            {
                RenderCompactCourseRow(pair.baseName, pair.profCourse, pair.missionCourse, acLevel);
            }

            GUILayout.EndScrollView();

            SharedUIComponents.EndCard();
        }

        /// <summary>
        /// Render a single compact training course row with hover effects
        /// </summary>
        private void RenderCompactCourseRow(string baseName, TrainingTemplate profCourse, TrainingTemplate missionCourse, int acLevel)
        {
            // Calculate row rect for hover detection
            Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.Height(20));
            bool isHovered = rowRect.Contains(Event.current.mousePosition);

            // Get status for background color
            string status = GetCourseStatus(profCourse, missionCourse, acLevel, out Color statusColor);
            
            // Draw very subtle status-based background
            if (Event.current.type == EventType.Repaint)
            {
                Color bgColor;
                if (status.Contains("Locked") || status.Contains("AC Lv"))
                {
                    // Locked - very subtle red tint
                    bgColor = new Color(1f, 0.3f, 0.3f, 0.03f);
                }
                else if (status == "Researching")
                {
                    // Researching - very subtle yellow tint
                    bgColor = new Color(1f, 1f, 0.3f, 0.03f);
                }
                else
                {
                    // Ready - very subtle green tint
                    bgColor = new Color(0.3f, 1f, 0.3f, 0.03f);
                }
                
                // Intensify slightly on hover
                if (isHovered)
                {
                    bgColor.a *= 2.5f;
                }
                
                var bgTex = SharedUIComponents.MakeTex(2, 2, bgColor);
                GUI.DrawTexture(rowRect, bgTex);
            }

            // Style with hover effect
            var cellStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = isHovered ? 13 : 12,
                normal = { textColor = isHovered ? Color.white : new Color(0.8f, 0.8f, 0.8f) }
            };

            // Tooltip for entire row
            string tooltip = GetCourseTooltip(profCourse, missionCourse);

            float currentX = rowRect.x;

            // Part/Training Name
            var nameContent = new GUIContent(baseName, tooltip);
            GUI.Label(new Rect(currentX, rowRect.y, 150, rowRect.height), nameContent, cellStyle);
            currentX += 150;

            // Status (based on worst case between prof and mission)
            var statusStyle = new GUIStyle(cellStyle)
            {
                normal = { textColor = isHovered ? statusColor : new Color(statusColor.r * 0.8f, statusColor.g * 0.8f, statusColor.b * 0.8f) }
            };
            var statusContent = new GUIContent(status, tooltip);
            GUI.Label(new Rect(currentX, rowRect.y, 100, rowRect.height), statusContent, statusStyle);
            currentX += 100;

            // Proficiency Button/Status
            RenderCourseButton(new Rect(currentX, rowRect.y, 80, rowRect.height), profCourse, acLevel, isHovered, tooltip);
            currentX += 80;

            // Mission Button/Status
            if (missionCourse != null)
            {
                RenderCourseButton(new Rect(currentX, rowRect.y, 80, rowRect.height), missionCourse, acLevel, isHovered, tooltip);
            }
            else
            {
                var naStyle = new GUIStyle(cellStyle)
                {
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(new Rect(currentX, rowRect.y, 80, rowRect.height), new GUIContent("-", tooltip), naStyle);
            }
        }

        /// <summary>
        /// Get overall status for a course pair
        /// </summary>
        private string GetCourseStatus(TrainingTemplate profCourse, TrainingTemplate missionCourse, int acLevel, out Color statusColor)
        {
            // Check proficiency first
            if (profCourse.isLocked)
            {
                statusColor = new Color(1f, 176f / 255f, 153f / 255f);
                return "🔒 Locked";
            }
            if (profCourse.ACLevelRequirement > acLevel)
            {
                statusColor = new Color(1f, 176f / 255f, 153f / 255f);
                return $"AC Lv{profCourse.ACLevelRequirement + 1}";
            }
            if (profCourse.isTemporary)
            {
                statusColor = Color.yellow;
                return "Researching";
            }
            
            statusColor = new Color(0.5f, 1f, 0.5f);
            return "Ready";
        }

        /// <summary>
        /// Get status order for sorting (0 = Ready, 1 = Researching, 2 = Locked)
        /// </summary>
        private int GetCourseStatusOrder(TrainingTemplate profCourse, TrainingTemplate missionCourse, int acLevel)
        {
            // Check proficiency first (worst case determines order)
            if (profCourse.isLocked || profCourse.ACLevelRequirement > acLevel)
            {
                return 2; // Locked
            }
            if (profCourse.isTemporary)
            {
                return 1; // Researching
            }
            
            return 0; // Ready
        }

        /// <summary>
        /// Get tooltip with full course details
        /// </summary>
        private string GetCourseTooltip(TrainingTemplate profCourse, TrainingTemplate missionCourse)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.Append("Times shown are the training durations once started.");
            
            // Add parts info once (same for both proficiency and mission)
            if (profCourse != null && !string.IsNullOrEmpty(profCourse.PartsTooltip))
            {
                sb.Append($"\n\n{profCourse.PartsTooltip}");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Render a button or status indicator for a single course with time and color coding
        /// </summary>
        private void RenderCourseButton(Rect rect, TrainingTemplate course, int acLevel, bool isHovered, string tooltip)
        {
            int reqLevel = course.ACLevelRequirement;
            bool isACLocked = reqLevel > acLevel;
            bool isTechLocked = course.isLocked;
            bool isAnyLocked = isACLocked || isTechLocked;

            // Get time string
            string timeStr = KSPUtil.PrintDateDeltaCompact(course.time, true, false);

            if (isAnyLocked)
            {
                // Locked - gray label with time
                var lockStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = isHovered ? 13 : 12,
                    normal = { textColor = new Color(0.4f, 0.4f, 0.4f) },
                    alignment = TextAnchor.MiddleCenter
                };
                string lockTooltip = isTechLocked ? "Tech not researched" : $"Requires AC level {reqLevel + 1}";
                GUI.Label(rect, new GUIContent(timeStr, tooltip + "\n\n" + lockTooltip), lockStyle);
            }
            else if (course.isTemporary)
            {
                // Researching - yellow label with time
                var tempStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = isHovered ? 13 : 12,
                    normal = { textColor = new Color(0.8f, 0.8f, 0.3f) },
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(rect, new GUIContent(timeStr, tooltip + "\n\nTech being researched"), tempStyle);
            }
            else
            {
                // Ready - clickable KSP button with rounded corners
                var buttonStyle = new GUIStyle(HighLogic.Skin.button)
                {
                    fontSize = isHovered ? 13 : 12
                };
                GUI.enabled = true;
                if (GUI.Button(rect, new GUIContent(timeStr, tooltip), buttonStyle))
                {
                    _selectedCourse = new TrainingCourse(course);
                    TopWindow.RequestUIReset();
                }
            }
        }

        /// <summary>
        /// Render student selector for the selected course
        /// </summary>
        private void RenderStudentSelector()
        {
            // Course info row
            GUILayout.BeginHorizontal();
            var infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = SharedUIComponents.Colors.TextSecondary }
            };
            
            int remaining = _selectedCourse.SeatMax > 0 ? _selectedCourse.SeatMax - _selectedCourse.Students.Count : 0;
            bool underMin = _selectedCourse.SeatMin > _selectedCourse.Students.Count;
            
            if (_selectedCourse.SeatMax > 0)
            {
                string seatsText = $"Seats: {_selectedCourse.Students.Count}/{_selectedCourse.SeatMax}";
                if (remaining > 0)
                    seatsText += $" ({remaining} available)";
                if (underMin)
                    seatsText += $" - Need {_selectedCourse.SeatMin - _selectedCourse.Students.Count} more";
                    
                var seatsStyle = underMin ? new GUIStyle(infoStyle) { normal = { textColor = new Color(1f, 0.5f, 0f) } } : infoStyle;
                GUILayout.Label(seatsText, seatsStyle);
            }
            
            if (_selectedCourse.Students.Count > 0)
            {
                GUILayout.Space(10);
                double timeLeft = _selectedCourse.GetTimeLeft();
                GUILayout.Label($"Duration: {KSPUtil.PrintDateDeltaCompact(timeLeft, true, false)} | Completes: {KSPUtil.PrintDate(Planetarium.GetUniversalTime() + timeLeft, false)}", infoStyle);
                
                if (RP0.Crew.CrewHandler.Instance.RetirementEnabled)
                {
                    GUILayout.Space(10);
                    GUILayout.Label($"Retirement increase (avg): {KSPUtil.PrintDateDeltaCompact(_selectedCourse.AverageRetireExtension(), true, false)}", infoStyle);
                }
            }
            
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Student selection table matching Astronaut Corps style
            SharedUIComponents.BeginCard("Select Students");

            // Header row with proper styling - using manual positioning to match data rows
            Rect headerRowRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.Height(20));
            
            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = SharedUIComponents.Colors.TextSecondary }
            };
            
            float currentX = headerRowRect.x;
            GUI.Label(new Rect(currentX, headerRowRect.y, 40, headerRowRect.height), "☑", headerStyle);
            currentX += 40;
            var rHeaderStyle = new GUIStyle(headerStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(currentX, headerRowRect.y, 35, headerRowRect.height), "R", rHeaderStyle);
            currentX += 35;
            GUI.Label(new Rect(currentX, headerRowRect.y, 40, headerRowRect.height), "Lvl", headerStyle);
            currentX += 40;
            GUI.Label(new Rect(currentX, headerRowRect.y, 150, headerRowRect.height), "Name", headerStyle);
            currentX += 150;
            GUI.Label(new Rect(currentX, headerRowRect.y, 110, headerRowRect.height), "Training", headerStyle);
            currentX += 110;
            GUI.Label(new Rect(currentX, headerRowRect.y, 105, headerRowRect.height), "Completes", headerStyle);
            currentX += 105;
            GUI.Label(new Rect(currentX, headerRowRect.y, 105, headerRowRect.height), "NLT", headerStyle);
            currentX += 105;
            GUI.Label(new Rect(currentX, headerRowRect.y, 105, headerRowRect.height), "NET", headerStyle);

            // Student list
            _studentSelectorScroll = GUILayout.BeginScrollView(_studentSelectorScroll, GUILayout.Height(250));

            // Get crew members and sort by name
            var availableCrew = _currentSnapshot.CrewMembers
                .Where(c => c.PCM.rosterStatus == ProtoCrewMember.RosterStatus.Available)
                .OrderBy(c => c.Name)
                .ToList();

            foreach (var crew in availableCrew)
            {
                RenderStudentSelectionRow(crew);
            }

            if (availableCrew.Count == 0)
            {
                var emptyStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = SharedUIComponents.Colors.TextSecondary },
                    alignment = TextAnchor.MiddleCenter
                };
                GUILayout.Label("No available astronauts", emptyStyle);
            }

            GUILayout.EndScrollView();

            SharedUIComponents.EndCard();
        }

        /// <summary>
        /// Render a single student selection row matching the Astronaut Corps style
        /// </summary>
        private void RenderStudentSelectionRow(CrewMemberInfo crew)
        {
            ProtoCrewMember pcm = crew.PCM;
            bool isInTraining = _activeMap.ContainsKey(pcm);
            bool isSelected = _selectedCourse.Students.Contains(pcm);
            bool canSelect = !isInTraining && _selectedCourse.MeetsStudentReqs(pcm);
            bool seatsFull = _selectedCourse.SeatMax > 0 && _selectedCourse.Students.Count >= _selectedCourse.SeatMax;

            // Calculate row rect for hover detection
            Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.Height(20));
            bool isHovered = rowRect.Contains(Event.current.mousePosition);

            // Determine role color (subtle)
            string role = pcm.trait ?? "Unknown";
            Color roleBaseColor;
            if (role.StartsWith("Pilot"))
                roleBaseColor = new Color(1f, 0.7f, 0.7f, 0.08f); // Light red tint
            else if (role.StartsWith("Scientist"))
                roleBaseColor = new Color(0.5f, 0.75f, 1f, 0.08f); // Blue tint
            else if (role.StartsWith("Engineer"))
                roleBaseColor = new Color(1f, 0.65f, 0.3f, 0.08f); // Orange tint
            else
                roleBaseColor = new Color(0.5f, 0.5f, 0.5f, 0.08f); // Gray tint

            // Draw role-based background (subtle)
            if (Event.current.type == EventType.Repaint)
            {
                Color bgColor = isHovered
                    ? new Color(roleBaseColor.r, roleBaseColor.g, roleBaseColor.b, roleBaseColor.a * 2.5f)
                    : roleBaseColor;
                var bgTex = SharedUIComponents.MakeTex(2, 2, bgColor);
                GUI.DrawTexture(rowRect, bgTex);
                
                // Draw very faint vertical lines between columns
                Color lineColor = new Color(1f, 1f, 1f, 0.05f);
                var lineTex = SharedUIComponents.MakeTex(1, 1, lineColor);
                float[] columnPositions = { 40, 75, 115, 265, 375, 480, 585 };
                foreach (float xPos in columnPositions)
                {
                    GUI.DrawTexture(new Rect(rowRect.x + xPos, rowRect.y, 1, rowRect.height), lineTex);
                }
            }

            // Style with hover effect
            var cellStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = isHovered ? 13 : 12,
                normal = { textColor = isHovered ? Color.white : new Color(0.8f, 0.8f, 0.8f) }
            };

            // Dim text if can't select
            if (!canSelect || (seatsFull && !isSelected))
            {
                cellStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            }

            float currentX = rowRect.x;

            // Checkbox - allow deselection even if can't select (for removing already selected)
            GUI.enabled = (canSelect && (!seatsFull || isSelected)) || isSelected;
            Rect checkboxRect = new Rect(currentX + 5, rowRect.y + 2, 20, rowRect.height - 4);
            bool newSelected = GUI.Toggle(checkboxRect, isSelected, "");
            if (newSelected != isSelected)
            {
                if (newSelected)
                    _selectedCourse.AddStudent(pcm);
                else
                    _selectedCourse.RemoveStudent(pcm);
            }
            GUI.enabled = true;
            currentX += 40;

            // Role (trait) - single letter with color coding
            string roleShort = role.StartsWith("Engineer") ? "E" :
                              role.StartsWith("Pilot") ? "P" :
                              role.StartsWith("Scientist") ? "S" : "?";
            Color roleColor = role.StartsWith("Pilot") ? new Color(1f, 0.6f, 0.6f) : // Light red
                             role.StartsWith("Scientist") ? new Color(0.5f, 0.75f, 1f) : // Blue
                             role.StartsWith("Engineer") ? new Color(1f, 0.65f, 0.3f) : // Orange
                             Color.gray;
            
            var roleStyle = new GUIStyle(cellStyle)
            {
                normal = { textColor = isHovered ? Color.white : roleColor },
                alignment = TextAnchor.MiddleCenter
            };
            var roleContent = new GUIContent(roleShort, role);
            GUI.Label(new Rect(currentX, rowRect.y, 35, rowRect.height), roleContent, roleStyle);
            currentX += 35;

            // Level
            int level = pcm.experienceLevel;
            GUI.Label(new Rect(currentX, rowRect.y, 40, rowRect.height), level.ToString(), cellStyle);
            currentX += 40;

            // Name
            GUI.Label(new Rect(currentX, rowRect.y, 150, rowRect.height), crew.Name, cellStyle);
            currentX += 150;

            // Training (current training course name or "-")
            string trainingText = string.IsNullOrEmpty(crew.CurrentTraining) ? "-" : crew.CurrentTraining;
            GUI.Label(new Rect(currentX, rowRect.y, 110, rowRect.height), trainingText, cellStyle);
            currentX += 110;

            // Completes (training completion date - raw month/day/year format)
            string completesText = "-";
            if (crew.TrainingFinishTime > 0)
            {
                completesText = KSPUtil.PrintDate(crew.TrainingFinishTime, false);
            }
            var completesContent = new GUIContent(completesText, "");
            GUI.Label(new Rect(currentX, rowRect.y, 105, rowRect.height), completesContent, cellStyle);
            currentX += 105;

            // Retires NLT (No Later Than - raw month/day/year format)
            string retireNLTText = "-";
            if (crew.InactiveUntil > 0)
            {
                retireNLTText = KSPUtil.PrintDate(crew.InactiveUntil, false);
            }
            var nltContent = new GUIContent(retireNLTText, "");
            GUI.Label(new Rect(currentX, rowRect.y, 105, rowRect.height), nltContent, cellStyle);
            currentX += 105;

            // Retires NET (No Earlier Than - raw month/day/year format)
            string retireNETText = "N/A";
            if (crew.RetireTime > 0)
            {
                retireNETText = KSPUtil.PrintDate(crew.RetireTime, false);
            }
            var netContent = new GUIContent(retireNETText, "");
            GUI.Label(new Rect(currentX, rowRect.y, 105, rowRect.height), netContent, cellStyle);
        }

        private void UpdateActiveCourseMap()
        {
            _activeMap.Clear();
            foreach (TrainingCourse course in RP0.Crew.CrewHandler.Instance.TrainingCourses)
            {
                foreach (ProtoCrewMember student in course.Students)
                {
                    _activeMap[student] = course;
                }
            }
        }
    }
}
