using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Connection;

using Game.Scripts.Core.Helpers;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.Player.Data;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.Screens;
using Game.GameResources;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.UI.MainMenu;
using NewDropDude.Script.API.ServerManagers;

namespace Game.Scripts.UI.Tree
{
    public class DevelopmentTree : NetworkBehaviour
    {
        public Transform animationPanel;
        public Transform fractionTreePrefab;
        public Transform starterContainerPrefab;
        public FactionContainer factionContainerPrefab;
        public TreeGrid treeGridPrefab;
        public TreeItem treeItemPrefab;

        public Transform buttonsContainer;
        public Button factionButtonPrefab;
        public Button buttonBack;

        public ArrowDrawer arrowDrawer;
        public RectTransform arrowsLayerPrefab;

        private WarriorGraphResponse[] _graphs = Array.Empty<WarriorGraphResponse>();
        private WarriorDto[] _vehicleLites = Array.Empty<WarriorDto>();
        private string[] _factionCodes = Array.Empty<string>();

        private readonly List<Transform> _fractionRoots = new();
        private readonly List<Button> _factionButtons = new();
        private readonly Dictionary<string, string> _factionNameByCode = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<FactionView> _views = new();

        // кеш: володіння/XP/анлоки
        private readonly HashSet<int> _ownedIds = new();
        private readonly Dictionary<int, int> _xpByOwnedWarriorId = new(); // WarriorId -> Xp
        private readonly HashSet<int> _unlockedSuccessors = new(); // Successor WarriorId

        public bool isInitialized;

        private class FactionView
        {
            public RectTransform Root;
            public RectTransform ArrowsLayer;
            public WarriorGraphEdge[] Edges = Array.Empty<WarriorGraphEdge>();
            public Dictionary<int, RectTransform> NodeMap = new();
        }

        private void Awake()
        {
            if (buttonBack != null)
            {
                buttonBack.onClick.AddListener(() =>
                {
                    MenuManager.OpenMenu(MenuType.MainMenu);
                    RobotView.UpdateUI();
                });
            }
        }

        public WarriorDto GetVehicleLite(int id)
        {
            if (_vehicleLites == null || _vehicleLites.Length == 0) return null;
            foreach (WarriorDto v in _vehicleLites)
                if (v.id == id) return v;
            return null;
        }

        public async void Init()
        {
            if (!isInitialized)
            {
                isInitialized = true;
                bool ok = await LoadDataFromServer();
                isInitialized = ok;
            }

            await UpdateUI();
        }

        private async UniTask<bool> LoadDataFromServer()
        {
            _factionNameByCode.Clear();
            _ownedIds.Clear();
            _xpByOwnedWarriorId.Clear();
            _unlockedSuccessors.Clear();

            // юніти каталогу
            (bool okAll, _, WarriorDto[] items) = await WarriorsManager.GetAll();
            _vehicleLites = items ?? Array.Empty<WarriorDto>();

            if (!okAll || _vehicleLites.Length == 0)
            {
                Popup.ShowText("No vehicle data received from server!", Color.red);
                _graphs = Array.Empty<WarriorGraphResponse>();
                _factionCodes = Array.Empty<string>();
                return okAll;
            }

            foreach (WarriorDto v in _vehicleLites)
            {
                if (!string.IsNullOrWhiteSpace(v.cultureCode) && !_factionNameByCode.ContainsKey(v.cultureCode))
                    _factionNameByCode[v.cultureCode] = string.IsNullOrWhiteSpace(v.cultureName) ? v.cultureCode : v.cultureName;
            }

            _factionCodes = _vehicleLites
                .Select(v => v.cultureCode)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // графи
            List<WarriorGraphResponse> graphs = new(_factionCodes.Length);
            foreach (string culture in _factionCodes)
            {
                (bool okGraph, _, WarriorGraphResponse graph) = await WarriorsManager.GetGraph(culture);
                if (okGraph && graph != null && graph.nodes != null && graph.nodes.Length > 0)
                    graphs.Add(graph);
                else
                {
                    Popup.ShowText($"Failed to get graph for culture: {culture}", Color.red);
                    graphs.Add(new WarriorGraphResponse { nodes = Array.Empty<WarriorGraphNode>(), edges = Array.Empty<WarriorGraphEdge>() });
                }
            }
            _graphs = graphs.ToArray();

            // профіль гравця (володіння, coins, xp)
            IPlayerClientInfo info = ServiceLocator.Get<IPlayerClientInfo>();
            if (info != null && info.Profile != null && info.Profile.ownedWarriors != null)
            {
                foreach (var ow in info.Profile.ownedWarriors)
                {
                    _ownedIds.Add(ow.warriorId);
                    _xpByOwnedWarriorId[ow.warriorId] = ow.xp;
                }
            }

            // завантажити анлоки з сервера (якщо бекенд готовий), інакше підтягнути локальний кеш
            await LoadUnlocksSafe();

            return true;
        }

