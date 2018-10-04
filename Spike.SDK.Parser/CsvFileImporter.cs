using System;
using System.IO;

namespace Spike.SDK.Parser
{
    public class Counters
    {
        public int Total => Success + Skipped + Failed;

        public int Success { get; set; }

        public int Skipped { get; set; }

        public int Failed { get; set; }

        public override string ToString()
        {
            return $"Total Imported Items is [{Total}]: Successful [{Success}] Skipped [{Skipped}] Failed [{Failed}]";
        }
    }

    /// <summary>
    /// Import class helper
    /// </summary>
    /// <typeparam name="T">Representative concretion of line item</typeparam>
    /// <typeparam name="TE">ENUM with Property name and column position</typeparam>
    public class CsvFileImporter<T, TE>
        where TE : struct
    {
        public char Delimiter { get; }

        public CsvFileImporter(char delimiter = ',')
        {
            Delimiter = delimiter;
        }

        public T ConvertLine(string line)
        {
            var newImport = Activator.CreateInstance<T>();
            var columns = Enum.GetValues(typeof(TE));
            var seperatedValues = line.Split(Delimiter);

            foreach (var column in columns)
            {
                var propertyInfo = newImport?.GetType()?.GetProperty(column.ToString());

                if (propertyInfo == null)
                {
                    throw new Exception($"Could not find property [{column}] in class [{typeof(T)}]");
                }

                propertyInfo.SetValue(newImport, seperatedValues[(int)column], null);
            }

            return newImport;
        }

        public void ImportCsv(string filePath, Action<T, Counters> processLine, ref Counters counters, int skipLines = 0)
        {
            var lineCount = 0;

            using (var reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    lineCount++;

                    var line = reader.ReadLine();
                    if (lineCount <= skipLines) continue;

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var import = ConvertLine(line);
                    processLine(import, counters);
                }
            }
        }
    }
}
