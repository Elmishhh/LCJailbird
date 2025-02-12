using BepInEx;
using BepInEx.Logging;
using System.IO;
using System.Reflection;
using UnityEngine;
using LethalLib;
using LethalLib.Modules;
using System.Collections;

namespace LCJailbird
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private Item jailbirditemprops;

        private void Patch()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            LoadAssetBundle();

            Patch();
        }

        private void LoadAssetBundle()
        {
            using (Stream bundlestream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LCJailbird.Resources.jailbirdassets"))
            {
                AssetBundle bundle = AssetBundle.LoadFromStream(bundlestream);
                if (bundle == null)
                {
                    Logger.LogError("JAILBIRD BUNDLE IS NULL");
                    return;
                }
                jailbirditemprops = bundle.LoadAsset<Item>("JailbirdItemProps");
                if (jailbirditemprops == null)
                {
                    Logger.LogError("JAILBIRD ITEM IS NULL");
                    return;
                }

                JailbirdShovel jbitem = jailbirditemprops.spawnPrefab.AddComponent<JailbirdShovel>();
                jbitem.itemProperties = jailbirditemprops;
                jbitem.grabbable = true;
                jbitem.reelUpSFX = bundle.LoadAsset<AudioClip>("Charge_Start_fixed");
                jbitem.swingSFX = bundle.LoadAsset<AudioClip>("Charge_Swing_fixed");
                jbitem.chargeSFX = bundle.LoadAsset<AudioClip>("Charge_Run_fixed");
                jbitem.hitSFX = new AudioClip[1];
                jbitem.hitSFX[0] = bundle.LoadAsset<AudioClip>("Normal_Hit");
                jbitem.jailbirdAudio = jailbirditemprops.spawnPrefab.transform.GetComponent<AudioSource>();

                jailbirditemprops.twoHandedAnimation = true;
                jailbirditemprops.grabAnim = "HoldLung";
                jailbirditemprops.holdButtonUse = true;

                jailbirditemprops.spawnPrefab.transform.GetChild(0).transform.localPosition = new Vector3(0, 0, -0.05f); // read the next comment on why i'm doing this
                jailbirditemprops.spawnPrefab.transform.GetChild(0).transform.localRotation = Quaternion.Euler(270, 290, 0); // for those who are unfortunate enough to read my code, i'm sorry i could not be bothered to build and replace the bundle every 2 seconds when i'm tweaking the rotation
            }
            Utilities.FixMixerGroups(jailbirditemprops.spawnPrefab);

            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "???";
            Items.RegisterShopItem(jailbirditemprops, null, null, node, 500);
            NetworkPrefabs.RegisterNetworkPrefab(jailbirditemprops.spawnPrefab);

            Logger.LogInfo("successfully set-up item");
        }
    }
}
