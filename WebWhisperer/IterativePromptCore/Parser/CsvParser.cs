using CsvHelper;
using OpenAI_API.Moderation;
using System.Globalization;
using System.Text;
using WebWhisperer.IterativePromptCore.Types;

namespace WebWhisperer.IterativePromptCore.Parser
{
    public static class CsvParser
    {
        /// <summary>
        /// Parsing CSV file to List of <see cref="Field"/>s from given path.
        /// </summary>
        /// <param name="filePath">Path to a CSV file.</param>
        /// <returns>List of <see cref="Field"/>s from input.</returns>
        public static List<Field> ParseCsvFile(string filePath, string delimiter)
        {
            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                return ParseCsvStream(reader, delimiter);
            }
        }

        /// <summary>
        /// Parsing CSV file to List of <see cref="Field"/>s. from given stream.
        /// </summary>
        /// <param name="filePath">Input stream</param>
        /// <returns>List of <see cref="Field"/>s from input.</returns>
        public static List<Field> ParseCsvFile(Stream fileStream, string delimiter)
        {
            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                return ParseCsvStream(reader, delimiter);
            }
        }

        private static List<Field> ParseCsvStream(StreamReader reader, string delimiter)
        {
            using (var csvReader = new CsvHelper.CsvReader(
                reader,
                new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture) 
                { 
                    Delimiter = delimiter,
                    Encoding = Encoding.UTF8
                })
            )
            {
                var result = new List<Field>();
                bool headersLoaded = false;
                string[] headers = null;
                FieldDataType[] dataTypes = null;
                Dictionary<int, Field> fieldDict = new Dictionary<int, Field>();

                int index = 0;
                foreach (var row in csvReader.GetRecords<dynamic>())
                {
                    var expandedRow = row as IDictionary<string, object>;
                    
                    if (expandedRow != null)
                    {
                        if (!headersLoaded)
                        {
                            headers = expandedRow.Keys.ToArray();
                            dataTypes = new FieldDataType[headers.Length];
                            headersLoaded = true;
                        }

                        if (index == 0 && headersLoaded)
                        {
                            for (int i = 0; i < headers.Length; i++)
                            {
                                if (double.TryParse(expandedRow[headers[i]].ToString(), CultureInfo.CurrentCulture, out double number))
                                    dataTypes[i] = FieldDataType.Number;
                                else if (DateTime.TryParse(expandedRow[headers[i]].ToString(), CultureInfo.CurrentCulture, out DateTime datetime))
                                    dataTypes[i] = FieldDataType.Date;
                                else if (bool.TryParse(expandedRow[headers[i]].ToString(), out bool bolean))
                                    dataTypes[i] = FieldDataType.Bool;
                                else
                                    dataTypes[i] = FieldDataType.String;
                            }

                            for (int i = 0; i < headers.Length; i++)
                            {
                                var header = headers[i];
                                var field = new Field()
                                {
                                    Header = new Header(header, dataTypes[i], i),
                                    Data = new List<Cell>()
                                };
                                fieldDict.Add(i, field);
                            }
                        }

                        // regularly parse data, row by row
                        for (int i = 0; i < expandedRow.Values.Count; i++)
                        {
                            if (fieldDict.ContainsKey(i))
                            {
                                fieldDict[i].Data.Add(new Cell() { Content = expandedRow[headers[i]].ToString(), Index = index });
                            }
                        }
                    }
                    index++;
                }


                foreach (var field in fieldDict.Values)
                {
                    result.Add(field);
                }
                return result;
            }
        }

        /// <summary>
        /// Parsing the inner List of <see cref="Field"/> representation back to CSV file.
        /// </summary>
        /// <param name="fields"><see cref="List{Field}"/> input fields to parse.</param>
        /// <param name="outputFilePath">Output filePath.</param>
        public static void ParseFieldsIntoCsv(IEnumerable<Field> fields, string outputFilePath)
        {
            using (var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var fieldList = new List<Field>();
                foreach (var field in fields)
                {
                    csv.WriteField(field.Header.Name);
                    fieldList.Add(field);
                }
                csv.NextRecord();

                // until not all lines empty
                int currentRow = 0;
                bool rowEmpty = false;
                while (!rowEmpty)
                {
                    foreach (var field in fieldList)
                    {
                        if (currentRow < field.Data.Count)
                        {
                            csv.WriteField(field.Data[currentRow].Content);
                            rowEmpty = false;
                        }
                        else
                        {
                            rowEmpty = true;
                        }
                    }
                    csv.NextRecord();
                    currentRow++;
                }
            }
        }

        /// <summary>
        /// Parsing the inner List of <see cref="Field"/> representation back to CSV file, returning in string.
        /// </summary>
        /// <param name="fields"><see cref="List{Field}"/> input fields to parse.</param>
        /// <param name="outputFilePath">Output filePath.</param>
        public static string ParseFieldsIntoCsv(IEnumerable<Field> fields)
        {
            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var fieldList = new List<Field>();
                foreach (var field in fields)
                {
                    csv.WriteField(field.Header.Name);
                    fieldList.Add(field);
                }
                csv.NextRecord();

                // until not all lines empty
                int currentRow = 0;
                bool rowEmpty = false;
                while (!rowEmpty)
                {
                    foreach (var field in fieldList)
                    {
                        if (currentRow < field.Data.Count)
                        {
                            csv.WriteField(field.Data[currentRow].Content);
                            rowEmpty = false;
                        }
                        else
                        {
                            rowEmpty = true;
                        }
                    }
                    csv.NextRecord();
                    currentRow++;
                }

                return writer.ToString();
            }
        }
    }
}
