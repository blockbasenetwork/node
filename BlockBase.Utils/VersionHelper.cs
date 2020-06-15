using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BlockBase.Utils
{
    public static class VersionHelper
    {
        public static int ConvertFromVersionString(string versionString)
        {
            var splitVersionString = versionString.Split(".");
            var convertedSplitVerionString = new List<string>();

            foreach (var splitString in splitVersionString)
            {
                var splitStringToAdd = splitString;
                if (splitString.Length == 1)
                    splitStringToAdd = $"0{splitString}";
                convertedSplitVerionString.Add(splitStringToAdd);
            }

            var joinedString = String.Join("", convertedSplitVerionString);

            return Convert.ToInt32(joinedString);
        }

        public static string ConvertFromVersionInt(int versionInt)
        {
            var versionString = versionInt.ToString();
            var lengthCheck = 2;

            while (lengthCheck <= 5)
            {
                if (versionString.Length > lengthCheck)
                    versionString = versionString.Insert(versionString.Length - lengthCheck, ".");
                else
                    versionString = versionString.Insert(0, "0.");

                lengthCheck += 3;
            }

            string output = Regex.Replace(versionString, "(?<=\\.)0(?!\\.)", "");

            return output;
        }
    }
}
