using System.IO;
using BallisticSource.Mods;
using UnityEngine;

namespace BallisticNG.CodeMods
{
    /// <summary>
    /// The register class for the custom shield mod.
    /// </summary>
    public class CustomShieldRegister : ModRegister
    {
        public override void OnRegistered()
        {
            RegisterMod("Custom Shield", "Adam Chivers", "1.0");

            /*---Create the custom shield behaviour object and make sure it isn't destroyed on scene loads---*/
            GameObject customShield = new GameObject("[ Custom Shield Mod ]");
            Object.DontDestroyOnLoad(customShield);
            
            /*---Attach the shield behaviour to our new object and provide it the location it will use for the settings ini---*/
            CustomShieldBehaviour shieldBehaviour = customShield.AddComponent<CustomShieldBehaviour>();
            shieldBehaviour.IniLocation = Path.Combine(ModLocation, "Shield Settings.ini");

        }
    }

    /// <summary>
    /// Handles the re-generation of the shield with our specified settings.
    /// </summary>
    public class CustomShieldBehaviour : MonoBehaviour
    {
        /*---Ini Settings---*/
        public string IniLocation;
        public bool CustomShieldActive = true;
        public bool UseTeamColor = false;
        public Color CustomShieldColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);

        /// <summary>
        /// This is a Unity method called when this MonoBehaviour has started for the first time.
        /// </summary>
        private void Start()
        {
            LoadSettings();

            BallisticEvents.Race.OnShipSpawned += OnShipSpawned;
        }

        /// <summary>
        /// Called whenever a ship has just been spawned.
        /// </summary>
        /// <param name="ship"></param>
        private void OnShipSpawned(ShipRefs ship)
        {
            /*---Do nothing if the mod isn't activated---*/
            if (!CustomShieldActive) return;

            /*---Apply the custom color---*/
            ship.Effects.TargetShieldColor = UseTeamColor ? ship.Settings.REF_ENGINECOL_BRIGHT : CustomShieldColor;
        }

        /// <summary>
        /// Loads the mods settings from the ini file.
        /// </summary>
        private void LoadSettings()
        {
            if (!File.Exists(IniLocation))
            {
                CreateSettings();
                return;
            }

            INIParser ini = new INIParser();
            ini.Open(IniLocation);

            CustomShieldActive = ini.ReadValue("Settings", "Mod Active", CustomShieldActive);
            UseTeamColor = ini.ReadValue("Settings", "Use Team Color", UseTeamColor);
            CustomShieldColor.r = (float)ini.ReadValue("Settings", "Custom Shield Color R", CustomShieldColor.r);
            CustomShieldColor.g = (float)ini.ReadValue("Settings", "Custom Shield Color G", CustomShieldColor.g);
            CustomShieldColor.b = (float)ini.ReadValue("Settings", "Custom Shield Color B", CustomShieldColor.b);
            CustomShieldColor.a = (float)ini.ReadValue("Settings", "Custom Shield Color A", CustomShieldColor.a);

            ini.Close();
        }

        /// <summary>
        /// Creates the settings ini file.
        /// </summary>
        private void CreateSettings()
        {
            INIParser ini = new INIParser();
            ini.Open(IniLocation);

            ini.WriteValue("Settings", "Mod Active", CustomShieldActive);
            ini.WriteValue("Settings", "Use Team Color", UseTeamColor);
            ini.WriteValue("Settings", "Custom Shield Color R", CustomShieldColor.r);
            ini.WriteValue("Settings", "Custom Shield Color G", CustomShieldColor.g);
            ini.WriteValue("Settings", "Custom Shield Color B", CustomShieldColor.b);
            ini.WriteValue("Settings", "Custom Shield Color A", CustomShieldColor.a);

            ini.Close();
        }
    }
}
