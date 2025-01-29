using BepInEx;
using BepInEx.Logging;
using System.Reflection;
using MonoMod.RuntimeDetour.HookGen;
using AlwaysPlayFinalDay.Patches;
using MyceliumNetworking;
using Zorro.Settings;
using UnityEngine;

namespace AlwaysPlayFinalDay;

// since this alters the gameplay experience by playing through the final day,
// I have set it to "not vanilla"
[ContentWarningPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION, false)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class FixR2Modman : BaseUnityPlugin
{
    const uint myceliumNetworkModId = 61813; // meaningless, as long as it is the same between all the clients
    internal new static ManualLogSource Logger { get; private set; } = null!;

    private void Awake()
    {
        // Jan 2025 - make sure CW update doesnt destroy this mod /////////////////
        Logger = base.Logger;

        var gameObject = new GameObject("AlwaysPlayFinalDayPluginR2ModMan")
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        gameObject.AddComponent<AlwaysPlayFinalDay>();

        AlwaysPlayFinalDay.Instance.SetLogger(base.Logger);

        DontDestroyOnLoad(gameObject);
        ////////////////////////////////////////////////////////////////////////////
    }
}

public class AlwaysPlayFinalDay : MonoBehaviour // prev. BaseUnityPlugin
{
    // Actual mod logic

    const uint myceliumNetworkModId = 61812; // meaningless, as long as it is the same between all the clients
    public static AlwaysPlayFinalDay Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;

    public bool PlayFinalDayEvenIfQuotaNotMet { get; private set; }

    public bool Debug_InitSurfaceActive; // helper boolean

    // Jan 2025 - make sure CW update doesnt destroy this mod /////////////////
    public void SetLogger(ManualLogSource logger)
    {
        Logger = logger;
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }
    ////////////////////////////////////////////////////////////////////////////
    

    private void Awake()
    {
        // Logger = base.Logger;
        Instance = this;

        HookAll();
    }

    private void Start()
    {
        MyceliumNetwork.RegisterNetworkObject(Instance, myceliumNetworkModId);
    }

    void OnDestroy()
    {
        MyceliumNetwork.DeregisterNetworkObject(Instance, myceliumNetworkModId);
    }

    internal static void HookAll()
    {
        SurfaceNetworkHandlerPatch.Init();
        PhotonGameLobbyHandlerPatch.Init();
    }

    internal static void UnhookAll()
    {
        HookEndpointManager.RemoveAllOwnedBy(Assembly.GetExecutingAssembly());
    }

    public bool IsFinalDayAndQuotaNotMet()
    {
        return SurfaceNetworkHandler.RoomStats != null && SurfaceNetworkHandler.RoomStats.IsQuotaDay && !SurfaceNetworkHandler.RoomStats.CalculateIfReachedQuota();
    }

    public void SetPlayFinalDayEvenIfQuotaNotMet(bool playFinalDayEvenIfQuotaNotMet)
    {
        PlayFinalDayEvenIfQuotaNotMet = playFinalDayEvenIfQuotaNotMet;
    }

    [ContentWarningSetting]
    public class EnableAllowCrewToWatchFootageEvenIfQuotaNotMetSetting : BoolSetting, IExposedSetting
    {
        public SettingCategory GetSettingCategory() => SettingCategory.Mods;

        public override void ApplyValue()
        {
            AlwaysPlayFinalDay.Instance.SetPlayFinalDayEvenIfQuotaNotMet(Value);
        }
        //[Warning: Unity Log] Failed to load setting of type AlwaysPlayFinalDay.AlwaysPlayFinalDay+EnableAllowCrewToWatchFootageEvenIfQuotaNotMetSetting from PlayerPrefs.
        public string GetDisplayName() => "AlwaysPlayFinalDay: Allow crew to view their camera footage on final day, even if the footage won't reach quota (uses the host's game settings) \nWithout this setting, the third day ends immediately.";

        protected override bool GetDefaultValue() => true;
    }
}
