using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LumenWorks.Framework.IO.Csv;
using System.Data;

namespace DuplicateClientsCleanup
{
    public class Helper
    {
        public static void ParseCsv(Dictionary<string, List<ClientModel>> clientDict, string csvPath = @"C:\rawnew.csv")
        {
            var csvTable = new DataTable();
            char delimiter = ',';

            if (!File.Exists(csvPath))
            {
                throw new FileNotFoundException($"CSV file was not found: {csvPath}", csvPath);
            }

            using (var csvReader = new CsvReader(new StreamReader(File.OpenRead(csvPath)), true, delimiter: delimiter))
            {
                csvTable.Load(csvReader);
            }

            try
            {
                for (int i = 0; i < csvTable.Rows.Count; i++)
                {
                    var reader = new ClientModel
                    {
                        ClientId = Convert.ToInt32(csvTable.Rows[i][1]),
                        FirstName = Convert.ToString(csvTable.Rows[i][2]) ?? string.Empty,
                        MiddleName = Convert.ToString(csvTable.Rows[i][3]) ?? string.Empty,
                        LastCorpName = Convert.ToString(csvTable.Rows[i][4]) ?? string.Empty,
                        AccountNumber = Convert.ToString(csvTable.Rows[i][5]) ?? string.Empty,
                        IsLinked = Convert.ToString(csvTable.Rows[i][6]) ?? string.Empty,
                        LinkedAccountNumber = Convert.ToString(csvTable.Rows[i][7]) ?? string.Empty,
                        TaskId = string.IsNullOrWhiteSpace(csvTable.Rows[i][8].ToString()) ? 0 : Convert.ToInt32(csvTable.Rows[i][8])
                    };

                    if (!clientDict.ContainsKey(reader.AccountNumber))
                    {
                        clientDict.Add(reader.AccountNumber, new List<ClientModel> { reader });
                    }
                    else
                    {
                        clientDict[reader.AccountNumber].Add(reader);
                    }
                }
                // There is a case where Account numbers are null in CSV, Ignoring them
                clientDict.Remove("");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Parsing CSV: " + ex.Message);
            }
        }

        // Delete is only to be performed on clients that have a similar account number and name
        // checking names here for , and whitespaces at the end
        public static bool checkSimilarNames(List<ClientModel> clientModel)
        {
            try
            {
                // Remove ',' from the end
                foreach (var client in clientModel)
                {
                    // cleanup on names, removing whitespaces and , from last name only and getting a full name string to compare, also ignoring case
                    var name = client.FirstName + client.MiddleName + client.LastCorpName;
                    name = name.TrimEnd();
                    if (name.EndsWith(','))
                    {
                        name = name.Remove(name.Length - 1);
                    }
                    client.FullName = name.ToLower();
                }
                return clientModel.All(x => x.FullName == clientModel.First().FullName);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error Cleaning Names " + ex.Message);
                return false;
            }
        }
    }
}
