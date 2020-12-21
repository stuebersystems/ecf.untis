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

using Enbrea.Untis.Xml;
using System.Collections.Generic;

namespace Ecf.Untis
{
    /// <summary>
    /// Extensions for <see cref="UntisSubject"/>
    /// </summary>
    public static class UntisSubjectExtensions
    {
        public static string GetEcfDescription(this UntisSubject subject, List<UntisDescription> descriptions)
        {
            var description = descriptions.Find(x => x.Id == subject.DescriptionId);

            if (description != null)
            {
                return description.ShortName;
            }
            else
            {
                return null;
            }
        }
    }
}