        private async UniTask LoadUnlocksSafe()
        {
            string token = RegisterServer.GetMyToken();
            if (!string.IsNullOrEmpty(token))
            {
                // пробуємо сервер
                var (ok, _, ids) = await ResearchManager.GetMyUnlocked(token);
                if (ok && ids != null)
                {
                    _unlockedSuccessors.Clear();
                    foreach (int id in ids) _unlockedSuccessors.Add(id);
                    SaveLocalUnlocks(); // синхронізуємо локальний кеш
                    return;
                }
            }

            // локальний кеш
            LoadLocalUnlocks();
        }

        private void SaveLocalUnlocks()
        {
            string key = "Tree_Unlocks";
            string payload = string.Join(",", _unlockedSuccessors);
            PlayerPrefs.SetString(key, payload);
            PlayerPrefs.Save();
        }

        private void LoadLocalUnlocks()
        {
            string key = "Tree_Unlocks";
            _unlockedSuccessors.Clear();
            string s = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrWhiteSpace(s)) return;
            string[] parts = s.Split(',');
            foreach (var p in parts)
                if (int.TryParse(p, out int id)) _unlockedSuccessors.Add(id);
        }

        public async UniTask UpdateUI()
        {
            WipeUI();
            _views.Clear();

            if (_factionCodes == null || _factionCodes.Length == 0 || _graphs == null || _graphs.Length == 0)
            {
                Popup.ShowText("No data to build UI!", Color.red);
                return;
            }

            BuildFactionButtons();

            for (int i = 0; i < _graphs.Length; i++)
            {
                await BuildFactionTreeUI(_factionCodes[i], _graphs[i]);
            }

            if (_fractionRoots.Count > 0) SetActiveContainer(0);

            await GameplayAssistant.RebuildAllLayouts(_fractionRoots);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            DrawArrowsAll();
        }

        public void SetActiveContainer(int number)
        {
            for (int i = 0; i < _fractionRoots.Count; i++)
            {
                Transform t = _fractionRoots[i];
                if (t != null) t.gameObject.SetActive(i == number);
            }
        }

        private async UniTask BuildFactionTreeUI(string cultureCode, WarriorGraphResponse graphResponse)
        {
            if (graphResponse == null || graphResponse.nodes == null || graphResponse.nodes.Length == 0) return;

            Transform rootT = Instantiate(fractionTreePrefab, animationPanel);
            rootT.name = $"FactionRoot_{cultureCode}";
            _fractionRoots.Add(rootT);

            RectTransform arrowsLayer = null;
            if (arrowsLayerPrefab != null)
            {
                arrowsLayer = Instantiate(arrowsLayerPrefab, rootT).GetComponent<RectTransform>();
                arrowsLayer.name = "ArrowsLayer";
                arrowsLayer.SetAsFirstSibling();
            }

            Transform starterContainer = Instantiate(starterContainerPrefab, rootT);
            starterContainer.name = "StarterContainer";

            Dictionary<int, RectTransform> nodeMap = new();

            WarriorGraphNode starter = graphResponse.nodes
                .Where(n => n.level == 1)
                .OrderBy(n => n.code)
                .FirstOrDefault()
                ?? graphResponse.nodes.OrderBy(n => n.level).ThenBy(n => n.code).First();

            CreateTreeItemFromNode(starterContainer, starter, nodeMap);

            FactionContainer columns = Instantiate(factionContainerPrefab, rootT);
            columns.name = "ColumnsContainer";

            BuildColumn(columns.transform, graphResponse, "Light", "Light_Column", nodeMap);
            BuildColumn(columns.transform, graphResponse, "Ranged", "Ranged_Column", nodeMap);
            BuildColumn(columns.transform, graphResponse, "Heavy", "Heavy_Column", nodeMap);

            _views.Add(new FactionView
            {
                Root = (RectTransform)rootT,
                ArrowsLayer = arrowsLayer,
                Edges = graphResponse.edges ?? Array.Empty<WarriorGraphEdge>(),
                NodeMap = nodeMap
            });

            await UniTask.Yield();
        }

