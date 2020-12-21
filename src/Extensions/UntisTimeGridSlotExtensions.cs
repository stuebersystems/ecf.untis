﻿#region ENBREA - Copyright (C) 2020 STÜBER SYSTEMS GmbH
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

namespace Ecf.Untis
{
    /// <summary>
    /// Extensions for <see cref="UntisTimeGridSlot"/>
    /// </summary>
    public static class UntisTimeGridSlotExtensions
    {
        public static DateTimeOffset GetEcfEndTime(this UntisTimeGridSlot timeGridSlot)
        {
            var d = new DateTimeOffset(1899, 12, 30, 0, 0, 0, new TimeSpan(1, 0, 0));

            return d.AddMinutes(timeGridSlot.EndTime.TotalMinutes);
        }

        public static string GetEcfLabel(this UntisTimeGridSlot timeGridSlot)
        {
            if (string.IsNullOrEmpty(timeGridSlot.Label))
            {
                return timeGridSlot.Period.ToString();
            }
            else 
            {
                return timeGridSlot.Label;
            }
        }

        public static DateTimeOffset GetEcfStartTime(this UntisTimeGridSlot timeGridSlot)
        {
            var d = new DateTimeOffset(1899, 12, 30, 0, 0, 0, new TimeSpan(1, 0, 0));
           
            return d.AddMinutes(timeGridSlot.StartTime.TotalMinutes);
        }
    }
}
