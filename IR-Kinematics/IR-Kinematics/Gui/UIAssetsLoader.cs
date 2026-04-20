using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;


namespace IR_Kinematics.Gui
{
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class UIAssetsLoader : MonoBehaviour
	{
		internal static AssetBundle IRAssetBundle;

		// gizmo
		internal static GameObject gizmo;

		// windows
		internal static GameObject controlWindowPrefab;
		internal static GameObject controlWindowGroupLinePrefab;

		// popups
		internal static GameObject uiSettingsWindowPrefab;

		internal static GameObject basicTooltipPrefab;

		// images and icons
		internal static List<Texture2D> iconAssets;
		internal static List<UnityEngine.Sprite> spriteAssets;
		
		public static bool allPrefabsReady = false;

		public IEnumerator LoadBundle(string location)
		{
			while(!Caching.ready)
				yield return null;

			using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(location))
			{
				yield return www.SendWebRequest();

				IRAssetBundle = DownloadHandlerAssetBundle.GetContent(www);

				LoadBundleAssets();
			}
		}
		
		private void LoadBundleAssets()
		{
			var prefabs = IRAssetBundle.LoadAllAssets<GameObject>();
			int prefabCounter = 0;

			for(int i = 0; i < prefabs.Length; i++)
			{
				if(prefabs[i].name == "pointer_test")
					gizmo =  prefabs[i] as GameObject;

				if(prefabs[i].name == "FlightWindowPrefab")
				{
					controlWindowPrefab = prefabs[i] as GameObject;
					prefabCounter++;
					Logger.Log("Successfully loaded control window prefab", Logger.Level.Debug);
				}

				if(prefabs[i].name == "FlightWindowGroupLinePrefab")
				{
					controlWindowGroupLinePrefab = prefabs[i] as GameObject;
					prefabCounter++;
					Logger.Log("Successfully loaded control window Group prefab", Logger.Level.Debug);
				}

				if(prefabs[i].name == "UISettingsWindowPrefab")
				{
					uiSettingsWindowPrefab = prefabs[i] as GameObject;
					prefabCounter++;
					Logger.Log("Successfully loaded UI settings window prefab", Logger.Level.Debug);
				}

				if(prefabs[i].name == "BasicTooltipPrefab")
				{
					basicTooltipPrefab = prefabs[i] as GameObject;
					prefabCounter++;
					Logger.Log("Successfully loaded BasicTooltipPrefab", Logger.Level.Debug);
				}
			}

			allPrefabsReady = (prefabCounter > 3);

			spriteAssets = new List<UnityEngine.Sprite>();
			var sprites = IRAssetBundle.LoadAllAssets<UnityEngine.Sprite>();

			for(int i = 0; i < sprites.Length; i++)
			{
				if(sprites[i] != null)
				{
					spriteAssets.Add(sprites[i]);
					Logger.Log("Successfully loaded Sprite " + sprites[i].name, Logger.Level.Debug);
				}
			}

			iconAssets = new List<Texture2D>();
			var icons = IRAssetBundle.LoadAllAssets<Texture2D>();

			for(int i = 0; i < icons.Length; i++)
			{
				if(icons[i] != null)
				{
					iconAssets.Add(icons[i]);
					Logger.Log("Successfully loaded texture " + icons[i].name, Logger.Level.Debug);
				}
			}
		}

		public void Start()
		{
			if(allPrefabsReady)
				return;

			var assemblyFile = Assembly.GetExecutingAssembly().Location;
			var bundlePath = "file://" + assemblyFile.Replace(new FileInfo(assemblyFile).Name, "").Replace("\\","/") + "../AssetBundles/";

			Logger.Log("Loading bundles from BundlePath: " + bundlePath, Logger.Level.Debug);

			Caching.ClearCache();

			StartCoroutine(LoadBundle(bundlePath + "ir_ik_ui_objects.ksp"));
		}

		public void OnDestroy()
		{
		}
	}
}
