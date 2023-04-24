using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;

namespace SnakeAI
{
    public class AIData
    {
        public dynamic choices { get; set; }
        public dynamic data { get; set; }

        public dynamic error { get; set; }

    }


    public class Deserialize
    {
        public static string ParseResponse(string input, int sessionID, string startChar = "Solid Snake:", string startChar2 = "liquid", string startChar3 = "ocelot", string startChar4 = "meryl")
        {
            dynamic jsonString = input;

            AIData text =
                JsonSerializer.Deserialize<AIData>(jsonString);

            if (text == null) { return "API error!"; };
            //if (text.error.ToString() != null) { return text.error.ToString(); }
            string modifiedString = "";
            try
            {
                modifiedString = text.choices.ToString().Replace("{", "");
            }
            catch (Exception e) 
            {
                WriteToLog(e.ToString(), sessionID);
                return "";
            }

            //initial cleanup
            modifiedString = modifiedString.Replace("(", "");
            modifiedString = modifiedString.Replace("}", "");
            modifiedString = modifiedString.Replace(")", "");
            modifiedString = modifiedString.Replace("[", "");
            modifiedString = modifiedString.Replace("\"", "");
            WriteToLog("TEST DEBUG TEST DEBUG____text string:" + modifiedString, sessionID);
            modifiedString = modifiedString.Replace("text", char.ToUpper(startChar[0]) + startChar.Substring(1));
            modifiedString = modifiedString.Replace("\\n\\n", Environment.NewLine);
            modifiedString = modifiedString.Replace("\\n", Environment.NewLine);

            //accents
            modifiedString = modifiedString.Replace("é", "e");
            modifiedString = modifiedString.Replace("è", "e");
            modifiedString = modifiedString.Replace("â", "a");
            modifiedString = modifiedString.Replace("î", "i");
            modifiedString = modifiedString.Replace("ô", "o");
            modifiedString = modifiedString.Replace("ñ", "n");
            modifiedString = modifiedString.Replace("ü", "u");
            modifiedString = modifiedString.Replace("ï", "i");
            modifiedString = modifiedString.Replace("ç", "c");
            modifiedString = modifiedString.Replace("`", "'");

            //character specific
            modifiedString = modifiedString.Replace("Liquid Snake:", "Liquid:");
            modifiedString = modifiedString.Replace("LS:", "Liquid:");
                modifiedString = modifiedString.Replace("L:", "Liquid:");

            modifiedString = modifiedString.Replace("Solid Snake:", "Snake:");
            modifiedString = modifiedString.Replace("SS:", "Snake:");

                modifiedString = modifiedString.Replace("R:", "Raiden:");
                modifiedString = modifiedString.Replace("Colonel Campbell:", "Campbell:");
                modifiedString = modifiedString.Replace("CC:", "Campbell:");
                modifiedString = modifiedString.Replace("RC:", "Campbell:");
                modifiedString = modifiedString.Replace("C:", "Campbell:");
                modifiedString = modifiedString.Replace("Roy Campbell:", "Campbell:");
            modifiedString = modifiedString.Replace("Colonel:", "Campbell:");
            modifiedString = modifiedString.Replace("colonel:", "Campbell:");
            modifiedString = modifiedString.Replace("Colonel Cambach:", "Campbell:");
            modifiedString = modifiedString.Replace("colonel cambach:", "Campbell:");
            modifiedString = modifiedString.Replace("colonel c Campbell:", "Campbell:");


            modifiedString = modifiedString.Replace("OC:", "Otacon:");


            modifiedString = modifiedString.Replace("Ling:", "Mei Ling:");
            modifiedString = modifiedString.Replace("Mei Mei", "Mei");
            modifiedString = modifiedString.Replace("Mei:", "Mei Ling:");
            modifiedString = modifiedString.Replace("ML:", "Mei Ling:");

            modifiedString = modifiedString.Replace("MM:", "Miller:");
            modifiedString = modifiedString.Replace("M:", "Meryl:");


            modifiedString = modifiedString.Replace("Nas:", "Nastasha:");
               
            modifiedString = modifiedString.Replace("NN:", "Nastasha:");
            modifiedString = modifiedString.Replace("N:", "Nastasha:");
            modifiedString = modifiedString.Replace("Nasty:", "Nastasha:");
            modifiedString = modifiedString.Replace("Nashasha:", "Nastasha:");
            modifiedString = modifiedString.Replace("Nastasha Romanenko:", "Nastasha:");
            modifiedString = modifiedString.Replace("Wolf:", "Sniper Wolf:");
            modifiedString = modifiedString.Replace("Sniper Sniper", "Sniper");
            modifiedString = modifiedString.Replace("Sniper:", "Sniper Wolf:");
            modifiedString = modifiedString.Replace("SW:", "Sniper Wolf:");
            modifiedString = modifiedString.Replace("Octopus:", "Decoy Octopus:");
            modifiedString = modifiedString.Replace("Decoy Decoy", "Decoy");
            modifiedString = modifiedString.Replace("Decoy:", "Decoy Octopus:");
            modifiedString = modifiedString.Replace("DO:", "Decoy Octopus:");
            modifiedString = modifiedString.Replace("OC:", "Otacon:");
            modifiedString = modifiedString.Replace("Ocon:", "Otacon:");
            modifiedString = modifiedString.Replace("Master Miller:", "Miller:");
            modifiedString = modifiedString.Replace("Master:", "Miller:");
            modifiedString = modifiedString.Replace("Revolver Ocelot:", "Ocelot:");
            modifiedString = modifiedString.Replace("Revolver:", "Ocelot:");
            modifiedString = modifiedString.Replace("RO:", "Ocelot:");
            modifiedString = modifiedString.Replace("RA:", "Raiden:");

            modifiedString = modifiedString.Replace(" interjects:", "");


            //clean up
            modifiedString = modifiedString.Replace(",index: 0,logprobs: null,finish_reason: stop]", "");
            modifiedString = modifiedString.Replace(",index: 0,logprobs: null,finish_reason: length]", "");
            modifiedString = modifiedString.Replace(",index:0,logprobs:null,finish_reason:stop]", "");
            modifiedString = modifiedString.Replace(",index:0,logprobs:null,finish_reason:length]", "");
            modifiedString = modifiedString.Replace(",index:0,logprobs:null,finish_reason:null]", "");
            modifiedString = modifiedString.Replace(",index: 0,logprobs: null,finish_reason: null]", "");
            modifiedString = modifiedString.Replace("\\", "");

            if (modifiedString.IndexOf("In response to") != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.IndexOf("In response to"));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.IndexOf("The conversation") != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.IndexOf("The conversation"));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.IndexOf("Their conversation") != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.IndexOf("Their conversation"));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.IndexOf("After their conversation") != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.IndexOf("After their conversation"));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.IndexOf("They are from the video game") != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.IndexOf("They are from the video game"));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.IndexOf("is a sarcastic soldier and spy") != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.IndexOf("is a sarcastic soldier and spy"));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }

            if (modifiedString.ToLower().IndexOf(startChar + " and " + startChar2) != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.ToLower().IndexOf(startChar + " and " + startChar2));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.ToLower().IndexOf(startChar + " and " + startChar3) != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.ToLower().IndexOf(startChar + " and " + startChar3));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.ToLower().IndexOf(startChar + " and " + startChar4) != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.ToLower().IndexOf(startChar + " and " + startChar4));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }


            if (modifiedString.ToLower().IndexOf(startChar2 + " and " + startChar) != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.ToLower().IndexOf(startChar2 + " and " + startChar));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.ToLower().IndexOf(startChar2 + " and " + startChar3) != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.ToLower().IndexOf(startChar2 + " and " + startChar3));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.ToLower().IndexOf(startChar2 + " and " + startChar4) != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.ToLower().IndexOf(startChar2 + " and " + startChar4));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }


            if (modifiedString.ToLower().IndexOf(startChar3 + " and " + startChar) != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.ToLower().IndexOf(startChar3 + " and " + startChar));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.ToLower().IndexOf(startChar3 + " and " + startChar2) != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.ToLower().IndexOf(startChar3 + " and " + startChar2));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.ToLower().IndexOf(startChar3 + " and " + startChar4) != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.ToLower().IndexOf(startChar3 + " and " + startChar4));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }


            if (modifiedString.ToLower().IndexOf(startChar4 + " and " + startChar) != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.ToLower().IndexOf(startChar4 + " and " + startChar));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.ToLower().IndexOf(startChar4 + " and " + startChar2) != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.ToLower().IndexOf(startChar4 + " and " + startChar2));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }
            if (modifiedString.ToLower().IndexOf(startChar4 + " and " + startChar3) != -1)
            {
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
                modifiedString = modifiedString.Remove(modifiedString.ToLower().IndexOf(startChar4 + " and " + startChar3));
                WriteToLog("Modified string before remove:" + modifiedString, sessionID);
            }


                return modifiedString;

        }

        public static string ParseImageResponse(string input, int sessionID)
        {
            dynamic jsonString = input;

            AIData text =
                JsonSerializer.Deserialize<AIData>(jsonString);

            if (text == null) { return "API error!"; };
            try
            {
                string modifiedString = text.data.ToString();
                WriteToLog(modifiedString, sessionID);
                int urlIndex = modifiedString.IndexOf("https");
                WriteToLog(urlIndex.ToString(), sessionID);
                int urlIndexEnd = modifiedString.IndexOf("\"", urlIndex);
                WriteToLog(urlIndexEnd.ToString(), sessionID);
                modifiedString = modifiedString.Substring(urlIndex, urlIndexEnd - urlIndex);
                return modifiedString;
            }
            catch (Exception ex) 
            {
                WriteToLog(ex.ToString(), sessionID);   
                return text.error.ToString();
            }
        }

        private static void WriteToLog(string logData, int sessionID)
        {
            if (!File.Exists("DeserializeLog_" + sessionID + ".txt"))
            {
                using (StreamWriter sw = File.CreateText("DeserializeLog_" + sessionID + ".txt"))
                {
                    sw.WriteLine(DateAndTime.Now + " - Log file created at " + System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\log_" + sessionID + ".txt");
                    sw.WriteLine(DateAndTime.Now + " - " + logData);
                }
            }
            else
            {
                using StreamWriter file = new("DeserializeLog_" + sessionID + ".txt", append: true);
                file.WriteLine(DateAndTime.Now + " - " + logData);
            }
        }

    }

}


