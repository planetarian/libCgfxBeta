using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libCgfx;

namespace ReadCgfx
{
    class Program
    {
#if DEBUG
        private static int verbosity = 5;
#else
        private static int verbosity = 0;
#endif

        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
                Run(args);
#if !DEBUG
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
            }
#else
            Console.ReadLine();
#endif
        }

        static void Run(IList<string> args)
        {
            if (args.Count == 0)
            {
                Console.WriteLine("No file specified.");
                return;
            }

            var files = new List<string>();

            int a = 0;
            while (a < args.Count)
            {
                string arg = args[a++];
                if (arg.ToLower().StartsWith("-v"))
                {
                    string param = arg.Length > 2 ? arg.Substring(2) : args[a++];
                    if (!Int32.TryParse(param, out verbosity))
                        throw new ArgumentException("-v expects verbosity level parameter.");
                }
                else
                {
                    files.Add(arg);
                }
            }

            var cgfxObjects = new List<Cgfx>();
            const string texDir = "Textures";
            foreach (string file in files)
            {
                Console.WriteLine("Reading " + file);
                Console.WriteLine();

                var cgfx = new Cgfx(file, PrintMessage);
                cgfxObjects.Add(cgfx);
                if (!Directory.Exists(texDir))
                    Directory.CreateDirectory(texDir);
                foreach (KeyValuePair<string, Texture> tx in cgfx.Textures)
                {
                    File.WriteAllBytes(
                        texDir + "\\" + tx.Key +
                        "." + tx.Value.RawTextureFormat.ToString().ToLower() + ".bin",
                        tx.Value.RawData);
                    File.WriteAllBytes(texDir + "\\" + tx.Key + ".argb.bin", tx.Value.Data);
                }
                Console.WriteLine();
            }
        }

        public static void PrintMessage(string message, int indentLevel, int verbosityLevel)
        {
            if (verbosity >= verbosityLevel)
                Console.WriteLine(String.Empty.PadLeft(indentLevel, ' ') + message);
        }
    }
}
