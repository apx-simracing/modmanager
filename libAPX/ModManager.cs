using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace libAPX
{
    public class ModManager
    {
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
    }
}
