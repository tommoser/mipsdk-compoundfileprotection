using Microsoft.InformationProtection;
using Microsoft.InformationProtection.Exceptions;
using Microsoft.InformationProtection.File;
using Microsoft.InformationProtection.Protection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipSdk_CompoundFileProtection
{    
    public class Action : IDisposable
    {
        private AuthDelegateImpl authDelegate;
        private AppInfoExtended appInfo;
        private IFileProfile profile;
        private IFileEngine engine;
        private MipContext mipContext;

        // Used to pass in options for labeling the file.
        public struct FileOptions
        {
            public string FileName;
            public string OutputName;
            public string LabelId;
            public DataState DataState;
            public AssignmentMethod AssignmentMethod;
            public ActionSource ActionSource;
            public bool IsAuditDiscoveryEnabled;
            public bool GenerateChangeAuditEvent;
        }

        public Action(AppInfoExtended appInfo)
        {
            this.appInfo = appInfo;

            // Initialize AuthDelegateImplementation using AppInfo. 
            authDelegate = new AuthDelegateImpl(this.appInfo);

            // Initialize SDK DLLs. If DLLs are missing or wrong type, this will throw an exception

            MIP.Initialize(MipComponent.File);

            // This method in AuthDelegateImplementation triggers auth against Graph so that we can get the user ID.            

            // Create profile.
            profile = CreateFileProfile(appInfo);

            // Create engine providing Identity from authDelegate to assist with service discovery.
            engine = CreateFileEngine(new Identity("MyService"));
        }

        public void Dispose()
        {
            profile.UnloadEngineAsync(engine.Settings.EngineId).GetAwaiter().GetResult();
            engine.Dispose();
            profile.Dispose();
            mipContext.ShutDown();
            mipContext.Dispose();
        }

        private IFileProfile CreateFileProfile(ApplicationInfo appInfo)
        {
            // Initialize MipContext
            mipContext = MIP.CreateMipContext(appInfo, "mip_data", LogLevel.Trace, null, null);

            // Initialize file profile settings to create/use local state.                
            var profileSettings = new FileProfileSettings(mipContext,
                CacheStorageType.OnDiskEncrypted,
                new ConsentDelegateImpl());

            // Use MIP.LoadFileProfileAsync() providing settings to create IFileProfile. 
            // IFileProfile is the root of all SDK operations for a given application.
            var profile = Task.Run(async () => await MIP.LoadFileProfileAsync(profileSettings)).Result;
            return profile;
        }

        private IFileEngine CreateFileEngine(Identity identity)
        {

            // If the profile hasn't been created, do that first. 
            if (profile == null)
            {
                profile = CreateFileProfile(appInfo);
            }

            // Create file settings object. Passing in empty string for the first parameter, engine ID, will cause the SDK to generate a GUID.
            // Locale settings are supported and should be provided based on the machine locale, particular for client applications.
            var engineSettings = new FileEngineSettings(identity.Email, authDelegate, "", "en-US")
            {
                // Provide the identity for service discovery.
                Cloud = Cloud.Commercial
            };

            // Add the IFileEngine to the profile and return.
            var engine = Task.Run(async () => await profile.AddEngineAsync(engineSettings)).Result;

            return engine;
        }

        private IFileHandler CreateFileHandler(FileOptions options)
        {
            // Create the handler using options from FileOptions. Assumes that the engine was previously created and stored in private engine object.
            // There's probably a better way to pass/store the engine, but this is a sample ;)
            var handler = Task.Run(async () => await engine.CreateFileHandlerAsync(options.FileName, options.FileName, options.IsAuditDiscoveryEnabled)).Result;
            return handler;
        }

        private IFileHandler CreateFileHandler(string filename)
        {
            // Create the handler using options from FileOptions. Assumes that the engine was previously created and stored in private engine object.
            // There's probably a better way to pass/store the engine, but this is a sample ;)
            var handler = Task.Run(async () => await engine.CreateFileHandlerAsync(filename, filename, false)).Result;
            return handler;
        }

        public void ListLabels()
        {
            // Get labels from the engine and return.
            // For a user principal, these will be user specific.
            // For a service principal, these may be service specific or global.
            var labels = engine.SensitivityLabels; 

            // Enumerate parent and child labels and print name / ID.
            foreach (var label in labels)
            {
                Console.WriteLine(string.Format("{0}: {1} - {2}", label.Sensitivity, label.Name, label.Id));

                if (label.Children.Count > 0)
                {
                    foreach (Label child in label.Children)
                    {
                        Console.WriteLine(string.Format("\t{0}: {1} - {2}", label.Sensitivity, child.Name, child.Id));
                    }
                }
            }

            
        }
        

        /// <summary>
        ///  This function demonstrates a "Workarond" for labeling multiple files in MIP SDK with the same PL. 
        ///  This may be useful when you have a compound file format that contains files with different labels, but you have a need to reuse the same 
        ///  PL across the files. 
        ///  
        ///  PL reuse has the benefits of decreasing service roundtrips for compound files. It will also cause all of the files using the same PL to be revoked if the PL is revoked. 
        /// </summary>
        /// <param name="fileToLabelMap"></param>
        public void LabelMultipleFiles(Dictionary<string,string> fileToLabelMap)
        {
            FileOptions options = new FileOptions()
            {
                ActionSource = ActionSource.Manual,
                AssignmentMethod = AssignmentMethod.Standard,
                DataState = DataState.Rest,
                GenerateChangeAuditEvent = true,
                IsAuditDiscoveryEnabled = true
            };

            LabelingOptions labelingOptions = new LabelingOptions()
            {
                AssignmentMethod = AssignmentMethod.Standard
            };

            ProtectionSettings protectionSettings = new ProtectionSettings()
            {
                PFileExtensionBehavior = PFileExtensionBehavior.Default
            };

            // Create a dictionary that maps each label to the file on disk that will be used as a template for the PL. 
            Dictionary<string, string> labelToProtectionFileTemplate = new Dictionary<string, string>();

            // Get the distinct list of all label IDs from our input set. 
            var distinctLabels = fileToLabelMap.Values.Distinct();
            
            // Iterate through the list of label IDs and generate a labeled and protected (if applicable) template file for each. 
            foreach (var label in distinctLabels)
            {
                // Build the temp file path from a plaintext template file. 
                options.FileName = @"D:\mip\testfiles\compound\input\temp.txt";
                options.OutputName = @"D:\mip\testfiles\compound\temp\" + label + ".txt";

                // Create a file handler and set the label for this template file. 
                var handler = CreateFileHandler(options);
                handler.SetLabel(engine.GetLabelById(label), labelingOptions, protectionSettings);
                var result = Task.Run(async () => await handler.CommitAsync(options.OutputName)).Result;

                // Write the labeled output file to the label-to-protected file map. 
                labelToProtectionFileTemplate.Add(label, options.OutputName);
            }


            // Iterate through all files in the input set. 
            foreach(var item in fileToLabelMap)
            {
                // Set the input, output file names, and the desired label id. 
                options.FileName = item.Key;
                //options.OutputName = Path.Combine(@"D:\mip\testfiles\compound\input\", Path.GetFileName(item.Key));
                options.LabelId = item.Value;

                // Build new input and output paths, using the temp paths.
                //options.FileName = options.OutputName;
                options.OutputName = Path.Combine(@"D:\mip\testfiles\compound\output\", Path.GetFileName(item.Key));

                // Create a file handler pointing to the input file.                 
                var handler = CreateFileHandler(options);

                // Fetch the path to the desired input file. 
                var fileTemplate = labelToProtectionFileTemplate[options.LabelId];

                // Fetch "source" protectionhandler from file template. 
                // The PL from this file will be used to create the protection on the other files of the same label. 
                // NOTE: I didn't test with a non-protected label, so be sure to catch any exceptions here or validate that the file should be protected at all. 
                var protectionHandlerTemplate = CreateFileHandler(fileTemplate).Protection;

        
                // Apply Label + Protection to File and store in temp directory. 
                // This operation isn't complete and the file needs to have protection re-applied from the template PL
                handler.SetLabel(engine.GetLabelById(options.LabelId), labelingOptions, protectionSettings);
                handler.SetProtection(protectionHandlerTemplate);
                var result = Task.Run(async () => await handler.CommitAsync(options.OutputName)).Result;
                
                
                

                // Create a handler for the final file output. 
                //var handler2 = CreateFileHandler(options);

                // Set protection using handler from template protection handler.
                //handler2.SetProtection(protectionHandlerTemplate);

                // Commit
                // At this point, you should have a file that is protected with the same PL as the template. 
                //result = Task.Run(async () => await handler2.CommitAsync(options.OutputName)).Result;

                // dump content Id for new item to validate
                // You should have a set of Content IDs and Label Ids. They should match across files (the same label across files will have the same content Id). 
                var validationHandler = CreateFileHandler(options.OutputName).Protection;
                Console.WriteLine("File Name: {0}\r\nLabel Id: {1}\r\nContent Id: {2}\r\n", options.OutputName, options.LabelId, validationHandler.ContentId);                
            }
                                                    
        }

    }
}
