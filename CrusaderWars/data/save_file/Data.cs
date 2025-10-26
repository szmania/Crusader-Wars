using System.IO;
using System.Text.RegularExpressions;

namespace CrusaderWars.data.save_file
{
    // NOTE: This file contains only the new logic for extracting dynasties.
    // You will need to merge this with your existing Data.cs file,
    // which contains the other methods for the SearchKeys class.
    internal static partial class SearchKeys
    {
        static bool isDynasties = false;
        static int dynasties_bracket_count = 0;
        public static void Dynasties(string line)
        {
            if (line.Trim() == "dynasties={")
            {
                isDynasties = true;
            }

            if (isDynasties)
            {
                File.AppendAllText(Writter.DataFilesPaths.Dynasties_Path(), line + "\n");

                if (line.Contains("{"))
                {
                    dynasties_bracket_count++;
                }
                if (line.Contains("}"))
                {
                    dynasties_bracket_count--;
                }

                if (dynasties_bracket_count == 0 && isDynasties)
                {
                    isDynasties = false;
                }
            }
        }
    }
}
