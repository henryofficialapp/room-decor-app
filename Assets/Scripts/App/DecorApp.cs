using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RoomDecor.Core;
using UnityEngine;

namespace RoomDecor.App
{
    /// <summary>
    /// The whole prototype: choose a room type, enter its size, place
    /// furniture in 3D and see the estimated cost. The screens use Unity's
    /// immediate-mode GUI so the prototype needs no scene setup; a designed
    /// UI replaces it once the flow is settled.
    /// </summary>
    public sealed class DecorApp : MonoBehaviour
    {
        enum Page
        {
            ChooseRoom,
            RoomSize,
            Design,
            Cost,
        }

        const float SizeStepMeters = 0.1f;
        const float TapMaxMovePixels = 12f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<DecorApp>() == null)
                new GameObject("DecorApp").AddComponent<DecorApp>();
        }

        Page page = Page.ChooseRoom;

        // Catalog and room
        CatalogLoader.Result catalog;
        RoomType roomType;
        float widthMeters = 3.6f, lengthMeters = 4.2f, heightMeters = 2.5f;
        Room room;
        IReadOnlyList<Recommendation> recommendations = new Recommendation[0];

        // Design state: items standing in the room, plus items that are bought
        // but not drawn (a mattress, curtains).
        readonly List<PlacedItem> placed = new List<PlacedItem>();
        readonly List<CatalogItem> notDrawn = new List<CatalogItem>();
        readonly Dictionary<PlacedItem, FurnitureView> views = new Dictionary<PlacedItem, FurnitureView>();
        IReadOnlyList<PlacementIssue> issues = new PlacementIssue[0];
        PlacedItem selected;

        // Scene objects
        Camera cam;
        CameraRig rig;
        GameObject roomRoot;
        Transform furnitureRoot;

        // Pointer gesture
        bool gestureActive, gestureOnUi, draggingItem;
        Vector2 gestureStart, lastPointer;
        Vector3 grabOffset;
        float lastPinchDistance;

        // UI
        float uiScale = 1f;
        Rect safe;
        readonly List<Rect> uiRects = new List<Rect>();
        bool shopOpen = true;
        Vector2 shopScroll, costScroll;
        string toast;
        float toastUntil;
        GUIStyle title, body, small, button, bigButton, panel;

        void Awake()
        {
            catalog = CatalogLoader.LoadSample();
            Application.targetFrameRate = 60;

            cam = Camera.main;
            if (cam == null)
            {
                var camObject = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camObject.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.86f, 0.88f, 0.90f);
            cam.nearClipPlane = 0.05f;
            rig = cam.gameObject.AddComponent<CameraRig>();

            if (FindFirstObjectByType<Light>() == null)
            {
                var sun = new GameObject("Light").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.0f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(55, -30, 0);
            }
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);
        }

        // ---------------------------------------------------------------- Flow

        void ChooseRoomType(RoomType type)
        {
            if (type != roomType || room == null)
            {
                ClearDesign();
                roomType = type;
            }
            page = Page.RoomSize;
        }

        void StartDesigning()
        {
            room = new Room(roomType, widthMeters, lengthMeters, heightMeters);
            recommendations = RecommendationEngine.Recommend(room, catalog.Items);
            RebuildScene();
            page = Page.Design;
        }

        void RebuildScene()
        {
            if (roomRoot != null) Destroy(roomRoot);
            roomRoot = RoomView.Build(room, out var walls);
            furnitureRoot = new GameObject("Furniture").transform;
            furnitureRoot.SetParent(roomRoot.transform, false);
            views.Clear();

            // Keep what was placed before a size change, pulled back inside the new walls.
            foreach (var p in placed.ToList())
            {
                if (!PlacementRules.ClampInsideRoom(room, p))
                {
                    placed.Remove(p);
                    ShowToast($"{p.Item.Name} no longer fits and was removed.");
                    continue;
                }
                views[p] = FurnitureView.Create(p, furnitureRoot);
            }
            rig.Init(cam, room, walls);
            selected = null;
            RefreshIssues();
        }

        void ClearDesign()
        {
            placed.Clear();
            notDrawn.Clear();
            selected = null;
            if (roomRoot != null) Destroy(roomRoot);
            roomRoot = null;
            views.Clear();
            room = null;
        }

        void Add(CatalogItem item)
        {
            if (!FurnitureCategoryInfo.IsPlacedOnFloor(item.Category))
            {
                notDrawn.Add(item);
                ShowToast($"Added {item.Name} to your list. It isn't drawn in 3D.");
                return;
            }

            var p = new PlacedItem(item, room.WidthMeters / 2, room.LengthMeters / 2);
            if (!PlacementRules.ClampInsideRoom(room, p))
            {
                p.RotateQuarterTurn();
                if (!PlacementRules.ClampInsideRoom(room, p))
                {
                    ShowToast($"{item.Name} is too big for this room.");
                    return;
                }
            }
            placed.Add(p);
            views[p] = FurnitureView.Create(p, furnitureRoot);
            Select(p);
            shopOpen = false;
            ShowToast("Drag it into place. Tap Rotate to turn it.");
        }

        void Remove(PlacedItem p)
        {
            placed.Remove(p);
            if (views.TryGetValue(p, out var view)) Destroy(view.gameObject);
            views.Remove(p);
            if (selected == p) selected = null;
            RefreshIssues();
        }

        void RotateSelected()
        {
            if (selected == null) return;
            selected.RotateQuarterTurn();
            if (!PlacementRules.ClampInsideRoom(room, selected))
            {
                // Turned, it no longer fits; turn it back.
                for (int i = 0; i < 3; i++) selected.RotateQuarterTurn();
                PlacementRules.ClampInsideRoom(room, selected);
                ShowToast("There isn't room to turn it that way.");
            }
            views[selected].SyncTransform();
            RefreshIssues();
        }

        void Select(PlacedItem p)
        {
            selected = p;
            RefreshIssues();
        }

        void RefreshIssues()
        {
            issues = room == null ? new PlacementIssue[0] : PlacementRules.FindIssues(room, placed);
            var problems = new HashSet<PlacedItem>(issues.SelectMany(i => new[] { i.Item, i.Other }).Where(p => p != null));
            foreach (var pair in views)
            {
                Color? tint = null;
                if (problems.Contains(pair.Key)) tint = Palette.Problem;
                else if (pair.Key == selected) tint = Palette.Selected;
                pair.Value.SetTint(tint);
            }
        }

        CostEstimate Estimate() =>
            CostEstimator.Estimate(placed.Select(p => p.Item).Concat(notDrawn), DateTime.UtcNow);

        void ShowToast(string message)
        {
            toast = message;
            toastUntil = Time.time + 3f;
        }

        // ---------------------------------------------------------------- Input

        void Update()
        {
            if (page != Page.Design || room == null) return;

            if (Input.touchCount >= 2)
            {
                HandlePinch(Input.GetTouch(0).position, Input.GetTouch(1).position);
                gestureActive = false;
                return;
            }
            lastPinchDistance = 0;

            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) PointerDown(touch.position);
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) PointerMove(touch.position);
                else PointerUp(touch.position);
            }
            else if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0)) PointerDown(Input.mousePosition);
                else if (Input.GetMouseButton(0)) PointerMove(Input.mousePosition);
                else if (Input.GetMouseButtonUp(0)) PointerUp(Input.mousePosition);

                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f && !IsOverUi(Input.mousePosition)) rig.Zoom(scroll * 10f);
            }
        }

        void PointerDown(Vector2 position)
        {
            gestureActive = true;
            gestureStart = lastPointer = position;
            gestureOnUi = IsOverUi(position);
            draggingItem = false;
            if (gestureOnUi) return;

            var ray = cam.ScreenPointToRay(position);
            if (Physics.Raycast(ray, out var hit, 100f) && hit.collider.TryGetComponent(out FurnitureView view))
            {
                Select(view.Placed);
                draggingItem = true;
                if (FloorPoint(position, out var floor))
                    grabOffset = new Vector3((float)view.Placed.X, 0, (float)view.Placed.Z) - floor;
            }
        }

        void PointerMove(Vector2 position)
        {
            if (!gestureActive || gestureOnUi) return;
            var delta = position - lastPointer;
            lastPointer = position;

            if (draggingItem && selected != null)
            {
                if (!FloorPoint(position, out var floor)) return;
                var target = floor + grabOffset;
                selected.X = target.x;
                selected.Z = target.z;
                PlacementRules.ClampInsideRoom(room, selected);
                views[selected].SyncTransform();
                RefreshIssues();
            }
            else
            {
                rig.Drag(delta);
            }
        }

        void PointerUp(Vector2 position)
        {
            if (!gestureActive) return;
            bool wasTap = (position - gestureStart).magnitude < TapMaxMovePixels;
            if (wasTap && !gestureOnUi && !draggingItem && selected != null) Select(null);
            gestureActive = false;
            draggingItem = false;
        }

        void HandlePinch(Vector2 a, Vector2 b)
        {
            float distance = Vector2.Distance(a, b);
            if (lastPinchDistance > 0) rig.Zoom((distance - lastPinchDistance) * 0.3f);
            lastPinchDistance = distance;
        }

        bool FloorPoint(Vector2 screenPosition, out Vector3 point)
        {
            var ray = cam.ScreenPointToRay(screenPosition);
            var floor = new Plane(Vector3.up, Vector3.zero);
            if (floor.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }
            point = default;
            return false;
        }

        /// <summary>Screen position (origin bottom-left) is inside a panel drawn last frame.</summary>
        bool IsOverUi(Vector2 screenPosition)
        {
            var gui = new Vector2(screenPosition.x, UnityEngine.Screen.height - screenPosition.y) / uiScale;
            return uiRects.Any(r => r.Contains(gui));
        }

        // ---------------------------------------------------------------- UI

        void OnGUI()
        {
            EnsureStyles();
            uiScale = Mathf.Max(1f, Mathf.Min(UnityEngine.Screen.width, UnityEngine.Screen.height) / 400f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1));
            var s = UnityEngine.Screen.safeArea;
            safe = new Rect(s.x / uiScale, (UnityEngine.Screen.height - s.yMax) / uiScale, s.width / uiScale, s.height / uiScale);

            if (Event.current.type == EventType.Layout) uiRects.Clear();

            switch (page)
            {
                case Page.ChooseRoom: DrawChooseRoom(); break;
                case Page.RoomSize: DrawRoomSize(); break;
                case Page.Design: DrawDesign(); break;
                case Page.Cost: DrawCost(); break;
            }

            if (!string.IsNullOrEmpty(toast) && Time.time < toastUntil)
            {
                var r = new Rect(safe.x + 16, safe.y + 64, safe.width - 32, 48);
                GUI.Box(r, toast, panel);
            }
        }

        void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
            body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 15 };
            bigButton = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true };
            panel = new GUIStyle(GUI.skin.box) { fontSize = 14, wordWrap = true, alignment = TextAnchor.MiddleCenter };
        }

        /// <summary>Draws a background panel and remembers it so touches on it don't move the room.</summary>
        void Panel(Rect r)
        {
            GUI.Box(r, GUIContent.none);
            uiRects.Add(r);
        }

        void DrawChooseRoom()
        {
            var area = Inset(safe, 16);
            GUILayout.BeginArea(area);
            GUILayout.Label("What room are you decorating?", title);
            GUILayout.Space(12);
            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                var firstPicks = RecommendationEngine.CategoriesFor(type)
                    .Where(rule => rule.Priority == Priority.Essential)
                    .Take(3)
                    .Select(rule => FurnitureCategoryInfo.DisplayName(rule.Category).ToLowerInvariant());
                var label = RoomName(type) + "\n<size=13>Starts with " + string.Join(", ", firstPicks) + "</size>";
                if (GUILayout.Button(label, RichButton(), GUILayout.Height(72))) ChooseRoomType(type);
                GUILayout.Space(8);
            }
            GUILayout.EndArea();
        }

        void DrawRoomSize()
        {
            var area = Inset(safe, 16);
            GUILayout.BeginArea(area);
            GUILayout.Label($"How big is the {RoomName(roomType).ToLowerInvariant()}?", title);
            GUILayout.Label("Measure wall to wall. Room scanning with the camera comes in a later version.", body);
            GUILayout.Space(16);

            widthMeters = Stepper("Width", widthMeters, (float)Room.MinSideMeters, (float)Room.MaxSideMeters);
            lengthMeters = Stepper("Length", lengthMeters, (float)Room.MinSideMeters, (float)Room.MaxSideMeters);
            heightMeters = Stepper("Ceiling height", heightMeters, (float)Room.MinHeightMeters, (float)Room.MaxHeightMeters);

            GUILayout.Space(8);
            GUILayout.Label($"Floor area: {widthMeters * lengthMeters:0.0} m² ({Units.MetersToFeet(widthMeters) * Units.MetersToFeet(lengthMeters):0} sq ft)", body);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Start decorating", bigButton, GUILayout.Height(56))) StartDesigning();
            GUILayout.Space(8);
            if (GUILayout.Button("Back", button, GUILayout.Height(44))) page = Page.ChooseRoom;
            GUILayout.EndArea();
        }

        float Stepper(string label, float meters, float min, float max)
        {
            GUILayout.Label(label, body);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("−", bigButton, GUILayout.Width(56), GUILayout.Height(48))) meters -= SizeStepMeters;
            GUILayout.Label($"{meters:0.0} m  ({FeetAndInches(meters)})", title, GUILayout.Height(48));
            if (GUILayout.Button("+", bigButton, GUILayout.Width(56), GUILayout.Height(48))) meters += SizeStepMeters;
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            return Mathf.Clamp(Mathf.Round(meters * 10f) / 10f, min, max);
        }

        void DrawDesign()
        {
            var estimate = Estimate();

            // Top bar
            var top = new Rect(safe.x, safe.y, safe.width, 56);
            Panel(top);
            if (GUI.Button(new Rect(top.x + 8, top.y + 8, 72, 40), "Back", button))
            {
                page = Page.RoomSize;
                return;
            }
            var status = $"{RoomName(roomType)} · {room.WidthMeters:0.0} × {room.LengthMeters:0.0} m";
            int problemCount = issues.Count;
            if (problemCount > 0) status += $"\n<color=#d03a2f>{problemCount} placement problem{(problemCount == 1 ? "" : "s")}</color>";
            GUI.Label(new Rect(top.x + 88, top.y + 6, top.width - 200, 48), status, RichLabel());
            if (GUI.Button(new Rect(top.xMax - 104, top.y + 8, 96, 40), Money(estimate.Total, estimate.Currency), button))
            {
                page = Page.Cost;
                return;
            }

            if (catalog.IsSample)
            {
                var banner = new Rect(safe.x, top.yMax, safe.width, 24);
                Panel(banner);
                GUI.Label(banner, "  Sample products and prices for testing, not real store data", small);
            }

            // Walk controls
            if (rig.Mode == ViewMode.Walk)
            {
                var fwd = new Rect(safe.xMax - 72, safe.center.y - 64, 64, 56);
                var back = new Rect(safe.xMax - 72, safe.center.y + 8, 64, 56);
                uiRects.Add(fwd);
                uiRects.Add(back);
                // A held RepeatButton reports on every GUI event; move once per frame, on Repaint.
                bool repaint = Event.current.type == EventType.Repaint;
                if (GUI.RepeatButton(fwd, "▲", bigButton) && repaint) rig.WalkForFrame(1f);
                if (GUI.RepeatButton(back, "▼", bigButton) && repaint) rig.WalkForFrame(-1f);
            }

            // Bottom bar: selected item, view modes, shop toggle
            float sheetHeight = shopOpen ? safe.height * 0.5f : 0f;
            float barHeight = selected != null ? 112 : 64;
            var bar = new Rect(safe.x, safe.yMax - barHeight - sheetHeight, safe.width, barHeight);
            Panel(bar);

            float y = bar.y + 8;
            if (selected != null)
            {
                GUI.Label(new Rect(bar.x + 12, y, bar.width - 200, 40),
                    $"{selected.Item.Name}\n<size=12>{selected.Item.Store} · {Money(selected.Item.Price, selected.Item.Currency)}</size>", RichLabel());
                if (GUI.Button(new Rect(bar.xMax - 184, y, 84, 40), "Rotate", button)) RotateSelected();
                if (GUI.Button(new Rect(bar.xMax - 92, y, 84, 40), "Remove", button)) Remove(selected);
                y += 48;
            }

            float bw = (bar.width - 16 - 3 * 8) / 4;
            ViewButton(new Rect(bar.x + 8, y, bw, 44), "3D", ViewMode.Orbit);
            ViewButton(new Rect(bar.x + 8 + (bw + 8), y, bw, 44), "Plan", ViewMode.TopDown);
            ViewButton(new Rect(bar.x + 8 + 2 * (bw + 8), y, bw, 44), "Walk", ViewMode.Walk);
            if (GUI.Button(new Rect(bar.x + 8 + 3 * (bw + 8), y, bw, 44), shopOpen ? "Close" : "Add items", button))
                shopOpen = !shopOpen;

            if (shopOpen) DrawShop(new Rect(safe.x, safe.yMax - sheetHeight, safe.width, sheetHeight));
        }

        void ViewButton(Rect r, string label, ViewMode mode)
        {
            bool on = GUI.Toggle(r, rig.Mode == mode, label, button);
            if (on && rig.Mode != mode) rig.SetMode(mode);
        }

        void DrawShop(Rect sheet)
        {
            Panel(sheet);
            GUILayout.BeginArea(Inset(sheet, 8));
            GUILayout.Label($"Suggested for your {RoomName(roomType).ToLowerInvariant()}", body);
            shopScroll = GUILayout.BeginScrollView(shopScroll);

            Priority? lastPriority = null;
            foreach (var rec in recommendations)
            {
                if (rec.Priority != lastPriority)
                {
                    GUILayout.Space(6);
                    GUILayout.Label(rec.Priority == Priority.Essential ? "<b>Start with these</b>" : "<b>Nice to have</b>", RichLabel());
                    lastPriority = rec.Priority;
                }

                GUILayout.Label(FurnitureCategoryInfo.DisplayName(rec.Category), title);
                if (rec.Items.Count == 0)
                {
                    GUILayout.Label("Nothing in the catalog fits this room.", small);
                    continue;
                }
                foreach (var item in rec.Items)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{item.Name}\n<size=12>{item.Store} · {item.WidthMeters * 100:0} × {item.DepthMeters * 100:0} × {item.HeightMeters * 100:0} cm</size>", RichLabel());
                    GUILayout.Label(Money(item.Price, item.Currency), body, GUILayout.Width(80));
                    if (GUILayout.Button("Add", button, GUILayout.Width(64), GUILayout.Height(40))) Add(item);
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawCost()
        {
            var estimate = Estimate();
            var area = Inset(safe, 16);
            GUILayout.BeginArea(area);
            GUILayout.Label("Estimated cost", title);
            GUILayout.Label(Money(estimate.Total, estimate.Currency), new GUIStyle(title) { fontSize = 34 });
            GUILayout.Label(CostEstimate.Disclaimer, small);
            if (estimate.HasStalePrices)
                GUILayout.Label("<color=#d03a2f>Some prices were last checked more than a day ago.</color>", RichLabel());
            GUILayout.Label("We may earn a commission when you buy through these links.", small);
            GUILayout.Space(8);

            costScroll = GUILayout.BeginScrollView(costScroll);
            foreach (var line in estimate.Lines)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                var qty = line.Quantity > 1 ? $"{line.Quantity} × " : "";
                GUILayout.Label($"{qty}{line.Item.Name}\n<size=12>{line.Item.Store} · price checked {line.Item.PriceCheckedUtc:MMM d, yyyy}</size>", RichLabel());
                GUILayout.Label(Money(line.Subtotal, estimate.Currency), body, GUILayout.Width(88));
                GUI.enabled = !string.IsNullOrEmpty(line.Item.BuyUrl);
                if (GUILayout.Button("Buy", button, GUILayout.Width(56), GUILayout.Height(40))) Application.OpenURL(line.Item.BuyUrl);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            if (estimate.Lines.Count == 0) GUILayout.Label("Nothing added yet.", body);

            if (estimate.TotalByStore.Count > 1)
            {
                GUILayout.Space(8);
                GUILayout.Label("<b>By store</b>", RichLabel());
                foreach (var pair in estimate.TotalByStore)
                    GUILayout.Label($"{pair.Key}: {Money(pair.Value, estimate.Currency)}", body);
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Back to the room", bigButton, GUILayout.Height(56))) page = Page.Design;
            GUILayout.EndArea();
        }

        // ---------------------------------------------------------------- Helpers

        GUIStyle RichLabel() => new GUIStyle(body) { richText = true };
        GUIStyle RichButton() => new GUIStyle(bigButton) { richText = true };

        static Rect Inset(Rect r, float by) => new Rect(r.x + by, r.y + by, r.width - 2 * by, r.height - 2 * by);

        static string RoomName(RoomType type)
        {
            switch (type)
            {
                case RoomType.LivingRoom: return "Living room";
                case RoomType.DiningRoom: return "Dining room";
                case RoomType.HomeOffice: return "Home office";
                case RoomType.KidsRoom: return "Kids' room";
                default: return type.ToString();
            }
        }

        static string Money(decimal amount, string currency)
        {
            var number = amount.ToString("N2", CultureInfo.InvariantCulture);
            return currency == "USD" ? "$" + number : number + " " + currency;
        }

        static string FeetAndInches(float meters)
        {
            int inches = Mathf.RoundToInt(meters / 0.0254f);
            return $"{inches / 12} ft {inches % 12} in";
        }
    }
}
