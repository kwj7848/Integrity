using System;
using System.IO;
using UnityEngine;

namespace Integrity.Editor
{
    [Serializable]
    public class IntegritySettings
    {
        const string SettingsPath = "ProjectSettings/IntegritySettings.json";

        // Inspector Reference Check
        public bool validateSerializeFields = true;
        public bool blockPlayOnMissing = true;
        public bool blockBuildOnMissing = true;

        static IntegritySettings _instance;

        public static IntegritySettings Instance
        {
            get
            {
                if (_instance == null)
                    Load();
                return _instance;
            }
        }

        static void Load()
        {
            _instance = new IntegritySettings();
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _instance = JsonUtility.FromJson<IntegritySettings>(json);
            }
        }

        public static void Save()
        {
            var json = JsonUtility.ToJson(Instance, true);
            File.WriteAllText(SettingsPath, json);
        }
    }
}
