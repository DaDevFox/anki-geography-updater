using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Linq;


/* 
 * 
 * findCards
 * Returns an array of card IDs for a given query. Functionally identical to guiBrowse but doesn't use the GUI for better performance.
 * Sample request:
 * Sample result:
 * cardsToNotes
 * 
 * Returns an unordered array of note IDs for the given card IDs. For cards with the same note, the ID is only given once in the array.
 * 
Sample request:

 * 
 * 
 * 
 * 
 * 
 */

static class Convertor
{
    static readonly HttpClient client = new HttpClient();

    static readonly int columns = 3;

    private static List<string>[] _fieldData = new List<string>[columns];
    private static string targetDeck = "geography::countries-capitals-flags";

    static void LoadFields(string filename)
    {
        StreamReader file = File.OpenText(Path.Join(AppContext.BaseDirectory, filename));

        for (int i = 0; i < columns; i++)
            _fieldData[i] = new List<string>();


        string? current = null;
        while ((current = file.ReadLine()) != null)
        {
            string[] split = current.Split(',');

            for (int i = 0; i < columns; i++)
                _fieldData[i].Add(split[i]);
        }
    }

    static void Main(string[] args)
    {
        try
        {
            MainRoutine(args).Wait();
        }
        catch (Exception e)
        {
            Console.WriteLine($"exception! \n{e}");
        }
    }

    static async Task MainRoutine(string[] args)
    {
        string file = "index.csv";
        if (args.Length > 0)
            file = args[0];
        string[] columnFields = { "countryName", "capitalName", "countryInfo" };
        LoadFields(file);

        for (int i = 0; i < _fieldData[0].Count; i++)
        {
            string country = _fieldData[0][i];

            long id = await GetCountryCardID(country);
            if (id == -1) // TODO: add card
                continue;

            await UpdateCountry(id, country, _fieldData[1][i], _fieldData[2][i]);
        }

        Console.WriteLine("Complete");
        //Console.ReadLine();
    }

    static async Task<long> GetCountryCardID(string country)
    {
        try
        {
            long id = -1;

            string json = $"{{\"action\": \"findNotes\",\r\n\"version\": 6,\r\n\"params\": {{\r\n\"query\": \"deck:{targetDeck} countryName:*\\\"{country}\\\"*\"\r\n    }}\r\n}}";
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(
                    "http://localhost:8765",
                     new StringContent(json, Encoding.UTF8, "application/json"));


                var responseString = await response.Content.ReadAsStringAsync();
                JToken value = JsonConvert.DeserializeObject<JObject>(responseString).GetValue("result").Values().First();
                id = value.Value<long>();
                //Console.WriteLine(response);

                Console.WriteLine($"country {country}; {(id != -1 ? $"has note (id {id})" : "no note")}");
                return id;
            }

        }
        catch (Exception e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
        }

        return -1;
    }

    static async Task UpdateCountry(long noteID, string country, string capital, string info)
    {
        try
        {

            string json = $"{{\r\n    \"action\": \"updateNoteFields\",\r\n    \"version\": 6,\r\n    \"params\": {{\r\n        \"note\": {{\r\n            \"id\": {noteID},\r\n            \"fields\": {{\r\n                \"capitalName\": \"{capital}\",\r\n                \"countryInfo\": \"{info}\"\r\n            }}\r\n        }}\r\n    }}\r\n}}";
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(
                    "http://localhost:8765",
                     new StringContent(json, Encoding.UTF8, "application/json"));

                Console.WriteLine(response);

                //var responseString = await response.Content.ReadAsStringAsync();
                //JToken value = JsonConvert.DeserializeObject<JObject>(responseString).GetValue("result").Values().First();
                //id = value.Value<long>();

                Console.WriteLine($"country {country}; updated");
                return;
            }

        }
        catch (Exception e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
        }
    }
}