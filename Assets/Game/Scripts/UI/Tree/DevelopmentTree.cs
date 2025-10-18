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

        private readonly HashSet<int> _ownedIds = new();
        private readonly Dictionary<int, int> _xpByOwnedWarriorId = new(); // WarriorId -> Xp
        private readonly HashSet<int> _unlockedSuccessors = new(); // Successor WarriorId
        private int _freeXpCache = 0;

        private readonly Color _xpDefaultColor = Color.white;
        private readonly Color _xpReadyColor = new Color(1f, 0.88f, 0.2f);

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
            _freeXpCache = 0;

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
                if (!string.IsNullOrWhiteSpace(v.cultureCode) && !_factionNameByCode.ContainsKey(v.cultureCode))
                    _factionNameByCode[v.cultureCode] = string.IsNullOrWhiteSpace(v.cultureName) ? v.cultureCode : v.cultureName;

            _factionCodes = _vehicleLites
                .Select(v => v.cultureCode)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            List<WarriorGraphResponse> graphs = new(_factionCodes.Length);
            foreach (string culture in _factionCodes)
            {
                (bool okGraph, _, WarriorGraphResponse graph) = await WarriorsManager.GetGraph(culture);
                graphs.Add(okGraph && graph != null && graph.nodes != null && graph.nodes.Length > 0
                    ? graph
                    : new WarriorGraphResponse { nodes = Array.Empty<WarriorGraphNode>(), edges = Array.Empty<WarriorGraphEdge>() });
                if (!okGraph) Popup.ShowText($"Failed to get graph for culture: {culture}", Color.red);
            }
            _graphs = graphs.ToArray();

            RefreshPlayerCachesFromServiceLocator();
            await LoadUnlocksSafe();

            return true;
        }

        private void RefreshPlayerCachesFromServiceLocator()
        {
            IPlayerClientInfo info = ServiceLocator.Get<IPlayerClientInfo>();
            if (info != null && info.Profile != null)
            {
                _freeXpCache = info.Profile.freeXp;
                if (info.Profile.ownedWarriors != null)
                {
                    foreach (var ow in info.Profile.ownedWarriors)
                    {
                        _ownedIds.Add(ow.warriorId);
                        _xpByOwnedWarriorId[ow.warriorId] = ow.xp;
                    }
                }
            }
        }

        private async UniTask LoadUnlocksSafe()
        {
            string token = RegisterServer.GetMyToken();
            if (!string.IsNullOrEmpty(token))
            {
                var (ok, _, ids) = await ResearchManager.GetMyUnlocked(token);
                if (ok && ids != null)
                {
                    _unlockedSuccessors.Clear();
                    foreach (int id in ids) _unlockedSuccessors.Add(id);
                    SaveLocalUnlocks();
                    return;
                }
            }
            LoadLocalUnlocks();
        }

        private void SaveLocalUnlocks()
        {
            PlayerPrefs.SetString("Tree_Unlocks", string.Join(",", _unlockedSuccessors));
            PlayerPrefs.Save();
        }

        private void LoadLocalUnlocks()
        {
            _unlockedSuccessors.Clear();
            string s = PlayerPrefs.GetString("Tree_Unlocks", "");
            if (string.IsNullOrWhiteSpace(s)) return;
            foreach (var p in s.Split(','))
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
                await BuildFactionTreeUI(_factionCodes[i], _graphs[i]);

            if (_fractionRoots.Count > 0) SetActiveContainer(0);

            await GameplayAssistant.RebuildAllLayouts(_fractionRoots);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            DrawArrowsAll();
        }

        public void SetActiveContainer(int number)
        {
            for (int i = 0; i < _fractionRoots.Count; i++)
                if (_fractionRoots[i] != null)
                    _fractionRoots[i].gameObject.SetActive(i == number);
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
                CreateTreeItemFromNode(grid.transform, n, nodeMap);
        }

        private void CreateTreeItemFromNode(Transform parent, WarriorGraphNode node, Dictionary<int, RectTransform> nodeMap)
        {
            if (node == null) return;

            TreeItem item = Instantiate(treeItemPrefab, parent);

            item.vehicleName.text = node.name;
            item.vehicleType = ParseVehicleClass(node.@class);
            item.level.text = node.level.ToString();
            item.isClose.SetActive(!node.isVisible);

            bool isHave = _ownedIds.Contains(node.id);
            item.isHave.gameObject.SetActive(isHave);

            item.image.sprite = ResourceManager.GetIcon(node.code);

            WarriorDto liteInfo = GetVehicleLite(node.id);
            item.price.text = liteInfo != null ? liteInfo.purchaseCost.ToString() : "0";

            bool isLevel1 = node.level <= 1;
            bool isUnlocked = isLevel1 || _unlockedSuccessors.Contains(node.id) || isHave;

            if (!isHave && !isUnlocked)
            {
                item.SetActiveCoinsView(false);
                item.SetActiveXpView(true);
                SetupXpResearchView(item, node);
            }
            else
            {
                item.SetActiveCoinsView(true);
                item.SetActiveXpView(false);
                item.xp.color = _xpDefaultColor;
                SetupCoinsBuyView(item, node, liteInfo);
            }

            nodeMap[node.id] = item.rectTransform;
        }

        private void SetupXpResearchView(TreeItem item, WarriorGraphNode node)
        {
            item.xp.text = "…";
            item.xp.color = _xpDefaultColor;
            item.button.onClick.RemoveAllListeners();

            _ = SetupXpAsync();

            async UniTaskVoid SetupXpAsync()
            {
                RefreshPlayerCachesFromServiceLocator();

                var (okLinks, _, links) = await WarriorsManager.GetResearchFrom(node.id);
                if (!okLinks || links == null || links.Length == 0)
                {
                    item.xp.text = "-";
                    item.xp.color = _xpDefaultColor;
                    item.button.onClick.AddListener(() => Popup.ShowText("No research path.", Color.red));
                    return;
                }

                // На плитці — загальна вартість дослідження (мінімальна з ребер)
                int minRequired = links.Min(l => l.requiredXp);
                item.xp.text = minRequired.ToString();

                // Кандидати-предки, якими володіємо
                var ownedCandidates = links
                    .Where(l => _ownedIds.Contains(l.predecessorId))
                    .Select(l =>
                    {
                        int predXp = _xpByOwnedWarriorId.TryGetValue(l.predecessorId, out var x) ? x : 0;
                        int needFromFree = Mathf.Max(0, l.requiredXp - predXp);
                        bool enoughTotal = (predXp + _freeXpCache) >= l.requiredXp;
                        return new
                        {
                            link = l,
                            predXp,
                            needFromFree,
                            enoughTotal
                        };
                    })
                    .OrderBy(x => x.needFromFree)
                    .ThenBy(x => x.link.requiredXp)
                    .ToList();

                var bestOwned = ownedCandidates.FirstOrDefault();

                // Жовтий колір, якщо сумарно вистачає (XP предка + Free XP)
                if (bestOwned != null && bestOwned.enoughTotal)
                    item.xp.color = _xpReadyColor;
                else
                    item.xp.color = _xpDefaultColor;

                // Клік — Confirm із повною ціною та розкладом джерел
                item.button.onClick.AddListener(() =>
                {
                    if (bestOwned != null)
                    {
                        int req = bestOwned.link.requiredXp;
                        int spendFromPred = Mathf.Min(req, bestOwned.predXp);
                        int spendFree = Mathf.Max(0, req - spendFromPred);

                        string title =
                            $"Research '{node.name}'?\n" +
                            $"Cost: {req} (from predecessor: {spendFromPred}, Free XP: {spendFree})";

                        Popup.ShowText(title, Color.green, () =>
                        {
                            TryResearchWithFreeXp(node.id, bestOwned.link.predecessorId, req, bestOwned.predXp);
                        }, TypePopup.Confirm);
                    }
                    else
                    {
                        // немає у власності жодного предка — просто показуємо повну ціну
                        var bestAny = links.OrderBy(l => l.requiredXp).First();
                        string msg =
                            $"Research '{node.name}'?\n" +
                            $"Cost: {bestAny.requiredXp}\n" +
                            $"You need to own a predecessor.";
                        Popup.ShowText(msg, Color.yellow);
                    }
                });
            }
        }

        private async void TryResearchWithFreeXp(int successorId, int predecessorId, int requiredOnPred, int currentPredXp)
        {
            Helpers.Loading.Show();

            RefreshPlayerCachesFromServiceLocator();

            int needFromFree = Mathf.Max(0, requiredOnPred - currentPredXp);

            string token = RegisterServer.GetMyToken();
            bool converted = true;

            if (needFromFree > 0)
            {
                if (_freeXpCache < needFromFree)
                {
                    Helpers.Loading.Hide();
                    int lack = needFromFree - _freeXpCache;
                    Popup.ShowText($"Not enough Free XP. Need {lack} more.", Color.red);
                    return;
                }

                var (okConv, msgConv) = await UserWarriorsManager.ConvertFreeXp(predecessorId, needFromFree, token);
                if (!okConv)
                {
                    converted = false;
                    Popup.ShowText(string.IsNullOrWhiteSpace(msgConv) ? "Free XP conversion failed." : msgConv, Color.red);
                }
            }

            bool unlocked = false;
            if (converted)
            {
                var (ok, msg) = await ResearchManager.Unlock(successorId, predecessorId, token);
                unlocked = ok;
                if (!ok && !string.IsNullOrWhiteSpace(msg))
                    Popup.ShowText(msg, Color.red);
            }

            await ReloadAndRebuildAsync();

            Helpers.Loading.Hide();
            if (unlocked) Popup.ShowText("Researched!", Color.green);
        }

        private void SetupCoinsBuyView(TreeItem item, WarriorGraphNode node, WarriorDto liteInfo)
        {
            item.button.onClick.RemoveAllListeners();

            IPlayerClientInfo info = ServiceLocator.Get<IPlayerClientInfo>();
            if (info == null || info.Profile == null) return;

            bool have = _ownedIds.Contains(node.id);
            if (have)
            {
                item.button.onClick.AddListener(() => { /* already owned */ });
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
                _ = ReloadAndRebuildAsync();
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

        private async UniTask ReloadAndRebuildAsync()
        {
            ProfileServer.UpdateProfile();
            await UniTask.Delay(50);
            await LoadDataFromServer();
            await UpdateUI();
        }
    }
}
