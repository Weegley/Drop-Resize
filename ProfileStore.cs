using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace DropResize
{
    public class ProfileStore
    {
        private readonly string filePath;
        private readonly string settingsPath;

        public ProfileStore()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Drop&Resize");
            Directory.CreateDirectory(folder);
            filePath = Path.Combine(folder, "profiles.xml");
            settingsPath = Path.Combine(folder, "settings.xml");
            MigrateLegacyFiles();
        }

        public List<ResizeProfile> Load()
        {
            if (!File.Exists(filePath))
            {
                var defaults = ResizeProfile.CreateBuiltInProfiles();
                Save(defaults);
                return defaults;
            }

            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var serializer = new XmlSerializer(typeof(List<ResizeProfile>));
                    var loaded = (List<ResizeProfile>)serializer.Deserialize(stream);
                    return MergeWithBuiltInDefaults(loaded);
                }
            }
            catch
            {
                // Corrupt profile data should not prevent the app from opening.
                return ResizeProfile.CreateBuiltInProfiles();
            }
        }

        public void Save(List<ResizeProfile> profiles)
        {
            using (var stream = File.Create(filePath))
            {
                var serializer = new XmlSerializer(typeof(List<ResizeProfile>));
                serializer.Serialize(stream, profiles);
            }
        }

        public ResizeProfile GetDefaultBuiltIn(string id)
        {
            return ResizeProfile.CreateBuiltInProfiles().FirstOrDefault(p => p.Id == id);
        }

        public string LoadLastProfileId()
        {
            if (!File.Exists(settingsPath))
            {
                return null;
            }

            try
            {
                using (var stream = File.OpenRead(settingsPath))
                {
                    var serializer = new XmlSerializer(typeof(ProfileStoreSettings));
                    return ((ProfileStoreSettings)serializer.Deserialize(stream)).LastProfileId;
                }
            }
            catch
            {
                // Missing or corrupt settings are non-fatal; the first profile will be selected.
                return null;
            }
        }

        public void SaveLastProfileId(string profileId)
        {
            using (var stream = File.Create(settingsPath))
            {
                var serializer = new XmlSerializer(typeof(ProfileStoreSettings));
                serializer.Serialize(stream, new ProfileStoreSettings { LastProfileId = profileId });
            }
        }

        private static List<ResizeProfile> MergeWithBuiltInDefaults(List<ResizeProfile> loaded)
        {
            var result = loaded ?? new List<ResizeProfile>();
            var defaults = ResizeProfile.CreateBuiltInProfiles();
            for (var i = 0; i < defaults.Count; i++)
            {
                var builtIn = defaults[i];
                if (!result.Any(p => p.Id == builtIn.Id))
                {
                    result.Insert(Math.Min(result.Count, i), builtIn);
                }
            }

            foreach (var profile in result)
            {
                if (string.IsNullOrEmpty(profile.Id))
                {
                    profile.Id = Guid.NewGuid().ToString("N");
                }

                if (defaults.Any(p => p.Id == profile.Id))
                {
                    profile.IsBuiltIn = true;
                }

                profile.ResizeMode = ResizeMode.Fit;
                if (string.IsNullOrWhiteSpace(profile.Suffix))
                {
                    profile.Suffix = "_resized";
                }
            }

            return result;
        }

        private void MigrateLegacyFiles()
        {
            var legacyFolder = AppDomain.CurrentDomain.BaseDirectory;
            CopyLegacyFile(Path.Combine(legacyFolder, "profiles.xml"), filePath);
            CopyLegacyFile(Path.Combine(legacyFolder, "settings.xml"), settingsPath);
        }

        private static void CopyLegacyFile(string sourcePath, string targetPath)
        {
            if (!File.Exists(sourcePath) || File.Exists(targetPath))
            {
                return;
            }

            try
            {
                File.Copy(sourcePath, targetPath);
            }
            catch
            {
                // Legacy migration is best-effort; defaults are still available.
            }
        }
    }

    public class ProfileStoreSettings
    {
        public string LastProfileId { get; set; }
    }
}
