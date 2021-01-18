﻿#region ENBREA - Copyright (C) 2021 STÜBER SYSTEMS GmbH
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

using Enbrea.Ecf;
using Enbrea.Untis.Xml;
using System;
using System.Collections.Generic;

namespace Ecf.Untis
{
    /// <summary>
    /// Extensions for <see cref="UntisTimeGrid"/>
    /// </summary>
    public static class UntisTimeGridExtensions
    {
        public static string GetEcfCode(this UntisTimeGrid timeGrid)
        {
            if (string.IsNullOrEmpty(timeGrid.Name))
            {
                return "Standard";
            }
            else 
            {
                return timeGrid.Name;
            }
        }

        public static List<EcfTimeSlot> GetEcfTimeSlots(this UntisTimeGrid timeGrid)
        {
            var timeSlots = new List<EcfTimeSlot>();

            foreach (var slot in timeGrid.Slots.FindAll(x => x.Day == DayOfWeek.Monday))
            {
                timeSlots.Add(new EcfTimeSlot()
                {
                    Label = slot.GetEcfLabel(),
                    StartTime = slot.GetEcfStartTime(),
                    EndTime = slot.GetEcfEndTime()
                });
            }

            return timeSlots;
        }
    }
}


