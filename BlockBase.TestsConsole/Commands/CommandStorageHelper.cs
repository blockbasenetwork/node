using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace BlockBase.TestsConsole.Commands
{
    public class CommandStorageHelper
    {
        private string Path { get; set; }
        private List<string> _commands;

        public CommandStorageHelper(string path)
        {
            Path = path;
            if (!File.Exists(Path)) File.Create(Path).Dispose();
        }

        private List<string> LoadInputCommandsFromFile()
        {
            try
            {
                var fileTextContent = File.ReadAllText(Path);
                List<string> commands;

                if (!string.IsNullOrWhiteSpace(fileTextContent))
                {
                    commands = JsonConvert.DeserializeObject<List<string>>(fileTextContent);
                }
                else
                {
                    commands = new List<string>();
                }
                return commands;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading commands" + "/n" + ex);
                return new List<string>();
            }
        }

        public List<string> LoadInputCommands()
        {
            if (_commands == null) _commands = LoadInputCommandsFromFile();

            return _commands;
        }

        public void SaveInputCommand(string commandStr)
        {
            try
            {
                var fileTextContent = File.ReadAllText(Path);

                List<string> commands = new List<string>(LoadInputCommands());

                if (!commands.Contains(commandStr))
                {
                    commands.Add(commandStr);
                    if (commands.Count > 20) commands.RemoveAt(0);

                    //deal with "cached commands" for quicker retrieval
                    _commands.Add(commandStr);
                    if (_commands.Count > 20) _commands.RemoveAt(0);
                }

                File.WriteAllText(Path, JsonConvert.SerializeObject(commands));
            }
            catch (Exception ex)
            {
                var e = ex;
                Console.WriteLine("Error saving command.");
            }
        }

        public void ClearStorageCommand()
        {
            try
            {
                File.WriteAllText(Path, "");
                _commands = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error clearing command." + "/n" + ex);
            }
        }
    }
}