using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Game.Systems.Endings;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [System.Serializable]
        private struct EndingSlotConfig
        {
            public string endingId;
            public string unlockedTextKey;
        }

        private enum MenuState
        {
            Root,
            Endings
        }

        [Header("Refs")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private GameObject titleObject;
        [SerializeField] private TMP_Text newGameText;
        [SerializeField] private TMP_Text endingsText;
        [SerializeField] private GameObject endingsPanel;
        [SerializeField] private TMP_Text endingsTitleText;
        [SerializeField] private TMP_Text endingUpText;
        [SerializeField] private TMP_Text endingRightText;
        [SerializeField] private TMP_Text endingDownText;
        [SerializeField] private TMP_Text endingLeftText;
        [SerializeField] private Image endingUpImage;
        [SerializeField] private Image endingRightImage;
        [SerializeField] private Image endingDownImage;
        [SerializeField] private Image endingLeftImage;
        [SerializeField] private Image endingUpQuestionImage;
        [SerializeField] private Image endingRightQuestionImage;
        [SerializeField] private Image endingDownQuestionImage;
        [SerializeField] private Image endingLeftQuestionImage;
        [SerializeField] private TMP_Text backText;
        [SerializeField] private TMP_Text statusText;

        [Header("Flow")]
        [SerializeField] private string bootSceneName = "Boot";
        [SerializeField] private string firstGameplayScene = "Room_Lab_PlayerHouse";
        [SerializeField] private string firstSpawnId = string.Empty;
        [SerializeField] private bool clearRuntimeOnNewGame = true;

        [Header("Localization Keys")]
        [SerializeField] private string newGameTextKey = "ui.mainmenu.new_game";
        [SerializeField] private string endingsTextKey = "ui.mainmenu.endings";
        [SerializeField] private string endingsTitleTextKey = "ui.mainmenu.endings_title";
        [SerializeField] private string backTextKey = "ui.mainmenu.back";
        [SerializeField] private string lockedEndingTextKey = "ui.mainmenu.ending.locked";
        [SerializeField] private LocalizationCatalog menuLocalizationCatalog;
        [SerializeField] private string menuLocale = "zh-CN";

        [Header("Endings Slots (Up / Right / Down / Left)")]
        [SerializeField] private EndingSlotConfig upEnding = new EndingSlotConfig
        {
            endingId = "ending.up",
            unlockedTextKey = "ui.mainmenu.ending.up"
        };
        [SerializeField] private EndingSlotConfig rightEnding = new EndingSlotConfig
        {
            endingId = "ending.right",
            unlockedTextKey = "ui.mainmenu.ending.right"
        };
        [SerializeField] private EndingSlotConfig downEnding = new EndingSlotConfig
        {
            endingId = "ending.down",
            unlockedTextKey = "ui.mainmenu.ending.down"
        };
        [SerializeField] private EndingSlotConfig leftEnding = new EndingSlotConfig
        {
            endingId = "ending.left",
            unlockedTextKey = "ui.mainmenu.ending.left"
        };

        [Header("Visual")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color dimColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        [Header("Display Lock")]
        [SerializeField] private bool lockResolutionOnMainMenu = true;
        [SerializeField] private int lockedWidth = 1920;
        [SerializeField] private int lockedHeight = 1080;

        [Header("Hit Area")]
        [SerializeField] private bool useRenderedTextHitArea = true;
        [SerializeField] private Vector2 renderedTextHitPadding = new Vector2(6f, 4f);

        private MenuState state = MenuState.Root;
        private int hoveredRootIndex = -1;
        private bool hoveredBack;
        private readonly Dictionary<string, string> menuLocalizationMap = new Dictionary<string, string>(StringComparer.Ordinal);
        private bool menuLocalizationLoaded;

        private const string LockedEndingFallback = "\uFF1F\uFF1F\uFF1F";
        private const string BackTextFallback = "\u8FD4\u56DE";

        private void Awake()
        {
            ApplyMainMenuResolutionLock();
            TryAutoBind();
            EnsureMenuLocalizationLoaded();
            SetState(MenuState.Root);
            RefreshEndingsText();
            RefreshVisuals();
        }

        private IEnumerator Start()
        {
            // Some platforms may apply display mode after scene load.
            // Reapply once on next frame to keep the window size locked.
            yield return null;
            ApplyMainMenuResolutionLock();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;
            ApplyMainMenuResolutionLock();
        }

        private void Update()
        {
            if (input == null)
                input = FindFirstObjectByType<PlayerInputReader>();

            if (input == null) return;

            UpdateHover();
            HandleConfirmAndCancelInput();
            HandleMouseClickInput();

            RefreshVisuals();
        }

        private void TryAutoBind()
        {
            if (input == null)
                input = FindFirstObjectByType<PlayerInputReader>();

            if (titleObject == null)
                titleObject = FindObjectByName("Title");

            if (newGameText == null)
                newGameText = FindTextByName("NewGameText");

            if (endingsText == null)
                endingsText = FindTextByName("EndingsText");

            if (endingsPanel == null)
            {
                Transform t = transform.Find("Canvas/EndingsPanel");
                endingsPanel = t != null ? t.gameObject : null;
            }

            if (endingsTitleText == null)
                endingsTitleText = FindTextByName("EndingsTitleText");

            if (endingUpText == null)
                endingUpText = FindTextByName("EndingUpText");

            if (endingRightText == null)
                endingRightText = FindTextByName("EndingRightText");

            if (endingDownText == null)
                endingDownText = FindTextByName("EndingDownText");

            if (endingLeftText == null)
                endingLeftText = FindTextByName("EndingLeftText");

            if (endingUpImage == null)
                endingUpImage = FindImageByName("EndingUpImage");

            if (endingRightImage == null)
                endingRightImage = FindImageByName("EndingRightImage");

            if (endingDownImage == null)
                endingDownImage = FindImageByName("EndingDownImage");

            if (endingLeftImage == null)
                endingLeftImage = FindImageByName("EndingLeftImage");

            if (endingUpQuestionImage == null)
                endingUpQuestionImage = FindImageByName("EndingUpQuestionImage");

            if (endingRightQuestionImage == null)
                endingRightQuestionImage = FindImageByName("EndingRightQuestionImage");

            if (endingDownQuestionImage == null)
                endingDownQuestionImage = FindImageByName("EndingDownQuestionImage");

            if (endingLeftQuestionImage == null)
                endingLeftQuestionImage = FindImageByName("EndingLeftQuestionImage");

            if (backText == null)
                backText = FindTextByName("BackText");

            if (statusText == null)
                statusText = FindTextByName("StatusText");
        }

        private TMP_Text FindTextByName(string objectName)
        {
            var all = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == objectName)
                    return all[i];
            }

            return null;
        }

        private Image FindImageByName(string objectName)
        {
            var all = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == objectName)
                    return all[i];
            }

            return null;
        }

        private GameObject FindObjectByName(string objectName)
        {
            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == objectName)
                    return all[i].gameObject;
            }

            return null;
        }

        private void SetState(MenuState next)
        {
            state = next;
            hoveredRootIndex = -1;
            hoveredBack = false;

            if (endingsPanel != null)
                endingsPanel.SetActive(state == MenuState.Endings);

            if (state == MenuState.Endings)
                RefreshEndingsText();

            RefreshRootVisibility();
        }

        private void HandleConfirmAndCancelInput()
        {
            input.ConsumeContinueDown();
            input.ConsumeInteractDown();
            bool cancel = input.ConsumeCancelDown();

            if (state == MenuState.Endings)
            {
                if (cancel)
                    SetState(MenuState.Root);
            }
        }

        private void HandleMouseClickInput()
        {
            if (!input.ConsumeClickDown(out Vector2 clickPos))
                return;

            if (state == MenuState.Root)
            {
                int clicked = GetHoveredRootOption(clickPos);
                if (clicked >= 0)
                {
                    ExecuteRootOption(clicked);
                }
                return;
            }

            if (state == MenuState.Endings)
            {
                if (IsTextHit(GetBackClickText(), clickPos))
                    SetState(MenuState.Root);
            }
        }

        private void UpdateHover()
        {
            Vector2 pointer = input.PointerPos;

            if (state == MenuState.Root)
            {
                hoveredRootIndex = GetHoveredRootOption(pointer);
                hoveredBack = false;
                return;
            }

            if (state == MenuState.Endings)
            {
                hoveredRootIndex = -1;
                hoveredBack = IsTextHit(GetBackClickText(), pointer);
            }
        }

        private int GetHoveredRootOption(Vector2 screenPos)
        {
            if (IsTextHit(newGameText, screenPos)) return 0;
            if (IsTextHit(endingsText, screenPos)) return 1;
            return -1;
        }

        private void ExecuteRootOption(int index)
        {
            switch (index)
            {
                case 0:
                    StartNewGame();
                    break;
                case 1:
                    SetState(MenuState.Endings);
                    break;
            }
        }

        private void StartNewGame()
        {
            if (string.IsNullOrWhiteSpace(firstGameplayScene))
            {
                if (statusText != null) statusText.text = string.Empty;
                Debug.LogError("[MainMenuController] firstGameplayScene is empty.");
                return;
            }

            if (clearRuntimeOnNewGame && GameRoot.I != null)
                GameRoot.I.ResetRuntimeForNewGame();

            if (GameRoot.I != null)
            {
                GameRoot.I.TransitionTo(firstGameplayScene, firstSpawnId);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(bootSceneName))
                {
                    Debug.LogError("[MainMenuController] bootSceneName is empty.");
                    SceneManager.LoadScene(firstGameplayScene);
                    return;
                }

                BootLoader.RequestStartGame(firstGameplayScene, firstSpawnId);
                SceneManager.LoadScene(bootSceneName);
            }
        }

        private void RefreshEndingsText()
        {
            if (endingsTitleText != null)
                endingsTitleText.text = Loc(endingsTitleTextKey);

            if (backText != null)
                backText.text = LocOrFallback(backTextKey, BackTextFallback);

            RefreshEndingSlot(upEnding, endingUpText, endingUpImage, endingUpQuestionImage);
            RefreshEndingSlot(rightEnding, endingRightText, endingRightImage, endingRightQuestionImage);
            RefreshEndingSlot(downEnding, endingDownText, endingDownImage, endingDownQuestionImage);
            RefreshEndingSlot(leftEnding, endingLeftText, endingLeftImage, endingLeftQuestionImage);
        }

        private void RefreshVisuals()
        {
            RefreshRootVisibility();

            if (newGameText != null)
            {
                bool highlighted = state == MenuState.Root && hoveredRootIndex == 0;
                newGameText.color = highlighted ? highlightColor : (state == MenuState.Root ? normalColor : dimColor);
                newGameText.text = Loc(newGameTextKey);
            }

            if (endingsText != null)
            {
                bool highlighted = state == MenuState.Root && hoveredRootIndex == 1;
                endingsText.color = highlighted ? highlightColor : (state == MenuState.Root ? normalColor : dimColor);
                endingsText.text = Loc(endingsTextKey);
            }

            if (backText != null)
            {
                bool highlighted = state == MenuState.Endings && hoveredBack;
                backText.color = highlighted ? highlightColor : (state == MenuState.Endings ? normalColor : dimColor);
            }

            if (endingsPanel != null)
                endingsPanel.SetActive(state == MenuState.Endings);
        }

        private void RefreshRootVisibility()
        {
            bool showRoot = state == MenuState.Root;

            if (titleObject != null && titleObject.activeSelf != showRoot)
                titleObject.SetActive(showRoot);

            if (newGameText != null && newGameText.gameObject.activeSelf != showRoot)
                newGameText.gameObject.SetActive(showRoot);

            if (endingsText != null && endingsText.gameObject.activeSelf != showRoot)
                endingsText.gameObject.SetActive(showRoot);
        }

        private TMP_Text GetBackClickText()
        {
            return backText;
        }

        private void RefreshEndingSlot(EndingSlotConfig slot, TMP_Text text, Image unlockedImage, Image lockedImage)
        {
            bool unlocked = !string.IsNullOrWhiteSpace(slot.endingId) && EndingCollectionService.IsUnlocked(slot.endingId);

            if (text != null)
                text.text = unlocked ? Loc(slot.unlockedTextKey) : LocOrFallback(lockedEndingTextKey, LockedEndingFallback);

            if (unlockedImage != null)
                unlockedImage.gameObject.SetActive(unlocked);

            if (lockedImage != null)
                lockedImage.gameObject.SetActive(!unlocked);
        }

        private string Loc(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;

            var loc = GameRoot.I != null ? GameRoot.I.Localization : null;
            if (loc != null)
            {
                string value = loc.Get(key);
                if (!IsMissingLocalizationValue(value, key))
                    return value;
            }

            EnsureMenuLocalizationLoaded();
            if (menuLocalizationMap.TryGetValue(key, out string localValue))
                return localValue;

            return $"[{key}]";
        }

        private string LocOrFallback(string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key)) return fallback;
            string value = Loc(key);
            return IsMissingLocalizationValue(value, key) ? fallback : value;
        }

        private static bool IsMissingLocalizationValue(string value, string key)
        {
            if (string.IsNullOrEmpty(value)) return true;
            return string.Equals(value, key, StringComparison.Ordinal) ||
                   string.Equals(value, "[" + key + "]", StringComparison.Ordinal);
        }

        private void EnsureMenuLocalizationLoaded()
        {
            if (menuLocalizationLoaded) return;
            menuLocalizationLoaded = true;

            menuLocalizationMap.Clear();
            if (menuLocalizationCatalog == null || string.IsNullOrWhiteSpace(menuLocale)) return;

            var table = menuLocalizationCatalog.Get(menuLocale.Trim());
            if (table == null || table.csv == null) return;

            ParseCsvToMap(table.csv.text, menuLocalizationMap);
        }

        private static void ParseCsvToMap(string csv, Dictionary<string, string> outMap)
        {
            var rows = ReadCsvRows(csv);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count < 2) continue;

                string rowKey = row[0].Trim();
                rowKey = rowKey.TrimStart('\uFEFF');
                if (string.IsNullOrEmpty(rowKey) || string.Equals(rowKey, "key", StringComparison.Ordinal)) continue;

                outMap[rowKey] = row[1];
            }
        }

        private static List<List<string>> ReadCsvRows(string csv)
        {
            var result = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool inQuotes = false;

            if (string.IsNullOrEmpty(csv))
            {
                result.Add(row);
                return result;
            }

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                }
                else if (c == '\r')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                    result.Add(row);
                    row = new List<string>();

                    if (i + 1 < csv.Length && csv[i + 1] == '\n')
                        i++;
                }
                else if (c == '\n')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                    result.Add(row);
                    row = new List<string>();
                }
                else
                {
                    cell.Append(c);
                }
            }

            row.Add(cell.ToString());
            result.Add(row);
            return result;
        }

        private bool IsTextHit(TMP_Text text, Vector2 screenPos)
        {
            if (text == null || !text.gameObject.activeInHierarchy) return false;

            RectTransform rect = text.rectTransform;
            if (rect == null) return false;

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            if (!useRenderedTextHitArea)
                return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, cam, out Vector2 localPoint))
                return false;

            text.ForceMeshUpdate();
            Bounds textBounds = text.textBounds;
            if (textBounds.size.sqrMagnitude <= 0.0001f)
                return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam);

            Vector2 min = new Vector2(textBounds.min.x, textBounds.min.y) - renderedTextHitPadding;
            Vector2 max = new Vector2(textBounds.max.x, textBounds.max.y) + renderedTextHitPadding;

            return localPoint.x >= min.x && localPoint.x <= max.x &&
                   localPoint.y >= min.y && localPoint.y <= max.y;
        }

        private void ApplyMainMenuResolutionLock()
        {
            if (!lockResolutionOnMainMenu) return;

            int width = Mathf.Max(320, lockedWidth);
            int height = Mathf.Max(180, lockedHeight);

            Screen.fullScreen = false;
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }
    }
}
