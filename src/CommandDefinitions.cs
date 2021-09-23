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

using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Ecf.Untis
{
    public static class CommandDefinitions
    {
        public static Command Export()
        {
            var command = new Command("export", "Exports data from Untis to ECF files")
            {
                new Option<FileInfo>(new[] { "--config", "-c" }, "Path to existing JSON configuration file")
                {
                    IsRequired = true
                },
            };
            command.Handler = CommandHandler.Create<FileInfo>(
                async (config) =>
                    await CommandHandlers.Export(config));

            return command;
        }
    }
}