        private void BuildColumn(Transform parent, WarriorGraphResponse graphResponse, string className, string columnName, Dictionary<int, RectTransform> nodeMap)
        {
            TreeGrid grid = Instantiate(treeGridPrefab, parent);
            grid.name = columnName;
            grid.Init(ParseVehicleClass(className));

            IEnumerable<WarriorGraphNode> nodes = graphResponse.nodes
                .Where(n => string.Equals(n.@class, className, StringComparison.OrdinalIgnoreCase) && n.level >= 2)
                .OrderBy(n => n.level)
                .ThenBy(n => n.code);

            foreach (WarriorGraphNode n in nodes)
            {
                CreateTreeItemFromNode(grid.transform, n, nodeMap);
            }
        }

        private void CreateTreeItemFromNode(Transform parent, WarriorGraphNode node, Dictionary<int, RectTransform> nodeMap)
        {
            if (node == null) return;

            TreeItem item = Instantiate(treeItemPrefab, parent);

            item.vehicleName.text = node.name;
            item.vehicleType = ParseVehicleClass(node.@class);
            item.level.text = node.level.ToString();
            item.isClose.SetActive(!node.isVisible);

            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            bool isHave = _ownedIds.Contains(node.id);
            item.isHave.gameObject.SetActive(isHave);

            Sprite sprite = ResourceManager.GetIcon(node.code);
            item.image.sprite = sprite;

            WarriorDto liteInfo = GetVehicleLite(node.id);
            item.price.text = liteInfo != null ? liteInfo.purchaseCost.ToString() : "0";

            bool isLevel1 = node.level <= 1;
            bool isUnlocked = isLevel1 || _unlockedSuccessors.Contains(node.id) || isHave;

            if (!isHave && !isUnlocked)
            {
                // показати XP на дослідження з найближчого предка
                item.SetActiveCoinsView(false);
                item.SetActiveXpView(true);
                SetupXpResearchView(item, node);
            }
            else
            {
                // купівля за монети (або вже володіє)
                item.SetActiveCoinsView(true);
                item.SetActiveXpView(false);
                SetupCoinsBuyView(item, node, liteInfo);
            }

            RectTransform rt = item.rectTransform;
            nodeMap[node.id] = rt;
        }

        private void SetupXpResearchView(TreeItem item, WarriorGraphNode node)
        {
            // завантажуємо вимоги
            item.xp.text = "…";
            item.button.onClick.RemoveAllListeners();

            _ = SetupXpAsync();

            async UniTaskVoid SetupXpAsync()
            {
                // читаємо лінки (предки) з бекенду
                var (okLinks, _, links) = await WarriorsManager.GetResearchFrom(node.id);
                if (!okLinks || links == null || links.Length == 0)
                {
                    item.xp.text = "-";
                    item.button.onClick.AddListener(() => Popup.ShowText("No research path.", Color.red));
                    return;
                }

                // вибір предка: спершу той, яким володіємо; якщо декілька — з найменшою вимогою XP
                var candidates = links
                    .Where(l => _ownedIds.Contains(l.predecessorId))
                    .OrderBy(l => l.requiredXp)
                    .ToList();

                if (candidates.Count == 0)
                {
                    item.xp.text = "Need predecessor";
                    item.button.onClick.AddListener(() => Popup.ShowText("Own a predecessor first.", Color.yellow));
                    return;
                }

                var link = candidates[0];
                int myXp = _xpByOwnedWarriorId.TryGetValue(link.predecessorId, out var x) ? x : 0;
                int need = Mathf.Max(0, link.requiredXp - myXp);

                if (need > 0)
                {
                    item.xp.text = need.ToString();
                    item.button.onClick.AddListener(() =>
                    {
                        Popup.ShowText($"Need {need} XP on predecessor to research.", Color.yellow);
                    });
                    return;
                }

                // достатньо XP — даємо кнопку "Research"
                item.xp.text = "Ready";
                item.button.onClick.AddListener(() =>
                {
                    TryResearch(node.id, link.predecessorId, item);
                });
            }
        }

        private async void TryResearch(int successorId, int predecessorId, TreeItem item)
        {
            Helpers.Loading.Show();

            string token = RegisterServer.GetMyToken();
            bool serverOk = false;

            if (!string.IsNullOrEmpty(token))
            {
                var (ok, msg) = await ResearchManager.Unlock(successorId, predecessorId, token);
                serverOk = ok;
                if (!ok && !string.IsNullOrWhiteSpace(msg))
                {
                    // якщо сервер відмовився — покажемо повідомлення і НЕ робимо лок-анлок
                    Popup.ShowText(msg, Color.red);
                }
            }

            if (!serverOk)
            {
                // fallback: локальний unlock без списання XP (тимчасово)
                _unlockedSuccessors.Add(successorId);
                SaveLocalUnlocks();
            }

            // оновлюємо відображення айтема (переходимо у Coins-режим)
            item.SetActiveCoinsView(true);
            item.SetActiveXpView(false);

            // кнопка покупки
            WarriorDto lite = GetVehicleLite(successorId);
            
            SetupCoinsBuyView(item, new WarriorGraphNode
            {
                id = successorId, code = lite != null ? lite.code : "", name = item.vehicleName.text
            }, lite);

            Helpers.Loading.Hide();
        }

