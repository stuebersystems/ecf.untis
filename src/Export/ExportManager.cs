#region ENBREA - Copyright (C) 2021 STÜBER SYSTEMS GmbH
/*    
 *    ENBREA
 *    
 *    Copyright (C) 2021 STÜBER SYSTEMS GmbH
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

using Enbrea.Csv;
using Enbrea.Ecf;
using Enbrea.Untis.Gpu;
using Enbrea.Untis.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ecf.Untis
{
    public class ExportManager : CustomManager
    {
        private int _recordCounter = 0;
        private int _tableCounter = 0;
        private HashSet<uint> _untisAbsencesCache;
        private UntisDocument _untisDocument;
        public ExportManager(
            Configuration config,
            CancellationToken cancellationToken = default,
            EventWaitHandle cancellationEvent = default)
            : base(config, cancellationToken, cancellationEvent)
        {
        }

        public async override Task Execute()
        {
            try
            {
                // Load Untis XML export file
                _untisDocument = UntisDocument.Load(Path.Combine(_config.EcfExport.SourceFolderName, "untis.xml"));

                _untisAbsencesCache = new HashSet<uint>();

                // Init counters
                _tableCounter = 0;
                _recordCounter = 0;

                // Report status
                Console.WriteLine();
                Console.WriteLine("[Extracting] Start...");

                // Preperation
                PrepareExportFolder();

                // Education
                await Execute(EcfTables.Departments, _untisDocument, async (r, w, h) => await ExportDepartments(r, w, h));
                await Execute(EcfTables.Rooms, _untisDocument, async (r, w, h) => await ExportRooms(r, w, h));
                await Execute(EcfTables.Subjects, _untisDocument, async (r, w, h) => await ExportSubjects(r, w, h));
                await Execute(EcfTables.SchoolClasses, _untisDocument, async (r, w, h) => await ExportSchoolClasses(r, w, h));
                await Execute(EcfTables.Teachers, _untisDocument, async (r, w, h) => await ExportTeachers(r, w, h));
                await Execute(EcfTables.Students, _untisDocument, async (r, w, h) => await ExportStudents(r, w, h));
                await Execute(EcfTables.StudentSchoolClassAttendances, _untisDocument, async (r, w, h) => await ExportStudentSchoolClassAttendances(r, w, h));
                await Execute(EcfTables.TeacherAbsenceReasons, "GPU012.txt", async (r, w, h) => await ExportAbsenceReasons(r, w, h));
                await Execute(EcfTables.TeacherAbsences, "GPU013.txt", async (r, w, h) => await ExportTeacherAbsences(r, w, h));
                await Execute(EcfTables.SchoolClassAbsenceReasons, "GPU012.txt", async (r, w, h) => await ExportAbsenceReasons(r, w, h));
                await Execute(EcfTables.SchoolClassAbsences, "GPU013.txt", async (r, w, h) => await ExportSchoolClassAbsences(r, w, h));
                await Execute(EcfTables.RoomAbsenceReasons, "GPU012.txt", async (r, w, h) => await ExportAbsenceReasons(r, w, h));
                await Execute(EcfTables.RoomAbsences, "GPU013.txt", async (r, w, h) => await ExportRoomAbsences(r, w, h));
                await Execute(EcfTables.Timeframes, _untisDocument, async (r, w, h) => await ExportTimeframes(r, w, h));
                await Execute(EcfTables.Vaccations, _untisDocument, async (r, w, h) => await ExportVaccations(r, w, h));
                await Execute(EcfTables.Courses, _untisDocument, async (r, w, h) => await ExportCourses(r, w, h));
                await Execute(EcfTables.ScheduledLessons, _untisDocument, async (r, w, h) => await ExportScheduledLessons(r, w, h));
                await Execute(EcfTables.LessonGaps, "GPU014.txt", async (r, w, h) => await ExportLessonGaps(r, w, h));
                await Execute(EcfTables.SubstituteLessons, "GPU014.txt", async (r, w, h) => await ExportSubstituteLessons(r, w, h));

                // Report status
                Console.WriteLine($"[Extracting] {_tableCounter} table(s) and {_recordCounter} record(s) extracted");
            }
            catch
            {
                // Report error 
                Console.WriteLine();
                Console.WriteLine($"[Error] Extracting failed. Only {_tableCounter} table(s) and {_recordCounter} record(s) extracted");
                throw;
            }
        }

        private async Task Execute(string ecfTableName, UntisDocument untisDocument, Func<UntisDocument, EcfTableWriter, string[], Task<int>> action)
        {
            if (ShouldExportTable(ecfTableName, out var ecfFile))
            {
                // Report status
                Console.WriteLine($"[Extracting] [{ecfTableName}] Start...");

                // Generate ECF file name
                var ecfFileName = Path.ChangeExtension(Path.Combine(_config.EcfExport.TargetFolderName, ecfTableName), "csv");

                // Create ECF file stream and ECF Writer for export
                using var ecfWriterStream = new FileStream(ecfFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                using var ecfWriter = new CsvWriter(ecfWriterStream, Encoding.UTF8);

                // Call table specific action
                var ecfRecordCounter = await action(untisDocument, new EcfTableWriter(ecfWriter), ecfFile?.Headers);

                // Inc counters
                _recordCounter += ecfRecordCounter;
                _tableCounter++;

                // Report status
                Console.WriteLine($"[Extracting] [{ecfTableName}] {ecfRecordCounter} record(s) extracted");
            }
        }

        private async Task Execute(string ecfTableName, string gpuFileName, Func<CsvReader, EcfTableWriter, string[], Task<int>> action)
        {
            if (ShouldExportTable(ecfTableName, out var ecfFile))
            {
                // Report status
                Console.WriteLine($"[Extracting] [{ecfTableName}] Start...");

                // Open CSV file stream and CSV Reader for import
                using var csvReaderStream = new FileStream(Path.Combine(_config.EcfExport.SourceFolderName, gpuFileName), FileMode.Open, FileAccess.Read, FileShare.None);
                using var csvReader = new CsvReader(csvReaderStream, _config.EcfExport.Utf8 ? Encoding.UTF8 : Encoding.GetEncoding(28591));
                csvReader.Configuration.Separator = ',';

                // Generate ECF file name
                var ecfFileName = Path.ChangeExtension(Path.Combine(_config.EcfExport.TargetFolderName, ecfTableName), "csv");

                // Create ECF file stream and ECF Writer for export
                using var ecfWriterStream = new FileStream(ecfFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                using var ecfWriter = new CsvWriter(ecfWriterStream, Encoding.UTF8);

                // Call table specific action
                var ecfRecordCounter = await action(csvReader, new EcfTableWriter(ecfWriter), ecfFile?.Headers);

                // Inc counters
                _recordCounter += ecfRecordCounter;
                _tableCounter++;

                // Report status
                Console.WriteLine($"[Extracting] [{ecfTableName}] {ecfRecordCounter} record(s) extracted");
            }
        }

        private async Task<int> ExportAbsenceReasons(CsvReader csvReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code,
                    EcfHeaders.Name,
                    EcfHeaders.StatisticalCode);
            }

            var gpuReader = new GpuReader<GpuAbsenceReason>(csvReader);

            await foreach (var reason in gpuReader.ReadAsync())
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, reason.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.Code, reason.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.Name, reason.LongName);
                ecfTableWriter.TrySetValue(EcfHeaders.StatisticalCode, reason.StatisticalCode);

                await ecfTableWriter.WriteAsync();

                ecfRecordCounter++;
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportCourses(UntisDocument untisDocument, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Title,
                    EcfHeaders.BlockNo,
                    EcfHeaders.Description,
                    EcfHeaders.SubjectId,
                    EcfHeaders.SchoolClassIdList,
                    EcfHeaders.TeacherIdList,
                    EcfHeaders.ValidFrom,
                    EcfHeaders.ValidTo);
            }

            foreach (var lesson in untisDocument.Lessons)
            {
                if (!string.IsNullOrEmpty(lesson.SubjectId))
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, lesson.Id);
                    ecfTableWriter.TrySetValue(EcfHeaders.Title, lesson.GetEcfCourseTitle(_untisDocument.Subjects));
                    ecfTableWriter.TrySetValue(EcfHeaders.BlockNo, lesson.Block);
                    ecfTableWriter.TrySetValue(EcfHeaders.Description, lesson.Text);
                    ecfTableWriter.TrySetValue(EcfHeaders.SubjectId, lesson.SubjectId);
                    ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassIdList, lesson.ClassIds);
                    ecfTableWriter.TrySetValue(EcfHeaders.TeacherIdList, lesson.GetEcfTeacherIdList());
                    ecfTableWriter.TrySetValue(EcfHeaders.ValidFrom, lesson.ValidFrom);
                    ecfTableWriter.TrySetValue(EcfHeaders.ValidTo, lesson.ValidTo);

                    await ecfTableWriter.WriteAsync();

                    ecfRecordCounter++;
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportDepartments(UntisDocument untisDocument, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code,
                    EcfHeaders.Name);
            }

            foreach (var department in untisDocument.Departments)
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, department.Id);
                ecfTableWriter.TrySetValue(EcfHeaders.Code, department.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.Name, department.LongName);

                await ecfTableWriter.WriteAsync();

                ecfRecordCounter++;
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportLessonGaps(CsvReader csvReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.LessonId,
                    EcfHeaders.Reasons,
                    EcfHeaders.Resolutions,
                    EcfHeaders.Description,
                    EcfHeaders.TemporalExpressions);
            }

            var gpuReader = new GpuReader<GpuSubstitution>(csvReader);

            await foreach (var substitution in gpuReader.ReadAsync())
            {
                if (substitution.Date >= _untisDocument.GeneralSettings.TermBeginDate &&
                    substitution.Date <= _untisDocument.GeneralSettings.TermEndDate &&
                    substitution.GetEcfLessonId(_untisDocument.Lessons) != null)
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, substitution.GetEcfLessonGapId());
                    ecfTableWriter.TrySetValue(EcfHeaders.LessonId, substitution.GetEcfLessonId(_untisDocument.Lessons));
                    ecfTableWriter.TrySetValue(EcfHeaders.Reasons, substitution.GetEcfReasons(_untisAbsencesCache));
                    ecfTableWriter.TrySetValue(EcfHeaders.Resolutions, substitution.GetEcfResolutions());
                    ecfTableWriter.TrySetValue(EcfHeaders.Description, substitution.Remark);
                    ecfTableWriter.TrySetValue(EcfHeaders.TemporalExpressions, substitution.GetEcfTemporalExpressions(_untisDocument.TimeGrids, _untisDocument.Lessons));

                    await ecfTableWriter.WriteAsync();

                    ecfRecordCounter++;
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportRoomAbsences(CsvReader csvReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.RoomId,
                    EcfHeaders.StartTimepoint,
                    EcfHeaders.EndTimepoint,
                    EcfHeaders.ReasonId,
                    EcfHeaders.Description);
            }

            var gpuReader = new GpuReader<GpuAbsence>(csvReader);

            await foreach (var absence in gpuReader.ReadAsync())
            {
                if (absence.Type == GpuAbsenceType.Room)
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, absence.Id);
                    ecfTableWriter.TrySetValue(EcfHeaders.RoomId, absence.GetUntisRoomId());
                    ecfTableWriter.TrySetValue(EcfHeaders.StartTimepoint, absence.StartDate);
                    ecfTableWriter.TrySetValue(EcfHeaders.EndTimepoint, absence.EndDate);
                    ecfTableWriter.TrySetValue(EcfHeaders.ReasonId, absence.Reason);
                    ecfTableWriter.TrySetValue(EcfHeaders.Description, absence.Text);

                    await ecfTableWriter.WriteAsync();

                    _untisAbsencesCache.Add(absence.Id);
                    ecfRecordCounter++;
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportRooms(UntisDocument untisDocument, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code,
                    EcfHeaders.Name,
                    EcfHeaders.Description,
                    EcfHeaders.DepartmentId,
                    EcfHeaders.Capacity,
                    EcfHeaders.Color);
            }

            foreach (var room in untisDocument.Rooms)
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, room.Id);
                ecfTableWriter.TrySetValue(EcfHeaders.Code, room.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.Name, room.LongName);
                ecfTableWriter.TrySetValue(EcfHeaders.Description, room.GetEcfDescription(untisDocument.Descriptions));
                ecfTableWriter.TrySetValue(EcfHeaders.DepartmentId, room.DepartmentId);
                ecfTableWriter.TrySetValue(EcfHeaders.Capacity, room.Capacity);
                ecfTableWriter.TrySetValue(EcfHeaders.Color, room.BackgroundColor);

                await ecfTableWriter.WriteAsync();

                ecfRecordCounter++;
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportScheduledLessons(UntisDocument untisDocument, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.CourseId,
                    EcfHeaders.SubjectId,
                    EcfHeaders.SchoolClassIdList,
                    EcfHeaders.TeacherIdList,
                    EcfHeaders.RoomIdList,
                    EcfHeaders.TemporalExpressions);
            }

            foreach (var lesson in untisDocument.Lessons)
            {
                if (!string.IsNullOrEmpty(lesson.SubjectId))
                {
                    foreach (var lessonTime in lesson.Times.FindAll(x => x.SlotGroupFirstSlot == null))
                    {
                        ecfTableWriter.TrySetValue(EcfHeaders.Id, lessonTime.GetEcfId(lesson));
                        ecfTableWriter.TrySetValue(EcfHeaders.CourseId, lesson.Id);
                        ecfTableWriter.TrySetValue(EcfHeaders.SubjectId, lesson.SubjectId);
                        ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassIdList, lesson.ClassIds);
                        ecfTableWriter.TrySetValue(EcfHeaders.TeacherIdList, lesson.GetEcfTeacherIdList());
                        ecfTableWriter.TrySetValue(EcfHeaders.RoomIdList, lessonTime.GetEcfRoomIdList());
                        ecfTableWriter.TrySetValue(EcfHeaders.TemporalExpressions, lesson.GetEcfTemporalExpressions(lessonTime, untisDocument.GeneralSettings));

                        await ecfTableWriter.WriteAsync();

                        ecfRecordCounter++;
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSchoolClassAbsences(CsvReader csvReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.RoomId,
                    EcfHeaders.StartTimepoint,
                    EcfHeaders.EndTimepoint,
                    EcfHeaders.SchoolClassId,
                    EcfHeaders.Description);
            }

            var gpuReader = new GpuReader<GpuAbsence>(csvReader);

            await foreach (var absence in gpuReader.ReadAsync())
            {
                if (absence.Type == GpuAbsenceType.Class)
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, absence.Id);
                    ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassId, absence.GetUntisSchoolClassId());
                    ecfTableWriter.TrySetValue(EcfHeaders.StartTimepoint, absence.StartDate);
                    ecfTableWriter.TrySetValue(EcfHeaders.EndTimepoint, absence.EndDate);
                    ecfTableWriter.TrySetValue(EcfHeaders.ReasonId, absence.Reason);
                    ecfTableWriter.TrySetValue(EcfHeaders.Description, absence.Text);

                    await ecfTableWriter.WriteAsync();

                    _untisAbsencesCache.Add(absence.Id);
                    ecfRecordCounter++;
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSchoolClasses(UntisDocument untisDocument, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code,
                    EcfHeaders.Name1,
                    EcfHeaders.Description,
                    EcfHeaders.DepartmentId,
                    EcfHeaders.Color,
                    EcfHeaders.ValidFrom,
                    EcfHeaders.ValidTo);
            }

            foreach (var schoolClass in untisDocument.Classes)
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, schoolClass.Id);
                ecfTableWriter.TrySetValue(EcfHeaders.Code, schoolClass.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.Name1, schoolClass.LongName);
                ecfTableWriter.TrySetValue(EcfHeaders.Description, schoolClass.GetEcfDescription(untisDocument.Descriptions));
                ecfTableWriter.TrySetValue(EcfHeaders.DepartmentId, schoolClass.DepartmentId);
                ecfTableWriter.TrySetValue(EcfHeaders.Color, schoolClass.BackgroundColor);
                ecfTableWriter.TrySetValue(EcfHeaders.ValidFrom, schoolClass.ValidFrom);
                ecfTableWriter.TrySetValue(EcfHeaders.ValidTo, schoolClass.ValidTo);

                await ecfTableWriter.WriteAsync();

                ecfRecordCounter++;
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudents(UntisDocument untisDocument, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.LastName,
                    EcfHeaders.FirstName,
                    EcfHeaders.Gender,
                    EcfHeaders.Birthdate);
            }

            foreach (var student in untisDocument.Students)
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, student.Id);
                ecfTableWriter.TrySetValue(EcfHeaders.LastName, student.LastName);
                ecfTableWriter.TrySetValue(EcfHeaders.FirstName, student.FirstName);
                ecfTableWriter.TrySetValue(EcfHeaders.Gender, student.GetEcfGender());
                ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, student.GetEcfBirthdate());

                await ecfTableWriter.WriteAsync();

                ecfRecordCounter++;
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSchoolClassAttendances(UntisDocument untisDocument, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.StudentId,
                    EcfHeaders.SchoolClassId);
            }

            foreach (var student in untisDocument.Students)
            {
                ecfTableWriter.TrySetValue(EcfHeaders.StudentId, student.Id);
                ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassId, student.ClassId);

                await ecfTableWriter.WriteAsync();

                ecfRecordCounter++;
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSubjects(UntisDocument untisDocument, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code,
                    EcfHeaders.Name,
                    EcfHeaders.Description,
                    EcfHeaders.Color);
            }

            foreach (var subject in untisDocument.Subjects)
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, subject.Id);
                ecfTableWriter.TrySetValue(EcfHeaders.Code, subject.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.Name, subject.LongName);
                ecfTableWriter.TrySetValue(EcfHeaders.Description, subject.GetEcfDescription(untisDocument.Descriptions));
                ecfTableWriter.TrySetValue(EcfHeaders.Color, subject.BackgroundColor);

                await ecfTableWriter.WriteAsync();

                ecfRecordCounter++;
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSubstituteLessons(CsvReader csvReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.CourseId,
                    EcfHeaders.RoomIdList,
                    EcfHeaders.SchoolClassIdList,
                    EcfHeaders.TeacherIdList,
                    EcfHeaders.TemporalExpressions);
            }

            var gpuReader = new GpuReader<GpuSubstitution>(csvReader);

            await foreach (var substitution in gpuReader.ReadAsync())
            {
                if (substitution.Date >= _untisDocument.GeneralSettings.TermBeginDate &&
                    substitution.Date <= _untisDocument.GeneralSettings.TermEndDate &&
                    substitution.Type != GpuSubstitutionType.Cancellation && 
                    substitution.Type != GpuSubstitutionType.Exemption &&
                    substitution.GetEcfCourseId(_untisDocument.Lessons) != null)
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, substitution.GetEcfId());
                    ecfTableWriter.TrySetValue(EcfHeaders.CourseId, substitution.GetEcfCourseId(_untisDocument.Lessons));
                    ecfTableWriter.TrySetValue(EcfHeaders.RoomIdList, substitution.GetEcfRoomIdList());
                    ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassIdList, substitution.GetEcfSchoolClassIdList());
                    ecfTableWriter.TrySetValue(EcfHeaders.TeacherIdList, substitution.GetEcfTeacherIdList());
                    ecfTableWriter.TrySetValue(EcfHeaders.TemporalExpressions, substitution.GetEcfTemporalExpressions(_untisDocument.TimeGrids, _untisDocument.Lessons));

                    await ecfTableWriter.WriteAsync();

                    ecfRecordCounter++;
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportTeacherAbsences(CsvReader csvReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.TeacherId,
                    EcfHeaders.StartTimepoint,
                    EcfHeaders.EndTimepoint,
                    EcfHeaders.ReasonId,
                    EcfHeaders.Description);
            }

            var gpuReader = new GpuReader<GpuAbsence>(csvReader);

            await foreach (var absence in gpuReader.ReadAsync())
            {
                if (absence.Type == GpuAbsenceType.Teacher)
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, absence.Id);
                    ecfTableWriter.TrySetValue(EcfHeaders.TeacherId, absence.GetUntisTeacherId());
                    ecfTableWriter.TrySetValue(EcfHeaders.StartTimepoint, absence.StartDate);
                    ecfTableWriter.TrySetValue(EcfHeaders.EndTimepoint, absence.EndDate);
                    ecfTableWriter.TrySetValue(EcfHeaders.ReasonId, absence.Reason);
                    ecfTableWriter.TrySetValue(EcfHeaders.Description, absence.Text);

                    await ecfTableWriter.WriteAsync();

                    _untisAbsencesCache.Add(absence.Id);
                    ecfRecordCounter++;
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportTeachers(UntisDocument untisDocument, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code,
                    EcfHeaders.LastName,
                    EcfHeaders.FirstName,
                    EcfHeaders.Gender,
                    EcfHeaders.Email,
                    EcfHeaders.Color);
            }

            foreach (var teacher in untisDocument.Teachers)
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, teacher.Id);
                ecfTableWriter.TrySetValue(EcfHeaders.Code, teacher.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.LastName, teacher.LastName);
                ecfTableWriter.TrySetValue(EcfHeaders.FirstName, teacher.FirstName);
                ecfTableWriter.TrySetValue(EcfHeaders.Gender, teacher.GetEcfGender());
                ecfTableWriter.TrySetValue(EcfHeaders.Email, teacher.Email);
                ecfTableWriter.TrySetValue(EcfHeaders.Color, teacher.BackgroundColor);

                await ecfTableWriter.WriteAsync();

                ecfRecordCounter++;
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportTimeframes(UntisDocument untisDocument, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code,
                    EcfHeaders.Name,
                    EcfHeaders.TimeSlots);
            }

            foreach (var timeGrid in untisDocument.TimeGrids)
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, timeGrid.GetEcfCode());
                ecfTableWriter.TrySetValue(EcfHeaders.Code, timeGrid.GetEcfCode());
                ecfTableWriter.TrySetValue(EcfHeaders.Name, timeGrid.GetEcfCode());
                ecfTableWriter.TrySetValue(EcfHeaders.TimeSlots, timeGrid.GetEcfTimeSlots());

                await ecfTableWriter.WriteAsync();

                ecfRecordCounter++;
            }

            return ecfRecordCounter;
        }
        private async Task<int> ExportVaccations(UntisDocument untisDocument, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Title,
                    EcfHeaders.Description,
                    EcfHeaders.TemporalExpressions);
            }

            foreach (var holiday in untisDocument.Holidays)
            {
                ecfTableWriter.TrySetValue(EcfHeaders.Id, holiday.Id);
                ecfTableWriter.TrySetValue(EcfHeaders.Title, holiday.ShortName);
                ecfTableWriter.TrySetValue(EcfHeaders.Description, holiday.LongName);
                ecfTableWriter.TrySetValue(EcfHeaders.TemporalExpressions, holiday.GetEcfTemporalExpressions());

                await ecfTableWriter.WriteAsync();

                ecfRecordCounter++;
            }

            return ecfRecordCounter;
        }
    }
}
