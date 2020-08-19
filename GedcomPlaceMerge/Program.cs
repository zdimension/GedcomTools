using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GedcomPlaceMerge
{
    internal static class Program
    {
        /// <summary>
        /// The command was used incorrectly, e.g., with the wrong number of arguments, a bad flag, a bad syntax in a parameter, or whatever.
        /// </summary>
        private const int ExUsage = 64;
        
        /// <summary>
        /// An input file (not a system file) did not exist or was not readable.
        /// </summary>
        private const int ExNoinput = 66;
        
        private const int ExitSuccess = 0;
        
        private static int Main(string[] args)
        {
            Console.WriteLine("GedcomPlaceMerge 0.1 - (c) 2020 zdimension");

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: gpmerge <file.ged>");
                Console.WriteLine("Creates two backups (file.ged.old and file.ged.old.2).");
                return ExUsage;
            }

            var file = args[0];

            if (!File.Exists(file))
            {
                Console.WriteLine("File does not exist.");
                return ExNoinput;
            }

            Console.WriteLine("Commands: . (custom entry), i (ignore), q (quit), s (save)");

            Console.WriteLine("start");
            
            if (File.Exists(file + ".old.2"))
            {
                File.Delete(file + ".old.2");
            }

            if (File.Exists(file + ".old"))
            {
                File.Move(file + ".old", file + ".old.2");
            }

            File.Copy(file, file + ".old");

            string[] lines;
            
            try
            {
                lines = File.ReadAllLines(file, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                Console.WriteLine("Unable to read the file.\n");
                Console.WriteLine(ex);
                return ExNoinput;
            }

            var linesPlaces = new List<(int, string)>();
            for (var i = 0; i < lines.Length; i++)
            {
                var cur = lines[i];
                if (TryGetPlace(cur, out var place))
                {
                    linesPlaces.Add((i, place));
                }
            }

            void Save()
            {
                Console.Write("Saving... ");
                File.WriteAllLines(file, lines, Encoding.UTF8);
                Console.WriteLine("OK");
            }

            Console.WriteLine("end read");
            
            var grp = linesPlaces.GroupBy(p =>
                    p.Item2.Split(',')[0]
                        .ToUpperInvariant()
                        .RemoveDiacritics()
                        .Replace('-', ' '))
                .OrderBy(g => g.Key);
            
            foreach (var g in grp)
            {
                var items = g.GroupBy(p => p.Item2).OrderBy(item => item.Key).ToList();
                if (items.Count <= 1)
                    continue;
                Console.WriteLine("*** " + g.Key);
                var max = 0;
                for (var i = 0; i < items.Count; i++)
                {
                    Console.WriteLine("{0,2} : {1,5}x   {2}", i, items[i].Count(), items[i].Key);
                    if (items[i].Key.Length > items[max].Key.Length ||
                        (items[i].Key.Length == items[max].Key.Length &&
                         (items[i].Key.Count(c => c == '-') > items[max].Key.Count(c => c == '-') ||
                          items[i].Count() > items[max].Count())))
                        max = i;
                }

                string place;

                if (items.Count == 2 && items[0].Key[1..] == items[1].Key[1..] &&
                    char.ToUpper(items[0].Key[0]) == items[1].Key[0])
                {
                    Console.WriteLine("* auto change to {0}", items[1].Key);
                    place = items[1].Key;
                }
                else
                {
                    while (true)
                    {
                        Console.Write("[{0} - {1}] [./i/q/s] ", max, items[max].Key);
                        
                        var inp = Console.ReadLine();

                        if (string.IsNullOrWhiteSpace(inp))
                        {
                            place = items[max].Key;
                            break;
                        }

                        switch (inp)
                        {
                            case "q":
                                goto end;
                            case "i":
                                goto ignore;
                            case "s":
                                Save();
                                break;
                        }

                        if (inp == ".")
                        {
                            Console.Write(">>> ");
                            place = Console.ReadLine();
                            break;
                        }

                        if (int.TryParse(inp, out var res) && res < items.Count)
                        {
                            place = items[res].Key;
                            break;
                        }
                    }

                    if (place != "")
                        place = char.ToUpper(place[0]) + place[1..];
                }

                var changed = 0;
                foreach (var item in items
                    .Where(t => t.Key != place)
                    .SelectMany(t => t))
                {
                    lines[item.Item1] = "2 PLAC " + place;
                    changed++;
                }

                Console.WriteLine("{0,4} changed to {1}", changed, place);

                ignore: ;
            }

            end:

            Save();

            return ExitSuccess;
        }

        #region "https://stackoverflow.com/a/34272324/2196124"

        private static readonly Dictionary<string, string> ForeignCharacters = new Dictionary<string, string>
        {
            { "äæǽ", "ae" },
            { "öœ", "oe" },
            { "ü", "ue" },
            { "Ä", "Ae" },
            { "Ü", "Ue" },
            { "Ö", "Oe" },
            { "ÀÁÂÃÄÅǺĀĂĄǍΑΆẢẠẦẪẨẬẰẮẴẲẶА", "A" },
            { "àáâãåǻāăąǎªαάảạầấẫẩậằắẵẳặа", "a" },
            { "Б", "B" },
            { "б", "b" },
            { "ÇĆĈĊČ", "C" },
            { "çćĉċč", "c" },
            { "Д", "D" },
            { "д", "d" },
            { "ÐĎĐΔ", "Dj" },
            { "ðďđδ", "dj" },
            { "ÈÉÊËĒĔĖĘĚΕΈẼẺẸỀẾỄỂỆЕЭ", "E" },
            { "èéêëēĕėęěέεẽẻẹềếễểệеэ", "e" },
            { "Ф", "F" },
            { "ф", "f" },
            { "ĜĞĠĢΓГҐ", "G" },
            { "ĝğġģγгґ", "g" },
            { "ĤĦ", "H" },
            { "ĥħ", "h" },
            { "ÌÍÎÏĨĪĬǏĮİΗΉΊΙΪỈỊИЫ", "I" },
            { "ìíîïĩīĭǐįıηήίιϊỉịиыї", "i" },
            { "Ĵ", "J" },
            { "ĵ", "j" },
            { "ĶΚК", "K" },
            { "ķκк", "k" },
            { "ĹĻĽĿŁΛЛ", "L" },
            { "ĺļľŀłλл", "l" },
            { "М", "M" },
            { "м", "m" },
            { "ÑŃŅŇΝН", "N" },
            { "ñńņňŉνн", "n" },
            { "ÒÓÔÕŌŎǑŐƠØǾΟΌΩΏỎỌỒỐỖỔỘỜỚỠỞỢО", "O" },
            { "òóôõōŏǒőơøǿºοόωώỏọồốỗổộờớỡởợо", "o" },
            { "П", "P" },
            { "п", "p" },
            { "ŔŖŘΡР", "R" },
            { "ŕŗřρр", "r" },
            { "ŚŜŞȘŠΣС", "S" },
            { "śŝşșšſσςс", "s" },
            { "ȚŢŤŦτТ", "T" },
            { "țţťŧт", "t" },
            { "ÙÚÛŨŪŬŮŰŲƯǓǕǗǙǛŨỦỤỪỨỮỬỰУ", "U" },
            { "ùúûũūŭůűųưǔǖǘǚǜυύϋủụừứữửựу", "u" },
            { "ÝŸŶΥΎΫỲỸỶỴЙ", "Y" },
            { "ýÿŷỳỹỷỵй", "y" },
            { "В", "V" },
            { "в", "v" },
            { "Ŵ", "W" },
            { "ŵ", "w" },
            { "ŹŻŽΖЗ", "Z" },
            { "źżžζз", "z" },
            { "ÆǼ", "AE" },
            { "ß", "ss" },
            { "Ĳ", "IJ" },
            { "ĳ", "ij" },
            { "Œ", "OE" },
            { "ƒ", "f" },
            { "ξ", "ks" },
            { "π", "p" },
            { "β", "v" },
            { "μ", "m" },
            { "ψ", "ps" },
            { "Ё", "Yo" },
            { "ё", "yo" },
            { "Є", "Ye" },
            { "є", "ye" },
            { "Ї", "Yi" },
            { "Ж", "Zh" },
            { "ж", "zh" },
            { "Х", "Kh" },
            { "х", "kh" },
            { "Ц", "Ts" },
            { "ц", "ts" },
            { "Ч", "Ch" },
            { "ч", "ch" },
            { "Ш", "Sh" },
            { "ш", "sh" },
            { "Щ", "Shch" },
            { "щ", "shch" },
            { "ЪъЬь", "" },
            { "Ю", "Yu" },
            { "ю", "yu" },
            { "Я", "Ya" },
            { "я", "ya" },
        };

        #endregion

        private static bool TryFindEquivalent(char c, out string equiv)
        {
            foreach (var (key, value) in ForeignCharacters)
            {
                if (key.Contains(c))
                {
                    equiv = value;
                    return true;
                }
            }

            equiv = null!;
            return false;
        }

        private static string RemoveDiacritics(this string s)
        {
            var text = new StringBuilder();

            foreach (var c in s)
            {
                if (TryFindEquivalent(c, out var equiv))
                    text.Append(equiv);
                else
                    text.Append(c);
            }

            return text.ToString();
        }

        private static bool TryGetPlace(string s, out string place)
        {
            if (s.StartsWith("2 PLAC "))
            {
                place = s[7..];
                return true;
            }

            place = null!;
            return false;
        }
    }
}