        private void SetupCoinsBuyView(TreeItem item, WarriorGraphNode node, WarriorDto liteInfo)
        {
            item.button.onClick.RemoveAllListeners();

            IPlayerClientInfo info = ServiceLocator.Get<IPlayerClientInfo>();
            if (info == null || info.Profile == null) return;

            bool have = _ownedIds.Contains(node.id);
            if (have)
            {
                // вже володіє — нічого не робимо
                item.button.onClick.AddListener(() => { /* вже куплено */ });
                return;
            }

            int bolts = info.Profile.coins;
            if (liteInfo == null)
            {
                item.button.onClick.AddListener(() => Popup.ShowText("No price data.", Color.red));
                return;
            }

            if (bolts >= liteInfo.purchaseCost)
            {
                item.button.onClick.AddListener(() =>
                {
                    Popup.ShowText($"Do you want buy?\nprice: {liteInfo.purchaseCost}", Color.green, () =>
                    {
                        Helpers.Loading.Show();
                        BuyRPC(ClientManager.Connection.ClientId, liteInfo.code);
                    }, TypePopup.Confirm);
                });
            }
            else
            {
                item.button.onClick.AddListener(() =>
                    Popup.ShowText($"Not enough coins. Need {liteInfo.purchaseCost}", Color.red));
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void BuyRPC(int clientID, string code)
        {
            Buy(clientID, code);
        }

        private async void Buy(int clientID, string code)
        {
            string token = RegisterServer.GetToken(clientID);
            (bool ok, string message) result = await UserWarriorsManager.BuyWarrior(code, token);

            if (!ServerManager.Clients.TryGetValue(clientID, out NetworkConnection senderConn))
                return;

            TargetRpcBuy(senderConn, result.ok, result.message);
        }

        [TargetRpc]
        private void TargetRpcBuy(NetworkConnection target, bool success, string errorMessage)
        {
            if (success)
            {
                ProfileServer.UpdateProfile();
            }
            else
            {
                Popup.ShowText(errorMessage, Color.red);
            }

            MenuManager.OpenMenu(MenuType.MainMenu);
            RobotView.UpdateUI();
            Helpers.Loading.Hide();
        }

        private Vehicle ParseVehicleClass(string cls)
        {
            if (string.Equals(cls, "Ranged", StringComparison.OrdinalIgnoreCase)) return Vehicle.Ranged;
            if (string.Equals(cls, "Heavy", StringComparison.OrdinalIgnoreCase)) return Vehicle.Heavy;
            return Vehicle.Light;
        }

        private async UniTask RedrawActive(int index)
        {
            await GameplayAssistant.RebuildAllLayouts(_fractionRoots);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            if (arrowDrawer == null || index < 0 || index >= _views.Count) return;

            FactionView v = _views[index];
            if (v.ArrowsLayer == null || v.Edges == null || v.Edges.Length == 0) return;

            arrowDrawer.Draw(v.Edges, v.NodeMap, v.ArrowsLayer);
        }

        private void DrawArrowsAll()
        {
            if (arrowDrawer == null) return;

            foreach (FactionView v in _views)
            {
                if (v.ArrowsLayer == null || v.Edges == null || v.Edges.Length == 0) continue;
                arrowDrawer.Draw(v.Edges, v.NodeMap, v.ArrowsLayer);
            }
        }

        private void WipeUI()
        {
            foreach (Transform t in _fractionRoots.Where(t => t != null))
                DestroyImmediate(t.gameObject);
            _fractionRoots.Clear();

            foreach (Button b in _factionButtons.Where(b => b != null))
                DestroyImmediate(b.gameObject);
            _factionButtons.Clear();
        }

        private void BuildFactionButtons()
        {
            if (buttonsContainer == null || factionButtonPrefab == null) return;

            for (int i = 0; i < _factionCodes.Length; i++)
            {
                int idx = i;
                string code = _factionCodes[i];
                string title = _factionNameByCode.TryGetValue(code, out string n) && !string.IsNullOrWhiteSpace(n) ? n : code;

                Button btn = Instantiate(factionButtonPrefab, buttonsContainer);
                _factionButtons.Add(btn);

                TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = title;

                btn.onClick.AddListener(() =>
                {
                    SetActiveContainer(idx);
                    _ = RedrawActive(idx);
                });
            }
        }
    }
}
