using System.IO;
using System;
using Newtonsoft.Json;
using RadiusSaveConvertor;

namespace ITRSaveTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "ITR Save Convertor by Neo_Kesha";
            if (args.Length < 1)
            {
                PrintHelp();
                return;
            }
            var filePath = args[0];
            if (!File.Exists(filePath))
            {
                return;
            }

            var ext = Path.GetExtension(filePath);
            var path = Path.GetDirectoryName(filePath);
            var name = Path.GetFileNameWithoutExtension(filePath);
            if (ext == ".save")
            {
                ITR_SaveFile save = null;
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var reader = new BinaryReader(stream);
                    save = new ITR_SaveFile(reader);
                }
                using (var writer = new StreamWriter(Path.Combine(path, name + ".json")))
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.Indented };
                    writer.Write(JsonConvert.SerializeObject(save, settings));
                }
                return;
            }
            if (ext == ".json")
            {
                ITR_SaveFile save = null;
                using (var reader = new StreamReader(filePath))
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                    save = JsonConvert.DeserializeObject<ITR_SaveFile>(reader.ReadToEnd(), settings);
                }
                using (var stream = new FileStream(Path.Combine(path, name + ".save"), FileMode.Create, FileAccess.Write))
                {
                    var writer = new BinaryWriter(stream);
                    save.Write(writer);
                }
                return;
            }

            PrintHelp();
            return;
        }

        public static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("Drag and Drop *.save file onto application icon to convert it to *.json");
            Console.WriteLine("Drag and Drop *.json file onto application icon to convert it to *.save");
            Console.WriteLine("Application made by Neo_Kesha");
            Console.WriteLine("Press Any Key to close application...");
            Console.ReadKey();

        }
    }

}