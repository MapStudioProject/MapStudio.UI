using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace MapStudio.UI
{
    public class RecentFileHandler
    {
        const int MRUnumber = 8;

        public static void LoadRecentList(string filePath, List<string> recentList)
        {
            recentList.Clear();
            if (!File.Exists(filePath)) return;
            
            var lines = File.ReadAllLines(filePath);
            foreach (var file in lines)
                if (File.Exists(file)) recentList.Add(file);
        }

        public static void SaveRecentFile(string recentFile, string filePath, List<string> recentList)
        {
            LoadRecentList(filePath, recentList); //load list from file
            if (recentList.Contains(recentFile)) //prevent duplication on recent list
                recentList.Remove(recentFile);
            recentList.Insert(0, recentFile); //insert given path into list

            //writing menu list to file
            //create file called "Recent.txt" located on app folder
            StreamWriter stringToWrite = new StreamWriter(filePath);
            for (int i = 0; i < Math.Min(recentList.Count, MRUnumber); i++)
                stringToWrite.WriteLine(recentList[i]); //write list to stream
            
            stringToWrite.Flush(); //write stream to file
            stringToWrite.Close(); //close the stream and reclaim memory
        }
    }
}
