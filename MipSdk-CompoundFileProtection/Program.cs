using System;
using System.Collections.Generic;
using System.IO;

namespace MipSdk_CompoundFileProtection
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigSettings config = new ConfigSettings();
            Dictionary<string, string> fileToLabelMap = new Dictionary<string, string>();

            AppInfoExtended appInfo = new AppInfoExtended()
            {
                ApplicationId = config.ClientId,
                ApplicationName = config.AppName,
                ApplicationVersion = config.AppVersion,
                IsMultiTenantApp = Convert.ToBoolean(config.IsMultiTenantApp),
                RedirectUri = config.RedirectUri,
                TenantId = config.TenantId
            };

            Action action = new Action(appInfo);

            string rootPath = @"D:\mip\testfiles\compound\input";
            Random rand = new Random();

            // Build a mapping of label to file. 
            // Randomly selects one of the two labels below.
            // This is for demo purposes. Replace with your own label Id if testing. 
            for(int i = 1; i <= 6; i++)
            {
                List<string> labelsInUse = new List<string>()
                {
                    "cf3f4243-49e2-4f99-af45-df2b9e7146fd",
                    "836ff34f-b604-4a62-a68c-d6be4205d569"
                };

                string filePath = Path.Combine(rootPath, "test" + i.ToString() + ".txt");
                string selectedLabel = labelsInUse[rand.Next(0, 2)];
                fileToLabelMap.Add(filePath, selectedLabel);
            }            

            //action.ListLabels();

            // Pass set of all files and labels to SDK functions. 
            action.LabelMultipleFiles(fileToLabelMap);
        }
    }
}
