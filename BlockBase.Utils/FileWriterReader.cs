using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlockBase.Utils
{
    public static class FileWriterReader
    {
        public static void Write(string fileName, string text, System.IO.FileMode fileMode)
        {
            using (FileStream file = new FileStream(fileName, fileMode, FileAccess.Write, FileShare.Read))
            {
                using (StreamWriter writer = new StreamWriter(file, Encoding.Unicode))
                {
                    writer.WriteLine(text);
                }
            }
        }

        public static IList<string> Read(string fileName)
        {
            var fileLines = new List<string>();
            try
            {
                using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (StreamReader sr = new StreamReader(file, Encoding.Unicode))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            fileLines.Add(line);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return fileLines;
            }
            return fileLines;

        }

        public static void RemoveLines
        (string fileName, string expression)
        {
            using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                using (var reader = new StreamReader(file, Encoding.Unicode))
                {
                    using (StreamWriter writer = new StreamWriter(file, Encoding.Unicode))
                    {
                        string line = null;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains(expression))
                                continue;
                            writer.WriteLine(line);
                        }
                    }
                }
            }
        }



    }
}