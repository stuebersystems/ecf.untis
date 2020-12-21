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

using Enbrea.Untis.Gpu;

namespace Ecf.Untis
{
    /// <summary>
    /// Extensions for <see cref="GpuAbsence"/>
    /// </summary>
    public static class GpuAbsenceExtensions
    {
        public static string GetUntisRoomId(this GpuAbsence absence)
        {
            if (string.IsNullOrEmpty(absence.ShortName))
            {
                return null;
            }
            else
            {
                return "RM_" + absence.ShortName;
            }
        }

        public static string GetUntisTeacherId(this GpuAbsence absence)
        {
            if (string.IsNullOrEmpty(absence.ShortName))
            {
                return null;
            }
            else 
            {
                return "TR_" + absence.ShortName;
            }
        }

        public static string GetUntisSchoolClassId(this GpuAbsence absence)
        {
            if (string.IsNullOrEmpty(absence.ShortName))
            {
                return null;
            }
            else
            {
                return "CL_" + absence.ShortName;
            }
        }
    }
}