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

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ecf.Untis
{
    public static class JsonDocumentUtils
    {
        /// <summary>
        /// Merges two JSON files. If the target JSON file does not exist the source JSON file is just copied.
        /// </summary>
        /// <param name="tergetFileName">The path to the target JSON file</param>
        /// <param name="sourceFileName">The path to the source JSON file which is merged into the target JSON file</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous merge operation.</returns>
        public static async Task MergeFilesAsync(string tergetFileName, string sourceFileName, CancellationToken cancellationToken = default)
        {
            if (File.Exists(sourceFileName))
            {
                if (File.Exists(tergetFileName))
                {
                    using var jsonTargetDoc = await ParseFileAsync(tergetFileName, cancellationToken);
                    using var jsonSourceDoc = await ParseFileAsync(sourceFileName, cancellationToken);

                    using var jsonOutputStream = new FileStream(tergetFileName, FileMode.Create, FileAccess.Write, FileShare.None);
                    using var jsonOutputWriter = new Utf8JsonWriter(jsonOutputStream, new JsonWriterOptions { Indented = true });

                    JsonElement jsonTargetRoot = jsonTargetDoc.RootElement;
                    JsonElement jsonSourceRoot = jsonSourceDoc.RootElement;

                    if (jsonTargetRoot.ValueKind != JsonValueKind.Object)
                    {
                        throw new InvalidOperationException($"The original JSON document to merge new content into must be an object type. Instead it is {jsonTargetRoot.ValueKind}.");
                    }

                    if (jsonTargetRoot.ValueKind != jsonSourceRoot.ValueKind)
                    {
                        jsonTargetRoot.WriteTo(jsonOutputWriter);
                    }
                    else
                    {
                        MergeObjects(jsonOutputWriter, jsonTargetRoot, jsonSourceRoot);
                    }
                }
                else
                {
                    File.Copy(sourceFileName, tergetFileName);
                }
            }
            else
            {
                throw new FileNotFoundException($"Template file \"{sourceFileName}\") does not exists.");
            }
        }


        /// <summary>
        /// Parses the given JSON file into a JsonDocument. 
        /// </summary>
        /// <param name="fileName">The path to the JSON file.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task to produce a JsonDocument representation of the JSON file.</returns>
        public static async Task<JsonDocument> ParseFileAsync(string fileName, CancellationToken cancellationToken = default)
        {
            if (File.Exists(fileName))
            {
                using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                return await JsonDocument.ParseAsync(fileStream, default, cancellationToken);
            }
            else
            {
                throw new FileNotFoundException($"File \"{fileName}\") does not exists.");
            }
        }

        /// <summary>
        /// Merges two JSON objects and writes the result to the JSON stream.
        /// </summary>
        /// <param name="jsonWriter">The JSON writer which write the result of the merge reqeust</param>
        /// <param name="element1">The target JSON object</param>
        /// <param name="element2">The source JSON object</param>
        private static void MergeObjects(Utf8JsonWriter jsonWriter, JsonElement element1, JsonElement element2)
        {
            Debug.Assert(element1.ValueKind == JsonValueKind.Object);
            Debug.Assert(element2.ValueKind == JsonValueKind.Object);

            jsonWriter.WriteStartObject();

            foreach (JsonProperty property in element1.EnumerateObject())
            {
                string propertyName = property.Name;

                JsonValueKind newValueKind;

                if (element2.TryGetProperty(propertyName, out JsonElement newValue) && (newValueKind = newValue.ValueKind) != JsonValueKind.Null)
                {
                    JsonElement originalValue = property.Value;
                    JsonValueKind originalValueKind = originalValue.ValueKind;

                    if (newValueKind == JsonValueKind.Object && originalValueKind == JsonValueKind.Object)
                    {
                        jsonWriter.WritePropertyName(propertyName);
                        MergeObjects(jsonWriter, originalValue, newValue);
                    }
                    else
                    {
                        property.WriteTo(jsonWriter);
                    }
                }
                else
                {
                    property.WriteTo(jsonWriter);
                }
            }

            foreach (JsonProperty property in element2.EnumerateObject())
            {
                if (!element1.TryGetProperty(property.Name, out _))
                {
                    property.WriteTo(jsonWriter);
                }
            }

            jsonWriter.WriteEndObject();
        }
    }
}