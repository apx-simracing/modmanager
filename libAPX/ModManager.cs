using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace libAPX
{
    public class ModManager
    {
        public List<APXServer> getAPXEnabledServers()
        {
            List<APXServer> result = new List<APXServer>();

            // stub only
            APXServer test = new APXServer();
            test.Name = "[APX] aston"; 
            test.Host = "localhost";
            test.Port = 61290; //http port
            test.Session = "PRACTICE1";
            test.Track = "Malaysia North Loop";
            test.Vehicles.Add("AstonMartin_Vantage_GT3_2019");

            test.RecieverUrl = "http://localhost:8080";

            result.Add(test);

            return result;
        }
        public void deletePackageFile(string name)
        {
            if (name != null && name.Contains("rfcmp"))
            {
                File.Delete(@"F:\Steam\steamapps\common\rFactor 2\Packages\" + name);
            }
            
        }
        public bool installMod(string packagePath) { 


            ProcessStartInfo cmd = new ProcessStartInfo();
            cmd.WorkingDirectory = @"F:\Steam\steamapps\common\rFactor 2\";
            cmd.FileName = @"""F:\Steam\steamapps\common\rFactor 2\Bin64\ModMgr.exe""";
            cmd.UseShellExecute = true;
            if (Path.IsPathRooted(packagePath)){
                cmd.Arguments = @"-q -i""" + packagePath + "\"";
            } else
            {
                cmd.Arguments = @"-q -i""F:\Steam\steamapps\common\rFactor 2\Packages\" + packagePath + "\"";
            }
            cmd.CreateNoWindow = true;

            Process p = Process.Start(cmd);
            p.WaitForExit();

            return p.ExitCode == 0;
        }
        public bool deleteMod(Mod mod)
        {
            if (mod.Children.Count > 0)
            {
                return false;
            }

            if (mod.UsedBy.Count > 0)
            {
                foreach (String eventName in mod.UsedBy)
                {
                    String fullPath = @"F:\Steam\steamapps\common\rFactor 2\Manifests\" + eventName + ".mft";
                    File.Delete(fullPath);
                }
            }

            String path = String.Format(@"F:\Steam\steamapps\common\rFactor 2\Installed\{0}\{1}\{2}\",mod.Type, mod.Name, mod.Version);
            bool success = false;
            try
            {

                Directory.Delete(path, true);
                success =  !Directory.Exists(path);
            } catch (Exception e)
            {
                success = false;
            }
            return success;
        }
        public List<String> getPackages()
        {
            List<string> allPackages = new List<string>();
            string baseFolder = @"F:\Steam\steamapps\common\rFactor 2\Packages";
            foreach (string file in Directory.EnumerateFiles(baseFolder, "*.rfcmp", SearchOption.TopDirectoryOnly))
            {
                    FileInfo info = new FileInfo(file);
                    allPackages.Add(info.Name);
            }
            return allPackages;
        }
        public List<String> getManifests()
        {
            List<string> allManifests = new List<string>();
            string baseFolder = @"F:\Steam\steamapps\common\rFactor 2\Manifests";
            foreach (string file in Directory.EnumerateFiles(baseFolder, "*.mft", SearchOption.TopDirectoryOnly))
            {
                allManifests.Add(file);
            }
            return allManifests;
        }
        public List<Mod> getInstalledMods()
        {
            List<Mod> result = new List<Mod>();
            string baseFolder = @"F:\Steam\steamapps\common\rFactor 2\Installed";
            List<string> manifests = getManifestPaths(baseFolder);
            List<string> eventManifests = getManifests();
            foreach (string manifest in manifests)
            {
                // parse manifest
                Dictionary<string, object> iniContent = parseManifest(manifest);
                if (iniContent.Count > 0)
                {

                    Mod mod = new Mod();
                    try
                    {

                        mod.Name = (string)iniContent.GetValueOrDefault("Name");
                        mod.Signature = (string)iniContent.GetValueOrDefault("Signature");
                        mod.Version = (string)iniContent.GetValueOrDefault("Version");
                        mod.Type = (ModType)iniContent.GetValueOrDefault("Type");
                        mod.Children = new List<Mod>();
                        mod.UsedBy = new List<string>();
                        mod.BaseSignature = (string)iniContent.GetValueOrDefault("BaseSignature");
                    }
                    catch (Exception e)
                    {
                        // Might be missing baseSignature
                    }
                    // check if a manifest uses this mod
                    foreach(String eventManifest in eventManifests)
                    {
                        String content = File.ReadAllText(eventManifest);
                        if (content.Contains("Signature=" + mod.Signature))
                        {

                            FileInfo fInfo = new FileInfo(eventManifest);
                            mod.UsedBy.Add(fInfo.Name.Replace(".mft",""));
                        }
                    }
                    result.Add(mod);
                }
            }
            List<Mod> derivedMods = result.FindAll(l => l.BaseSignature != null);
            
            foreach (Mod derived in derivedMods)
            {
                string baseSignature = derived.BaseSignature;

                foreach (Mod parent in result)
                {

                    if (parent.Signature == baseSignature)
                    {
                        parent.Children.Add(derived);
                        break;
                    }
                }
            }
            /* the result is a mix from a tree and a flat list, for easier handling of the mods. An entry might have a set of children or nothing */
            result.RemoveAll(l => l.BaseSignature != null);
            return result;
        }

        public Dictionary<string, object> parseManifest(String path)
        {
            String[] needles = new string[] {"Name", "Version", "Signature", "BaseSignature", "Type"};
            Dictionary<string, object> result = new Dictionary<string, object>();
            using (StreamReader sr = new StreamReader(path))
            {
                string line, section = "";
                while ((line = sr.ReadLine()) != null)
                {
                    foreach(string needle in needles)
                    {
                        if (line.StartsWith(needle))
                        {
                            string[] parts = line.Split("=");
                            string value = parts[1];

                            if (needle == "Type")
                            {
                                result.Add(needle, (ModType)Int16.Parse(value));
                            } else
                            {

                                result.Add(needle, value);
                            }
                            break;
                        }
                    }
                }
            }
            return result;
        }
        private List<string> getManifestPaths(string rootFolder)
        {
            List<string> result = new List<string>();
            foreach (string file in Directory.EnumerateFiles(rootFolder, "*.*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".mft"))
                {
                    result.Add(file);
                }
            }
            return result;
        }
        public void revertTransaction()
        {
            throw new NotImplementedException();
        }

        public void runSimulation(string host, int targetPort)
        {
            ProcessStartInfo cmd = new ProcessStartInfo();
            cmd.WorkingDirectory = @"F:\Steam\steamapps\common\rFactor 2\";
            cmd.FileName = @"""F:\Steam\steamapps\common\rFactor 2\Bin64\rFactor2.exe""";
            cmd.UseShellExecute = false;
            cmd.Arguments = @"+trace=4 +multiplayer +connect "+host+":" + targetPort.ToString();
            cmd.CreateNoWindow = true;

            Process p = Process.Start(cmd);
            p.WaitForExit();
        }

        public TransactionResult installModPackage(String downloadUrl)
        {
            TransactionResult result = new TransactionResult();
            String rf2Root = @"F:\Steam\steamapps\common\rFactor 2";
            String rootPath = @"F:\Steam\steamapps\common\rFactor 2\apx\download";

            if (Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            using (var client = new WebClient())
            {
                string tempPath = Path.GetTempFileName();
                client.DownloadFile(downloadUrl, tempPath);

                /* unpack modpack */

                Stream inStream = File.OpenRead(tempPath);
                Stream gzipStream = new GZipInputStream(inStream);

                TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
                tarArchive.ExtractContents(rootPath);
                tarArchive.Close();

                gzipStream.Close();
                inStream.Close();

                File.Delete(tempPath);

               /* installMod the content */

                foreach(string kind in new string[] { "Installed\\Vehicles", "Installed\\Locations", "Installed\\rFm", "Manifests" })
                {
                    string path = @"F:\Steam\steamapps\common\rFactor 2\apx\download\" + kind;

                    if (Directory.Exists(path))
                    {
                        if (kind.Contains("Vehicles") || kind.Contains("Locations"))
                        {
                            string[] componentToInstall = Directory.GetDirectories(path);

                            foreach (string component in componentToInstall)
                            {
                                string[] versionsToInstall = Directory.GetDirectories(component);
                                foreach (string version in versionsToInstall)
                                {

                                    String relativePath = version.Replace(path, "");
                                    String targetPath = rf2Root + "\\" + kind + relativePath;
                                    if (Directory.Exists(targetPath))
                                    {
                                        Directory.Delete(targetPath, true);
                                    }
                                    Directory.Move(version, targetPath);
                                    result.installedComponents.Add(targetPath);
                                }
                            }
                        } else
                        {
                            string[] filesToInstall = Directory.GetFiles(path);
                            foreach(string file in filesToInstall)
                            {
                                String relativePath = file.Replace(path, "");
                                String targetPath = rf2Root + "\\" + kind + relativePath;
                                result.installedComponents.Add(targetPath);

                                File.Move(file, targetPath, true);
                            }
                           
                        }
                    
                    }
                }

            }
            return result;
        }
        public void commitTransaction(List<string> foldersToKeep)
        {

            String rootPath = @"F:\Steam\steamapps\common\rFactor 2\";

            if (!Directory.Exists(rootPath + "apx"))
            {
                // Create backup folders
                Directory.CreateDirectory(rootPath + "apx");
            }


            string[] allFoldersVehicles = Directory.GetDirectories(rootPath + @"\Installed\Vehicles");
            string[] allFoldersLocations = Directory.GetDirectories(rootPath + @"\Installed\Locations");

            foreach(String folder in foldersToKeep)
            {
                DirectoryInfo parent = Directory.GetParent(folder); // E. g. ASton_Martin_GT3_2019

                String[] allFolders = Directory.GetDirectories(parent.FullName);

                foreach (String children in allFolders)
                {
                    if (!foldersToKeep.Contains(children))
                    {
                        // move it away

                        DirectoryInfo folderRemovalInfo = new DirectoryInfo(children);

                        if (!Directory.Exists(rootPath + "apx" + "\\" + parent.Name + "\\"))
                        {
                            Directory.CreateDirectory(rootPath + "apx" + "\\" + parent.Name + "\\");
                        }

                        if (!Directory.Exists(rootPath + "apx" + "\\" + parent.Name + "\\" + folderRemovalInfo.Name))
                        {
                            Directory.Move(folder, rootPath + "apx" + "\\" + parent.Name + "\\" + folderRemovalInfo.Name);
                        }

                    }
                }
            }
        }

        public List<String> getFoldersToKeep(String apxUrl)
        {
            WebClient test = new WebClient();
            String content = test.DownloadString(apxUrl);
            dynamic signatures = JsonConvert.DeserializeObject(content);

            dynamic mod = signatures.mod;

            JArray fileSignatures = signatures.signatures;


            // Check rFactor 2 for mods suitable to the BaseSignatur
            List<String> foldersToKeep = new List<string>();
            Dictionary<string, Mod> seenManifests = new Dictionary<string, Mod>();

            foreach (JObject foundMod in fileSignatures)
            {
                String name = (String)foundMod.GetValue("Name");
                Boolean isVehicle = (int)foundMod.GetValue("Type") == 2;

                String path = "Installed\\" + (isVehicle ? "Vehicles" : "Locations") + "\\" + name;
                if (foundMod.ContainsKey("BaseSignature"))
                {
                    String rootPath = @"F:\Steam\steamapps\common\rFactor 2\" + path;



                    String baseSignature = (String)foundMod.GetValue("BaseSignature");
                    String[] allManifests = Directory.GetFiles(rootPath, "*.mft", SearchOption.AllDirectories);


                    List<String> signatureNeedles = new List<string>();
                    /* collect all mod infos */
                    foreach (string file in Directory.GetFiles(rootPath, "*.mft", SearchOption.AllDirectories))
                    {
                        Dictionary<string, object> iniContent = this.parseManifest(file);

                        string modSignature = (string)iniContent.GetValueOrDefault("Signature");
                        String manifestBaseSignature = (string)iniContent.GetValueOrDefault("BaseSignature");

                        String parentPath = Directory.GetParent(file).FullName;
                        if (!seenManifests.ContainsKey(parentPath))
                        {
                            Mod manifestMod = new Mod();
                            manifestMod.BaseSignature = manifestBaseSignature;
                            manifestMod.Signature = modSignature;
                            manifestMod.Name = Directory.GetParent(parentPath).Name;
                            manifestMod.Version = Directory.GetParent(file).Name;
                            seenManifests.Add(parentPath, manifestMod);
                        }
                    }

                    /* Identify the tree */
                    do {
                        foreach (KeyValuePair<string, Mod> kvp in seenManifests)
                        {
                            if (baseSignature == null)
                            {
                                break;
                            }
                            String manifestSignature = kvp.Value.Signature;
                            String manifestBaseSignature = kvp.Value.BaseSignature;

                            if (manifestSignature == baseSignature)
                            {
                                foldersToKeep.Add(kvp.Key);
                                baseSignature = manifestBaseSignature;
                            }

                        }
                    }while(baseSignature != null);
                }
            }
            return foldersToKeep;
        }
    }
}
