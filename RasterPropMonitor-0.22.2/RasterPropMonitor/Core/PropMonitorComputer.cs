using System;
using System.Collections.Generic;
using UnityEngine;

namespace JSI
{
    public class RasterPropMonitorComputer : PartModule
    {
        // The only public configuration variable.
        [KSPField]
        public bool debugLogging = true;

        // The OTHER public configuration variable.
        [KSPField]
        public string storedStrings = string.Empty;
        private string[] storedStringsArray;

        // Persistence for internal modules.
        [KSPField(isPersistant = true)]
        public string data = "";
        private Dictionary<string, int> persistentVars = new Dictionary<string, int>();

        // Yes, it's a really braindead way of doing it, but I ran out of elegant ones,
        // because nothing appears to work as documented -- IF it's documented.
        // This one is sure to work and isn't THAT much of a performance drain, really.
        // Pull requests welcome
        // Vessel description storage and related code.
        [KSPField(isPersistant = true)]
        public string vesselDescription = string.Empty;
        private string vesselDescriptionForDisplay = string.Empty;
        private readonly string editorNewline = ((char)0x0a).ToString();
        private string lastVesselDescription = string.Empty;

        // Public functions:
        // Request the instance, create it if one doesn't exist:
        public static RasterPropMonitorComputer Instantiate(MonoBehaviour referenceLocation)
        {
            var thatProp = referenceLocation as InternalProp;
            var thatPart = referenceLocation as Part;
            if (thatPart == null)
            {
                if (thatProp == null)
                {
                    throw new ArgumentException("Cannot instantiate RPMC in this location.");
                }
                thatPart = thatProp.part;
            }
            for (int i = 0; i < thatPart.Modules.Count; i++)
            {
                if (thatPart.Modules[i].ClassName == typeof(RasterPropMonitorComputer).Name)
                {
                    return thatPart.Modules[i] as RasterPropMonitorComputer;
                }
            }
            return thatPart.AddModule(typeof(RasterPropMonitorComputer).Name) as RasterPropMonitorComputer;
        }

        // Page handler interface for vessel description page.
        // Analysis disable UnusedParameter
        public string VesselDescriptionRaw(int screenWidth, int screenHeight)
        {
            // Analysis restore UnusedParameter
            return vesselDescriptionForDisplay.UnMangleConfigText();
        }

        // Analysis disable UnusedParameter
        public string VesselDescriptionWordwrapped(int screenWidth, int screenHeight)
        {
            // Analysis restore UnusedParameter
            return JUtil.WordWrap(vesselDescriptionForDisplay.UnMangleConfigText(), screenWidth);
        }


        public void Start()
        {
            JUtil.LogMessage(this, "Setting RasterPropMonitor debugging to {0}", debugLogging);
            JUtil.debugLoggingEnabled = debugLogging;

            if (!HighLogic.LoadedSceneIsEditor)
            {
                RPMVesselComputer comp = RPMVesselComputer.Instance(vessel);
                comp.SetVesselDescription(vesselDescription);

                // Make sure we have the description strings parsed.
                string[] descriptionStrings = vesselDescription.UnMangleConfigText().Split(JUtil.LineSeparator, StringSplitOptions.None);
                for (int i = 0; i < descriptionStrings.Length; i++)
                {
                    if (descriptionStrings[i].StartsWith("AG", StringComparison.Ordinal) && descriptionStrings[i][3] == '=')
                    {
                        uint groupID;
                        if (uint.TryParse(descriptionStrings[i][2].ToString(), out groupID))
                        {
                            descriptionStrings[i] = string.Empty;
                        }
                    }
                }
                vesselDescriptionForDisplay = string.Join(Environment.NewLine, descriptionStrings).MangleConfigText();

                // Now let's parse our stored strings...
                if (!string.IsNullOrEmpty(storedStrings))
                {
                    storedStringsArray = storedStrings.Split('|');
                }

                ParseData();
            }
        }

        #region Persistence
        private void ParseData()
        {
            persistentVars.Clear();
            if (!string.IsNullOrEmpty(data))
            {
                string[] varstring = data.Split('|');
                for (int i = 0; i < varstring.Length; ++i)
                {
                    string[] tokens = varstring[i].Split('$');
                    int value;
                    if (tokens.Length == 2 && int.TryParse(tokens[1], out value))
                    {
                        persistentVars.Add(tokens[0], value);
                    }
                }

                if (JUtil.debugLoggingEnabled)
                {
                    JUtil.LogMessage(this, "Parsed persistence string 'data' into {0} entries", persistentVars.Count);
                }
            }
        }

        private void StoreData()
        {
            var tokens = new List<string>();
            foreach (KeyValuePair<string, int> item in persistentVars)
            {
                tokens.Add(item.Key + "$" + item.Value);
            }

            data = string.Join("|", tokens.ToArray());
        }

        internal bool GetBool(string persistentVarName, bool defaultValue)
        {
            if (persistentVars.ContainsKey(persistentVarName))
            {
                int value = GetVar(persistentVarName);
                return (value > 0);
            }
            else
            {
                return defaultValue;
            }
        }

        internal int GetVar(string persistentVarName, int defaultValue)
        {
            if (persistentVars.ContainsKey(persistentVarName))
            {
                return persistentVars[persistentVarName];
            }
            else
            {
                return defaultValue;
            }
        }

        internal int GetVar(string persistentVarName)
        {
            try
            {
                return persistentVars[persistentVarName];
            }
            catch
            {
                JUtil.LogErrorMessage(this, "Someone called GetVar({0}) without making sure the value existed", persistentVarName);
            }

            return int.MinValue;
        }

        internal bool HasVar(string persistentVarName)
        {

            return persistentVars.ContainsKey(persistentVarName);
        }

        internal void SetVar(string persistentVarName, int varvalue)
        {
            if (persistentVars.ContainsKey(persistentVarName))
            {
                persistentVars[persistentVarName] = varvalue;
            }
            else
            {
                persistentVars.Add(persistentVarName, varvalue);
            }

            StoreData();
        }

        internal void SetVar(string persistentVarName, bool varvalue)
        {
            SetVar(persistentVarName, varvalue ? 1 : 0);
        }

        
        internal string GetStoredString(int index)
        {
            if (storedStringsArray != null && index <= storedStringsArray.Length)
            {
                return storedStringsArray[index];
            }
            else
            {
                return "";
            }
        }
        #endregion

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                // well, it looks sometimes it might become null..
                string s = EditorLogic.fetch.shipDescriptionField != null ? EditorLogic.fetch.shipDescriptionField.Text : string.Empty;
                if (s != lastVesselDescription)
                {
                    lastVesselDescription = s;
                    // For some unclear reason, the newline in this case is always 0A, rather than Environment.NewLine.
                    vesselDescription = s.Replace(editorNewline, "$$$");
                }
            }
        }
    }

}
