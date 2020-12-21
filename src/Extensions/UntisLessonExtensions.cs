#region ENBREA - Copyright (C) 2020 STÜBER SYSTEMS GmbH
/*    
 *    ENBREA 
 *    
 *    Copyright (C) 2020 STÜBER SYSTEMS GmbH
 *
 *    This program is free software: you can redistribute it and/or modify
 *    it under the terms of the GNU Affero General Public License, version 3,
 *    as published by the Free Software Foundation.
 *
 *    This program is distributed in the hope that it will be useful,
 *    but WITHOUT ANY WARRANTY; without even the implied warranty of
 *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *    GNU Affero General Public License for more details.
 *
 *    You should have received a copy of the GNU Affero General Public License
 *    along with this program. If not, see <http://www.gnu.org/licenses/>.
 *
 */
#endregion

using Enbrea.Ecf;
using Enbrea.Untis.Xml;
using System;
using System.Collections.Generic;

namespace Ecf.Untis
{
    /// <summary>
    /// Extensions for <see cref="UntisLesson"/>
    /// </summary>
    public static class UntisLessonExtensions
    {
        public static List<string> GetEcfTeacherIdList(this UntisLesson lesson)
        {
            var idList = new List<string>();

            if (!string.IsNullOrEmpty(lesson.TeacherId))
            {
                idList.Add(lesson.TeacherId);
            }

            return idList;
        }

        public static List<EcfTemporalExpression> GetEcfTemporalExpressions(this UntisLesson lesson, UntisLessonTime lessonTime, UntisGeneralSettings generalSettings)
        {
            var temporalExpressions = new List<EcfTemporalExpression>();

            if (lesson.Occurence != null)
            {
                foreach (var date in lesson.GetDateInstances(generalSettings.SchoolYearBeginDate, lessonTime.Day))
                {
                    temporalExpressions.Add(new EcfOneTimeExpression()
                    {
                        Operation = EcfTemporalExpressionOperation.Include,
                        StartTimepoint = new DateTimeOffset(date).AddMinutes(lessonTime.StartTime.TotalMinutes),
                        EndTimepoint = new DateTimeOffset(date).AddMinutes(lessonTime.SlotGroupEndTime == null ? lessonTime.EndTime.TotalMinutes : ((TimeSpan)lessonTime.SlotGroupEndTime).TotalMinutes)
                    });
                }
            }
            else
            {
                temporalExpressions.Add(new EcfWeeklyTimePeriodExpression()
                {
                    Operation = EcfTemporalExpressionOperation.Include,
                    StartTimepoint = new DateTimeOffset((DateTime)lesson.StartDate).AddMinutes(lessonTime.StartTime.TotalMinutes),
                    EndTimepoint = new DateTimeOffset((DateTime)lesson.EndDate).AddMinutes(lessonTime.SlotGroupEndTime == null ? lessonTime.EndTime.TotalMinutes : ((TimeSpan)lessonTime.SlotGroupEndTime).TotalMinutes),
                    DaysOfWeek = lessonTime.GetEcfDaysOfWeek(),
                    WeeksInterval = 1,
                    ValidFrom = lesson.ValidFrom != null ? new Date((DateTime)lesson.ValidFrom) : (Date?)null,
                    ValidTo = lesson.ValidTo != null ? new Date((DateTime)lesson.ValidTo) : (Date?)null
                });
            }

            return temporalExpressions;
        }

        public static string GetEcfCourseTitle(this UntisLesson lesson, List<UntisSubject> subjects)
        {
            var subject = subjects.Find(x => x.Id == lesson.SubjectId);

            if (subject != null)
            {
                return subject.ShortName;
            }
            else
            {
                return null;
            }
        }

        public static uint GetGroupId(this UntisLesson lesson)
        {
            if ((lesson.ShortName != null) && (lesson.ShortName.Length > 2))
            {
                return uint.Parse(lesson.ShortName.Remove(lesson.ShortName.Length - 2));
            }
            else
            {
                throw new Exception("Error");
            }
        }
    }
}


