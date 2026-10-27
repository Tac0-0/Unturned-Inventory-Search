using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace InventorySearchPrototype
{
    /// <summary>
    /// Experimental, read-only, client-side Unturned inventory search.
    /// Does not invoke any server RPCs or modify an ItemJar / inventory page.
    /// Requires BepInEx 5 and a no-BattlEye game launch.
    /// </summary>
    [BepInPlugin("dev.tac0.unturned.inventorysearch", "Inventory Search Prototype", "1.3.2")]
    public sealed class InventorySearchPlugin : BaseUnityPlugin
    {
        private const float RefreshPeriodSeconds = 0.20f;
        private const float DimOpacity = 0.75f;
        private const float OutlineThickness = 3f;
        private static readonly Color DimColor = new Color(0f, 0f, 0f, DimOpacity);
        private static readonly Color OutlineColor = new Color(0.22f, 1f, 0.62f, 0.97f);
        private static readonly BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly FieldInfo SliderField =
            typeof(PlayerDashboardInventoryUI).GetField("characterSlider", StaticPrivate);
        private static readonly FieldInfo GridsField =
            typeof(PlayerDashboardInventoryUI).GetField("items", StaticPrivate);
        private static readonly FieldInfo SlotsField =
            typeof(PlayerDashboardInventoryUI).GetField("slots", StaticPrivate);

        private readonly Dictionary<SleekItem, ItemHighlight> highlights =
            new Dictionary<SleekItem, ItemHighlight>();
        private readonly HashSet<SleekItem> visibleItems = new HashSet<SleekItem>();
        private readonly List<SleekItem> removedItems = new List<SleekItem>();

        private static InventorySearchPlugin instance;
        // BepInEx's loader GameObject can become inactive immediately at startup
        // (observed on Unturned 2026-10-20). Its MonoBehaviour.Update then never runs.
        // This dedicated DontDestroyOnLoad root keeps polling independently.
        private static GameObject persistentRunnerObject;
        // Harmony callbacks can fire even when Unity has disabled the plugin component.
        // A standalone logger makes missing lifecycle callbacks observable.
        private static readonly BepInEx.Logging.ManualLogSource HookLog =
            BepInEx.Logging.Logger.CreateLogSource("Inventory Search Hooks");
        private static bool sawUpdateHook;
        private Harmony harmony;
        private int lastTickFrame = -1;
        private bool loggedHarmonyTick;
        private bool loggedInventoryOpen;

        private ISleekSlider originalSlider;
        private ISleekField searchField;
        private string searchText = string.Empty;
        private float nextRefresh;
        private bool wasInventoryActive;
        private bool warnedAboutUnavailableUi;
        private bool loggedUpdate;
        private bool loggedLocalPlayer;
        private bool lastDashboardState;
        private bool lastInventoryState;
        private float nextMissingUiReport;

        private void Awake()
        {
            instance = this;
            Logger.LogInfo("Inventory Search Prototype 1.3.2 loaded. Singleplayer / no-BattlEye testing only.");
            Logger.LogInfo("Reflection: characterSlider=" + (SliderField != null) +
                ", items=" + (GridsField != null) + ", slots=" + (SlotsField != null) +
                "; enabled=" + enabled + "; activeGameObject=" + gameObject.activeInHierarchy);
            try
            {
                if (persistentRunnerObject == null)
                {
                    persistentRunnerObject = new GameObject("Inventory Search Persistent Runner");
                    DontDestroyOnLoad(persistentRunnerObject);
                    persistentRunnerObject.AddComponent<InventorySearchRuntimeDriver>();
                    HookLog.LogInfo("PERSISTENT RUNNER CREATED: active=" +
                        persistentRunnerObject.activeInHierarchy +
                        ", scene=" + persistentRunnerObject.scene.name);
                }
            }
            catch (Exception ex)
            {
                HookLog.LogError("PERSISTENT RUNNER CREATION FAILED: " + ex);
            }

            if (SliderField == null || GridsField == null || SlotsField == null)
            {
                Logger.LogError("Unturned inventory UI fields changed. No UI modifications will be attempted.");
                enabled = false;
                return;
            }

            // Attach at the moment the game creates the inventory UI. This path
            // does not need Update() or a local player, and it should be called
            // whenever PlayerDashboardUI's constructor creates its inventory.
            try
            {
                harmony = new Harmony("dev.tac0.unturned.inventorysearch");
                var updateTarget = AccessTools.Method(typeof(PlayerUI), "Update");
                var inventoryOpen = AccessTools.Method(typeof(PlayerDashboardInventoryUI), "open");
                var inventoryCtor = AccessTools.Constructor(typeof(PlayerDashboardInventoryUI), Type.EmptyTypes);
                var dashboardCtor = AccessTools.Constructor(typeof(PlayerDashboardUI), Type.EmptyTypes);

                HookLog.LogInfo("Target check: PlayerUI.Update=" + (updateTarget != null) +
                    ", Inventory.open=" + (inventoryOpen != null) +
                    ", Inventory..ctor=" + (inventoryCtor != null) +
                    ", Dashboard..ctor=" + (dashboardCtor != null));

                if (updateTarget != null)
                    harmony.Patch(updateTarget, postfix: new HarmonyMethod(
                        AccessTools.Method(typeof(InventorySearchPlugin), nameof(OnGamePlayerUIUpdate))));
                if (inventoryOpen != null)
                    harmony.Patch(inventoryOpen, postfix: new HarmonyMethod(
                        AccessTools.Method(typeof(InventorySearchPlugin), nameof(OnInventoryOpen))));
                if (inventoryCtor != null)
                    harmony.Patch(inventoryCtor, postfix: new HarmonyMethod(
                        AccessTools.Method(typeof(InventorySearchPlugin), nameof(OnInventoryConstructed))));
                if (dashboardCtor != null)
                    harmony.Patch(dashboardCtor, postfix: new HarmonyMethod(
                        AccessTools.Method(typeof(InventorySearchPlugin), nameof(OnDashboardConstructed))));

                HookLog.LogInfo("Harmony hooks installed: PlayerUI.Update, Inventory.open, Inventory..ctor, Dashboard..ctor");
            }
            catch (Exception ex)
            {
                HookLog.LogError("Harmony installation failed: " + ex);
            }
        }

        // Unlike MonoBehaviour.Update, the inventory ctor's postfix executes
        // at the exact point at which the character slider is initialized.
        private static void OnInventoryConstructed()
        {
            HookLog.LogInfo("HOOK FIRED: PlayerDashboardInventoryUI constructor");
            AttachFromHook("inventory constructor");
        }

        private static void OnDashboardConstructed()
        {
            HookLog.LogInfo("HOOK FIRED: PlayerDashboardUI constructor");
            AttachFromHook("dashboard constructor");
        }

        private static void AttachFromHook(string location)
        {
            if (ReferenceEquals(instance, null))
            {
                HookLog.LogWarning("Cannot attach at " + location + ": plugin instance is null");
                return;
            }
            try
            {
                bool attached = instance.EnsureSearchField();
                HookLog.LogInfo("Attach at " + location + ": " + attached);
            }
            catch (Exception ex)
            {
                HookLog.LogError("Attach failed at " + location + ": " + ex);
            }
        }

        private static void OnGamePlayerUIUpdate()
        {
            if (!sawUpdateHook)
            {
                sawUpdateHook = true;
                HookLog.LogInfo("HOOK FIRED: PlayerUI.Update");
            }
            if (!ReferenceEquals(instance, null))
                instance.Tick();
        }

        private static void OnInventoryOpen()
        {
            HookLog.LogInfo("HOOK FIRED: PlayerDashboardInventoryUI.open");
            AttachFromHook("inventory open");
            if (!ReferenceEquals(instance, null))
                instance.Tick();
        }

        private void OnEnable()
        {
            if (Logger != null)
                Logger.LogInfo("MonoBehaviour OnEnable called");
        }

        private void OnDisable()
        {
            if (Logger != null)
                Logger.LogWarning("MonoBehaviour OnDisable called: frame=" + Time.frameCount +
                    ", enabled=" + enabled + ", activeGameObject=" + gameObject.activeInHierarchy +
                    ", appPlaying=" + Application.isPlaying);
        }

        // Also keep the original MonoBehaviour update path if Unity invokes it.
        private void Update() { Tick(); }

        // The runner is a different, active GameObject rather than a component
        // of BepInEx's loader object, which became inactive at frame zero.
        internal static void TickFromRunner()
        {
            // ReferenceEquals intentionally avoids UnityEngine.Object's overloaded
            // null check: a disabled BepInEx host must not block our managed logic.
            if (!ReferenceEquals(instance, null))
                instance.Tick();
        }

        private void Tick()
        {
            if (lastTickFrame == Time.frameCount)
                return;
            lastTickFrame = Time.frameCount;
            try
            {
                if (!loggedUpdate)
                {
                    loggedUpdate = true;
                    Logger.LogInfo("InventorySearch.Tick running; waiting for a local player and inventory UI.");
                }

                // Do not depend on two UI flags to *create* the search bar. The
                // slider is initialized only when the in-game dashboard UI exists,
                // so we can safely attach our control when it first becomes available.
                bool hasPlayer = Player.LocalPlayer != null;
                if (hasPlayer && !loggedLocalPlayer)
                {
                    loggedLocalPlayer = true;
                    Logger.LogInfo("Local player detected; looking for inventory slider.");
                }

                bool dashboardActive = PlayerDashboardUI.active;
                bool inventoryActive = PlayerDashboardInventoryUI.active;
                bool currentlyOpen = dashboardActive && inventoryActive;
                if (dashboardActive != lastDashboardState || inventoryActive != lastInventoryState)
                {
                    lastDashboardState = dashboardActive;
                    lastInventoryState = inventoryActive;
                    Logger.LogInfo("UI flags: dashboard=" + dashboardActive +
                        ", inventory=" + inventoryActive + ", player=" + hasPlayer);
                }

                if (hasPlayer)
                {
                    if (!EnsureSearchField())
                    {
                        if (Time.unscaledTime >= nextMissingUiReport)
                        {
                            nextMissingUiReport = Time.unscaledTime + 10f;
                            object slider = SliderField.GetValue(null);
                            Logger.LogWarning("Search field not attached yet: slider=" +
                                (slider == null ? "null" : slider.GetType().FullName));
                        }
                        return;
                    }
                }
                else
                {
                    return;
                }

                // The inventory is drawn only when both flags are active. Attach
                // in advance, but refresh search highlights only while visible.
                if (!currentlyOpen)
                {
                    if (wasInventoryActive)
                    {
                        ClearQuery();
                        RefreshHighlights();
                    }
                    wasInventoryActive = false;
                    return;
                }

                if (!wasInventoryActive)
                    Logger.LogInfo("Inventory opened; search field attached=" + (searchField != null));
                wasInventoryActive = true;

                // Some UI implementations can restore slider visibility on open.
                if (originalSlider != null && originalSlider.IsVisible)
                    originalSlider.IsVisible = false;
                if (searchField != null && !searchField.IsVisible)
                    searchField.IsVisible = true;

                bool controlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                if (controlHeld && Input.GetKeyDown(KeyCode.F))
                    searchField.FocusControl();

                if (Time.unscaledTime >= nextRefresh)
                {
                    nextRefresh = Time.unscaledTime + RefreshPeriodSeconds;
                    RefreshHighlights();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Inventory search UI update failed: " + ex);
                foreach (var visual in highlights.Values)
                {
                    try { visual.SetMode(HighlightMode.Normal); }
                    catch { /* May already be detached. */ }
                }
                enabled = false;
                RestoreSlider();
            }
        }

        private bool EnsureSearchField()
        {
            var currentSlider = SliderField.GetValue(null) as ISleekSlider;
            if (currentSlider == null || currentSlider.Parent == null)
            {
                if (!warnedAboutUnavailableUi)
                {
                    Logger.LogWarning("Character slider or parent is unavailable; waiting for Unturned to create the inventory.");
                    warnedAboutUnavailableUi = true;
                }
                return false;
            }
            warnedAboutUnavailableUi = false;

            if (searchField != null && ReferenceEquals(originalSlider, currentSlider))
                return true;

            // A fresh game session can reconstruct the inventory UI.
            if (searchField != null)
            {
                RestoreSlider();
                highlights.Clear();
            }

            originalSlider = currentSlider;
            Logger.LogInfo("Found slider widget=" + currentSlider.GetType().FullName +
                ", parent=" + currentSlider.Parent.GetType().FullName);
            searchField = Glazier.Get().CreateStringField();
            searchField.PlaceholderText = "Search items (Ctrl+F)";
            searchField.MaxLength = 64;
            searchField.TextAlignment = TextAnchor.MiddleLeft;
            searchField.SizeScale_X = currentSlider.SizeScale_X;
            searchField.SizeScale_Y = currentSlider.SizeScale_Y;
            searchField.SizeOffset_X = currentSlider.SizeOffset_X;
            searchField.SizeOffset_Y = 27f;
            searchField.PositionScale_X = currentSlider.PositionScale_X;
            searchField.PositionScale_Y = currentSlider.PositionScale_Y;
            searchField.PositionOffset_X = currentSlider.PositionOffset_X;
            searchField.PositionOffset_Y = 10f;
            searchField.Text = searchText;
            searchField.OnTextChanged += OnSearchChanged;
            searchField.OnTextEscaped += OnSearchEscaped;
            currentSlider.Parent.AddChild(searchField);
            currentSlider.IsVisible = false;

            Logger.LogInfo("Replaced character rotation slider with inventory search field; parent child count=" +
                currentSlider.Parent.GetChildCount());
            nextRefresh = 0f;
            return true;
        }

        private void OnSearchChanged(ISleekField field, string value)
        {
            searchText = (value ?? string.Empty).Trim();
            nextRefresh = 0f;
            RefreshHighlights();
        }

        private void OnSearchEscaped(ISleekField field)
        {
            ClearQuery();
            RefreshHighlights();
        }

        private void ClearQuery()
        {
            searchText = string.Empty;
            if (searchField != null)
            {
                searchField.Text = string.Empty;
                searchField.ClearFocus();
            }
            nextRefresh = 0f;
        }

        private void RefreshHighlights()
        {
            visibleItems.Clear();
            var pages = GridsField.GetValue(null) as SleekItems[];
            if (pages != null)
            {
                foreach (var page in pages)
                {
                    if (page == null)
                        continue;
                    // Includes clothing, backpack, opened storage, and nearby items.
                    foreach (var item in page.items)
                        UpdateItem(item);
                }
            }

            // Include the two equipped weapon slots.
            var slots = SlotsField.GetValue(null) as SleekSlot[];
            if (slots != null)
            {
                foreach (var slot in slots)
                {
                    if (slot != null)
                        UpdateItem(slot.item);
                }
            }

            // Discard detached UI nodes when a container changes or items move.
            removedItems.Clear();
            foreach (var entry in highlights)
            {
                if (!visibleItems.Contains(entry.Key))
                    removedItems.Add(entry.Key);
            }
            foreach (var stale in removedItems)
                highlights.Remove(stale);
        }

        private void UpdateItem(SleekItem item)
        {
            if (item == null)
                return;
            visibleItems.Add(item);

            ItemHighlight visual;
            if (string.IsNullOrEmpty(searchText))
            {
                if (highlights.TryGetValue(item, out visual))
                    visual.SetMode(HighlightMode.Normal);
                return;
            }

            ItemAsset asset = item.jar != null ? item.jar.GetAsset() : null;
            bool match = asset != null &&
                !string.IsNullOrEmpty(asset.itemName) &&
                asset.itemName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

            if (!highlights.TryGetValue(item, out visual))
            {
                visual = new ItemHighlight(item);
                highlights.Add(item, visual);
            }
            visual.SetMode(match ? HighlightMode.Match : HighlightMode.Dim);
        }

        private void RestoreSlider()
        {
            try
            {
                if (searchField != null)
                {
                    searchField.OnTextChanged -= OnSearchChanged;
                    searchField.OnTextEscaped -= OnSearchEscaped;
                    searchField.Parent?.RemoveChild(searchField);
                }
                if (originalSlider != null)
                    originalSlider.IsVisible = true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not fully restore old inventory UI during cleanup: " + ex.Message);
            }
            searchField = null;
            originalSlider = null;
        }

        private void OnDestroy()
        {
            // The hosting BepInEx GameObject can be unloaded even if the game is
            // still running. Retain our managed controller and Harmony hooks so
            // the independent runner can keep functioning.
            HookLog.LogWarning("BepInEx plugin host OnDestroy: frame=" + Time.frameCount +
                ", appPlaying=" + Application.isPlaying + ". Persistent runner remains active.");
            if (Application.isPlaying)
                return;
            try
            {
                if (harmony != null)
                    harmony.UnpatchSelf();
                if (ReferenceEquals(instance, this))
                    instance = null;
                foreach (var visual in highlights.Values)
                    visual.SetMode(HighlightMode.Normal);
                highlights.Clear();
                RestoreSlider();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Inventory Search cleanup: " + ex.Message);
            }
        }

        private enum HighlightMode { Normal, Match, Dim }

        private sealed class ItemHighlight
        {
            private readonly SleekItem item;
            private ISleekImage darken;
            private ISleekImage[] outline;

            public ItemHighlight(SleekItem item)
            {
                this.item = item;
            }

            public void SetMode(HighlightMode mode)
            {
                if (mode == HighlightMode.Dim && darken == null)
                {
                    darken = CreateImage(item, DimColor);
                    darken.SizeScale_X = 1f;
                    darken.SizeScale_Y = 1f;
                }
                if (mode == HighlightMode.Match && outline == null)
                    CreateOutline();

                if (darken != null)
                    darken.IsVisible = mode == HighlightMode.Dim;
                if (outline != null)
                {
                    foreach (var edge in outline)
                        edge.IsVisible = mode == HighlightMode.Match;
                }
            }

            private static ISleekImage CreateImage(SleekItem parent, Color color)
            {
                // Glazier image widgets are non-interactive by default in uGUI;
                // no click handlers are registered, so item dragging still works.
                var image = Glazier.Get().CreateImage();
                image.Texture = Texture2D.whiteTexture;
                image.TintColor = color;
                image.IsVisible = false;
                parent.AddChild(image);
                return image;
            }

            private void CreateOutline()
            {
                outline = new ISleekImage[4];
                // Child overlay dimensions follow the SleekItem's real rotated size.
                outline[0] = CreateImage(item, OutlineColor); // top
                outline[0].SizeScale_X = 1f;
                outline[0].SizeOffset_Y = OutlineThickness;

                outline[1] = CreateImage(item, OutlineColor); // bottom
                outline[1].PositionScale_Y = 1f;
                outline[1].PositionOffset_Y = -OutlineThickness;
                outline[1].SizeScale_X = 1f;
                outline[1].SizeOffset_Y = OutlineThickness;

                outline[2] = CreateImage(item, OutlineColor); // left
                outline[2].SizeOffset_X = OutlineThickness;
                outline[2].SizeScale_Y = 1f;

                outline[3] = CreateImage(item, OutlineColor); // right
                outline[3].PositionScale_X = 1f;
                outline[3].PositionOffset_X = -OutlineThickness;
                outline[3].SizeOffset_X = OutlineThickness;
                outline[3].SizeScale_Y = 1f;
            }
        }
    }

    /// <summary>
    /// Never attach this component to the BepInEx loader GameObject. That object
    /// was observed deactivated before the first Unity Update in this user's game.
    /// </summary>
    public sealed class InventorySearchRuntimeDriver : MonoBehaviour
    {
        private static readonly BepInEx.Logging.ManualLogSource Log =
            BepInEx.Logging.Logger.CreateLogSource("Inventory Search Runner");
        private bool announced;

        private void Awake()
        {
            Log.LogInfo("RUNNER AWAKE: active=" + gameObject.activeInHierarchy +
                ", frame=" + Time.frameCount);
        }

        private void OnEnable()
        {
            Log.LogInfo("RUNNER ONENABLE: active=" + gameObject.activeInHierarchy +
                ", frame=" + Time.frameCount);
        }

        private void Start()
        {
            Log.LogInfo("RUNNER START: frame=" + Time.frameCount);
        }

        private void Update()
        {
            if (!announced)
            {
                announced = true;
                Log.LogInfo("RUNNER UPDATE FIRED: frame=" + Time.frameCount);
            }
            InventorySearchPlugin.TickFromRunner();
        }

        private void OnDisable()
        {
            Log.LogWarning("RUNNER ONDISABLE: frame=" + Time.frameCount +
                ", active=" + gameObject.activeInHierarchy);
        }

        private void OnDestroy()
        {
            Log.LogWarning("RUNNER ONDESTROY: frame=" + Time.frameCount);
        }
    }
}