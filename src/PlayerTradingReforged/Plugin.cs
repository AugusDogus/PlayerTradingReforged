using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace PlayerTradingReforged
{
    [Serializable]
    public class StringLocalization
    {
        public string TradePending = "Trade confirmation pending. Items are held safely until the other player reconnects.";
        public string TradeInvalid = "Trade cancelled: the offered items could not be read. Both players need the same mod and item versions.";
        public string ReturnedItems = "Trade items returned. Make inventory space to recover any remaining items.";
        public string RequestTrade = "Request Trade";
        public string AcceptRequest = "Accept Trade";
        public string TradeRequestSent = "Trade request sent";
        public string TradeRecentlySent = "Trade request recently sent";
        public string StartedTradeWithX = "Started trading with";
        public string XWantsToTrade = "wants to trade";
        public string XHasCancelledTrade = "has cancelled the trade";
        public string LocalPlayerCancelledTrade = "You have cancelled the trade";
        public string CantStartNewTradeInstance = "You cannot start a new trade session";
        public string TradeSuccessful = "Trade successful";
        public string NotEnoughInventorySlots = "Not enough inventory slots";
        public string ToGiveWindowText = "You Will Give";
        public string ToReceiveWindowText = "You Will Receive";
        public string PartnerInventoryText = "Partner's Inventory";
        public string ReservedForTradeText = "{0} reserved for trade";
        public string YourInventoryText = "Your Inventory";
        public string NamedInventoryText = "{0}’s Inventory";
        public string ProjectedWeightText = "After trade";
        public string PartnerInventoryWaiting = "Waiting for Partner's Inventory";
        public string PartnerInventoryUnavailable = "Partner's Inventory Unavailable";
        public string AcceptTradeButtonText = "Accept Trade";
        public string ChangeTradeButtonText = "Change Trade";
        public string CancelTradeButtonText = "Cancel Trade";
        public string EditModeOn = "Edit UI Mode ON";
        public string EditModeOff = "Edit UI Mode OFF";
    }

    [BepInPlugin(PluginId, "Player Trading Reforged", PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginId = "augusdogus.mods.PlayerTradingReforged";
        public const string PluginVersion = "1.2.2";
        private static Plugin? _instance;
        private readonly Harmony _harmony = new Harmony(PluginId);
        private TradeHandler? _handler;
        private ZRoutedRpc? _network;
        public static StringLocalization Localization { get; private set; } = new StringLocalization();
        internal static event Action? OnLocalPlayerChanged;
        private static ConfigFile Settings => (_instance ?? throw new InvalidOperationException("Player Trading Reforged has not initialized.")).Config;
        public static ConfigEntry<bool> UseModifierKey => Settings.Bind("Keybinds", "useModifierKey", false, "Require a modifier when requesting or accepting a trade.");
        public static ConfigEntry<KeyCode> ModifierKey => Settings.Bind("Keybinds", "modifierKey", KeyCode.LeftAlt, "Trade modifier key.");
        public static ConfigEntry<KeyCode> EditWindowLayoutKey => Settings.Bind("Keybinds", "editWindowLayoutKey", KeyCode.F11, "Toggle window placement mode.");
        public static ConfigEntry<Vector2> ToGiveUserOffset => Offset("toGiveUserOffset");
        public static ConfigEntry<Vector2> ToReceiveUserOffset => Offset("toReceiveUserOffset");
        public static ConfigEntry<Vector2> PartnerInventoryUserOffset => Offset("partnerInventoryUserOffset");
        public static ConfigEntry<Vector2> AcceptButtonUserOffset => Offset("acceptButtonUserOffset");
        public static ConfigEntry<Vector2> CancelButtonUserOffset => Offset("cancelButtonUserOffset");
        private static ConfigEntry<Vector2> Offset(string key) => Settings.Bind("Offsets", key, Vector2.zero, "Trade UI position offset. Set to 0, 0 to reset.");

        private void Awake()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            { enabled = false; return; }
            _instance = this;
            _ = UseModifierKey; _ = ModifierKey; _ = EditWindowLayoutKey;
            _ = ToGiveUserOffset; _ = ToReceiveUserOffset; _ = AcceptButtonUserOffset; _ = CancelButtonUserOffset;
            _ = PartnerInventoryUserOffset;
            InitLocalization();
            _harmony.PatchAll(typeof(Plugin).Assembly);
        }

        private void InitLocalization()
        {
            string path = Path.Combine(Paths.ConfigPath, PluginId + ".strings.json");
            try
            {
                Directory.CreateDirectory(Paths.ConfigPath);
                if (File.Exists(path)) JsonUtility.FromJsonOverwrite(File.ReadAllText(path), Localization);
                else File.WriteAllText(path, JsonUtility.ToJson(Localization, true));
                if (Localization.PartnerInventoryText == "Partner's Inventory (Read Only)")
                    Localization.PartnerInventoryText = new StringLocalization().PartnerInventoryText;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Localization = new StringLocalization();
                Logger.LogWarning($"Cannot load localization at {path}: {error.Message}. Using English. Your file is unchanged; correct its JSON or file permissions and restart.");
            }
        }

        private void Update()
        {
            if (_handler != null && (_network != ZRoutedRpc.instance || !Player.m_localPlayer || !InventoryGui.instance))
            {
                _handler.Shutdown();
                Destroy(_handler.gameObject);
                _handler = null;
            }
            if (_handler != null || !Player.m_localPlayer || !InventoryGui.instance || ZRoutedRpc.instance == null) return;
            _network = ZRoutedRpc.instance;
            _handler = new GameObject("PlayerTradingReforged").AddComponent<TradeHandler>();
        }

        private void OnDestroy()
        {
            if (_handler != null) { _handler.Shutdown(); Destroy(_handler.gameObject); }
            _harmony.UnpatchSelf();
            _instance = null;
        }

        internal static void Warn(string message) => _instance?.Logger.LogWarning(message);
        public static void NewLocalPlayer() => OnLocalPlayerChanged?.Invoke();
    }
}
