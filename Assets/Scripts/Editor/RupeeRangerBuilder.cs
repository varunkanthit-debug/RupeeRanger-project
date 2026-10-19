using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using RupeeRanger.Core;
using RupeeRanger.Data;
using RupeeRanger.UI;
using RupeeRanger.Town;
using RupeeRanger.MiniGames;

namespace RupeeRanger.Editor
{
    [InitializeOnLoad]
    public static class RupeeRangerBuilder
    {
        private const string SCENE_DIR = "Assets/Scenes";
        private const string DATA_DIR = "Assets/Data";
        private const string SPRITES_DIR = "Assets/Sprites";
        private const string FONTS_DIR = "Assets/Fonts";

        static RupeeRangerBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("RupeeRanger_Rebuilt_Phase7_ResultScene_FINAL_v1", false))
                {
                    SessionState.SetBool("RupeeRanger_Rebuilt_Phase7_ResultScene_FINAL_v1", true);
                    Debug.Log("[RupeeRanger] Auto-building Result scene for Phase 7...");
                    CreateScriptableObjects(out _, out List<CharacterData> characters);
                    BuildResultScene(characters);
                }
            };
        }

        [MenuItem("Tools/RupeeRanger/Build Scenes", false, 1)]
        public static void BuildAllScenesFromMenu()
        {
            BuildAllScenesInternal(true);
        }

        public static void BuildAllScenesBatch()
        {
            BuildAllScenesInternal(false);
        }

        [MenuItem("Tools/RupeeRanger/Re-import Sprites", false, 2)]
        public static void ReimportAssets()
        {
            ConfigureSpriteImports();
            AssetDatabase.Refresh();
            Debug.Log("[RupeeRanger] Sprites re-imported!");
        }

        public static void BuildAllScenesInternal(bool showDialog)
        {
            try
            {
                Debug.Log("[RupeeRanger] Starting automated scene and asset build...");

                // 1. Ensure folders exist
                EnsureDirectories();

                // 2. Configure Sprite Import settings
                ConfigureSpriteImports();

                // 3. Create ScriptableObjects (Lifestyles, Characters, and Tasks)
                CreateScriptableObjects(out List<LifestyleConfig> lifestyles, out List<CharacterData> characters);
                CreateTaskScriptableObjects(out List<TaskData> tasks);

                // 4. Build Scenes
                string splashPath = BuildSplashScene();
                string setupPath = BuildPlayerSetupScene(lifestyles, characters);
                string expensePath = BuildExpenseSetupScene();
                string townPath = BuildTownScene(tasks);
                string resultPath = BuildResultScene(characters);
                string wordSearchPath = BuildWordSearchScene();
                string wordFinderPath = BuildWordFinderScene();
                string mathSpeedPath = BuildMathSpeedScene();
                string mathPuzzlePath = BuildMathPuzzleScene();
                string memoryMatchPath = BuildMemoryMatchScene();
                string quizMasterPath = BuildQuizMasterScene();

                // 5. Set Build Settings
                UpdateBuildSettings(new[] {
                    splashPath,
                    setupPath,
                    expensePath,
                    townPath,
                    resultPath,
                    wordSearchPath,
                    wordFinderPath,
                    mathSpeedPath,
                    mathPuzzlePath,
                    memoryMatchPath,
                    quizMasterPath
                });

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Open Splash scene
                EditorSceneManager.OpenScene(splashPath);

                Debug.Log("[RupeeRanger] Build complete! All 11 scenes created, configured, and added to Build Settings.");

                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "RupeeRanger",
                        "All 11 scenes built successfully!\n\nIncludes 6 Mini-Games:\n- Word Search\n- Word Finder\n- Maths Speed\n- Maths Puzzle\n- Memory Match (Arcade)\n- Quiz Master (Library)\n\nClick Play in the Editor to test!",
                        "OK"
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RupeeRanger] Build failed with error: {ex.Message}\n{ex.StackTrace}");
                if (showDialog)
                {
                    EditorUtility.DisplayDialog("RupeeRanger Build Error", $"Build failed: {ex.Message}", "OK");
                }
            }
        }

        private static void EnsureDirectories()
        {
            string[] dirs = {
                SCENE_DIR,
                DATA_DIR,
                SPRITES_DIR,
                FONTS_DIR,
                "Assets/Scripts/Core",
                "Assets/Scripts/UI",
                "Assets/Scripts/Data",
                "Assets/Scripts/Editor",
                "Assets/Prefabs",
                "Assets/Audio"
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }
        }

        #region Sprite Configuration

        public static void ConfigureSpriteImports()
        {
            if (!Directory.Exists(SPRITES_DIR)) return;

            string[] spriteFiles = Directory.GetFiles(SPRITES_DIR, "*.png", SearchOption.AllDirectories);
            foreach (string file in spriteFiles)
            {
                string assetPath = file.Replace('\\', '/');
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    bool modified = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        modified = true;
                    }

                    if (assetPath.Contains("/Town/"))
                    {
                        if (assetPath.Contains("tile_") || assetPath.Contains("prop_") || assetPath.Contains("building_") || assetPath.Contains("guide_arrow"))
                        {
                            if (importer.spritePixelsPerUnit != 32)
                            {
                                importer.spritePixelsPerUnit = 32;
                                modified = true;
                            }
                            if (importer.filterMode != FilterMode.Point)
                            {
                                importer.filterMode = FilterMode.Point;
                                modified = true;
                            }
                            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                            {
                                importer.textureCompression = TextureImporterCompression.Uncompressed;
                                modified = true;
                            }
                        }
                        else
                        {
                            if (importer.spritePixelsPerUnit != 100)
                            {
                                importer.spritePixelsPerUnit = 100;
                                modified = true;
                            }
                        }
                    }
                    else if (assetPath.Contains("char_"))
                    {
                        if (importer.spritePixelsPerUnit != 600)
                        {
                            importer.spritePixelsPerUnit = 600;
                            modified = true;
                        }
                    }
                    else
                    {
                        if (importer.spritePixelsPerUnit != 100)
                        {
                            importer.spritePixelsPerUnit = 100;
                            modified = true;
                        }
                    }

                    if (assetPath.Contains("rounded_box"))
                    {
                        Vector4 targetBorder = new Vector4(24, 24, 24, 24);
                        if (importer.spriteBorder != targetBorder)
                        {
                            importer.spriteBorder = targetBorder;
                            modified = true;
                        }
                    }
                    else if (assetPath.Contains("slider_track") || assetPath.Contains("slider_fill"))
                    {
                        Vector4 targetBorder = new Vector4(6, 4, 6, 4);
                        if (importer.spriteBorder != targetBorder)
                        {
                            importer.spriteBorder = targetBorder;
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport();
                    }
                }
            }
        }

        #endregion

        #region ScriptableObjects

        private static void CreateScriptableObjects(out List<LifestyleConfig> lifestyles, out List<CharacterData> characters)
        {
            lifestyles = new List<LifestyleConfig>();
            characters = new List<CharacterData>();

            Sprite iconBasic = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_lifestyle_basic.png");
            Sprite iconMiddle = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_lifestyle_middle.png");
            Sprite iconHigh = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_lifestyle_high.png");

            // 1. Basic Lifestyle
            LifestyleConfig basic = GetOrCreateAsset<LifestyleConfig>($"{DATA_DIR}/Lifestyle_Basic.asset");
            basic.lifestyleType = LifestyleType.Basic;
            basic.displayName = "Basic Lifestyle";
            basic.subtitle = "Frugal living";
            basic.monthlyIncome = 8000;
            basic.maxRent = 8000;
            basic.maxFood = 4000;
            basic.maxTransport = 1500;
            basic.maxEntertainment = 2500;
            basic.icon = iconBasic;
            basic.themeColor = new Color32(0x1E, 0x88, 0xE5, 0xFF);
            EditorUtility.SetDirty(basic);
            lifestyles.Add(basic);

            // 2. Middle Lifestyle
            LifestyleConfig middle = GetOrCreateAsset<LifestyleConfig>($"{DATA_DIR}/Lifestyle_Middle.asset");
            middle.lifestyleType = LifestyleType.Middle;
            middle.displayName = "Middle Lifestyle";
            middle.subtitle = "Comfortable living";
            middle.monthlyIncome = 15000;
            middle.maxRent = 15000;
            middle.maxFood = 8000;
            middle.maxTransport = 3000;
            middle.maxEntertainment = 5000;
            middle.icon = iconMiddle;
            middle.themeColor = new Color32(0x8E, 0x24, 0xAA, 0xFF);
            EditorUtility.SetDirty(middle);
            lifestyles.Add(middle);

            // 3. High Lifestyle
            LifestyleConfig high = GetOrCreateAsset<LifestyleConfig>($"{DATA_DIR}/Lifestyle_High.asset");
            high.lifestyleType = LifestyleType.High;
            high.displayName = "High Lifestyle";
            high.subtitle = "Premium living";
            high.monthlyIncome = 30000;
            high.maxRent = 30000;
            high.maxFood = 15000;
            high.maxTransport = 6000;
            high.maxEntertainment = 10000;
            high.icon = iconHigh;
            high.themeColor = new Color32(0xFF, 0x8F, 0x00, 0xFF);
            EditorUtility.SetDirty(high);
            lifestyles.Add(high);

            // Characters
            Sprite charPennySprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/char_penny.png");
            Sprite charEcoSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/char_eco.png");
            Sprite charTechSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/char_tech.png");
            Sprite charRoyalSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/char_royal.png");

            CharacterData penny = GetOrCreateAsset<CharacterData>($"{DATA_DIR}/Char_Penny.asset");
            penny.id = "penny";
            penny.characterName = "Penny Ranger";
            penny.sprite = charPennySprite;
            penny.unlockScore = 0;
            penny.description = "Smart, cheerful, and always ready to save rupees!";
            EditorUtility.SetDirty(penny);
            characters.Add(penny);

            CharacterData eco = GetOrCreateAsset<CharacterData>($"{DATA_DIR}/Char_Eco.asset");
            eco.id = "eco";
            eco.characterName = "Eco Ranger";
            eco.sprite = charEcoSprite;
            eco.unlockScore = 300;
            eco.description = "Green-minded explorer who saves on energy and bills!";
            EditorUtility.SetDirty(eco);
            characters.Add(eco);

            CharacterData tech = GetOrCreateAsset<CharacterData>($"{DATA_DIR}/Char_Tech.asset");
            tech.id = "tech";
            tech.characterName = "Tech Ranger";
            tech.sprite = charTechSprite;
            tech.unlockScore = 600;
            tech.description = "Tech-savvy wizard who optimizes every transaction!";
            EditorUtility.SetDirty(tech);
            characters.Add(tech);

            CharacterData royal = GetOrCreateAsset<CharacterData>($"{DATA_DIR}/Char_Royal.asset");
            royal.id = "royal";
            royal.characterName = "Royal Ranger";
            royal.sprite = charRoyalSprite;
            royal.unlockScore = 1000;
            royal.description = "The ultimate financial master of the kingdom!";
            EditorUtility.SetDirty(royal);
            characters.Add(royal);
        }

        private static void CreateTaskScriptableObjects(out List<TaskData> tasks)
        {
            tasks = new List<TaskData>();
            string tasksDir = $"{DATA_DIR}/Tasks";
            if (!Directory.Exists(tasksDir))
            {
                Directory.CreateDirectory(tasksDir);
            }

            string jsonPath = $"{DATA_DIR}/DayPlans.json";
            if (!File.Exists(jsonPath)) return;

            string json = File.ReadAllText(jsonPath);
            string wrappedJson = $"{{\"days\":{json}}}";
            DayScheduleData schedule = JsonUtility.FromJson<DayScheduleData>(wrappedJson);

            if (schedule != null && schedule.days != null)
            {
                foreach (var raw in schedule.days)
                {
                    string assetPath = $"{tasksDir}/Task_Day{raw.dayNumber}.asset";
                    TaskData task = GetOrCreateAsset<TaskData>(assetPath);
                    task.id = raw.id;
                    task.dayNumber = raw.dayNumber;
                    task.chapterTitle = raw.chapterTitle;
                    task.objectiveText = raw.objectiveText;
                    if (Enum.TryParse(raw.targetBuilding, out BuildingType bType))
                    {
                        task.targetBuilding = bType;
                    }
                    task.miniGameId = raw.miniGameId;
                    task.rewardAmount = raw.rewardAmount;
                    task.isEventDay = raw.isEventDay;
                    task.description = raw.description;
                    task.hintText = raw.hintText;

                    EditorUtility.SetDirty(task);
                    tasks.Add(task);
                }
            }
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        #endregion

        #region Scene Builders

        private static string BuildSplashScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(new Color32(0x18, 0x0F, 0x33, 0xFF));
            CreateEventSystem();
            EnsureGameManager();

            Canvas canvas = CreateCanvas();
            Transform root = canvas.transform;

            Sprite roundBox = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box.png");
            Sprite roundOutline = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box_outline.png");
            Sprite softOrb = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/soft_orb.png");
            Sprite waveCurve = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/wave_curve.png");
            Sprite moneyBag = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_moneybag.png");
            Sprite dice = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_dice.png");
            Sprite trophy = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_trophy.png");
            Sprite sunIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_theme_sun.png");
            Sprite moonIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_theme_moon.png");
            Sprite rupeeLogo = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_rupee_logo.png");

            // 1. Background image (dark purple)
            Image bg = CreateImage("Background", root, null, new Color32(0x18, 0x0F, 0x33, 0xFF));
            SetStretch(bg.rectTransform);

            // 2. Floating Orbs Container
            GameObject orbsContainer = CreateUIObject("FloatingOrbs", root);
            SetStretch(orbsContainer.GetComponent<RectTransform>());

            Image orb1 = CreateImage("Orb_Magenta", orbsContainer.transform, softOrb, new Color32(0xFF, 0x00, 0x99, 0x75));
            SetPositionAndSize(orb1.rectTransform, new Vector2(-650, 240), new Vector2(340, 340));
            UIOrbFloating float1 = orb1.gameObject.AddComponent<UIOrbFloating>();
            float1.moveSpeed = 0.7f;
            float1.moveDistanceX = 25f;
            float1.moveDistanceY = 30f;

            Image orb2 = CreateImage("Orb_Amber", orbsContainer.transform, softOrb, new Color32(0xFF, 0xAA, 0x00, 0x85));
            SetPositionAndSize(orb2.rectTransform, new Vector2(720, 200), new Vector2(380, 380));
            UIOrbFloating float2 = orb2.gameObject.AddComponent<UIOrbFloating>();
            float2.moveSpeed = 0.9f;
            float2.moveDistanceX = 30f;
            float2.moveDistanceY = 22f;

            Image orb3 = CreateImage("Orb_Cyan", orbsContainer.transform, softOrb, new Color32(0x00, 0xE5, 0xFF, 0x70));
            SetPositionAndSize(orb3.rectTransform, new Vector2(-320, -280), new Vector2(320, 320));
            UIOrbFloating float3 = orb3.gameObject.AddComponent<UIOrbFloating>();
            float3.moveSpeed = 1.1f;
            float3.moveDistanceX = 20f;
            float3.moveDistanceY = 25f;

            Image orb4 = CreateImage("Orb_Blue", orbsContainer.transform, softOrb, new Color32(0x29, 0x79, 0xFF, 0x70));
            SetPositionAndSize(orb4.rectTransform, new Vector2(360, -280), new Vector2(320, 320));
            UIOrbFloating float4 = orb4.gameObject.AddComponent<UIOrbFloating>();
            float4.moveSpeed = 0.85f;
            float4.moveDistanceX = 22f;
            float4.moveDistanceY = 28f;

            // 3. Bottom Wave curve
            Image wave = CreateImage("BottomWave", root, waveCurve, new Color32(0x4D, 0x21, 0xB8, 0xFF));
            RectTransform waveRect = wave.rectTransform;
            waveRect.anchorMin = new Vector2(0f, 0f);
            waveRect.anchorMax = new Vector2(1f, 0f);
            waveRect.pivot = new Vector2(0.5f, 0f);
            waveRect.sizeDelta = new Vector2(0, 260);
            waveRect.anchoredPosition = new Vector2(0, 0);

            // 4. Floating decorative dice
            Image decorDice = CreateImage("Decor_Dice", root, dice, Color.white);
            SetPositionAndSize(decorDice.rectTransform, new Vector2(-780, -360), new Vector2(90, 90));
            decorDice.transform.localRotation = Quaternion.Euler(0, 0, -18);
            UIOrbFloating floatDice = decorDice.gameObject.AddComponent<UIOrbFloating>();
            floatDice.moveSpeed = 1.2f;
            floatDice.moveDistanceX = 12f;
            floatDice.moveDistanceY = 15f;

            // 5. Theme Toggle Button (Top-Left)
            GameObject themeBtnObj = CreateUIObject("ThemeToggleButton", root);
            Image themeBtnBg = themeBtnObj.AddComponent<Image>();
            themeBtnBg.sprite = roundBox;
            themeBtnBg.type = Image.Type.Sliced;
            themeBtnBg.color = new Color32(0xFF, 0xFF, 0xFF, 0x24);
            SetPositionAndSize(themeBtnObj.GetComponent<RectTransform>(), new Vector2(-880, 460), new Vector2(56, 56));
            Button themeBtn = themeBtnObj.AddComponent<Button>();

            Image themeIcon = CreateImage("Icon", themeBtnObj.transform, sunIcon, Color.white);
            SetPositionAndSize(themeIcon.rectTransform, Vector2.zero, new Vector2(32, 32));

            // 6. Header Title & Subtitle
            GameObject titleGroup = CreateUIObject("TitleGroup", root);
            SetPositionAndSize(titleGroup.GetComponent<RectTransform>(), new Vector2(0, 240), new Vector2(1200, 200));

            Image titleLogo = CreateImage("TitleCrystal", titleGroup.transform, rupeeLogo, Color.white);
            SetPositionAndSize(titleLogo.rectTransform, new Vector2(-360, 45), new Vector2(68, 68));

            TextMeshProUGUI titleText = CreateText(
                "TitleText",
                titleGroup.transform,
                "<color=#FFD026>R</color><color=#FFA31A>u</color><color=#FFDC26>p</color><color=#FF3E82>e</color><color=#E040FB>e</color><color=#00E5FF>R</color><color=#40C4FF>a</color><color=#7C4DFF>n</color><color=#2979FF>g</color><color=#00E5FF>e</color><color=#00E5FF>r</color>",
                92,
                FontStyles.Bold,
                TextAlignmentOptions.Center
            );
            SetPositionAndSize(titleText.rectTransform, new Vector2(20, 45), new Vector2(800, 110));

            TextMeshProUGUI subtitleText = CreateText(
                "SubtitleText",
                titleGroup.transform,
                "30-Day Financial Survival Game",
                28,
                FontStyles.Normal,
                TextAlignmentOptions.Center
            );
            subtitleText.color = new Color32(0xD1, 0xCA, 0xFF, 0xFF);
            SetPositionAndSize(subtitleText.rectTransform, new Vector2(0, -35), new Vector2(800, 50));

            // 7. Three Feature Cards
            GameObject cardsContainer = CreateUIObject("CardsContainer", root);
            SetPositionAndSize(cardsContainer.GetComponent<RectTransform>(), new Vector2(0, -30), new Vector2(1200, 220));

            Image card1 = CreateCardItem(cardsContainer.transform, "Card_Money", new Vector2(-410, 0), roundBox, moneyBag, "Manage Money", "Learn to budget wisely");
            Image card2 = CreateCardItem(cardsContainer.transform, "Card_Events", new Vector2(0, 0), roundBox, dice, "Random Events", "Face unexpected challenges");
            Image card3 = CreateCardItem(cardsContainer.transform, "Card_Learn", new Vector2(410, 0), roundBox, trophy, "Win & Learn", "Master financial skills");

            // 8. Play Now Button
            GameObject playBtnObj = CreateUIObject("PlayNowButton", root);
            Image playBtnBg = playBtnObj.AddComponent<Image>();
            playBtnBg.sprite = roundBox;
            playBtnBg.type = Image.Type.Sliced;
            playBtnBg.color = new Color32(0x7C, 0x4D, 0xFF, 0xFF);
            SetPositionAndSize(playBtnObj.GetComponent<RectTransform>(), new Vector2(0, -290), new Vector2(290, 76));

            Image playOutline = CreateImage("OutlineGlow", playBtnObj.transform, roundOutline, new Color32(0x00, 0xE6, 0x76, 0xFF));
            playOutline.type = Image.Type.Sliced;
            SetStretch(playOutline.rectTransform);

            Button playBtn = playBtnObj.AddComponent<Button>();

            TextMeshProUGUI playText = CreateText(
                "PlayText",
                playBtnObj.transform,
                "Play Now",
                30,
                FontStyles.Bold,
                TextAlignmentOptions.Center
            );
            playText.color = Color.white;
            SetStretch(playText.rectTransform);

            // 9. Attach Splash Controller
            SplashScreenUI splashCtrl = canvas.gameObject.AddComponent<SplashScreenUI>();
            splashCtrl.playNowButton = playBtn;
            splashCtrl.themeToggleButton = themeBtn;
            splashCtrl.themeToggleIcon = themeIcon;
            splashCtrl.sunIcon = sunIcon;
            splashCtrl.moonIcon = moonIcon;

            // 10. Attach Theme Manager
            ThemeManager tm = canvas.gameObject.AddComponent<ThemeManager>();
            tm.mainBackgroundImage = bg;
            tm.bottomWaveImage = wave;
            tm.cardBackgrounds = new[] { card1, card2, card3 };
            tm.titleTexts = new TMP_Text[] { };
            tm.subtitleTexts = new TMP_Text[] { subtitleText };

            string path = $"{SCENE_DIR}/Splash.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static Image CreateCardItem(
            Transform parent,
            string name,
            Vector2 pos,
            Sprite bgSprite,
            Sprite iconSprite,
            string title,
            string sub)
        {
            GameObject cardObj = CreateUIObject(name, parent);
            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = bgSprite;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color32(0x2A, 0x1E, 0x58, 0xB8);
            SetPositionAndSize(cardObj.GetComponent<RectTransform>(), pos, new Vector2(380, 205));

            Image outline = CreateImage("Border", cardObj.transform, AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box_outline.png"), new Color(1f, 1f, 1f, 0.12f));
            outline.type = Image.Type.Sliced;
            SetStretch(outline.rectTransform);

            Image iconImg = CreateImage("Icon", cardObj.transform, iconSprite, Color.white);
            iconImg.preserveAspect = true;
            SetPositionAndSize(iconImg.rectTransform, new Vector2(0, 36), new Vector2(75, 85));

            TextMeshProUGUI titleTxt = CreateText("Title", cardObj.transform, title, 24, FontStyles.Bold, TextAlignmentOptions.Center);
            titleTxt.color = Color.white;
            SetPositionAndSize(titleTxt.rectTransform, new Vector2(0, -22), new Vector2(340, 34));

            TextMeshProUGUI subTxt = CreateText("Subtitle", cardObj.transform, sub, 18, FontStyles.Normal, TextAlignmentOptions.Center);
            subTxt.color = new Color32(0xC7, 0xC3, 0xE8, 0xFF);
            SetPositionAndSize(subTxt.rectTransform, new Vector2(0, -56), new Vector2(340, 30));

            return cardBg;
        }

        private static string BuildPlayerSetupScene(
            List<LifestyleConfig> lifestyles,
            List<CharacterData> characters)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(new Color32(0x0F, 0x0D, 0x24, 0xFF));
            CreateEventSystem();
            EnsureGameManager();

            Canvas canvas = CreateCanvas();
            Transform root = canvas.transform;

            Sprite roundBox = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box.png");
            Sprite roundOutline = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box_outline.png");
            Sprite lockIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_lock.png");
            Sprite rupeeLogo = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_rupee_logo.png");

            // 1. Background image (deep navy purple)
            Image bg = CreateImage("Background", root, null, new Color32(0x0F, 0x0D, 0x24, 0xFF));
            SetStretch(bg.rectTransform);

            // 2. Top Header Bar
            GameObject topBar = CreateUIObject("TopBar", root);
            SetPositionAndSize(topBar.GetComponent<RectTransform>(), new Vector2(0, 480), new Vector2(1760, 60));

            Image logoImg = CreateImage("LogoIcon", topBar.transform, rupeeLogo, Color.white);
            SetPositionAndSize(logoImg.rectTransform, new Vector2(-820, 0), new Vector2(44, 44));

            TextMeshProUGUI brandText = CreateText("BrandText", topBar.transform, "RupeeRanger", 26, FontStyles.Bold, TextAlignmentOptions.Left);
            brandText.color = Color.white;
            SetPositionAndSize(brandText.rectTransform, new Vector2(-650, 0), new Vector2(280, 44));

            TextMeshProUGUI lifetimeScoreText = CreateText("CareerScoreText", topBar.transform, "Lifetime Score: 0 pts", 22, FontStyles.Bold, TextAlignmentOptions.Right);
            lifetimeScoreText.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(lifetimeScoreText.rectTransform, new Vector2(680, 0), new Vector2(360, 44));

            // 3. Page Title & Subtitle
            TextMeshProUGUI title = CreateText("PageTitle", root, "Player Setup", 50, FontStyles.Bold, TextAlignmentOptions.Center);
            title.color = Color.white;
            SetPositionAndSize(title.rectTransform, new Vector2(0, 395), new Vector2(900, 60));

            TextMeshProUGUI subtitle = CreateText("PageSubtitle", root, "Let's get you started on your financial journey!", 22, FontStyles.Normal, TextAlignmentOptions.Center);
            subtitle.color = new Color32(0xA5, 0xA1, 0xD6, 0xFF);
            SetPositionAndSize(subtitle.rectTransform, new Vector2(0, 345), new Vector2(900, 35));

            // 4. Personal Information Card
            GameObject personalCard = CreateUIObject("PersonalInformationCard", root);
            Image personalBg = personalCard.AddComponent<Image>();
            personalBg.sprite = roundBox;
            personalBg.type = Image.Type.Sliced;
            personalBg.color = new Color32(0x19, 0x18, 0x3B, 0xFF);
            SetPositionAndSize(personalCard.GetComponent<RectTransform>(), new Vector2(0, 205), new Vector2(1140, 165));

            Image personalOutline = CreateImage("Outline", personalCard.transform, roundOutline, new Color(1f, 1f, 1f, 0.08f));
            personalOutline.type = Image.Type.Sliced;
            SetStretch(personalOutline.rectTransform);

            TextMeshProUGUI personalHeader = CreateText("Header", personalCard.transform, "Personal Information", 22, FontStyles.Bold, TextAlignmentOptions.Left);
            personalHeader.color = Color.white;
            SetPositionAndSize(personalHeader.rectTransform, new Vector2(-390, 48), new Vector2(320, 32));

            TextMeshProUGUI nameLabel = CreateText("NameLabel", personalCard.transform, "Your Name", 17, FontStyles.Normal, TextAlignmentOptions.Left);
            nameLabel.color = new Color32(0xA5, 0xA1, 0xD6, 0xFF);
            SetPositionAndSize(nameLabel.rectTransform, new Vector2(-260, 10), new Vector2(490, 24));

            TMP_InputField nameInput = CreateInputField("NameInputField", personalCard.transform, new Vector2(-260, -32), new Vector2(490, 50), "Enter your name", roundBox);

            TextMeshProUGUI ageLabel = CreateText("AgeLabel", personalCard.transform, "Your Age", 17, FontStyles.Normal, TextAlignmentOptions.Left);
            ageLabel.color = new Color32(0xA5, 0xA1, 0xD6, 0xFF);
            SetPositionAndSize(ageLabel.rectTransform, new Vector2(310, 10), new Vector2(390, 24));

            TMP_InputField ageInput = CreateInputField("AgeInputField", personalCard.transform, new Vector2(310, -32), new Vector2(390, 50), "5-14", roundBox);
            ageInput.characterValidation = TMP_InputField.CharacterValidation.Integer;
            ageInput.characterLimit = 2;

            TextMeshProUGUI ageValidationText = CreateText("AgeValidationText", personalCard.transform, "Enter your age (between 5 and 14)", 15, FontStyles.Normal, TextAlignmentOptions.Left);
            ageValidationText.color = new Color32(0x9E, 0x9B, 0xC4, 0xFF);
            SetPositionAndSize(ageValidationText.rectTransform, new Vector2(0, -68), new Vector2(1010, 22));

            // 5. Choose Your Lifestyle Card
            GameObject lifestyleCard = CreateUIObject("LifestyleSectionCard", root);
            Image lifestyleBg = lifestyleCard.AddComponent<Image>();
            lifestyleBg.sprite = roundBox;
            lifestyleBg.type = Image.Type.Sliced;
            lifestyleBg.color = new Color32(0x19, 0x18, 0x3B, 0xFF);
            SetPositionAndSize(lifestyleCard.GetComponent<RectTransform>(), new Vector2(0, -35), new Vector2(1140, 220));

            Image lifestyleOutline = CreateImage("Outline", lifestyleCard.transform, roundOutline, new Color(1f, 1f, 1f, 0.08f));
            lifestyleOutline.type = Image.Type.Sliced;
            SetStretch(lifestyleOutline.rectTransform);

            TextMeshProUGUI lifestyleHeader = CreateText("Header", lifestyleCard.transform, "Choose Your Lifestyle", 22, FontStyles.Bold, TextAlignmentOptions.Left);
            lifestyleHeader.color = Color.white;
            SetPositionAndSize(lifestyleHeader.rectTransform, new Vector2(-390, 75), new Vector2(320, 32));

            List<LifestyleCardItem> lifestyleViews = new List<LifestyleCardItem>();
            Vector2[] lifePositions = { new Vector2(-340, -18), new Vector2(0, -18), new Vector2(340, -18) };

            for (int i = 0; i < 3; i++)
            {
                LifestyleCardItem item = CreateLifestyleCardObject($"LifestyleCard_{i}", lifestyleCard.transform, lifePositions[i], roundBox, roundOutline);
                lifestyleViews.Add(item);
            }

            // 6. Choose Your Character Row
            GameObject charSectionCard = CreateUIObject("CharacterSectionCard", root);
            Image charSectionBg = charSectionCard.AddComponent<Image>();
            charSectionBg.sprite = roundBox;
            charSectionBg.type = Image.Type.Sliced;
            charSectionBg.color = new Color32(0x19, 0x18, 0x3B, 0xFF);
            SetPositionAndSize(charSectionCard.GetComponent<RectTransform>(), new Vector2(0, -235), new Vector2(1140, 150));

            Image charOutline = CreateImage("Outline", charSectionCard.transform, roundOutline, new Color(1f, 1f, 1f, 0.08f));
            charOutline.type = Image.Type.Sliced;
            SetStretch(charOutline.rectTransform);

            TextMeshProUGUI charHeader = CreateText("Header", charSectionCard.transform, "Choose Your Ranger", 20, FontStyles.Bold, TextAlignmentOptions.Left);
            charHeader.color = Color.white;
            SetPositionAndSize(charHeader.rectTransform, new Vector2(-390, 48), new Vector2(320, 28));

            List<CharacterCardItem> charViews = new List<CharacterCardItem>();
            Vector2[] charPositions = { new Vector2(-380, -15), new Vector2(-126, -15), new Vector2(126, -15), new Vector2(380, -15) };

            for (int i = 0; i < 4; i++)
            {
                CharacterCardItem item = CreateCharacterCardObject($"CharCard_{i}", charSectionCard.transform, charPositions[i], roundBox, roundOutline, lockIcon);
                charViews.Add(item);
            }

            // 7. Feedback Toast Text
            TextMeshProUGUI feedbackToast = CreateText("FeedbackToast", root, "", 16, FontStyles.Normal, TextAlignmentOptions.Center);
            feedbackToast.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(feedbackToast.rectTransform, new Vector2(0, -325), new Vector2(1140, 24));

            // 8. Start Game Button
            GameObject startBtnObj = CreateUIObject("StartGameButton", root);
            Image startBtnBg = startBtnObj.AddComponent<Image>();
            startBtnBg.sprite = roundBox;
            startBtnBg.type = Image.Type.Sliced;
            startBtnBg.color = new Color32(0xFF, 0x47, 0x6E, 0xFF);
            SetPositionAndSize(startBtnObj.GetComponent<RectTransform>(), new Vector2(0, -395), new Vector2(1140, 72));

            Button startBtn = startBtnObj.AddComponent<Button>();

            TextMeshProUGUI startBtnText = CreateText("StartBtnText", startBtnObj.transform, "Start Game", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            startBtnText.color = Color.white;
            SetStretch(startBtnText.rectTransform);

            // Attach PlayerSetupUI
            PlayerSetupUI setupCtrl = canvas.gameObject.AddComponent<PlayerSetupUI>();
            setupCtrl.lifetimeScoreText = lifetimeScoreText;
            setupCtrl.nameInputField = nameInput;
            setupCtrl.ageInputField = ageInput;
            setupCtrl.ageValidationText = ageValidationText;
            setupCtrl.availableLifestyles = lifestyles;
            setupCtrl.lifestyleCardViews = lifestyleViews;
            setupCtrl.availableCharacters = characters;
            setupCtrl.characterCardViews = charViews;
            setupCtrl.feedbackToastText = feedbackToast;
            setupCtrl.startGameButton = startBtn;
            setupCtrl.startGameButtonImage = startBtnBg;

            string path = $"{SCENE_DIR}/PlayerSetup.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static LifestyleCardItem CreateLifestyleCardObject(
            string name,
            Transform parent,
            Vector2 pos,
            Sprite roundBox,
            Sprite roundOutline)
        {
            GameObject obj = CreateUIObject(name, parent);
            Image bg = obj.AddComponent<Image>();
            bg.sprite = roundBox;
            bg.type = Image.Type.Sliced;
            bg.color = new Color32(0x1B, 0x18, 0x42, 0xFF);
            SetPositionAndSize(obj.GetComponent<RectTransform>(), pos, new Vector2(330, 140));

            Button btn = obj.AddComponent<Button>();

            Image outline = CreateImage("OutlineGlow", obj.transform, roundOutline, new Color32(0x00, 0xE5, 0xFF, 0xFF));
            outline.type = Image.Type.Sliced;
            outline.enabled = false;
            SetStretch(outline.rectTransform);

            Image icon = CreateImage("Icon", obj.transform, null, Color.white);
            SetPositionAndSize(icon.rectTransform, new Vector2(-105, 10), new Vector2(56, 56));

            TextMeshProUGUI title = CreateText("Title", obj.transform, "Title", 18, FontStyles.Bold, TextAlignmentOptions.Left);
            title.color = Color.white;
            SetPositionAndSize(title.rectTransform, new Vector2(25, 26), new Vector2(180, 26));

            TextMeshProUGUI sub = CreateText("Subtitle", obj.transform, "Subtitle", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            sub.color = new Color32(0x9E, 0x9B, 0xC4, 0xFF);
            SetPositionAndSize(sub.rectTransform, new Vector2(25, 4), new Vector2(180, 20));

            TextMeshProUGUI income = CreateText("Income", obj.transform, "Rs. 15,000/mo", 18, FontStyles.Bold, TextAlignmentOptions.Left);
            income.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetPositionAndSize(income.rectTransform, new Vector2(25, -24), new Vector2(180, 26));

            LifestyleCardItem item = obj.AddComponent<LifestyleCardItem>();
            item.cardButton = btn;
            item.cardBackgroundImage = bg;
            item.outlineHighlightImage = outline;
            item.iconImage = icon;
            item.titleText = title;
            item.subtitleText = sub;
            item.incomeText = income;

            return item;
        }

        private static CharacterCardItem CreateCharacterCardObject(
            string name,
            Transform parent,
            Vector2 pos,
            Sprite roundBox,
            Sprite roundOutline,
            Sprite lockIcon)
        {
            GameObject obj = CreateUIObject(name, parent);
            Image bg = obj.AddComponent<Image>();
            bg.sprite = roundBox;
            bg.type = Image.Type.Sliced;
            bg.color = new Color32(0x1B, 0x18, 0x42, 0xFF);
            SetPositionAndSize(obj.GetComponent<RectTransform>(), pos, new Vector2(245, 96));

            Button btn = obj.AddComponent<Button>();

            Image outline = CreateImage("OutlineGlow", obj.transform, roundOutline, new Color32(0xFF, 0x40, 0x81, 0xFF));
            outline.type = Image.Type.Sliced;
            outline.enabled = false;
            SetStretch(outline.rectTransform);

            Image avatar = CreateImage("Avatar", obj.transform, null, Color.white);
            SetPositionAndSize(avatar.rectTransform, new Vector2(-65, 0), new Vector2(64, 64));

            TextMeshProUGUI charName = CreateText("Name", obj.transform, "Ranger", 16, FontStyles.Bold, TextAlignmentOptions.Left);
            charName.color = Color.white;
            SetPositionAndSize(charName.rectTransform, new Vector2(36, 12), new Vector2(130, 24));

            GameObject lockOverlay = CreateUIObject("LockOverlay", obj.transform);
            SetStretch(lockOverlay.GetComponent<RectTransform>());

            Image lockBg = lockOverlay.AddComponent<Image>();
            lockBg.sprite = roundBox;
            lockBg.type = Image.Type.Sliced;
            lockBg.color = new Color32(0x10, 0x0E, 0x24, 0xCC);

            Image lockImg = CreateImage("LockIcon", lockOverlay.transform, lockIcon, Color.white);
            SetPositionAndSize(lockImg.rectTransform, new Vector2(-50, 0), new Vector2(28, 28));

            TextMeshProUGUI pointsText = CreateText("PointsText", lockOverlay.transform, "Needs 300 pts", 14, FontStyles.Bold, TextAlignmentOptions.Left);
            pointsText.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(pointsText.rectTransform, new Vector2(28, 0), new Vector2(130, 24));

            CharacterCardItem item = obj.AddComponent<CharacterCardItem>();
            item.cardButton = btn;
            item.cardBackgroundImage = bg;
            item.outlineHighlightImage = outline;
            item.characterAvatarImage = avatar;
            item.characterNameText = charName;
            item.lockOverlay = lockOverlay;
            item.lockIconImage = lockImg;
            item.pointsNeededText = pointsText;

            return item;
        }

        private static string BuildExpenseSetupScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(new Color32(0x0F, 0x12, 0x21, 0xFF));
            CreateEventSystem();
            EnsureGameManager();

            Canvas canvas = CreateCanvas();
            Transform root = canvas.transform;

            Sprite roundBox = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box.png");
            Sprite rupeeLogo = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_rupee_logo.png");
            Sprite moneyBag = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_moneybag.png");
            Sprite iconRent = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_rent.png");
            Sprite iconFood = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_food.png");
            Sprite iconTransport = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_transport.png");
            Sprite iconEntertainment = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_entertainment.png");
            Sprite sliderKnob = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/slider_knob.png");
            Sprite sliderTrack = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/slider_track.png");
            Sprite charPenny = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/char_penny.png");
            Sprite moonIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_theme_moon.png");

            // 1. Background (deep dark navy #0F1221)
            Image bg = CreateImage("Background", root, null, new Color32(0x0F, 0x12, 0x21, 0xFF));
            SetStretch(bg.rectTransform);

            // 2. Top Bar
            GameObject topBar = CreateUIObject("TopBar", root);
            SetPositionAndSize(topBar.GetComponent<RectTransform>(), new Vector2(0, 480), new Vector2(1760, 60));

            Image logoImg = CreateImage("LogoIcon", topBar.transform, rupeeLogo, Color.white);
            SetPositionAndSize(logoImg.rectTransform, new Vector2(-820, 0), new Vector2(44, 44));

            TextMeshProUGUI brandText = CreateText("BrandText", topBar.transform, "RupeeRanger", 26, FontStyles.Bold, TextAlignmentOptions.Left);
            brandText.color = Color.white;
            SetPositionAndSize(brandText.rectTransform, new Vector2(-650, 0), new Vector2(280, 44));

            GameObject themeBtnObj = CreateUIObject("ThemeToggleButton", topBar.transform);
            Image themeBtnBg = themeBtnObj.AddComponent<Image>();
            themeBtnBg.sprite = roundBox;
            themeBtnBg.type = Image.Type.Sliced;
            themeBtnBg.color = new Color32(0x1F, 0x1B, 0x3E, 0xFF);
            SetPositionAndSize(themeBtnObj.GetComponent<RectTransform>(), new Vector2(820, 0), new Vector2(44, 44));
            Button themeBtn = themeBtnObj.AddComponent<Button>();
            Image themeIcon = CreateImage("ThemeIcon", themeBtnObj.transform, moonIcon, Color.white);
            SetPositionAndSize(themeIcon.rectTransform, Vector2.zero, new Vector2(24, 24));

            // 3. Header Title & Subtitle
            TextMeshProUGUI title = CreateText("PageTitle", root, "Expense Setup", 46, FontStyles.Bold, TextAlignmentOptions.Center);
            title.color = Color.white;
            SetPositionAndSize(title.rectTransform, new Vector2(0, 415), new Vector2(900, 55));

            TextMeshProUGUI subtitle = CreateText("PageSubtitle", root, "Set your monthly expenses and see your initial savings!", 20, FontStyles.Normal, TextAlignmentOptions.Center);
            subtitle.color = new Color32(0x8E, 0x9B, 0xB0, 0xFF);
            SetPositionAndSize(subtitle.rectTransform, new Vector2(0, 372), new Vector2(900, 30));

            // 4. Interactive Character Stage (Town Places)
            GameObject charStage = CreateUIObject("CharacterInteractiveStage", root);
            Image stageBg = charStage.AddComponent<Image>();
            stageBg.sprite = roundBox;
            stageBg.type = Image.Type.Sliced;
            stageBg.color = new Color32(0x15, 0x1B, 0x2E, 0xFF);
            SetPositionAndSize(charStage.GetComponent<RectTransform>(), new Vector2(0, 318), new Vector2(820, 68));

            Image stageTrack = CreateImage("StageTrack", charStage.transform, roundBox, new Color32(0x22, 0x2A, 0x44, 0xFF));
            stageTrack.type = Image.Type.Sliced;
            SetPositionAndSize(stageTrack.rectTransform, new Vector2(0, -10), new Vector2(660, 4));

            Vector2[] stationPos = new Vector2[]
            {
                new Vector2(-270, -6),
                new Vector2(-90, -6),
                new Vector2(90, -6),
                new Vector2(270, -6)
            };

            string[] placeNames = new string[] { "Home", "Restaurant", "Bus Stop", "Game Zone" };
            Sprite[] placeIcons = new Sprite[] { iconRent, iconFood, iconTransport, iconEntertainment };

            RectTransform[] waypoints = new RectTransform[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject stObj = CreateUIObject($"Station_{placeNames[i]}", charStage.transform);
                waypoints[i] = stObj.GetComponent<RectTransform>();
                SetPositionAndSize(waypoints[i], stationPos[i], new Vector2(50, 50));

                Image stIcon = CreateImage("PlaceIcon", stObj.transform, placeIcons[i], new Color(1f, 1f, 1f, 0.45f));
                SetPositionAndSize(stIcon.rectTransform, new Vector2(0, 14), new Vector2(20, 20));

                TextMeshProUGUI stLabel = CreateText("PlaceLabel", stObj.transform, placeNames[i], 12, FontStyles.Normal, TextAlignmentOptions.Center);
                stLabel.color = new Color32(0x6A, 0x78, 0x90, 0xFF);
                SetPositionAndSize(stLabel.rectTransform, new Vector2(0, -12), new Vector2(80, 20));
            }

            // Character Unit (Walker)
            GameObject charUnit = CreateUIObject("CharacterWalker", charStage.transform);
            RectTransform charUnitRect = charUnit.GetComponent<RectTransform>();
            SetPositionAndSize(charUnitRect, stationPos[0], new Vector2(60, 60));

            Image charAvatar = CreateImage("Avatar", charUnit.transform, charPenny, Color.white);
            charAvatar.preserveAspect = true;
            SetPositionAndSize(charAvatar.rectTransform, new Vector2(0, 8), new Vector2(50, 68));

            GameObject speechBubble = CreateUIObject("SpeechBubble", charUnit.transform);
            Image bubbleBg = speechBubble.AddComponent<Image>();
            bubbleBg.sprite = roundBox;
            bubbleBg.type = Image.Type.Sliced;
            bubbleBg.color = new Color32(0x27, 0x1E, 0x54, 0xFF);
            SetPositionAndSize(speechBubble.GetComponent<RectTransform>(), new Vector2(0, 32), new Vector2(140, 22));

            TextMeshProUGUI charPlaceLabel = CreateText("PlaceText", speechBubble.transform, "Home (Rent)", 11, FontStyles.Bold, TextAlignmentOptions.Center);
            charPlaceLabel.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetStretch(charPlaceLabel.rectTransform);

            // 5. Card 1: Monthly Income Card
            GameObject incomeCard = CreateUIObject("MonthlyIncomeCard", root);
            Image incomeCardBg = incomeCard.AddComponent<Image>();
            incomeCardBg.sprite = roundBox;
            incomeCardBg.type = Image.Type.Sliced;
            incomeCardBg.color = new Color32(0x15, 0x1B, 0x2E, 0xFF);
            SetPositionAndSize(incomeCard.GetComponent<RectTransform>(), new Vector2(0, 222), new Vector2(820, 95));

            TextMeshProUGUI incomeLabel = CreateText("IncomeLabel", incomeCard.transform, "Your Monthly Income", 18, FontStyles.Normal, TextAlignmentOptions.Left);
            incomeLabel.color = new Color32(0x8E, 0x9B, 0xB0, 0xFF);
            SetPositionAndSize(incomeLabel.rectTransform, new Vector2(-230, 18), new Vector2(300, 28));

            TextMeshProUGUI monthlyIncomeText = CreateText("IncomeValue", incomeCard.transform, "Rs. 15,000", 34, FontStyles.Bold, TextAlignmentOptions.Left);
            monthlyIncomeText.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetPositionAndSize(monthlyIncomeText.rectTransform, new Vector2(-230, -18), new Vector2(300, 42));

            GameObject badgeObj = CreateUIObject("IncomeIconBadge", incomeCard.transform);
            Image badgeBg = badgeObj.AddComponent<Image>();
            badgeBg.sprite = roundBox;
            badgeBg.type = Image.Type.Sliced;
            badgeBg.color = new Color32(0x13, 0x2E, 0x35, 0xFF);
            SetPositionAndSize(badgeObj.GetComponent<RectTransform>(), new Vector2(345, 0), new Vector2(58, 58));

            Image incomeBagImg = CreateImage("MoneyBagIcon", badgeObj.transform, moneyBag, Color.white);
            SetPositionAndSize(incomeBagImg.rectTransform, Vector2.zero, new Vector2(36, 36));

            // 6. Card 2: Monthly Expenses Card
            GameObject expensesCard = CreateUIObject("MonthlyExpensesCard", root);
            Image expensesCardBg = expensesCard.AddComponent<Image>();
            expensesCardBg.sprite = roundBox;
            expensesCardBg.type = Image.Type.Sliced;
            expensesCardBg.color = new Color32(0x15, 0x1B, 0x2E, 0xFF);
            SetPositionAndSize(expensesCard.GetComponent<RectTransform>(), new Vector2(0, -5), new Vector2(820, 320));

            TextMeshProUGUI expensesHeader = CreateText("ExpensesHeader", expensesCard.transform, "Monthly Expenses", 24, FontStyles.Bold, TextAlignmentOptions.Left);
            expensesHeader.color = Color.white;
            SetPositionAndSize(expensesHeader.rectTransform, new Vector2(-285, 126), new Vector2(300, 34));

            Color32 trackColor = new Color32(0x1F, 0x27, 0x40, 0xFF);
            Color32 fillColor = new Color32(0xFF, 0x47, 0x6E, 0xFF);
            Color32 knobColor = Color.white;
            Color32 valueColor = new Color32(0xFF, 0x47, 0x6E, 0xFF);
            Color32 subLabelColor = new Color32(0x7A, 0x88, 0x9B, 0xFF);

            // --- Expense Row 0: Rent ---
            float rentY = 82f;
            Image rentIcon = CreateImage("RentIcon", expensesCard.transform, iconRent, Color.white);
            SetPositionAndSize(rentIcon.rectTransform, new Vector2(-365, rentY), new Vector2(28, 28));

            TextMeshProUGUI rentTitle = CreateText("RentTitle", expensesCard.transform, "Rent", 20, FontStyles.Bold, TextAlignmentOptions.Left);
            rentTitle.color = Color.white;
            SetPositionAndSize(rentTitle.rectTransform, new Vector2(-305, rentY), new Vector2(150, 28));

            TextMeshProUGUI rentValText = CreateText("RentValue", expensesCard.transform, "Rs. 3,000", 22, FontStyles.Bold, TextAlignmentOptions.Right);
            rentValText.color = valueColor;
            SetPositionAndSize(rentValText.rectTransform, new Vector2(345, rentY), new Vector2(150, 28));

            Slider rentSlider = CreateSlider("RentSlider", expensesCard.transform, new Vector2(0, rentY - 28), new Vector2(750, 20), sliderTrack, sliderKnob, trackColor, fillColor, knobColor);

            TextMeshProUGUI rentMin = CreateText("RentMin", expensesCard.transform, "Rs. 0", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            rentMin.color = subLabelColor;
            SetPositionAndSize(rentMin.rectTransform, new Vector2(-365, rentY - 46), new Vector2(80, 18));

            TextMeshProUGUI rentMax = CreateText("RentMax", expensesCard.transform, "Rs. 15,000", 13, FontStyles.Normal, TextAlignmentOptions.Right);
            rentMax.color = subLabelColor;
            SetPositionAndSize(rentMax.rectTransform, new Vector2(355, rentY - 46), new Vector2(100, 18));

            // --- Expense Row 1: Food ---
            float foodY = 18f;
            Image foodIcon = CreateImage("FoodIcon", expensesCard.transform, iconFood, Color.white);
            SetPositionAndSize(foodIcon.rectTransform, new Vector2(-365, foodY), new Vector2(28, 28));

            TextMeshProUGUI foodTitle = CreateText("FoodTitle", expensesCard.transform, "Food", 20, FontStyles.Bold, TextAlignmentOptions.Left);
            foodTitle.color = Color.white;
            SetPositionAndSize(foodTitle.rectTransform, new Vector2(-305, foodY), new Vector2(150, 28));

            TextMeshProUGUI foodValText = CreateText("FoodValue", expensesCard.transform, "Rs. 2,000", 22, FontStyles.Bold, TextAlignmentOptions.Right);
            foodValText.color = valueColor;
            SetPositionAndSize(foodValText.rectTransform, new Vector2(345, foodY), new Vector2(150, 28));

            Slider foodSlider = CreateSlider("FoodSlider", expensesCard.transform, new Vector2(0, foodY - 28), new Vector2(750, 20), sliderTrack, sliderKnob, trackColor, fillColor, knobColor);

            TextMeshProUGUI foodMin = CreateText("FoodMin", expensesCard.transform, "Rs. 0", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            foodMin.color = subLabelColor;
            SetPositionAndSize(foodMin.rectTransform, new Vector2(-365, foodY - 46), new Vector2(80, 18));

            TextMeshProUGUI foodMax = CreateText("FoodMax", expensesCard.transform, "Rs. 8,000", 13, FontStyles.Normal, TextAlignmentOptions.Right);
            foodMax.color = subLabelColor;
            SetPositionAndSize(foodMax.rectTransform, new Vector2(355, foodY - 46), new Vector2(100, 18));

            // --- Expense Row 2: Transport ---
            float transY = -46f;
            Image transIcon = CreateImage("TransportIcon", expensesCard.transform, iconTransport, Color.white);
            SetPositionAndSize(transIcon.rectTransform, new Vector2(-365, transY), new Vector2(28, 28));

            TextMeshProUGUI transTitle = CreateText("TransportTitle", expensesCard.transform, "Transport", 20, FontStyles.Bold, TextAlignmentOptions.Left);
            transTitle.color = Color.white;
            SetPositionAndSize(transTitle.rectTransform, new Vector2(-305, transY), new Vector2(150, 28));

            TextMeshProUGUI transValText = CreateText("TransportValue", expensesCard.transform, "Rs. 500", 22, FontStyles.Bold, TextAlignmentOptions.Right);
            transValText.color = valueColor;
            SetPositionAndSize(transValText.rectTransform, new Vector2(345, transY), new Vector2(150, 28));

            Slider transportSlider = CreateSlider("TransportSlider", expensesCard.transform, new Vector2(0, transY - 28), new Vector2(750, 20), sliderTrack, sliderKnob, trackColor, fillColor, knobColor);

            TextMeshProUGUI transMin = CreateText("TransportMin", expensesCard.transform, "Rs. 0", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            transMin.color = subLabelColor;
            SetPositionAndSize(transMin.rectTransform, new Vector2(-365, transY - 46), new Vector2(80, 18));

            TextMeshProUGUI transMax = CreateText("TransportMax", expensesCard.transform, "Rs. 3,000", 13, FontStyles.Normal, TextAlignmentOptions.Right);
            transMax.color = subLabelColor;
            SetPositionAndSize(transMax.rectTransform, new Vector2(355, transY - 46), new Vector2(100, 18));

            // --- Expense Row 3: Entertainment ---
            float entY = -110f;
            Image entIcon = CreateImage("EntertainmentIcon", expensesCard.transform, iconEntertainment, Color.white);
            SetPositionAndSize(entIcon.rectTransform, new Vector2(-365, entY), new Vector2(28, 28));

            TextMeshProUGUI entTitle = CreateText("EntertainmentTitle", expensesCard.transform, "Entertainment", 20, FontStyles.Bold, TextAlignmentOptions.Left);
            entTitle.color = Color.white;
            SetPositionAndSize(entTitle.rectTransform, new Vector2(-305, entY), new Vector2(150, 28));

            TextMeshProUGUI entValText = CreateText("EntertainmentValue", expensesCard.transform, "Rs. 1,000", 22, FontStyles.Bold, TextAlignmentOptions.Right);
            entValText.color = valueColor;
            SetPositionAndSize(entValText.rectTransform, new Vector2(345, entY), new Vector2(150, 28));

            Slider entertainmentSlider = CreateSlider("EntertainmentSlider", expensesCard.transform, new Vector2(0, entY - 28), new Vector2(750, 20), sliderTrack, sliderKnob, trackColor, fillColor, knobColor);

            TextMeshProUGUI entMin = CreateText("EntertainmentMin", expensesCard.transform, "Rs. 0", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            entMin.color = subLabelColor;
            SetPositionAndSize(entMin.rectTransform, new Vector2(-365, entY - 46), new Vector2(80, 18));

            TextMeshProUGUI entMax = CreateText("EntertainmentMax", expensesCard.transform, "Rs. 5,000", 13, FontStyles.Normal, TextAlignmentOptions.Right);
            entMax.color = subLabelColor;
            SetPositionAndSize(entMax.rectTransform, new Vector2(355, entY - 46), new Vector2(100, 18));

            // 7. Card 3: Live Calculation Card
            GameObject calcCard = CreateUIObject("LiveCalculationCard", root);
            Image calcCardBg = calcCard.AddComponent<Image>();
            calcCardBg.sprite = roundBox;
            calcCardBg.type = Image.Type.Sliced;
            calcCardBg.color = new Color32(0x15, 0x1B, 0x2E, 0xFF);
            SetPositionAndSize(calcCard.GetComponent<RectTransform>(), new Vector2(0, -255), new Vector2(820, 150));

            TextMeshProUGUI calcHeader = CreateText("CalcHeader", calcCard.transform, "Live Calculation", 24, FontStyles.Bold, TextAlignmentOptions.Left);
            calcHeader.color = Color.white;
            SetPositionAndSize(calcHeader.rectTransform, new Vector2(-285, 48), new Vector2(300, 32));

            // Row 1: Monthly Income
            TextMeshProUGUI cIncomeLabel = CreateText("CalcIncomeLabel", calcCard.transform, "Monthly Income", 18, FontStyles.Normal, TextAlignmentOptions.Left);
            cIncomeLabel.color = new Color32(0xA5, 0xA1, 0xD6, 0xFF);
            SetPositionAndSize(cIncomeLabel.rectTransform, new Vector2(-285, 16), new Vector2(200, 26));

            TextMeshProUGUI cIncomeVal = CreateText("CalcIncomeValue", calcCard.transform, "Rs. 15,000", 20, FontStyles.Bold, TextAlignmentOptions.Right);
            cIncomeVal.color = Color.white;
            SetPositionAndSize(cIncomeVal.rectTransform, new Vector2(345, 16), new Vector2(160, 26));

            // Row 2: Total Expenses
            TextMeshProUGUI cExpLabel = CreateText("CalcExpensesLabel", calcCard.transform, "Total Expenses", 18, FontStyles.Normal, TextAlignmentOptions.Left);
            cExpLabel.color = new Color32(0xA5, 0xA1, 0xD6, 0xFF);
            SetPositionAndSize(cExpLabel.rectTransform, new Vector2(-285, -14), new Vector2(200, 26));

            TextMeshProUGUI cExpVal = CreateText("CalcExpensesValue", calcCard.transform, "-Rs. 6,500", 20, FontStyles.Bold, TextAlignmentOptions.Right);
            cExpVal.color = valueColor;
            SetPositionAndSize(cExpVal.rectTransform, new Vector2(345, -14), new Vector2(160, 26));

            // Divider Line
            Image divider = CreateImage("DividerLine", calcCard.transform, roundBox, new Color32(0x24, 0x2D, 0x48, 0xFF));
            divider.type = Image.Type.Sliced;
            SetPositionAndSize(divider.rectTransform, new Vector2(0, -32), new Vector2(760, 1));

            // Row 3: Initial Savings
            TextMeshProUGUI cSavingsLabel = CreateText("CalcSavingsLabel", calcCard.transform, "Initial Savings", 18, FontStyles.Normal, TextAlignmentOptions.Left);
            cSavingsLabel.color = new Color32(0xA5, 0xA1, 0xD6, 0xFF);
            SetPositionAndSize(cSavingsLabel.rectTransform, new Vector2(-285, -52), new Vector2(200, 32));

            TextMeshProUGUI cSavingsVal = CreateText("CalcSavingsValue", calcCard.transform, "Rs. 8,500", 32, FontStyles.Bold, TextAlignmentOptions.Right);
            cSavingsVal.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetPositionAndSize(cSavingsVal.rectTransform, new Vector2(345, -52), new Vector2(200, 36));

            // Score below savings area
            TextMeshProUGUI scoreText = CreateText("CurrentScoreText", root, "Starting Score: 1,000 pts", 14, FontStyles.Bold, TextAlignmentOptions.Center);
            scoreText.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(scoreText.rectTransform, new Vector2(0, -336), new Vector2(400, 20));

            // Gentle Warning Banner for children (between calculation card and button)
            GameObject warnObj = CreateUIObject("SavingsWarningBanner", root);
            Image warnBg = warnObj.AddComponent<Image>();
            warnBg.sprite = roundBox;
            warnBg.type = Image.Type.Sliced;
            warnBg.color = new Color32(0x3B, 0x26, 0x08, 0xEE);
            SetPositionAndSize(warnObj.GetComponent<RectTransform>(), new Vector2(0, -336), new Vector2(820, 28));

            TextMeshProUGUI warnText = CreateText("WarningText", warnObj.transform, "Tip: Your savings are quite low! Try saving a little more so you have room for surprises in town.", 13, FontStyles.Normal, TextAlignmentOptions.Center);
            warnText.color = new Color32(0xFF, 0xD5, 0x4F, 0xFF);
            SetStretch(warnText.rectTransform);
            warnObj.SetActive(false);

            // 8. Start Simulation Button
            GameObject startBtnObj = CreateUIObject("StartSimulationButton", root);
            Image startBtnBg = startBtnObj.AddComponent<Image>();
            startBtnBg.sprite = roundBox;
            startBtnBg.type = Image.Type.Sliced;
            startBtnBg.color = new Color32(0xFF, 0x47, 0x6E, 0xFF);
            SetPositionAndSize(startBtnObj.GetComponent<RectTransform>(), new Vector2(0, -385), new Vector2(820, 58));

            Button startBtn = startBtnObj.AddComponent<Button>();

            TextMeshProUGUI startBtnText = CreateText("StartBtnText", startBtnObj.transform, "Start Simulation", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            startBtnText.color = Color.white;
            SetStretch(startBtnText.rectTransform);

            // 9. Wire ExpenseSetupUI controller
            ExpenseSetupUI setupCtrl = canvas.gameObject.AddComponent<ExpenseSetupUI>();
            setupCtrl.monthlyIncomeText = monthlyIncomeText;
            setupCtrl.incomeIcon = incomeBagImg;

            setupCtrl.rentSlider = rentSlider;
            setupCtrl.foodSlider = foodSlider;
            setupCtrl.transportSlider = transportSlider;
            setupCtrl.entertainmentSlider = entertainmentSlider;

            setupCtrl.rentValueText = rentValText;
            setupCtrl.foodValueText = foodValText;
            setupCtrl.transportValueText = transValText;
            setupCtrl.entertainmentValueText = entValText;

            setupCtrl.rentMinText = rentMin;
            setupCtrl.rentMaxText = rentMax;
            setupCtrl.foodMinText = foodMin;
            setupCtrl.foodMaxText = foodMax;
            setupCtrl.transportMinText = transMin;
            setupCtrl.transportMaxText = transMax;
            setupCtrl.entertainmentMinText = entMin;
            setupCtrl.entertainmentMaxText = entMax;

            setupCtrl.calcIncomeText = cIncomeVal;
            setupCtrl.calcExpensesText = cExpVal;
            setupCtrl.calcSavingsText = cSavingsVal;
            setupCtrl.currentScoreText = scoreText;
            setupCtrl.savingsWarningObject = warnObj;
            setupCtrl.savingsWarningText = warnText;

            setupCtrl.characterContainer = charUnitRect;
            setupCtrl.characterImage = charAvatar;
            setupCtrl.characterPlaceLabel = charPlaceLabel;
            setupCtrl.stationWaypoints = waypoints;

            setupCtrl.startSimulationButton = startBtn;

            // 10. Attach Theme Manager
            ThemeManager tm = canvas.gameObject.AddComponent<ThemeManager>();
            tm.mainBackgroundImage = bg;
            tm.cardBackgrounds = new[] { incomeCardBg, expensesCardBg, calcCardBg };
            tm.titleTexts = new TMP_Text[] { title };
            tm.subtitleTexts = new TMP_Text[] { subtitle };

            string path = $"{SCENE_DIR}/ExpenseSetup.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        #region Town Scene (Phase 3 & 4)

        private static string BuildTownScene(List<TaskData> tasks)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera & AudioListener
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            Camera cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x3B, 0x85, 0x26, 0xFF);
            cam.orthographic = true;
            cam.orthographicSize = 4.8f;
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0.5f, 0.5f, -10f);

            CreateEventSystem();
            EnsureGameManager();

            // 2. Load Sprites & Tiles
            Sprite sprGrass = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/tile_grass.png");
            Sprite sprRoad = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/tile_road.png");
            Sprite sprRoadLineH = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/tile_road_line_h.png");
            Sprite sprRoadLineV = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/tile_road_line_v.png");
            Sprite sprZebra = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/tile_zebra.png");
            Sprite sprPavement = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/tile_pavement.png");

            Sprite sprLamp = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/prop_lamp.png");
            Sprite sprTree = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/prop_tree.png");

            Sprite sprHome = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_home.png");
            Sprite sprCollege = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_college.png");
            Sprite sprBank = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_bank.png");
            Sprite sprShop = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_shop.png");
            Sprite sprHospital = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_hospital.png");
            Sprite sprFriend = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_friend.png");
            Sprite sprGameCentre = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_gamecentre.png");
            Sprite sprWordSearch = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_word_search.png") ?? sprGameCentre;
            Sprite sprWordFinder = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_word_finder.png") ?? sprGameCentre;
            Sprite sprMathsSpeed = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_maths_speed.png") ?? sprGameCentre;
            Sprite sprMathsPuzzle = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_maths_puzzle.png") ?? sprGameCentre;
            Sprite sprBusStop = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_busstop.png");
            Sprite sprLibrary = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_library.png");
            Sprite sprCommunityHall = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_community_hall.png");
            Sprite sprBakery = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_bakery.png");
            Sprite sprPark = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/building_park.png");

            Sprite sprJoyBase = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/joystick_base.png");
            Sprite sprJoyThumb = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/joystick_thumb.png");
            Sprite sprBtnInteract = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/btn_interact.png");

            Sprite roundBox = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box.png");
            Sprite defaultCharSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/char_penny.png");

            Tile tileGrass = GetOrCreateTile("Tile_Grass", sprGrass);
            Tile tileRoad = GetOrCreateTile("Tile_Road", sprRoad);
            Tile tileRoadLineH = GetOrCreateTile("Tile_RoadLineH", sprRoadLineH);
            Tile tileRoadLineV = GetOrCreateTile("Tile_RoadLineV", sprRoadLineV);
            Tile tileZebra = GetOrCreateTile("Tile_Zebra", sprZebra);
            Tile tilePavement = GetOrCreateTile("Tile_Pavement", sprPavement);

            // 3. Grid & Tilemaps
            GameObject gridObj = new GameObject("TownGrid", typeof(Grid));
            Grid grid = gridObj.GetComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            Tilemap groundTm = CreateTilemapLayer("GroundTilemap", gridObj.transform, 0);
            Tilemap roadTm = CreateTilemapLayer("RoadTilemap", gridObj.transform, 1);
            Tilemap pavementTm = CreateTilemapLayer("PavementTilemap", gridObj.transform, 2);

            // Populate Ground Tilemap (Grass)
            for (int x = -26; x <= 26; x++)
            {
                for (int y = -18; y <= 18; y++)
                {
                    groundTm.SetTile(new Vector3Int(x, y, 0), tileGrass);
                }
            }

            // Populate Road Tilemap:
            // Main East-West Highway
            for (int x = -26; x <= 26; x++)
            {
                if (x >= 7 && x <= 8)
                {
                    roadTm.SetTile(new Vector3Int(x, 0, 0), tileZebra);
                    roadTm.SetTile(new Vector3Int(x, 1, 0), tileZebra);
                }
                else
                {
                    roadTm.SetTile(new Vector3Int(x, 0, 0), tileRoad);
                    roadTm.SetTile(new Vector3Int(x, 1, 0), tileRoadLineH);
                }
            }

            // Central South Avenue (x = 0, 1 from y = -18 to -1)
            for (int y = -18; y <= -1; y++)
            {
                roadTm.SetTile(new Vector3Int(0, y, 0), tileRoad);
                roadTm.SetTile(new Vector3Int(1, y, 0), tileRoadLineV);
            }

            // West Cross Street (x = -14, -13 from y = -16 to 14)
            for (int y = -16; y <= 14; y++)
            {
                if (y != 0 && y != 1)
                {
                    roadTm.SetTile(new Vector3Int(-14, y, 0), tileRoad);
                    roadTm.SetTile(new Vector3Int(-13, y, 0), tileRoadLineV);
                }
            }

            // East Cross Street (x = 14, 15 from y = -16 to 14)
            for (int y = -16; y <= 14; y++)
            {
                if (y != 0 && y != 1)
                {
                    roadTm.SetTile(new Vector3Int(14, y, 0), tileRoad);
                    roadTm.SetTile(new Vector3Int(15, y, 0), tileRoadLineV);
                }
            }

            // Populate Pavement Tilemap:
            // Sidewalks framing East-West Highway
            for (int x = -26; x <= 26; x++)
            {
                if ((x >= -14 && x <= -13) || (x >= 0 && x <= 1) || (x >= 14 && x <= 15)) continue;

                pavementTm.SetTile(new Vector3Int(x, 2, 0), tilePavement);
                pavementTm.SetTile(new Vector3Int(x, -1, 0), tilePavement);
            }

            // Sidewalks framing Central South Avenue
            for (int y = -18; y <= -1; y++)
            {
                pavementTm.SetTile(new Vector3Int(-1, y, 0), tilePavement);
                pavementTm.SetTile(new Vector3Int(2, y, 0), tilePavement);
            }

            // Sidewalks framing West Street
            for (int y = -16; y <= 14; y++)
            {
                if (y >= -1 && y <= 2) continue;
                pavementTm.SetTile(new Vector3Int(-15, y, 0), tilePavement);
                pavementTm.SetTile(new Vector3Int(-12, y, 0), tilePavement);
            }

            // Sidewalks framing East Street
            for (int y = -16; y <= 14; y++)
            {
                if (y >= -1 && y <= 2) continue;
                pavementTm.SetTile(new Vector3Int(13, y, 0), tilePavement);
                pavementTm.SetTile(new Vector3Int(16, y, 0), tilePavement);
            }

            // Building Plazas / Courtyards
            FillPavementBox(pavementTm, tilePavement, -2, 3, 2, 5);      // Home
            FillPavementBox(pavementTm, tilePavement, -16, -12, 4, 8);   // College
            FillPavementBox(pavementTm, tilePavement, 12, 16, 4, 8);     // Bank
            FillPavementBox(pavementTm, tilePavement, -16, -12, -9, -5); // Shop
            FillPavementBox(pavementTm, tilePavement, 12, 16, -9, -5);   // Hospital
            FillPavementBox(pavementTm, tilePavement, -16, -12, -2, 2);  // Friend's House (West Street Middle)
            FillPavementBox(pavementTm, tilePavement, 6, 9, 1, 4);       // Bus Stop
            FillPavementBox(pavementTm, tilePavement, -8, -4, 4, 8);     // Word Search House (North-West)
            FillPavementBox(pavementTm, tilePavement, 4, 8, 4, 8);       // Maths Puzzle House (North-East)
            FillPavementBox(pavementTm, tilePavement, -8, -4, -9, -5);   // Word Finder House (South-West)
            FillPavementBox(pavementTm, tilePavement, 4, 8, -9, -5);     // Maths Speed House (South-East)
            FillPavementBox(pavementTm, tilePavement, -16, -12, -15, -11); // Game Centre Plaza (South-West Outer)
            FillPavementBox(pavementTm, tilePavement, 12, 16, 11, 15);   // Library Plaza (North-East Outer)
            FillPavementBox(pavementTm, tilePavement, -16, -12, 11, 15);  // City Park Plaza (North-West Outer)
            FillPavementBox(pavementTm, tilePavement, -2, 3, 11, 15);     // Community Hall Plaza (North Central)
            FillPavementBox(pavementTm, tilePavement, 12, 16, -2, 2);     // Bakery Plaza (East Street Middle)

            // 4. Props (Street Lamps & Trees)
            GameObject propsParent = new GameObject("Props");

            Vector2[] lampPositions = new Vector2[]
            {
                new Vector2(2.5f, -1.3f),
                new Vector2(-1.5f, -1.3f),
                new Vector2(2.5f, 2.5f),
                new Vector2(-1.5f, 2.5f),
                new Vector2(6.5f, 2.5f),
                new Vector2(10.5f, 2.5f),
                new Vector2(12.5f, -1.3f),
                new Vector2(-11.5f, -1.3f),
                new Vector2(12.5f, 2.5f),
                new Vector2(-11.5f, 2.5f)
            };
            foreach (var lPos in lampPositions)
            {
                CreateProp("StreetLamp", propsParent.transform, lPos, sprLamp, new Vector2(0.2f, 0.2f), new Vector2(0f, -0.6f), 6);
            }

            Vector2[] treePositions = new Vector2[]
            {
                new Vector2(-10.5f, 5f),
                new Vector2(10.5f, 5f),
                new Vector2(-7.5f, 1.5f),
                new Vector2(-7.5f, -2.5f),
                new Vector2(7.5f, -2.5f),
                new Vector2(-20f, 4f),
                new Vector2(-20f, -4f),
                new Vector2(20f, 4f),
                new Vector2(20f, -4f),
                new Vector2(-1.5f, -5f),
                new Vector2(1.5f, -5f)
            };
            foreach (var tPos in treePositions)
            {
                CreateProp("Tree", propsParent.transform, tPos, sprTree, new Vector2(0.35f, 0.35f), new Vector2(0f, -0.6f), 6);
            }

            // 5. Buildings
            GameObject buildingsParent = new GameObject("Buildings");

            TownBuilding homeBld = CreateBuilding(
                "Building_Home",
                buildingsParent.transform,
                new Vector2(0.5f, 4.0f),
                sprHome,
                BuildingType.Home,
                "Student Residence (Home)",
                "Your student home residence! Rest, review daily finances, and check budget advice.",
                new Vector2(2.4f, 1.6f),
                new Vector2(0.5f, 2.5f),
                new Vector2(2.2f, 1.2f),
                new Color32(0xFF, 0xD7, 0x00, 0xFF)
            );

            TownBuilding collegeBld = CreateBuilding(
                "Building_College",
                buildingsParent.transform,
                new Vector2(-13.5f, 6.0f),
                sprCollege,
                BuildingType.College,
                "College",
                "City College campus! Attend financial literacy classes, guest lectures, and quizzes.",
                new Vector2(3.6f, 2.0f),
                new Vector2(-13.5f, 4.3f),
                new Vector2(2.5f, 1.4f),
                new Color32(0xFF, 0x8C, 0x00, 0xFF)
            );

            TownBuilding bankBld = CreateBuilding(
                "Building_Bank",
                buildingsParent.transform,
                new Vector2(14.5f, 6.0f),
                sprBank,
                BuildingType.Bank,
                "Bank",
                "National Savings Bank! Manage savings deposits, track interest growth, and explore safe investments.",
                new Vector2(3.2f, 2.0f),
                new Vector2(14.5f, 4.3f),
                new Vector2(2.5f, 1.4f),
                new Color32(0x00, 0xE5, 0xFF, 0xFF)
            );

            TownBuilding shopBld = CreateBuilding(
                "Building_Shop",
                buildingsParent.transform,
                new Vector2(-13.5f, -6.0f),
                sprShop,
                BuildingType.Shop,
                "Shop",
                "Corner Supermarket & Store! Shop for daily necessities, compare discounts, and avoid impulse buying.",
                new Vector2(2.6f, 1.8f),
                new Vector2(-13.5f, -4.5f),
                new Vector2(2.2f, 1.4f),
                new Color32(0x00, 0xE6, 0x76, 0xFF)
            );

            TownBuilding hospitalBld = CreateBuilding(
                "Building_Hospital",
                buildingsParent.transform,
                new Vector2(14.5f, -6.0f),
                sprHospital,
                BuildingType.Hospital,
                "Hospital/Clinic",
                "City Hospital & Medical Clinic! Emergency healthcare, health insurance awareness, and medical funds.",
                new Vector2(3.2f, 2.0f),
                new Vector2(14.5f, -4.5f),
                new Vector2(2.5f, 1.4f),
                new Color32(0xFF, 0x52, 0x52, 0xFF)
            );

            TownBuilding friendBld = CreateBuilding(
                "Building_FriendsHouse",
                buildingsParent.transform,
                new Vector2(-14.5f, 0.0f),
                sprFriend,
                BuildingType.FriendsHouse,
                "Friend's House",
                "Your best friend's residence! Hang out, share tips, and navigate peer spending pressure.",
                new Vector2(2.6f, 1.8f),
                new Vector2(-14.5f, -1.4f),
                new Vector2(2.5f, 1.4f),
                new Color32(0xE0, 0x40, 0xFB, 0xFF)
            );

            TownBuilding wordSearchBld = CreateBuilding(
                "Building_WordSearchHouse",
                buildingsParent.transform,
                new Vector2(-6.0f, 6.0f),
                sprWordSearch,
                BuildingType.WordSearchHouse,
                "Word Search House",
                "Word Search Game House! Find all hidden financial keywords in the letter grid to complete today's task.",
                new Vector2(3.2f, 2.0f),
                new Vector2(-6.0f, 4.3f),
                new Vector2(2.5f, 1.4f),
                new Color32(0x00, 0xE5, 0xFF, 0xFF)
            );

            TownBuilding wordFinderBld = CreateBuilding(
                "Building_WordFinderHouse",
                buildingsParent.transform,
                new Vector2(-6.0f, -7.5f),
                sprWordFinder,
                BuildingType.WordFinderHouse,
                "Word Finder House",
                "Word Finder Game House! Connect letters to form savings words and complete today's task.",
                new Vector2(3.2f, 2.0f),
                new Vector2(-6.0f, -5.8f),
                new Vector2(2.5f, 1.4f),
                new Color32(0xFF, 0x40, 0x81, 0xFF)
            );

            TownBuilding mathsSpeedBld = CreateBuilding(
                "Building_MathsSpeedHouse",
                buildingsParent.transform,
                new Vector2(6.0f, -7.5f),
                sprMathsSpeed,
                BuildingType.MathsSpeedHouse,
                "Maths Speed House",
                "Maths Speed Game House! Answer rapid mental math problems against the clock to complete today's task.",
                new Vector2(3.2f, 2.0f),
                new Vector2(6.0f, -5.8f),
                new Vector2(2.5f, 1.4f),
                new Color32(0x7C, 0x4D, 0xFF, 0xFF)
            );

            TownBuilding mathsPuzzleBld = CreateBuilding(
                "Building_MathsPuzzleHouse",
                buildingsParent.transform,
                new Vector2(6.0f, 6.0f),
                sprMathsPuzzle,
                BuildingType.MathsPuzzleHouse,
                "Maths Puzzle House",
                "Maths Puzzle Game House! Solve math sequence puzzles and missing operators to complete today's task.",
                new Vector2(3.2f, 2.0f),
                new Vector2(6.0f, 4.3f),
                new Vector2(2.5f, 1.4f),
                new Color32(0xFF, 0xAB, 0x00, 0xFF)
            );

            TownBuilding busStopBld = CreateBuilding(
                "Building_BusStop",
                buildingsParent.transform,
                new Vector2(7.5f, 2.5f),
                sprBusStop,
                BuildingType.BusStop,
                "Bus Stop",
                "Central Bus Stop! Purchase discounted monthly transit passes to save daily commute costs.",
                new Vector2(1.8f, 1.4f),
                new Vector2(7.5f, 1.5f),
                new Vector2(1.8f, 1.4f),
                new Color32(0xFF, 0xFF, 0xFF, 0xFF)
            );

            TownBuilding gameCentreBld = CreateBuilding(
                "Building_GameCentre",
                buildingsParent.transform,
                new Vector2(-14.0f, -13.0f),
                sprGameCentre,
                BuildingType.GameCentre,
                "Neon Game Arcade",
                "Neon Game Arcade! Flip and match financial cards in Memory Match to win rewards!",
                new Vector2(3.2f, 2.0f),
                new Vector2(-14.0f, -11.3f),
                new Vector2(2.5f, 1.4f),
                new Color32(0xFF, 0x00, 0x7F, 0xFF)
            );

            TownBuilding libraryBld = CreateBuilding(
                "Building_Library",
                buildingsParent.transform,
                new Vector2(14.0f, 13.0f),
                sprLibrary,
                BuildingType.Library,
                "City Central Library",
                "City Central Library! Test your financial IQ in the Quiz Master challenge to earn bonus savings!",
                new Vector2(3.6f, 2.2f),
                new Vector2(14.0f, 11.3f),
                new Vector2(2.5f, 1.4f),
                new Color32(0x00, 0xC8, 0x53, 0xFF)
            );

            TownBuilding parkBld = CreateBuilding(
                "Building_Park",
                buildingsParent.transform,
                new Vector2(-14.0f, 13.0f),
                sprPark,
                BuildingType.Park,
                "City Green Park",
                "City Green Park! Take a free wellness walk, relax, and reflect on financial goals without spending.",
                new Vector2(3.2f, 2.0f),
                new Vector2(-14.0f, 11.3f),
                new Vector2(2.5f, 1.4f),
                new Color32(0x76, 0xFF, 0x03, 0xFF)
            );

            TownBuilding communityHallBld = CreateBuilding(
                "Building_CommunityHall",
                buildingsParent.transform,
                new Vector2(0.5f, 13.0f),
                sprCommunityHall,
                BuildingType.CommunityHall,
                "Town Community Hall",
                "Town Community Hall! Attend public workshops on taxes, citizen rights, and civic grants.",
                new Vector2(3.2f, 2.0f),
                new Vector2(0.5f, 11.3f),
                new Vector2(2.5f, 1.4f),
                new Color32(0xFF, 0x6D, 0x00, 0xFF)
            );

            TownBuilding bakeryBld = CreateBuilding(
                "Building_Bakery",
                buildingsParent.transform,
                new Vector2(14.5f, 0.0f),
                sprBakery,
                BuildingType.Bakery,
                "Sweet Crumbs Bakery",
                "Sweet Crumbs Bakery & Cafe! Practice budget dining, meal planning, and mindful snacking habits.",
                new Vector2(2.8f, 1.8f),
                new Vector2(14.5f, -1.4f),
                new Vector2(2.5f, 1.4f),
                new Color32(0xFF, 0x52, 0x52, 0xFF)
            );

            // 6. Player GameObject
            GameObject playerObj = new GameObject("Player", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(PlayerController2D));
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(0.5f, 0.5f, 0f);

            SpriteRenderer playerSr = playerObj.GetComponent<SpriteRenderer>();
            playerSr.sprite = defaultCharSprite;
            playerSr.sortingOrder = 10;

            Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
            playerRb.gravityScale = 0f;
            playerRb.freezeRotation = true;
            playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D playerCol = playerObj.GetComponent<CircleCollider2D>();
            playerCol.radius = 0.32f;
            playerCol.offset = new Vector2(0f, -0.28f);

            PlayerController2D playerCtrl = playerObj.GetComponent<PlayerController2D>();
            playerCtrl.spriteRenderer = playerSr;
            playerCtrl.rb = playerRb;
            playerCtrl.moveSpeed = 4.5f;

            // Attach CameraFollow2D
            CameraFollow2D camFollow = camObj.AddComponent<CameraFollow2D>();
            camFollow.target = playerObj.transform;
            camFollow.smoothSpeed = 5f;
            camFollow.minBounds = new Vector2(-22f, -16f);
            camFollow.maxBounds = new Vector2(22f, 16f);

            // 7. HUD Canvas
            Canvas canvas = CreateCanvas();
            Transform root = canvas.transform;

            // Top Header Bar
            GameObject topBarObj = CreateUIObject("TopBar", root);
            Image topBarBg = topBarObj.AddComponent<Image>();
            topBarBg.color = new Color32(0x0C, 0x2A, 0x21, 0xF5);
            RectTransform topBarRt = topBarObj.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0f, 1f);
            topBarRt.anchorMax = new Vector2(1f, 1f);
            topBarRt.pivot = new Vector2(0.5f, 1f);
            topBarRt.anchoredPosition = Vector2.zero;
            topBarRt.sizeDelta = new Vector2(0f, 62f);

            TextMeshProUGUI dayText = CreateText("DayText", topBarObj.transform, "DAY 1 / 30", 26, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            dayText.color = new Color32(0x00, 0xFF, 0xA3, 0xFF);
            SetPositionAndSize(dayText.rectTransform, new Vector2(-680f, 0f), new Vector2(240f, 50f));

            TextMeshProUGUI moneyText = CreateText("MoneyText", topBarObj.transform, "Rs. 5,060", 26, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            moneyText.color = new Color32(0x2D, 0xFF, 0xAA, 0xFF);
            SetPositionAndSize(moneyText.rectTransform, new Vector2(-340f, 0f), new Vector2(240f, 50f));

            TextMeshProUGUI debtText = CreateText("DebtText", topBarObj.transform, "Debt: Rs. 0", 22, FontStyles.Normal, TextAlignmentOptions.Center);
            debtText.color = new Color32(0xD5, 0xF9, 0xEE, 0xFF);
            SetPositionAndSize(debtText.rectTransform, new Vector2(-20f, 0f), new Vector2(220f, 50f));

            TextMeshProUGUI scoreText = CreateText("ScoreText", topBarObj.transform, "Score: 810", 26, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            scoreText.color = new Color32(0xFF, 0xCC, 0x00, 0xFF);
            SetPositionAndSize(scoreText.rectTransform, new Vector2(300f, 0f), new Vector2(220f, 50f));

            // Minimap (Top Right)
            GameObject minimapObj = CreateUIObject("MinimapPanel", root);
            Image minimapBg = minimapObj.AddComponent<Image>();
            minimapBg.color = new Color32(0x0B, 0x1E, 0x38, 0xF8);
            RectTransform minimapRt = minimapObj.GetComponent<RectTransform>();
            minimapRt.anchorMin = new Vector2(1f, 1f);
            minimapRt.anchorMax = new Vector2(1f, 1f);
            minimapRt.pivot = new Vector2(1f, 1f);
            minimapRt.anchoredPosition = new Vector2(-20f, -10f);
            minimapRt.sizeDelta = new Vector2(160f, 150f);

            GameObject mmBorder = CreateUIObject("Border", minimapObj.transform);
            Image mmBorderImg = mmBorder.AddComponent<Image>();
            mmBorderImg.sprite = roundBox;
            mmBorderImg.type = Image.Type.Sliced;
            mmBorderImg.color = new Color32(0x00, 0xD2, 0xFF, 0xA0);
            SetStretch(mmBorder.GetComponent<RectTransform>());

            TextMeshProUGUI mmTitle = CreateText("MMTitle", minimapObj.transform, "TOWN MAP", 13, FontStyles.Bold, TextAlignmentOptions.Center);
            mmTitle.color = new Color32(0x00, 0xE5, 0xFF, 0xFF);
            SetPositionAndSize(mmTitle.rectTransform, new Vector2(0f, 60f), new Vector2(140f, 20f));

            GameObject mmAreaObj = CreateUIObject("MapArea", minimapObj.transform);
            RectTransform mmAreaRt = mmAreaObj.GetComponent<RectTransform>();
            SetPositionAndSize(mmAreaRt, new Vector2(0f, -10f), new Vector2(136f, 108f));

            GameObject playerMarkerObj = CreateUIObject("PlayerMarker", mmAreaObj.transform);
            Image pmImg = playerMarkerObj.AddComponent<Image>();
            pmImg.color = new Color32(0x00, 0xFF, 0x77, 0xFF);
            RectTransform pmRt = playerMarkerObj.GetComponent<RectTransform>();
            pmRt.sizeDelta = new Vector2(8f, 8f);

            TownMinimap townMinimap = minimapObj.AddComponent<TownMinimap>();
            townMinimap.mapArea = mmAreaRt;
            townMinimap.playerMarker = pmRt;
            townMinimap.playerTransform = playerObj.transform;

            townMinimap.RegisterBuildingMarker(homeBld.transform.position, homeBld.mapMarkerColor, "Home");
            townMinimap.RegisterBuildingMarker(collegeBld.transform.position, collegeBld.mapMarkerColor, "College");
            townMinimap.RegisterBuildingMarker(bankBld.transform.position, bankBld.mapMarkerColor, "Bank");
            townMinimap.RegisterBuildingMarker(shopBld.transform.position, shopBld.mapMarkerColor, "Shop");
            townMinimap.RegisterBuildingMarker(hospitalBld.transform.position, hospitalBld.mapMarkerColor, "Hospital");
            townMinimap.RegisterBuildingMarker(friendBld.transform.position, friendBld.mapMarkerColor, "Friend");
            townMinimap.RegisterBuildingMarker(busStopBld.transform.position, busStopBld.mapMarkerColor, "Bus");
            townMinimap.RegisterBuildingMarker(wordSearchBld.transform.position, wordSearchBld.mapMarkerColor, "Word Search");
            townMinimap.RegisterBuildingMarker(wordFinderBld.transform.position, wordFinderBld.mapMarkerColor, "Word Finder");
            townMinimap.RegisterBuildingMarker(mathsSpeedBld.transform.position, mathsSpeedBld.mapMarkerColor, "Maths Speed");
            townMinimap.RegisterBuildingMarker(mathsPuzzleBld.transform.position, mathsPuzzleBld.mapMarkerColor, "Maths Puzzle");
            townMinimap.RegisterBuildingMarker(gameCentreBld.transform.position, gameCentreBld.mapMarkerColor, "Arcade");
            townMinimap.RegisterBuildingMarker(libraryBld.transform.position, libraryBld.mapMarkerColor, "Library");
            townMinimap.RegisterBuildingMarker(parkBld.transform.position, parkBld.mapMarkerColor, "Park");
            townMinimap.RegisterBuildingMarker(communityHallBld.transform.position, communityHallBld.mapMarkerColor, "Community");
            townMinimap.RegisterBuildingMarker(bakeryBld.transform.position, bakeryBld.mapMarkerColor, "Bakery");

            // Sub Header Bar
            GameObject subBarObj = CreateUIObject("SubBar", root);
            Image subBarBg = subBarObj.AddComponent<Image>();
            subBarBg.color = new Color32(0x06, 0x16, 0x14, 0xEE);
            RectTransform subBarRt = subBarObj.GetComponent<RectTransform>();
            subBarRt.anchorMin = new Vector2(0f, 1f);
            subBarRt.anchorMax = new Vector2(1f, 1f);
            subBarRt.pivot = new Vector2(0.5f, 1f);
            subBarRt.anchoredPosition = new Vector2(0f, -62f);
            subBarRt.sizeDelta = new Vector2(0f, 38f);

            TextMeshProUGUI chapterText = CreateText("ChapterText", subBarObj.transform, "Chapter 1: A New Month Begins", 17, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            chapterText.color = new Color32(0xE0, 0xF4, 0xEC, 0xFF);
            SetPositionAndSize(chapterText.rectTransform, new Vector2(-540f, 0f), new Vector2(500f, 32f));

            TextMeshProUGUI objText = CreateText("ObjectiveText", subBarObj.transform, "* Objective: Go to College", 17, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            objText.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(objText.rectTransform, new Vector2(160f, 0f), new Vector2(400f, 32f));

            // Floating Hint Text
            GameObject hintObj = CreateUIObject("HintBanner", root);
            TextMeshProUGUI hintText = hintObj.AddComponent<TextMeshProUGUI>();
            hintText.text = "Your new month begins. Walk with WASD/Arrows. Press [E] to interact with buildings & NPCs.";
            hintText.fontSize = 17;
            hintText.fontStyle = FontStyles.Normal;
            hintText.color = new Color(1f, 1f, 1f, 0.85f);
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.raycastTarget = false;
            SetPositionAndSize(hintObj.GetComponent<RectTransform>(), new Vector2(0f, 260f), new Vector2(900f, 40f));

            // Building Prompt Banner
            GameObject promptObj = CreateUIObject("BuildingPrompt", root);
            Image promptBg = promptObj.AddComponent<Image>();
            promptBg.sprite = roundBox;
            promptBg.type = Image.Type.Sliced;
            promptBg.color = new Color32(0x0C, 0x1E, 0x33, 0xF0);
            RectTransform promptRt = promptObj.GetComponent<RectTransform>();
            SetPositionAndSize(promptRt, new Vector2(0f, 100f), new Vector2(360f, 80f));

            TextMeshProUGUI promptText = CreateText("PromptText", promptObj.transform, "[E] Enter\nStudent Residence (Home)", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            promptText.color = new Color32(0x00, 0xEA, 0xFF, 0xFF);
            SetStretch(promptText.rectTransform);

            // Transaction Feed (Bottom Left)
            GameObject txPanel = CreateUIObject("TransactionPanel", root);
            Image txBg = txPanel.AddComponent<Image>();
            txBg.sprite = roundBox;
            txBg.type = Image.Type.Sliced;
            txBg.color = new Color32(0x08, 0x11, 0x1C, 0xDC);
            RectTransform txRt = txPanel.GetComponent<RectTransform>();
            txRt.anchorMin = new Vector2(0f, 0f);
            txRt.anchorMax = new Vector2(0f, 0f);
            txRt.pivot = new Vector2(0f, 0f);
            txRt.anchoredPosition = new Vector2(30f, 24f);
            txRt.sizeDelta = new Vector2(480f, 110f);

            TextMeshProUGUI txTitle = CreateText("TxTitle", txPanel.transform, "TRANSACTION FEED", 13, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            txTitle.color = new Color32(0x00, 0xD2, 0xFF, 0xFF);
            SetPositionAndSize(txTitle.rectTransform, new Vector2(-40f, 32f), new Vector2(380f, 24f));

            TextMeshProUGUI txContent = CreateText("TxContent", txPanel.transform, "[Day 1] Initial Budget Savings Deposited (+Rs. 5,000)\n[Day 1] Monthly Allowance Recorded (+Rs. 15,000)", 13, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            txContent.color = new Color32(0xD2, 0xEB, 0xE2, 0xFF);
            SetPositionAndSize(txContent.rectTransform, new Vector2(0f, -12f), new Vector2(440f, 50f));

            // Next Day Button (Bottom Right)
            GameObject nextDayObj = CreateUIObject("NextDayButton", root);
            Image nextDayImg = nextDayObj.AddComponent<Image>();
            nextDayImg.sprite = roundBox;
            nextDayImg.type = Image.Type.Sliced;
            nextDayImg.color = new Color32(0x13, 0x58, 0xB8, 0xFF);
            RectTransform nextDayRt = nextDayObj.GetComponent<RectTransform>();
            nextDayRt.anchorMin = new Vector2(1f, 0f);
            nextDayRt.anchorMax = new Vector2(1f, 0f);
            nextDayRt.pivot = new Vector2(1f, 0f);
            nextDayRt.anchoredPosition = new Vector2(-30f, 30f);
            nextDayRt.sizeDelta = new Vector2(230f, 52f);

            Button nextDayBtn = nextDayObj.AddComponent<Button>();
            nextDayBtn.interactable = false;

            TextMeshProUGUI nextDayText = CreateText("NextDayText", nextDayObj.transform, "NEXT DAY [N]", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            nextDayText.color = new Color32(0xC0, 0xD4, 0xFA, 0xFF);
            SetStretch(nextDayText.rectTransform);

            GameObject ndTipObj = CreateUIObject("NDTooltip", root);
            Image ndTipBg = ndTipObj.AddComponent<Image>();
            ndTipBg.sprite = roundBox;
            ndTipBg.type = Image.Type.Sliced;
            ndTipBg.color = new Color32(0x1F, 0x2A, 0x40, 0xF5);
            RectTransform ndTipRt = ndTipObj.GetComponent<RectTransform>();
            ndTipRt.anchorMin = new Vector2(1f, 0f);
            ndTipRt.anchorMax = new Vector2(1f, 0f);
            ndTipRt.pivot = new Vector2(1f, 0f);
            ndTipRt.anchoredPosition = new Vector2(-30f, 90f);
            ndTipRt.sizeDelta = new Vector2(300f, 36f);

            TextMeshProUGUI ndTipText = CreateText("TipText", ndTipObj.transform, "Complete today's objectives first!", 13, FontStyles.Normal, TextAlignmentOptions.Center);
            ndTipText.color = new Color32(0xFE, 0xC6, 0x30, 0xFF);
            SetStretch(ndTipText.rectTransform);
            ndTipObj.SetActive(false);

            // Touch Controls
            GameObject touchControls = CreateUIObject("TouchControls", root);
            SetStretch(touchControls.GetComponent<RectTransform>());

            GameObject joyBaseObj = CreateUIObject("JoystickBase", touchControls.transform);
            Image joyBaseImg = joyBaseObj.AddComponent<Image>();
            joyBaseImg.sprite = sprJoyBase;
            joyBaseImg.color = new Color(1f, 1f, 1f, 0.45f);
            RectTransform joyBaseRt = joyBaseObj.GetComponent<RectTransform>();
            joyBaseRt.anchorMin = new Vector2(0f, 0f);
            joyBaseRt.anchorMax = new Vector2(0f, 0f);
            joyBaseRt.pivot = new Vector2(0.5f, 0.5f);
            joyBaseRt.anchoredPosition = new Vector2(120f, 210f);
            joyBaseRt.sizeDelta = new Vector2(120f, 120f);

            GameObject joyThumbObj = CreateUIObject("JoystickThumb", joyBaseObj.transform);
            Image joyThumbImg = joyThumbObj.AddComponent<Image>();
            joyThumbImg.sprite = sprJoyThumb;
            joyThumbImg.color = new Color(1f, 1f, 1f, 0.8f);
            RectTransform joyThumbRt = joyThumbObj.GetComponent<RectTransform>();
            joyThumbRt.sizeDelta = new Vector2(50f, 50f);

            VirtualJoystick vj = joyBaseObj.AddComponent<VirtualJoystick>();
            vj.background = joyBaseRt;
            vj.handle = joyThumbRt;

            GameObject touchBtnObj = CreateUIObject("TouchInteractButton", touchControls.transform);
            Image touchBtnImg = touchBtnObj.AddComponent<Image>();
            touchBtnImg.sprite = sprBtnInteract;
            RectTransform touchBtnRt = touchBtnObj.GetComponent<RectTransform>();
            touchBtnRt.anchorMin = new Vector2(1f, 0f);
            touchBtnRt.anchorMax = new Vector2(1f, 0f);
            touchBtnRt.pivot = new Vector2(0.5f, 0.5f);
            touchBtnRt.anchoredPosition = new Vector2(-110f, 160f);
            touchBtnRt.sizeDelta = new Vector2(76f, 76f);

            Button touchBtn = touchBtnObj.AddComponent<Button>();

            TextMeshProUGUI touchBtnLabel = CreateText("TouchLabel", touchBtnObj.transform, "[E] INTERACT", 12, FontStyles.Bold, TextAlignmentOptions.Center);
            touchBtnLabel.color = Color.white;
            SetStretch(touchBtnLabel.rectTransform);

            // Building Entry Modal Dialog
            GameObject modalObj = CreateUIObject("BuildingModal", root);
            Image modalBg = modalObj.AddComponent<Image>();
            modalBg.sprite = roundBox;
            modalBg.type = Image.Type.Sliced;
            modalBg.color = new Color32(0x11, 0x1A, 0x2E, 0xFD);
            RectTransform modalRt = modalObj.GetComponent<RectTransform>();
            SetPositionAndSize(modalRt, Vector2.zero, new Vector2(680f, 400f));

            GameObject modalIconObj = CreateUIObject("ModalIcon", modalObj.transform);
            Image modalIconImg = modalIconObj.AddComponent<Image>();
            SetPositionAndSize(modalIconObj.GetComponent<RectTransform>(), new Vector2(0f, 120f), new Vector2(72f, 60f));

            TextMeshProUGUI modalTitle = CreateText("ModalTitle", modalObj.transform, "Building Name", 30, FontStyles.Bold, TextAlignmentOptions.Center);
            modalTitle.color = new Color32(0x00, 0xE5, 0xFF, 0xFF);
            SetPositionAndSize(modalTitle.rectTransform, new Vector2(0f, 60f), new Vector2(600f, 40f));

            TextMeshProUGUI modalDesc = CreateText("ModalDesc", modalObj.transform, "Building detailed description.", 18, FontStyles.Normal, TextAlignmentOptions.Center);
            modalDesc.color = new Color32(0xD6, 0xE4, 0xF5, 0xFF);
            SetPositionAndSize(modalDesc.rectTransform, new Vector2(0f, 5f), new Vector2(580f, 50f));

            TextMeshProUGUI modalAction = CreateText("ModalAction", modalObj.transform, "What you can do here...", 16, FontStyles.Normal, TextAlignmentOptions.Center);
            modalAction.color = new Color32(0x80, 0xFF, 0xBC, 0xFF);
            SetPositionAndSize(modalAction.rectTransform, new Vector2(0f, -50f), new Vector2(580f, 40f));

            GameObject modalCloseObj = CreateUIObject("CloseButton", modalObj.transform);
            Image modalCloseImg = modalCloseObj.AddComponent<Image>();
            modalCloseImg.sprite = roundBox;
            modalCloseImg.type = Image.Type.Sliced;
            modalCloseImg.color = new Color32(0xFF, 0x3E, 0x6C, 0xFF);
            RectTransform modalCloseRt = modalCloseObj.GetComponent<RectTransform>();
            SetPositionAndSize(modalCloseRt, new Vector2(0f, -125f), new Vector2(180f, 48f));

            Button modalCloseBtn = modalCloseObj.AddComponent<Button>();
            TextMeshProUGUI modalCloseTxt = CreateText("CloseText", modalCloseObj.transform, "Exit [ESC]", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            modalCloseTxt.color = Color.white;
            SetStretch(modalCloseTxt.rectTransform);

            GameObject modalPlayObj = CreateUIObject("PlayButton", modalObj.transform);
            Image modalPlayImg = modalPlayObj.AddComponent<Image>();
            modalPlayImg.sprite = roundBox;
            modalPlayImg.type = Image.Type.Sliced;
            modalPlayImg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            RectTransform modalPlayRt = modalPlayObj.GetComponent<RectTransform>();
            SetPositionAndSize(modalPlayRt, new Vector2(-110f, -125f), new Vector2(200f, 48f));

            Button modalPlayBtn = modalPlayObj.AddComponent<Button>();
            TextMeshProUGUI modalPlayTxt = CreateText("PlayText", modalPlayObj.transform, "Play Mini-Game", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            modalPlayTxt.color = Color.white;
            SetStretch(modalPlayTxt.rectTransform);
            modalPlayObj.SetActive(false);

            modalObj.SetActive(false);

            // 8. Guide Arrow & Highlighted Path
            Sprite sprArrow = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Town/guide_arrow.png");
            GameObject guideArrowObj = new GameObject("GuideArrow", typeof(SpriteRenderer));
            SpriteRenderer arrowSr = guideArrowObj.GetComponent<SpriteRenderer>();
            arrowSr.sprite = sprArrow;
            arrowSr.sortingOrder = 50;
            guideArrowObj.SetActive(false);

            GameObject pathObj = new GameObject("HighlightedPath", typeof(LineRenderer));
            LineRenderer pathLr = pathObj.GetComponent<LineRenderer>();
            pathLr.startWidth = 0.28f;
            pathLr.endWidth = 0.28f;
            pathLr.startColor = new Color32(0x00, 0xE5, 0xFF, 0xEE);
            pathLr.endColor = new Color32(0xFF, 0xD7, 0x00, 0xEE);
            pathLr.material = new Material(Shader.Find("Sprites/Default"));
            pathLr.sortingOrder = 15;
            pathLr.enabled = false;

            // 9. Mini-Game Modal Dialog (Placeholder)
            GameObject mgModalObj = CreateUIObject("MiniGameModal", root);
            Image mgModalBg = mgModalObj.AddComponent<Image>();
            mgModalBg.sprite = roundBox;
            mgModalBg.type = Image.Type.Sliced;
            mgModalBg.color = new Color32(0x0E, 0x1A, 0x2E, 0xFD);
            RectTransform mgModalRt = mgModalObj.GetComponent<RectTransform>();
            SetPositionAndSize(mgModalRt, Vector2.zero, new Vector2(720f, 440f));

            GameObject mgIconObj = CreateUIObject("MGIcon", mgModalObj.transform);
            Image mgIconImg = mgIconObj.AddComponent<Image>();
            SetPositionAndSize(mgIconObj.GetComponent<RectTransform>(), new Vector2(0f, 135f), new Vector2(72f, 60f));

            TextMeshProUGUI mgTitle = CreateText("MGTitle", mgModalObj.transform, "Building Mini-Game", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            mgTitle.color = new Color32(0x00, 0xE5, 0xFF, 0xFF);
            SetPositionAndSize(mgTitle.rectTransform, new Vector2(0f, 75f), new Vector2(640f, 38f));

            TextMeshProUGUI mgTaskName = CreateText("MGTaskName", mgModalObj.transform, "* Intro to Budgeting", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            mgTaskName.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(mgTaskName.rectTransform, new Vector2(0f, 35f), new Vector2(640f, 32f));

            TextMeshProUGUI mgDesc = CreateText("MGDesc", mgModalObj.transform, "Task description goes here.", 17, FontStyles.Normal, TextAlignmentOptions.Center);
            mgDesc.color = new Color32(0xD0, 0xE2, 0xF5, 0xFF);
            SetPositionAndSize(mgDesc.rectTransform, new Vector2(0f, -15f), new Vector2(620f, 50f));

            TextMeshProUGUI mgReward = CreateText("MGReward", mgModalObj.transform, "Reward: +Rs. 500 Savings & +50 Score", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            mgReward.color = new Color32(0x00, 0xFF, 0xA3, 0xFF);
            SetPositionAndSize(mgReward.rectTransform, new Vector2(0f, -65f), new Vector2(600f, 32f));

            GameObject mgCompleteObj = CreateUIObject("CompleteButton", mgModalObj.transform);
            Image mgCompImg = mgCompleteObj.AddComponent<Image>();
            mgCompImg.sprite = roundBox;
            mgCompImg.type = Image.Type.Sliced;
            mgCompImg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            RectTransform mgCompRt = mgCompleteObj.GetComponent<RectTransform>();
            SetPositionAndSize(mgCompRt, new Vector2(-120f, -135f), new Vector2(230f, 52f));

            Button mgCompBtn = mgCompleteObj.AddComponent<Button>();
            TextMeshProUGUI mgCompTxt = CreateText("CompText", mgCompleteObj.transform, "Start Challenge", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            mgCompTxt.color = Color.white;
            SetStretch(mgCompTxt.rectTransform);

            GameObject mgCancelObj = CreateUIObject("CancelButton", mgModalObj.transform);
            Image mgCancelImg = mgCancelObj.AddComponent<Image>();
            mgCancelImg.sprite = roundBox;
            mgCancelImg.type = Image.Type.Sliced;
            mgCancelImg.color = new Color32(0x3E, 0x4D, 0x63, 0xFF);
            RectTransform mgCancelRt = mgCancelObj.GetComponent<RectTransform>();
            SetPositionAndSize(mgCancelRt, new Vector2(130f, -135f), new Vector2(180f, 52f));

            Button mgCancelBtn = mgCancelObj.AddComponent<Button>();
            TextMeshProUGUI mgCancelTxt = CreateText("CancelText", mgCancelObj.transform, "Back to Town", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            mgCancelTxt.color = Color.white;
            SetStretch(mgCancelTxt.rectTransform);

            mgModalObj.SetActive(false);

            // 10. Event Day Modal Dialog (Supports 2 Interactive Choices)
            GameObject evModalObj = CreateUIObject("EventModal", root);
            Image evModalBg = evModalObj.AddComponent<Image>();
            evModalBg.sprite = roundBox;
            evModalBg.type = Image.Type.Sliced;
            evModalBg.color = new Color32(0x18, 0x12, 0x26, 0xFE);
            RectTransform evModalRt = evModalObj.GetComponent<RectTransform>();
            SetPositionAndSize(evModalRt, Vector2.zero, new Vector2(740f, 440f));

            TextMeshProUGUI evBadge = CreateText("EVBadge", evModalObj.transform, "★ RANDOM SURPRISE EVENT ★", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            evBadge.color = new Color32(0x00, 0xE5, 0xFF, 0xFF);
            SetPositionAndSize(evBadge.rectTransform, new Vector2(0f, 170f), new Vector2(480f, 30f));

            TextMeshProUGUI evTitle = CreateText("EVTitle", evModalObj.transform, "Surprise Financial Event", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            evTitle.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(evTitle.rectTransform, new Vector2(0f, 125f), new Vector2(680f, 42f));

            TextMeshProUGUI evDesc = CreateText("EVDesc", evModalObj.transform, "Unexpected event details.", 18, FontStyles.Normal, TextAlignmentOptions.Center);
            evDesc.color = new Color32(0xFD, 0xFD, 0xFD, 0xFF);
            SetPositionAndSize(evDesc.rectTransform, new Vector2(0f, 45f), new Vector2(660f, 95f));

            // Choice 1 Button
            GameObject evChoice1Obj = CreateUIObject("Choice1Button", evModalObj.transform);
            Image evChoice1Img = evChoice1Obj.AddComponent<Image>();
            evChoice1Img.sprite = roundBox;
            evChoice1Img.type = Image.Type.Sliced;
            evChoice1Img.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            RectTransform evChoice1Rt = evChoice1Obj.GetComponent<RectTransform>();
            SetPositionAndSize(evChoice1Rt, new Vector2(-170f, -65f), new Vector2(310f, 65f));
            Button evChoice1Btn = evChoice1Obj.AddComponent<Button>();
            TextMeshProUGUI evChoice1Txt = CreateText("Choice1Text", evChoice1Obj.transform, "Choice A", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            evChoice1Txt.color = Color.white;
            SetStretch(evChoice1Txt.rectTransform);

            // Choice 2 Button
            GameObject evChoice2Obj = CreateUIObject("Choice2Button", evModalObj.transform);
            Image evChoice2Img = evChoice2Obj.AddComponent<Image>();
            evChoice2Img.sprite = roundBox;
            evChoice2Img.type = Image.Type.Sliced;
            evChoice2Img.color = new Color32(0x02, 0x88, 0xD1, 0xFF);
            RectTransform evChoice2Rt = evChoice2Obj.GetComponent<RectTransform>();
            SetPositionAndSize(evChoice2Rt, new Vector2(170f, -65f), new Vector2(310f, 65f));
            Button evChoice2Btn = evChoice2Obj.AddComponent<Button>();
            TextMeshProUGUI evChoice2Txt = CreateText("Choice2Text", evChoice2Obj.transform, "Choice B", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            evChoice2Txt.color = Color.white;
            SetStretch(evChoice2Txt.rectTransform);

            TextMeshProUGUI evFeedback = CreateText("EVFeedback", evModalObj.transform, "", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            evFeedback.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetPositionAndSize(evFeedback.rectTransform, new Vector2(0f, -145f), new Vector2(680f, 40f));

            // Legacy Resolve Button (hidden unless single-action fallback event)
            GameObject evResolveObj = CreateUIObject("ResolveButton", evModalObj.transform);
            Image evResolveImg = evResolveObj.AddComponent<Image>();
            evResolveImg.sprite = roundBox;
            evResolveImg.type = Image.Type.Sliced;
            evResolveImg.color = new Color32(0xD3, 0x2F, 0x2F, 0xFF); // Coral red for mandatory
            RectTransform evResolveRt = evResolveObj.GetComponent<RectTransform>();
            SetPositionAndSize(evResolveRt, new Vector2(0f, -140f), new Vector2(460f, 54f));
            Button evResolveBtn = evResolveObj.AddComponent<Button>();
            TextMeshProUGUI evResolveTxt = CreateText("ResolveText", evResolveObj.transform, "PAY MANDATORY EXPENSE", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            evResolveTxt.color = Color.white;
            SetStretch(evResolveTxt.rectTransform);
            evResolveObj.SetActive(false);

            evModalObj.SetActive(false);

            // 11. Deficit Alert Modal (Loan System - matching Screenshot 2)
            GameObject defModalObj = CreateUIObject("DeficitAlertModal", root);
            Image defModalBg = defModalObj.AddComponent<Image>();
            defModalBg.sprite = roundBox;
            defModalBg.type = Image.Type.Sliced;
            defModalBg.color = new Color32(0x16, 0x1B, 0x2E, 0xFE); // Slate Navy
            RectTransform defModalRt = defModalObj.GetComponent<RectTransform>();
            SetPositionAndSize(defModalRt, Vector2.zero, new Vector2(740f, 440f));

            TextMeshProUGUI defBadge = CreateText("Badge", defModalObj.transform, "⚠ DEFICIT ALERT: INSUFFICIENT FUNDS!", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            defBadge.color = new Color32(0xFF, 0x3B, 0x30, 0xFF);
            SetPositionAndSize(defBadge.rectTransform, new Vector2(0f, 165f), new Vector2(680f, 35f));

            TextMeshProUGUI defSub = CreateText("Subtitle", defModalObj.transform, "Your savings dropped below Rs. 0. An emergency micro-loan has been approved to cover your expenses.", 14, FontStyles.Normal, TextAlignmentOptions.Center);
            defSub.color = new Color32(0xEA, 0xEE, 0xF4, 0xFF);
            SetPositionAndSize(defSub.rectTransform, new Vector2(0f, 120f), new Vector2(660f, 40f));

            TextMeshProUGUI defCover = CreateText("DeficitCover", defModalObj.transform, "Deficit to Cover: -Rs. 90", 17, FontStyles.Bold, TextAlignmentOptions.Center);
            defCover.color = new Color32(0xFF, 0x52, 0x52, 0xFF);
            SetPositionAndSize(defCover.rectTransform, new Vector2(0f, 65f), new Vector2(600f, 30f));

            TextMeshProUGUI defRelief = CreateText("ApprovedRelief", defModalObj.transform, "Approved Cash Relief: +Rs. 2,000", 17, FontStyles.Bold, TextAlignmentOptions.Center);
            defRelief.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetPositionAndSize(defRelief.rectTransform, new Vector2(0f, 30f), new Vector2(600f, 30f));

            TextMeshProUGUI defSurcharge = CreateText("Surcharge", defModalObj.transform, "Processing Surcharge: Rs. 100", 16, FontStyles.Normal, TextAlignmentOptions.Center);
            defSurcharge.color = Color.white;
            SetPositionAndSize(defSurcharge.rectTransform, new Vector2(0f, -5f), new Vector2(600f, 30f));

            TextMeshProUGUI defInterest = CreateText("Interest", defModalObj.transform, "Monthly Interest Rate: 5% Compounding", 16, FontStyles.Normal, TextAlignmentOptions.Center);
            defInterest.color = Color.white;
            SetPositionAndSize(defInterest.rectTransform, new Vector2(0f, -40f), new Vector2(600f, 30f));

            TextMeshProUGUI defDebt = CreateText("TotalDebt", defModalObj.transform, "Total Repayment Debt: Rs. 2,200", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            defDebt.color = new Color32(0xFF, 0xB3, 0x00, 0xFF);
            SetPositionAndSize(defDebt.rectTransform, new Vector2(0f, -75f), new Vector2(600f, 32f));

            TextMeshProUGUI defCredit = CreateText("CreditImpact", defModalObj.transform, "Credit Rating Impact: -60 Points", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            defCredit.color = new Color32(0xFF, 0x52, 0x52, 0xFF);
            SetPositionAndSize(defCredit.rectTransform, new Vector2(0f, -110f), new Vector2(600f, 30f));

            GameObject defBtnObj = CreateUIObject("AcceptButton", defModalObj.transform);
            Image defBtnImg = defBtnObj.AddComponent<Image>();
            defBtnImg.sprite = roundBox;
            defBtnImg.type = Image.Type.Sliced;
            defBtnImg.color = new Color32(0xD3, 0x2F, 0x2F, 0xFF);
            RectTransform defBtnRt = defBtnObj.GetComponent<RectTransform>();
            SetPositionAndSize(defBtnRt, new Vector2(0f, -165f), new Vector2(460f, 54f));
            Button defBtn = defBtnObj.AddComponent<Button>();
            TextMeshProUGUI defBtnTxt = CreateText("BtnText", defBtnObj.transform, "ACCEPT EMERGENCY STUDENT LOAN", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            defBtnTxt.color = Color.white;
            SetStretch(defBtnTxt.rectTransform);

            defModalObj.SetActive(false);

            // Bank Loan Repayment Modal
            GameObject bankModalObj = CreateUIObject("BankLoanModal", root);
            Image bankModalBg = bankModalObj.AddComponent<Image>();
            bankModalBg.sprite = roundBox;
            bankModalBg.type = Image.Type.Sliced;
            bankModalBg.color = new Color32(0x10, 0x1F, 0x33, 0xFE);
            RectTransform bankModalRt = bankModalObj.GetComponent<RectTransform>();
            SetPositionAndSize(bankModalRt, Vector2.zero, new Vector2(680f, 420f));

            TextMeshProUGUI bankTitle = CreateText("Title", bankModalObj.transform, "🏦 State Bank of Rupee — Loan Desk", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            bankTitle.color = new Color32(0x00, 0xE5, 0xFF, 0xFF);
            SetPositionAndSize(bankTitle.rectTransform, new Vector2(0f, 155f), new Vector2(620f, 35f));

            TextMeshProUGUI bankDesc = CreateText("Desc", bankModalObj.transform, "Repaying student debt early prevents daily 5% compounding interest and awards bonus credit score!", 15, FontStyles.Normal, TextAlignmentOptions.Center);
            bankDesc.color = Color.white;
            SetPositionAndSize(bankDesc.rectTransform, new Vector2(0f, 110f), new Vector2(600f, 40f));

            TextMeshProUGUI bankDebtTxt = CreateText("Debt", bankModalObj.transform, "Outstanding Debt: Rs. 0", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            bankDebtTxt.color = new Color32(0xFF, 0x52, 0x52, 0xFF);
            SetPositionAndSize(bankDebtTxt.rectTransform, new Vector2(0f, 45f), new Vector2(580f, 35f));

            TextMeshProUGUI bankSavTxt = CreateText("Savings", bankModalObj.transform, "Available Savings: Rs. 0", 18, FontStyles.Normal, TextAlignmentOptions.Center);
            bankSavTxt.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetPositionAndSize(bankSavTxt.rectTransform, new Vector2(0f, 5f), new Vector2(580f, 30f));

            GameObject bRepayFullObj = CreateUIObject("RepayFullButton", bankModalObj.transform);
            Image bRepayFullImg = bRepayFullObj.AddComponent<Image>();
            bRepayFullImg.sprite = roundBox;
            bRepayFullImg.type = Image.Type.Sliced;
            bRepayFullImg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            RectTransform bRepayFullRt = bRepayFullObj.GetComponent<RectTransform>();
            SetPositionAndSize(bRepayFullRt, new Vector2(0f, -55f), new Vector2(480f, 48f));
            Button bRepayFullBtn = bRepayFullObj.AddComponent<Button>();
            TextMeshProUGUI bRepayFullTxt = CreateText("Txt", bRepayFullObj.transform, "Repay Full Loan [+50 pts]", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            bRepayFullTxt.color = Color.white;
            SetStretch(bRepayFullTxt.rectTransform);

            GameObject bRepayPartObj = CreateUIObject("InstallmentButton", bankModalObj.transform);
            Image bRepayPartImg = bRepayPartObj.AddComponent<Image>();
            bRepayPartImg.sprite = roundBox;
            bRepayPartImg.type = Image.Type.Sliced;
            bRepayPartImg.color = new Color32(0x02, 0x88, 0xD1, 0xFF);
            RectTransform bRepayPartRt = bRepayPartObj.GetComponent<RectTransform>();
            SetPositionAndSize(bRepayPartRt, new Vector2(0f, -112f), new Vector2(480f, 48f));
            Button bRepayPartBtn = bRepayPartObj.AddComponent<Button>();
            TextMeshProUGUI bRepayPartTxt = CreateText("Txt", bRepayPartObj.transform, "Pay Installment (-Rs. 1,000) [+25 pts]", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            bRepayPartTxt.color = Color.white;
            SetStretch(bRepayPartTxt.rectTransform);

            GameObject bCloseObj = CreateUIObject("CloseButton", bankModalObj.transform);
            Image bCloseImg = bCloseObj.AddComponent<Image>();
            bCloseImg.sprite = roundBox;
            bCloseImg.type = Image.Type.Sliced;
            bCloseImg.color = new Color32(0x45, 0x5A, 0x64, 0xFF);
            RectTransform bCloseRt = bCloseObj.GetComponent<RectTransform>();
            SetPositionAndSize(bCloseRt, new Vector2(0f, -168f), new Vector2(220f, 42f));
            Button bCloseBtn = bCloseObj.AddComponent<Button>();
            TextMeshProUGUI bCloseTxt = CreateText("Txt", bCloseObj.transform, "Back to Town", 15, FontStyles.Bold, TextAlignmentOptions.Center);
            bCloseTxt.color = Color.white;
            SetStretch(bCloseTxt.rectTransform);

            bankModalObj.SetActive(false);

            // Floating Score Feedback Text
            GameObject floatScoreObj = CreateUIObject("FloatingScoreFeedback", root);
            RectTransform floatScoreRt = floatScoreObj.GetComponent<RectTransform>();
            floatScoreRt.anchorMin = new Vector2(0.5f, 1f);
            floatScoreRt.anchorMax = new Vector2(0.5f, 1f);
            floatScoreRt.pivot = new Vector2(0.5f, 0.5f);
            SetPositionAndSize(floatScoreRt, new Vector2(300f, -48f), new Vector2(160f, 36f));
            TextMeshProUGUI floatScoreTxt = CreateText("Txt", floatScoreObj.transform, "+50 pts", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            floatScoreTxt.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetStretch(floatScoreTxt.rectTransform);
            floatScoreObj.SetActive(false);

            // 12. Attach TownHUD Script
            TownHUD hud = canvas.gameObject.AddComponent<TownHUD>();
            hud.dayText = dayText;
            hud.moneyText = moneyText;
            hud.debtText = debtText;
            hud.scoreText = scoreText;

            hud.chapterText = chapterText;
            hud.objectiveText = objText;

            hud.hintObject = hintObj;
            hud.hintText = hintText;

            hud.promptContainer = promptObj;
            hud.promptText = promptText;

            hud.transactionPanel = txPanel;
            hud.transactionFeedText = txContent;

            hud.nextDayButton = nextDayBtn;
            hud.nextDayButtonText = nextDayText;
            hud.nextDayTooltipText = ndTipText;

            hud.touchControlsContainer = touchControls;
            hud.touchInteractButton = touchBtn;

            hud.minimap = townMinimap;

            hud.buildingModal = modalObj;
            hud.modalIcon = modalIconImg;
            hud.modalTitle = modalTitle;
            hud.modalDescription = modalDesc;
            hud.modalActionNote = modalAction;
            hud.modalCloseButton = modalCloseBtn;
            hud.modalPlayButton = modalPlayBtn;

            hud.miniGameModal = mgModalObj;
            hud.miniGameIcon = mgIconImg;
            hud.miniGameTitle = mgTitle;
            hud.miniGameTaskName = mgTaskName;
            hud.miniGameDescription = mgDesc;
            hud.miniGameRewardText = mgReward;
            hud.miniGameCompleteButton = mgCompBtn;
            hud.miniGameCancelButton = mgCancelBtn;

            hud.eventDayModal = evModalObj;
            hud.eventBadgeText = evBadge;
            hud.eventTitle = evTitle;
            hud.eventDescription = evDesc;
            hud.eventResolveButton = evResolveBtn;
            hud.eventResolveButtonText = evResolveTxt;
            hud.eventChoice1Button = evChoice1Btn;
            hud.eventChoice1Text = evChoice1Txt;
            hud.eventChoice2Button = evChoice2Btn;
            hud.eventChoice2Text = evChoice2Txt;
            hud.eventFeedbackText = evFeedback;

            hud.deficitAlertModal = defModalObj;
            hud.deficitBadgeText = defBadge;
            hud.deficitSubtitleText = defSub;
            hud.deficitCoverText = defCover;
            hud.deficitReliefText = defRelief;
            hud.deficitSurchargeText = defSurcharge;
            hud.deficitInterestText = defInterest;
            hud.deficitTotalDebtText = defDebt;
            hud.deficitCreditImpactText = defCredit;
            hud.deficitAcceptButton = defBtn;

            hud.bankLoanModal = bankModalObj;
            hud.bankLoanTitleText = bankTitle;
            hud.bankLoanDescText = bankDesc;
            hud.bankDebtText = bankDebtTxt;
            hud.bankSavingsText = bankSavTxt;
            hud.bankRepayFullButton = bRepayFullBtn;
            hud.bankRepayFullButtonText = bRepayFullTxt;
            hud.bankRepayInstallmentButton = bRepayPartBtn;
            hud.bankRepayInstallmentButtonText = bRepayPartTxt;
            hud.bankCloseButton = bCloseBtn;

            hud.floatingScoreText = floatScoreTxt;

            // 13. Create DayManager
            GameObject dmObj = new GameObject("DayManager", typeof(DayManager));
            DayManager dm = dmObj.GetComponent<DayManager>();
            dm.allDays = tasks;
            dm.player = playerCtrl;
            dm.playerTransform = playerObj.transform;
            dm.homeSpawnPosition = new Vector3(0.5f, 0.5f, 0f);
            dm.guideArrow = guideArrowObj;
            dm.guideArrowRenderer = arrowSr;
            dm.pathLineRenderer = pathLr;
            dm.townBuildings = new List<TownBuilding> {
                homeBld, collegeBld, bankBld, shopBld, hospitalBld, friendBld, busStopBld,
                wordSearchBld, wordFinderBld, mathsSpeedBld, mathsPuzzleBld,
                gameCentreBld, libraryBld, parkBld, communityHallBld, bakeryBld
            };
            dm.dayPlansJsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/DayPlans.json");

            GameObject remObj = new GameObject("RandomEventManager", typeof(RandomEventManager));
            GameObject mgmObj = new GameObject("MiniGameManager", typeof(RupeeRanger.MiniGames.MiniGameManager));

            string path = $"{SCENE_DIR}/Town.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static Tilemap CreateTilemapLayer(string name, Transform parent, int sortingOrder)
        {
            GameObject obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            obj.transform.SetParent(parent, false);
            TilemapRenderer tr = obj.GetComponent<TilemapRenderer>();
            tr.sortingOrder = sortingOrder;
            return obj.GetComponent<Tilemap>();
        }

        private static void FillPavementBox(Tilemap tm, Tile tile, int xMin, int xMax, int yMin, int yMax)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    tm.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        private static GameObject CreateProp(string name, Transform parent, Vector2 pos, Sprite sprite, Vector2 colSize, Vector2 colOffset, int sortingOrder)
        {
            GameObject obj = new GameObject(name, typeof(SpriteRenderer), typeof(CircleCollider2D));
            obj.transform.SetParent(parent, false);
            obj.transform.position = new Vector3(pos.x, pos.y, 0f);

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;

            CircleCollider2D col = obj.GetComponent<CircleCollider2D>();
            col.radius = colSize.x;
            col.offset = colOffset;

            return obj;
        }

        private static TownBuilding CreateBuilding(
            string name,
            Transform parent,
            Vector2 pos,
            Sprite sprite,
            BuildingType type,
            string bName,
            string desc,
            Vector2 wallColSize,
            Vector2 triggerPos,
            Vector2 triggerSize,
            Color markerColor)
        {
            GameObject obj = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D));
            obj.transform.SetParent(parent, false);
            obj.transform.position = new Vector3(pos.x, pos.y, 0f);

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;

            BoxCollider2D wallCol = obj.GetComponent<BoxCollider2D>();
            wallCol.size = wallColSize;
            wallCol.offset = new Vector2(0f, 0.4f);

            GameObject triggerObj = new GameObject("TriggerZone", typeof(BoxCollider2D), typeof(TownBuilding));
            triggerObj.transform.SetParent(obj.transform, false);
            triggerObj.transform.position = new Vector3(triggerPos.x, triggerPos.y, 0f);

            BoxCollider2D triggerCol = triggerObj.GetComponent<BoxCollider2D>();
            triggerCol.isTrigger = true;
            triggerCol.size = triggerSize;

            TownBuilding tb = triggerObj.GetComponent<TownBuilding>();
            tb.buildingType = type;
            tb.buildingName = bName;
            tb.description = desc;
            tb.buildingIcon = sprite;
            tb.mapMarkerColor = markerColor;

            // Floating World-Space Name Badge above building roof
            GameObject badgeCanvasObj = new GameObject("WorldBadge", typeof(RectTransform), typeof(Canvas));
            badgeCanvasObj.transform.SetParent(obj.transform, false);
            badgeCanvasObj.transform.localPosition = new Vector3(0f, wallColSize.y * 0.5f + 1.25f, 0f);

            Canvas badgeCanvas = badgeCanvasObj.GetComponent<Canvas>();
            badgeCanvas.renderMode = RenderMode.WorldSpace;
            badgeCanvas.sortingOrder = 25;

            RectTransform badgeRt = badgeCanvasObj.GetComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(360f, 60f);
            badgeRt.localScale = new Vector3(0.007f, 0.007f, 1f);

            Sprite roundBox = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box.png");

            // Pill background
            GameObject bgObj = new GameObject("BadgeBg", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(badgeCanvasObj.transform, false);
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = roundBox;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color32(0x0A, 0x12, 0x22, 0xF2);
            SetStretch(bgObj.GetComponent<RectTransform>());

            // Colored accent bar at bottom of badge
            GameObject accentObj = new GameObject("BadgeAccent", typeof(RectTransform), typeof(Image));
            accentObj.transform.SetParent(badgeCanvasObj.transform, false);
            Image accentImg = accentObj.GetComponent<Image>();
            accentImg.sprite = roundBox;
            accentImg.type = Image.Type.Sliced;
            accentImg.color = markerColor;
            RectTransform accentRt = accentObj.GetComponent<RectTransform>();
            SetPositionAndSize(accentRt, new Vector2(0f, -26f), new Vector2(330f, 6f));

            // Name Text
            GameObject txtObj = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(badgeCanvasObj.transform, false);
            SetStretch(txtObj.GetComponent<RectTransform>());
            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = bName.ToUpper();
            txt.fontSize = 28;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;

            return tb;
        }

        private static Tile GetOrCreateTile(string tileName, Sprite sprite)
        {
            string dir = "Assets/Data/Tiles";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string assetPath = $"{dir}/{tileName}.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                AssetDatabase.CreateAsset(tile, assetPath);
            }
            else
            {
                tile.sprite = sprite;
                EditorUtility.SetDirty(tile);
            }
            return tile;
        }

        #endregion

        [MenuItem("Tools/RupeeRanger/Rebuild Result Scene")]
        public static void MenuRebuildResultScene()
        {
            CreateScriptableObjects(out _, out List<CharacterData> characters);
            BuildResultScene(characters);
        }

        private static string BuildResultScene(List<CharacterData> characters)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(new Color32(0x0C, 0x10, 0x1F, 0xFF));
            CreateEventSystem();
            EnsureGameManager();

            Canvas canvas = CreateCanvas();
            Transform root = canvas.transform;

            Sprite roundBox = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box.png");
            Sprite sunIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_theme_sun.png");
            Sprite moonIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_theme_moon.png");
            Sprite trophyIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/icon_trophy.png");
            Sprite pennySprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/char_penny.png");

            // Full screen background
            Image bg = CreateImage("Background", root, null, new Color32(0x0C, 0x10, 0x1F, 0xFF));
            SetStretch(bg.rectTransform);

            ResultUI resultUI = canvas.gameObject.AddComponent<ResultUI>();
            resultUI.sunIcon = sunIcon;
            resultUI.moonIcon = moonIcon;

            // Top Bar
            GameObject topBar = CreateUIObject("TopBar", root);
            RectTransform topBarRt = topBar.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0, 1);
            topBarRt.anchorMax = new Vector2(1, 1);
            topBarRt.pivot = new Vector2(0.5f, 1);
            topBarRt.sizeDelta = new Vector2(0, 64);
            topBarRt.anchoredPosition = Vector2.zero;

            // Logo on left
            TextMeshProUGUI logoTxt = CreateText("LogoText", topBar.transform, "<color=#FF4081>₹</color> RupeeRanger", 26, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            SetPositionAndSize(logoTxt.rectTransform, new Vector2(-460, 0), new Vector2(280, 50));
            resultUI.logoText = logoTxt;

            // Theme toggle button on right
            GameObject themeBtnObj = CreateUIObject("ThemeToggleBtn", topBar.transform);
            Image themeBtnBg = themeBtnObj.AddComponent<Image>();
            themeBtnBg.sprite = roundBox;
            themeBtnBg.type = Image.Type.Sliced;
            themeBtnBg.color = new Color32(0x1F, 0x27, 0x47, 0xFF);
            SetPositionAndSize(themeBtnObj.GetComponent<RectTransform>(), new Vector2(460, 0), new Vector2(48, 48));

            Button themeBtn = themeBtnObj.AddComponent<Button>();
            resultUI.themeToggleButton = themeBtn;

            Image themeIconImg = CreateImage("Icon", themeBtnObj.transform, sunIcon, Color.white);
            SetPositionAndSize(themeIconImg.rectTransform, Vector2.zero, new Vector2(28, 28));
            resultUI.themeToggleIcon = themeIconImg;

            // ScrollView Container
            GameObject scrollView = CreateUIObject("ScrollView", root);
            RectTransform scrollRt = scrollView.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(0, 0);
            scrollRt.offsetMax = new Vector2(0, -64);

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            SetStretch(vpRt);
            viewport.AddComponent<RectMask2D>();

            GameObject content = CreateUIObject("Content", viewport.transform);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 1);
            contentRt.anchorMax = new Vector2(0.5f, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(1040, 1600);
            contentRt.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(30, 30, 20, 50);
            vlg.spacing = 22;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRt;
            scroll.content = contentRt;

            // --- 1. HERO SECTION ---
            GameObject heroObj = CreateUIObject("HeroSection", content.transform);
            heroObj.AddComponent<LayoutElement>().preferredHeight = 150;
            Image starImg = CreateImage("StarIcon", heroObj.transform, trophyIcon, new Color32(0xFF, 0xD7, 0x00, 0xFF));
            SetPositionAndSize(starImg.rectTransform, new Vector2(0, 45), new Vector2(48, 48));
            resultUI.starIcon = starImg;

            TextMeshProUGUI goTitle = CreateText("GameOverTitle", heroObj.transform, "Game Over!", 44, FontStyles.Bold, TextAlignmentOptions.Center);
            goTitle.color = Color.white;
            SetPositionAndSize(goTitle.rectTransform, new Vector2(0, -2), new Vector2(980, 50));
            resultUI.gameOverTitle = goTitle;

            TextMeshProUGUI congrats = CreateText("Congratulations", heroObj.transform, "Congratulations Adventurer! You completed 30 days of financial survival.", 20, FontStyles.Normal, TextAlignmentOptions.Center);
            congrats.color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(congrats.rectTransform, new Vector2(0, -42), new Vector2(980, 36));
            resultUI.congratulationsText = congrats;

            // --- 2. FINAL SCORE CARD ---
            GameObject scoreCard = CreateUIObject("FinalScoreCard", content.transform);
            scoreCard.AddComponent<LayoutElement>().preferredHeight = 160;
            Image scoreCardBg = scoreCard.AddComponent<Image>();
            scoreCardBg.sprite = roundBox;
            scoreCardBg.type = Image.Type.Sliced;
            scoreCardBg.color = new Color32(0x15, 0x1B, 0x33, 0xF0);

            TextMeshProUGUI scoreLabel = CreateText("ScoreLabel", scoreCard.transform, "Your Final Score", 17, FontStyles.Bold, TextAlignmentOptions.Center);
            scoreLabel.color = new Color32(0x81, 0x8C, 0xF8, 0xFF);
            SetPositionAndSize(scoreLabel.rectTransform, new Vector2(0, 46), new Vector2(900, 24));
            resultUI.scoreLabelText = scoreLabel;

            TextMeshProUGUI finalScoreVal = CreateText("ScoreValue", scoreCard.transform, "0", 62, FontStyles.Bold, TextAlignmentOptions.Center);
            finalScoreVal.color = new Color32(0xFA, 0xCC, 0x15, 0xFF); // Gold
            SetPositionAndSize(finalScoreVal.rectTransform, new Vector2(0, 0), new Vector2(900, 68));
            resultUI.finalScoreValueText = finalScoreVal;

            TextMeshProUGUI formulaTxt = CreateText("Formula", scoreCard.transform, "Score = (Savings × 2) - (Loan × 1.5)", 16, FontStyles.Normal, TextAlignmentOptions.Center);
            formulaTxt.color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(formulaTxt.rectTransform, new Vector2(0, -48), new Vector2(900, 24));
            resultUI.scoreFormulaText = formulaTxt;

            // --- 3. CHARACTER STAGE CARD ---
            GameObject charStage = CreateUIObject("CharacterStage", content.transform);
            charStage.AddComponent<LayoutElement>().preferredHeight = 150;
            Image charStageBg = charStage.AddComponent<Image>();
            charStageBg.sprite = roundBox;
            charStageBg.type = Image.Type.Sliced;
            charStageBg.color = new Color32(0x17, 0x1F, 0x3D, 0xE0);

            // Character portrait on left
            Image charImg = CreateImage("CharacterPortrait", charStage.transform, pennySprite, Color.white);
            SetPositionAndSize(charImg.rectTransform, new Vector2(-360, 0), new Vector2(110, 110));
            resultUI.characterImage = charImg;

            // Aura
            GameObject aura = CreateUIObject("AuraGlow", charStage.transform);
            Image auraImg = aura.AddComponent<Image>();
            auraImg.color = new Color32(0xFF, 0xD7, 0x00, 0x25);
            SetPositionAndSize(aura.GetComponent<RectTransform>(), new Vector2(-360, 0), new Vector2(130, 130));
            resultUI.celebrationAura = aura;

            // Name & Emotion Badge
            TextMeshProUGUI charNameTxt = CreateText("CharName", charStage.transform, "Penny Ranger", 24, FontStyles.Bold, TextAlignmentOptions.Left);
            charNameTxt.color = Color.white;
            SetPositionAndSize(charNameTxt.rectTransform, new Vector2(60, 25), new Vector2(650, 32));
            resultUI.characterNameText = charNameTxt;

            TextMeshProUGUI emotionBadge = CreateText("EmotionBadge", charStage.transform, "🌟 OUTSTANDING FINANCIAL RANGER!", 18, FontStyles.Bold, TextAlignmentOptions.Left);
            emotionBadge.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(emotionBadge.rectTransform, new Vector2(60, -15), new Vector2(650, 30));
            resultUI.characterEmotionBadgeText = emotionBadge;

            // Confetti Container inside Canvas overlay
            GameObject confettiObj = CreateUIObject("ConfettiContainer", root);
            RectTransform confettiRt = confettiObj.GetComponent<RectTransform>();
            SetStretch(confettiRt);
            resultUI.confettiContainer = confettiRt;

            // --- 4. AI FINANCIAL ADVICE SECTION (PROMINENT AT TOP!) ---
            GameObject adviceCard = CreateUIObject("AIAdviceSection", content.transform);
            Image adviceBg = adviceCard.AddComponent<Image>();
            adviceBg.sprite = roundBox;
            adviceBg.type = Image.Type.Sliced;
            adviceBg.color = new Color32(0x15, 0x1B, 0x33, 0xF0);

            VerticalLayoutGroup adviceVlg = adviceCard.AddComponent<VerticalLayoutGroup>();
            adviceVlg.padding = new RectOffset(24, 24, 20, 24);
            adviceVlg.spacing = 14;
            adviceVlg.childAlignment = TextAnchor.UpperLeft;
            adviceVlg.childControlWidth = true;
            adviceVlg.childControlHeight = false;
            adviceVlg.childForceExpandWidth = false;
            adviceVlg.childForceExpandHeight = false;

            ContentSizeFitter adviceCsf = adviceCard.AddComponent<ContentSizeFitter>();
            adviceCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI adviceTitle = CreateText("AdviceTitle", adviceCard.transform, "🤖 AI Financial Advice", 23, FontStyles.Bold, TextAlignmentOptions.Left);
            adviceTitle.color = Color.white;
            adviceTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
            resultUI.adviceSectionTitle = adviceTitle;

            GameObject adviceCardsBox = CreateUIObject("AdviceCardsContainer", adviceCard.transform);
            VerticalLayoutGroup acVlg = adviceCardsBox.AddComponent<VerticalLayoutGroup>();
            acVlg.spacing = 12;
            acVlg.childControlWidth = true;
            acVlg.childControlHeight = false;
            acVlg.childForceExpandWidth = false;
            acVlg.childForceExpandHeight = false;

            ContentSizeFitter acCsf = adviceCardsBox.AddComponent<ContentSizeFitter>();
            acCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            resultUI.adviceCardsContainer = adviceCardsBox.transform;

            // --- 5. FINANCIAL SUMMARY & PLAYER INFO (2-COLUMN ROW) ---
            GameObject summaryRow = CreateUIObject("SummaryRow", content.transform);
            summaryRow.AddComponent<LayoutElement>().preferredHeight = 250;

            HorizontalLayoutGroup rowHlg = summaryRow.AddComponent<HorizontalLayoutGroup>();
            rowHlg.spacing = 20;
            rowHlg.childAlignment = TextAnchor.MiddleCenter;
            rowHlg.childControlWidth = true;
            rowHlg.childControlHeight = true;
            rowHlg.childForceExpandWidth = true;
            rowHlg.childForceExpandHeight = true;

            // Card 5A: Financial Summary
            GameObject finSummaryCard = CreateUIObject("FinancialSummaryCard", summaryRow.transform);
            Image finBg = finSummaryCard.AddComponent<Image>();
            finBg.sprite = roundBox;
            finBg.type = Image.Type.Sliced;
            finBg.color = new Color32(0x15, 0x1B, 0x33, 0xF0);

            TextMeshProUGUI finTitle = CreateText("Title", finSummaryCard.transform, "Financial Summary", 21, FontStyles.Bold, TextAlignmentOptions.Left);
            finTitle.color = Color.white;
            SetPositionAndSize(finTitle.rectTransform, new Vector2(0, 85), new Vector2(410, 30));
            resultUI.summaryTitleText = finTitle;

            // Income
            CreateText("IncomeLabel", finSummaryCard.transform, "Total Income (30 days)", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(finSummaryCard.transform.Find("IncomeLabel").GetComponent<RectTransform>(), new Vector2(-95, 45), new Vector2(220, 26));
            TextMeshProUGUI incVal = CreateText("IncomeVal", finSummaryCard.transform, "₹450,000", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            incVal.color = new Color32(0x00, 0xF0, 0xA0, 0xFF);
            SetPositionAndSize(incVal.rectTransform, new Vector2(105, 45), new Vector2(200, 26));
            resultUI.totalIncomeValueText = incVal;

            // Expenses
            CreateText("ExpLabel", finSummaryCard.transform, "Total Expenses (30 days)", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(finSummaryCard.transform.Find("ExpLabel").GetComponent<RectTransform>(), new Vector2(-95, 10), new Vector2(220, 26));
            TextMeshProUGUI expVal = CreateText("ExpVal", finSummaryCard.transform, "-₹195,000", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            expVal.color = new Color32(0xFF, 0x4E, 0x78, 0xFF);
            SetPositionAndSize(expVal.rectTransform, new Vector2(105, 10), new Vector2(200, 26));
            resultUI.totalExpensesValueText = expVal;

            // Final Savings
            CreateText("SavLabel", finSummaryCard.transform, "Final Savings", 17, FontStyles.Bold, TextAlignmentOptions.Left).color = Color.white;
            SetPositionAndSize(finSummaryCard.transform.Find("SavLabel").GetComponent<RectTransform>(), new Vector2(-95, -30), new Vector2(220, 26));
            TextMeshProUGUI savVal = CreateText("SavVal", finSummaryCard.transform, "₹8,716.67", 19, FontStyles.Bold, TextAlignmentOptions.Right);
            savVal.color = new Color32(0x00, 0xF0, 0xA0, 0xFF);
            SetPositionAndSize(savVal.rectTransform, new Vector2(105, -30), new Vector2(200, 26));
            resultUI.finalSavingsValueText = savVal;

            // Total Loan Taken
            CreateText("LoanLabel", finSummaryCard.transform, "Total Loan Taken", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(finSummaryCard.transform.Find("LoanLabel").GetComponent<RectTransform>(), new Vector2(-95, -68), new Vector2(220, 26));
            TextMeshProUGUI loanVal = CreateText("LoanVal", finSummaryCard.transform, "₹0", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            loanVal.color = new Color32(0xFF, 0xC1, 0x07, 0xFF);
            SetPositionAndSize(loanVal.rectTransform, new Vector2(105, -68), new Vector2(200, 26));
            resultUI.totalLoanValueText = loanVal;

            // Card 5B: Player Info
            GameObject playerInfoCard = CreateUIObject("PlayerInfoCard", summaryRow.transform);
            Image piBg = playerInfoCard.AddComponent<Image>();
            piBg.sprite = roundBox;
            piBg.type = Image.Type.Sliced;
            piBg.color = new Color32(0x15, 0x1B, 0x33, 0xF0);

            TextMeshProUGUI piTitle = CreateText("Title", playerInfoCard.transform, "Player Info", 21, FontStyles.Bold, TextAlignmentOptions.Left);
            piTitle.color = Color.white;
            SetPositionAndSize(piTitle.rectTransform, new Vector2(0, 85), new Vector2(410, 30));
            resultUI.playerInfoTitleText = piTitle;

            // Player Name
            CreateText("NameLabel", playerInfoCard.transform, "Player Name", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(playerInfoCard.transform.Find("NameLabel").GetComponent<RectTransform>(), new Vector2(-95, 52), new Vector2(220, 24));
            TextMeshProUGUI nameVal = CreateText("NameVal", playerInfoCard.transform, "dds", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            nameVal.color = Color.white;
            SetPositionAndSize(nameVal.rectTransform, new Vector2(105, 52), new Vector2(200, 24));
            resultUI.playerNameValueText = nameVal;

            // Lifestyle
            CreateText("LifeLabel", playerInfoCard.transform, "Lifestyle", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(playerInfoCard.transform.Find("LifeLabel").GetComponent<RectTransform>(), new Vector2(-95, 22), new Vector2(220, 24));
            TextMeshProUGUI lifeVal = CreateText("LifeVal", playerInfoCard.transform, "Middle", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            lifeVal.color = new Color32(0xFF, 0x4E, 0x78, 0xFF);
            SetPositionAndSize(lifeVal.rectTransform, new Vector2(105, 22), new Vector2(200, 24));
            resultUI.lifestyleValueText = lifeVal;

            // Monthly Income
            CreateText("IncLabel", playerInfoCard.transform, "Monthly Income", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(playerInfoCard.transform.Find("IncLabel").GetComponent<RectTransform>(), new Vector2(-95, -8), new Vector2(220, 24));
            TextMeshProUGUI mIncVal = CreateText("IncVal", playerInfoCard.transform, "₹15,000", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            mIncVal.color = new Color32(0x00, 0xF0, 0xA0, 0xFF);
            SetPositionAndSize(mIncVal.rectTransform, new Vector2(105, -8), new Vector2(200, 24));
            resultUI.monthlyIncomeValueText = mIncVal;

            // Days Played
            CreateText("DaysLabel", playerInfoCard.transform, "Days Played", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(playerInfoCard.transform.Find("DaysLabel").GetComponent<RectTransform>(), new Vector2(-95, -38), new Vector2(220, 24));
            TextMeshProUGUI daysVal = CreateText("DaysVal", playerInfoCard.transform, "30 days", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            daysVal.color = Color.white;
            SetPositionAndSize(daysVal.rectTransform, new Vector2(105, -38), new Vector2(200, 24));
            resultUI.daysPlayedValueText = daysVal;

            // Random Events
            CreateText("EvLabel", playerInfoCard.transform, "Random Events", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(playerInfoCard.transform.Find("EvLabel").GetComponent<RectTransform>(), new Vector2(-95, -68), new Vector2(220, 24));
            TextMeshProUGUI evVal = CreateText("EvVal", playerInfoCard.transform, "7", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            evVal.color = new Color32(0xFF, 0xC1, 0x07, 0xFF);
            SetPositionAndSize(evVal.rectTransform, new Vector2(105, -68), new Vector2(200, 24));
            resultUI.randomEventsCountText = evVal;

            // --- 6. EVENTS ENCOUNTERED CARD (NO DUPLICATES) ---
            GameObject eventsCard = CreateUIObject("EventsEncounteredCard", content.transform);
            Image evBg = eventsCard.AddComponent<Image>();
            evBg.sprite = roundBox;
            evBg.type = Image.Type.Sliced;
            evBg.color = new Color32(0x15, 0x1B, 0x33, 0xF0);

            VerticalLayoutGroup evVlg = eventsCard.AddComponent<VerticalLayoutGroup>();
            evVlg.padding = new RectOffset(24, 24, 20, 24);
            evVlg.spacing = 16;
            evVlg.childAlignment = TextAnchor.UpperLeft;
            evVlg.childControlWidth = true;
            evVlg.childControlHeight = false;
            evVlg.childForceExpandWidth = false;
            evVlg.childForceExpandHeight = false;

            ContentSizeFitter evCsf = eventsCard.AddComponent<ContentSizeFitter>();
            evCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI evCardTitle = CreateText("EventsTitle", eventsCard.transform, "Events Encountered", 22, FontStyles.Bold, TextAlignmentOptions.Left);
            evCardTitle.color = Color.white;
            evCardTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;
            resultUI.eventsTitleText = evCardTitle;

            GameObject evGrid = CreateUIObject("EventsGrid", eventsCard.transform);
            GridLayoutGroup glg = evGrid.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(435, 46);
            glg.spacing = new Vector2(16, 12);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;

            ContentSizeFitter gridCsf = evGrid.AddComponent<ContentSizeFitter>();
            gridCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            resultUI.eventsContainer = evGrid.transform;

            // --- 7. ACTION BUTTONS ROW ---
            GameObject btnRow = CreateUIObject("ActionButtonsRow", content.transform);
            btnRow.AddComponent<LayoutElement>().preferredHeight = 75;

            HorizontalLayoutGroup btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 30;
            btnHlg.childAlignment = TextAnchor.MiddleCenter;
            btnHlg.childControlWidth = false;
            btnHlg.childControlHeight = false;
            btnHlg.childForceExpandWidth = false;
            btnHlg.childForceExpandHeight = false;

            // Play Again Button
            GameObject playAgainObj = CreateUIObject("PlayAgainButton", btnRow.transform);
            Image paBg = playAgainObj.AddComponent<Image>();
            paBg.sprite = roundBox;
            paBg.type = Image.Type.Sliced;
            paBg.color = new Color32(0x00, 0xC8, 0x53, 0xFF); // Green
            SetPositionAndSize(playAgainObj.GetComponent<RectTransform>(), Vector2.zero, new Vector2(280, 58));

            Button paBtn = playAgainObj.AddComponent<Button>();
            TextMeshProUGUI paTxt = CreateText("Text", playAgainObj.transform, "↺ Play Again", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            paTxt.color = Color.white;
            SetStretch(paTxt.rectTransform);
            resultUI.playAgainButton = paBtn;

            // Main Menu Button
            GameObject menuObj = CreateUIObject("MainMenuButton", btnRow.transform);
            Image menuBg = menuObj.AddComponent<Image>();
            menuBg.sprite = roundBox;
            menuBg.type = Image.Type.Sliced;
            menuBg.color = new Color32(0x4F, 0x46, 0xE5, 0xFF); // Indigo
            SetPositionAndSize(menuObj.GetComponent<RectTransform>(), Vector2.zero, new Vector2(280, 58));

            Button menuBtn = menuObj.AddComponent<Button>();
            TextMeshProUGUI menuTxt = CreateText("Text", menuObj.transform, "🏠 Main Menu", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            menuTxt.color = Color.white;
            SetStretch(menuTxt.rectTransform);
            resultUI.mainMenuButton = menuBtn;

            // --- 8. CELEBRATION MODAL OVERLAY ---
            GameObject celebModal = CreateUIObject("CelebrationModal", root);
            Image celebBg = celebModal.AddComponent<Image>();
            celebBg.color = new Color32(0x0A, 0x0E, 0x1A, 0xDD);
            SetStretch(celebModal.GetComponent<RectTransform>());

            GameObject celebCard = CreateUIObject("Card", celebModal.transform);
            Image cCardBg = celebCard.AddComponent<Image>();
            cCardBg.sprite = roundBox;
            cCardBg.type = Image.Type.Sliced;
            cCardBg.color = new Color32(0x1B, 0x24, 0x47, 0xFF);
            SetPositionAndSize(celebCard.GetComponent<RectTransform>(), Vector2.zero, new Vector2(580, 320));

            TextMeshProUGUI cTitle = CreateText("Title", celebCard.transform, "🎉 Outstanding 30-Day Survival!", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            cTitle.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(cTitle.rectTransform, new Vector2(0, 95), new Vector2(520, 45));
            resultUI.celebrationTitleText = cTitle;

            TextMeshProUGUI cMsg = CreateText("Message", celebCard.transform, "Incredible financial discipline! You survived 30 days and unlocked maximum rewards.", 18, FontStyles.Normal, TextAlignmentOptions.Center);
            cMsg.color = new Color32(0xDC, 0xE7, 0xFA, 0xFF);
            SetPositionAndSize(cMsg.rectTransform, new Vector2(0, 15), new Vector2(500, 90));
            resultUI.celebrationMessageText = cMsg;

            GameObject cBtnObj = CreateUIObject("CloseButton", celebCard.transform);
            Image cBtnBg = cBtnObj.AddComponent<Image>();
            cBtnBg.sprite = roundBox;
            cBtnBg.type = Image.Type.Sliced;
            cBtnBg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            SetPositionAndSize(cBtnObj.GetComponent<RectTransform>(), new Vector2(0, -95), new Vector2(220, 50));

            Button cBtn = cBtnObj.AddComponent<Button>();
            TextMeshProUGUI cBtnTxt = CreateText("Text", cBtnObj.transform, "Awesome!", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            cBtnTxt.color = Color.white;
            SetStretch(cBtnTxt.rectTransform);
            resultUI.celebrationCloseButton = cBtn;
            resultUI.celebrationModal = celebModal;
            celebModal.SetActive(false);

            // --- 9. CHARACTER UNLOCK MODAL OVERLAY ---
            GameObject unlockModal = CreateUIObject("CharacterUnlockModal", root);
            Image unBg = unlockModal.AddComponent<Image>();
            unBg.color = new Color32(0x0A, 0x0E, 0x1A, 0xEE);
            SetStretch(unlockModal.GetComponent<RectTransform>());

            GameObject unCard = CreateUIObject("Card", unlockModal.transform);
            Image unCardBg = unCard.AddComponent<Image>();
            unCardBg.sprite = roundBox;
            unCardBg.type = Image.Type.Sliced;
            unCardBg.color = new Color32(0x22, 0x1C, 0x48, 0xFF);
            SetPositionAndSize(unCard.GetComponent<RectTransform>(), Vector2.zero, new Vector2(560, 400));

            Image unCharImg = CreateImage("UnlockedCharacterImage", unCard.transform, pennySprite, Color.white);
            SetPositionAndSize(unCharImg.rectTransform, new Vector2(0, 85), new Vector2(130, 130));
            resultUI.unlockedCharacterImage = unCharImg;

            TextMeshProUGUI unName = CreateText("Name", unCard.transform, "🎉 New Character Unlocked!", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            unName.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(unName.rectTransform, new Vector2(0, -10), new Vector2(500, 36));
            resultUI.unlockedCharacterNameText = unName;

            TextMeshProUGUI unDesc = CreateText("Desc", unCard.transform, "You have reached the required career score and unlocked a new Ranger!", 17, FontStyles.Normal, TextAlignmentOptions.Center);
            unDesc.color = new Color32(0xC7, 0xC3, 0xE8, 0xFF);
            SetPositionAndSize(unDesc.rectTransform, new Vector2(0, -65), new Vector2(480, 60));
            resultUI.unlockedCharacterDescText = unDesc;

            GameObject unBtnObj = CreateUIObject("ClaimButton", unCard.transform);
            Image unBtnBg = unBtnObj.AddComponent<Image>();
            unBtnBg.sprite = roundBox;
            unBtnBg.type = Image.Type.Sliced;
            unBtnBg.color = new Color32(0xFF, 0x40, 0x81, 0xFF);
            SetPositionAndSize(unBtnObj.GetComponent<RectTransform>(), new Vector2(0, -145), new Vector2(220, 50));

            Button unBtn = unBtnObj.AddComponent<Button>();
            TextMeshProUGUI unBtnTxt = CreateText("Text", unBtnObj.transform, "Claim & Continue", 19, FontStyles.Bold, TextAlignmentOptions.Center);
            unBtnTxt.color = Color.white;
            SetStretch(unBtnTxt.rectTransform);
            resultUI.unlockModalCloseButton = unBtn;
            resultUI.characterUnlockModal = unlockModal;
            unlockModal.SetActive(false);

            string path = $"{SCENE_DIR}/Result.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[RupeeRangerBuilder] Built Result scene successfully at {path}");
            return path;
        }

        #region Mini-Game Scene Builders (Phase 5)

        private static string BuildWordSearchScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color32(0x1B, 0x18, 0x36, 0xFF));
            // EventSystem handled by Town / standalone Awake()

            Canvas canvas = CreateCanvas();
            canvas.sortingOrder = 50;
            Transform root = canvas.transform;
            Sprite roundBox = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box.png");

            Image bg = CreateImage("Background", root, null, new Color32(0x1A, 0x1E, 0x30, 0xFF));
            SetStretch(bg.rectTransform);

            // Top Header Bar
            GameObject headerObj = CreateUIObject("HeaderBar", root);
            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color32(0x12, 0x15, 0x24, 0xFA);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0f, 75f);

            TextMeshProUGUI titleTxt = CreateText("Title", headerObj.transform, "WORD SEARCH PUZZLE", 26, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            titleTxt.color = new Color32(0x00, 0xE5, 0xFF, 0xFF);
            SetPositionAndSize(titleTxt.rectTransform, new Vector2(-420f, 0f), new Vector2(380f, 40f));

            TextMeshProUGUI catTxt = CreateText("CategoryText", headerObj.transform, "Theme: Financial Basics (Easy)", 18, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            catTxt.color = new Color32(0xC0, 0xD4, 0xFA, 0xFF);
            SetPositionAndSize(catTxt.rectTransform, new Vector2(20f, 0f), new Vector2(380f, 32f));

            TextMeshProUGUI progTxt = CreateText("ProgressText", headerObj.transform, "Words Found: 0 / 5", 20, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            progTxt.color = new Color32(0x00, 0xFF, 0x99, 0xFF);
            SetPositionAndSize(progTxt.rectTransform, new Vector2(340f, 0f), new Vector2(250f, 32f));

            // Close Button [X]
            GameObject closeBtnObj = CreateUIObject("CloseButton", headerObj.transform);
            Image closeBtnBg = closeBtnObj.AddComponent<Image>();
            closeBtnBg.sprite = roundBox;
            closeBtnBg.type = Image.Type.Sliced;
            closeBtnBg.color = new Color32(0xFF, 0x47, 0x6E, 0xFF);
            RectTransform closeBtnRt = closeBtnObj.GetComponent<RectTransform>();
            SetPositionAndSize(closeBtnRt, new Vector2(490f, 0f), new Vector2(44f, 44f));
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            TextMeshProUGUI closeTxt = CreateText("CloseTxt", closeBtnObj.transform, "X", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            closeTxt.color = Color.white;
            SetStretch(closeTxt.rectTransform);

            // Left: Grid Card (700x700)
            GameObject gridCardObj = CreateUIObject("GridCard", root);
            Image gridCardBg = gridCardObj.AddComponent<Image>();
            gridCardBg.sprite = roundBox;
            gridCardBg.type = Image.Type.Sliced;
            gridCardBg.color = new Color32(0xFD, 0xFD, 0xF9, 0xFF);
            RectTransform gridCardRt = gridCardObj.GetComponent<RectTransform>();
            SetPositionAndSize(gridCardRt, new Vector2(-220f, -40f), new Vector2(700f, 700f));

            GameObject gridContainerObj = CreateUIObject("GridContainer", gridCardObj.transform);
            RectTransform gridContainerRt = gridContainerObj.GetComponent<RectTransform>();
            SetStretchWithMargin(gridContainerRt, 20f, 20f, 20f, 20f);
            GridLayoutGroup gridLayout = gridContainerObj.AddComponent<GridLayoutGroup>();
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            // Right: Word List Card (380x700)
            GameObject listCardObj = CreateUIObject("WordListCard", root);
            Image listCardBg = listCardObj.AddComponent<Image>();
            listCardBg.sprite = roundBox;
            listCardBg.type = Image.Type.Sliced;
            listCardBg.color = new Color32(0x13, 0x1E, 0x33, 0xF5);
            RectTransform listCardRt = listCardObj.GetComponent<RectTransform>();
            SetPositionAndSize(listCardRt, new Vector2(360f, -40f), new Vector2(380f, 700f));

            TextMeshProUGUI listTitle = CreateText("ListTitle", listCardObj.transform, "HIDDEN WORDS", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            listTitle.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(listTitle.rectTransform, new Vector2(0f, 310f), new Vector2(340f, 36f));

            GameObject wordListContainerObj = CreateUIObject("WordListContainer", listCardObj.transform);
            RectTransform wordListContainerRt = wordListContainerObj.GetComponent<RectTransform>();
            SetPositionAndSize(wordListContainerRt, new Vector2(0f, -20f), new Vector2(340f, 580f));
            VerticalLayoutGroup listVlg = wordListContainerObj.AddComponent<VerticalLayoutGroup>();
            listVlg.childAlignment = TextAnchor.UpperCenter;
            listVlg.spacing = 10f;
            listVlg.childControlWidth = true;
            listVlg.childControlHeight = false;
            listVlg.childForceExpandWidth = true;
            listVlg.childForceExpandHeight = false;

            // Feedback banner & tick
            GameObject feedbackObj = CreateUIObject("FeedbackBanner", root);
            TextMeshProUGUI feedbackTxt = feedbackObj.AddComponent<TextMeshProUGUI>();
            feedbackTxt.text = "Drag across letters to circle a word!";
            feedbackTxt.fontSize = 20;
            feedbackTxt.fontStyle = FontStyles.Bold;
            feedbackTxt.alignment = TextAlignmentOptions.Center;
            feedbackTxt.color = new Color32(0x00, 0xE5, 0xFF, 0xFF);
            SetPositionAndSize(feedbackObj.GetComponent<RectTransform>(), new Vector2(-220f, -420f), new Vector2(700f, 40f));

            GameObject tickObj = CreateUIObject("FeedbackTick", root);
            Image tickImg = tickObj.AddComponent<Image>();
            tickImg.sprite = roundBox;
            tickImg.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetPositionAndSize(tickObj.GetComponent<RectTransform>(), new Vector2(160f, -420f), new Vector2(36f, 36f));
            TextMeshProUGUI tickTxt = CreateText("TickTxt", tickObj.transform, "OK", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            tickTxt.color = Color.white;
            SetStretch(tickTxt.rectTransform);
            tickObj.SetActive(false);

            // Victory Panel
            GameObject victoryPanelObj = CreateUIObject("VictoryPanel", root);
            Image vicOverlay = victoryPanelObj.AddComponent<Image>();
            vicOverlay.color = new Color32(0x00, 0x00, 0x00, 0xD0);
            SetStretch(vicOverlay.rectTransform);

            GameObject vicCard = CreateUIObject("VicCard", victoryPanelObj.transform);
            Image vicCardBg = vicCard.AddComponent<Image>();
            vicCardBg.sprite = roundBox;
            vicCardBg.type = Image.Type.Sliced;
            vicCardBg.color = new Color32(0x13, 0x22, 0x3E, 0xFD);
            SetPositionAndSize(vicCard.GetComponent<RectTransform>(), Vector2.zero, new Vector2(560f, 380f));

            TextMeshProUGUI vicTitle = CreateText("VicTitle", vicCard.transform, "PUZZLE SOLVED!", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            vicTitle.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(vicTitle.rectTransform, new Vector2(0f, 120f), new Vector2(500f, 40f));

            TextMeshProUGUI vicMsg = CreateText("VicMsg", vicCard.transform, "Incredible job!\nYou found all hidden words!\n+50 Score Earned!", 20, FontStyles.Normal, TextAlignmentOptions.Center);
            vicMsg.color = Color.white;
            SetPositionAndSize(vicMsg.rectTransform, new Vector2(0f, 30f), new Vector2(500f, 90f));

            GameObject finBtnObj = CreateUIObject("FinishButton", vicCard.transform);
            Image finBtnBg = finBtnObj.AddComponent<Image>();
            finBtnBg.sprite = roundBox;
            finBtnBg.type = Image.Type.Sliced;
            finBtnBg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            SetPositionAndSize(finBtnObj.GetComponent<RectTransform>(), new Vector2(0f, -100f), new Vector2(260f, 56f));
            Button finBtn = finBtnObj.AddComponent<Button>();
            TextMeshProUGUI finTxt = CreateText("FinTxt", finBtnObj.transform, "Return to Town", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            finTxt.color = Color.white;
            SetStretch(finTxt.rectTransform);

            victoryPanelObj.SetActive(false);

            WordPuzzleMiniGame mg = canvas.gameObject.AddComponent<WordPuzzleMiniGame>();
            mg.titleText = titleTxt;
            mg.categoryText = catTxt;
            mg.progressText = progTxt;
            mg.feedbackText = feedbackTxt;
            mg.feedbackTick = tickImg;
            mg.gridContainer = gridContainerRt;
            mg.wordListContainer = wordListContainerRt;
            mg.victoryPanel = victoryPanelObj;
            mg.victoryMessage = vicMsg;
            mg.finishButton = finBtn;
            mg.closeButton = closeBtn;
            mg.wordListsJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/WordLists.json");

            string path = $"{SCENE_DIR}/MiniGame_WordSearch.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildWordFinderScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color32(0x17, 0x1E, 0x33, 0xFF));
            // EventSystem handled by Town / standalone Awake()

            Canvas canvas = CreateCanvas();
            canvas.sortingOrder = 50;
            Transform root = canvas.transform;
            Sprite roundBox = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box.png");

            Image bg = CreateImage("Background", root, null, new Color32(0x13, 0x1C, 0x30, 0xFF));
            SetStretch(bg.rectTransform);

            // Header Bar
            GameObject headerObj = CreateUIObject("HeaderBar", root);
            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color32(0x0C, 0x12, 0x22, 0xFA);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0f, 75f);

            TextMeshProUGUI titleTxt = CreateText("Title", headerObj.transform, "WORD FINDER", 26, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            titleTxt.color = new Color32(0x00, 0xE5, 0xFF, 0xFF);
            SetPositionAndSize(titleTxt.rectTransform, new Vector2(-420f, 0f), new Vector2(380f, 40f));

            TextMeshProUGUI subTxt = CreateText("SubTitle", headerObj.transform, "Form words using the given letters!", 18, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            subTxt.color = new Color32(0xC0, 0xD4, 0xFA, 0xFF);
            SetPositionAndSize(subTxt.rectTransform, new Vector2(20f, 0f), new Vector2(400f, 32f));

            GameObject closeBtnObj = CreateUIObject("CloseButton", headerObj.transform);
            Image closeBtnBg = closeBtnObj.AddComponent<Image>();
            closeBtnBg.sprite = roundBox;
            closeBtnBg.type = Image.Type.Sliced;
            closeBtnBg.color = new Color32(0xFF, 0x47, 0x6E, 0xFF);
            RectTransform closeBtnRt = closeBtnObj.GetComponent<RectTransform>();
            SetPositionAndSize(closeBtnRt, new Vector2(490f, 0f), new Vector2(44f, 44f));
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            TextMeshProUGUI closeTxt = CreateText("CloseTxt", closeBtnObj.transform, "X", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            closeTxt.color = Color.white;
            SetStretch(closeTxt.rectTransform);

            // Answer Boxes Area
            GameObject answerBoxesCard = CreateUIObject("AnswerBoxesCard", root);
            Image abBg = answerBoxesCard.AddComponent<Image>();
            abBg.sprite = roundBox;
            abBg.type = Image.Type.Sliced;
            abBg.color = new Color32(0x19, 0x25, 0x3F, 0xE0);
            RectTransform abRt = answerBoxesCard.GetComponent<RectTransform>();
            SetPositionAndSize(abRt, new Vector2(0f, 150f), new Vector2(800f, 320f));

            GameObject abContainer = CreateUIObject("BoxesContainer", answerBoxesCard.transform);
            RectTransform abContainerRt = abContainer.GetComponent<RectTransform>();
            SetStretchWithMargin(abContainerRt, 20f, 20f, 20f, 20f);
            VerticalLayoutGroup abVlg = abContainer.AddComponent<VerticalLayoutGroup>();
            abVlg.childAlignment = TextAnchor.MiddleCenter;
            abVlg.spacing = 16f;
            abVlg.childControlWidth = false;
            abVlg.childControlHeight = false;
            abVlg.childForceExpandWidth = false;
            abVlg.childForceExpandHeight = false;

            // Current Word Display Pill
            GameObject currPill = CreateUIObject("CurrentWordPill", root);
            Image currPillBg = currPill.AddComponent<Image>();
            currPillBg.sprite = roundBox;
            currPillBg.type = Image.Type.Sliced;
            currPillBg.color = new Color32(0x02, 0x88, 0xD1, 0xFF);
            SetPositionAndSize(currPill.GetComponent<RectTransform>(), new Vector2(0f, -40f), new Vector2(300f, 56f));

            TextMeshProUGUI currDisplay = CreateText("CurrentWordDisplay", currPill.transform, "", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            currDisplay.color = Color.white;
            SetStretch(currDisplay.rectTransform);

            // Dial Area
            GameObject dialCard = CreateUIObject("DialCard", root);
            Image dialCardBg = dialCard.AddComponent<Image>();
            dialCardBg.sprite = roundBox;
            dialCardBg.type = Image.Type.Sliced;
            dialCardBg.color = new Color32(0xFD, 0xFD, 0xFD, 0xF5);
            RectTransform dialCardRt = dialCard.GetComponent<RectTransform>();
            SetPositionAndSize(dialCardRt, new Vector2(0f, -250f), new Vector2(340f, 320f));

            GameObject letterBtnContainer = CreateUIObject("LetterBtnContainer", dialCard.transform);
            RectTransform letterBtnContainerRt = letterBtnContainer.GetComponent<RectTransform>();
            SetStretch(letterBtnContainerRt);

            // Shuffle Button
            GameObject shufObj = CreateUIObject("ShuffleButton", root);
            Image shufBg = shufObj.AddComponent<Image>();
            shufBg.sprite = roundBox;
            shufBg.type = Image.Type.Sliced;
            shufBg.color = new Color32(0x3B, 0x4B, 0x6B, 0xFF);
            SetPositionAndSize(shufObj.GetComponent<RectTransform>(), new Vector2(-230f, -250f), new Vector2(70f, 70f));
            Button shufBtn = shufObj.AddComponent<Button>();
            TextMeshProUGUI shufTxt = CreateText("ShufTxt", shufObj.transform, "SHUF", 28, FontStyles.Normal, TextAlignmentOptions.Center);
            shufTxt.color = Color.white;
            SetStretch(shufTxt.rectTransform);

            // Clear Button
            GameObject clrObj = CreateUIObject("ClearButton", root);
            Image clrBg = clrObj.AddComponent<Image>();
            clrBg.sprite = roundBox;
            clrBg.type = Image.Type.Sliced;
            clrBg.color = new Color32(0x3B, 0x4B, 0x6B, 0xFF);
            SetPositionAndSize(clrObj.GetComponent<RectTransform>(), new Vector2(230f, -250f), new Vector2(70f, 70f));
            Button clrBtn = clrObj.AddComponent<Button>();
            TextMeshProUGUI clrTxt = CreateText("ClrTxt", clrObj.transform, "DEL", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            clrTxt.color = Color.white;
            SetStretch(clrTxt.rectTransform);

            // Submit Button
            GameObject subObj = CreateUIObject("SubmitButton", root);
            Image subBg = subObj.AddComponent<Image>();
            subBg.sprite = roundBox;
            subBg.type = Image.Type.Sliced;
            subBg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            SetPositionAndSize(subObj.GetComponent<RectTransform>(), new Vector2(0f, -440f), new Vector2(240f, 52f));
            Button subBtn = subObj.AddComponent<Button>();
            TextMeshProUGUI subBtnTxt = CreateText("SubBtnTxt", subObj.transform, "SUBMIT WORD", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            subBtnTxt.color = Color.white;
            SetStretch(subBtnTxt.rectTransform);

            // Feedback banner & tick
            GameObject fbObj = CreateUIObject("FeedbackBanner", root);
            TextMeshProUGUI fbTxt = fbObj.AddComponent<TextMeshProUGUI>();
            fbTxt.text = "Tap letters to build words or type on keyboard!";
            fbTxt.fontSize = 20;
            fbTxt.fontStyle = FontStyles.Bold;
            fbTxt.alignment = TextAlignmentOptions.Center;
            fbTxt.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(fbObj.GetComponent<RectTransform>(), new Vector2(0f, -100f), new Vector2(800f, 36f));

            GameObject tickObj = CreateUIObject("FeedbackTick", root);
            Image tickImg = tickObj.AddComponent<Image>();
            tickImg.sprite = roundBox;
            tickImg.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetPositionAndSize(tickObj.GetComponent<RectTransform>(), new Vector2(280f, -100f), new Vector2(36f, 36f));
            TextMeshProUGUI tickTxt = CreateText("TickTxt", tickObj.transform, "OK", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            tickTxt.color = Color.white;
            SetStretch(tickTxt.rectTransform);
            tickObj.SetActive(false);

            // Victory Panel
            GameObject victoryPanelObj = CreateUIObject("VictoryPanel", root);
            Image vicOverlay = victoryPanelObj.AddComponent<Image>();
            vicOverlay.color = new Color32(0x00, 0x00, 0x00, 0xD0);
            SetStretch(vicOverlay.rectTransform);

            GameObject vicCard = CreateUIObject("VicCard", victoryPanelObj.transform);
            Image vicCardBg = vicCard.AddComponent<Image>();
            vicCardBg.sprite = roundBox;
            vicCardBg.type = Image.Type.Sliced;
            vicCardBg.color = new Color32(0x13, 0x22, 0x3E, 0xFD);
            SetPositionAndSize(vicCard.GetComponent<RectTransform>(), Vector2.zero, new Vector2(560f, 380f));

            TextMeshProUGUI vicTitle = CreateText("VicTitle", vicCard.transform, "ALL WORDS FOUND!", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            vicTitle.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(vicTitle.rectTransform, new Vector2(0f, 120f), new Vector2(500f, 40f));

            TextMeshProUGUI vicMsg = CreateText("VicMsg", vicCard.transform, "Awesome work!\nYou filled every word box!\n+50 Score Earned!", 20, FontStyles.Normal, TextAlignmentOptions.Center);
            vicMsg.color = Color.white;
            SetPositionAndSize(vicMsg.rectTransform, new Vector2(0f, 30f), new Vector2(500f, 90f));

            GameObject finBtnObj = CreateUIObject("FinishButton", vicCard.transform);
            Image finBtnBg = finBtnObj.AddComponent<Image>();
            finBtnBg.sprite = roundBox;
            finBtnBg.type = Image.Type.Sliced;
            finBtnBg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            SetPositionAndSize(finBtnObj.GetComponent<RectTransform>(), new Vector2(0f, -100f), new Vector2(260f, 56f));
            Button finBtn = finBtnObj.AddComponent<Button>();
            TextMeshProUGUI finTxt = CreateText("FinTxt", finBtnObj.transform, "Return to Town", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            finTxt.color = Color.white;
            SetStretch(finTxt.rectTransform);

            victoryPanelObj.SetActive(false);

            WordFinderMiniGame mg = canvas.gameObject.AddComponent<WordFinderMiniGame>();
            mg.titleText = titleTxt;
            mg.subTitleText = subTxt;
            mg.feedbackText = fbTxt;
            mg.feedbackTick = tickImg;
            mg.answerBoxesContainer = abContainerRt;
            mg.letterButtonsContainer = letterBtnContainerRt;
            mg.currentWordDisplay = currDisplay;
            mg.submitButton = subBtn;
            mg.clearButton = clrBtn;
            mg.shuffleButton = shufBtn;
            mg.victoryPanel = victoryPanelObj;
            mg.victoryMessage = vicMsg;
            mg.finishButton = finBtn;
            mg.closeButton = closeBtn;
            mg.wordListsJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/WordLists.json");

            string path = $"{SCENE_DIR}/MiniGame_WordFinder.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildMathSpeedScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color32(0x0C, 0x10, 0x1A, 0xFF));
            // EventSystem handled by Town / standalone Awake()

            Canvas canvas = CreateCanvas();
            canvas.sortingOrder = 50;
            Transform root = canvas.transform;
            Sprite roundBox = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box.png");

            Image bg = CreateImage("Background", root, null, new Color32(0x0E, 0x14, 0x22, 0xFF));
            SetStretch(bg.rectTransform);

            // Score | High Score Cards
            GameObject scoreCardObj = CreateUIObject("ScoreCard", root);
            Image scBg = scoreCardObj.AddComponent<Image>();
            scBg.sprite = roundBox;
            scBg.type = Image.Type.Sliced;
            scBg.color = new Color32(0x18, 0x22, 0x36, 0xFA);
            RectTransform scRt = scoreCardObj.GetComponent<RectTransform>();
            SetPositionAndSize(scRt, new Vector2(-180f, 400f), new Vector2(240f, 110f));

            TextMeshProUGUI scTitle = CreateText("ScoreTitle", scoreCardObj.transform, "Score", 18, FontStyles.Normal, TextAlignmentOptions.Center);
            scTitle.color = new Color32(0x9E, 0xAF, 0xCE, 0xFF);
            SetPositionAndSize(scTitle.rectTransform, new Vector2(0f, 25f), new Vector2(200f, 30f));

            TextMeshProUGUI scoreTxt = CreateText("ScoreVal", scoreCardObj.transform, "0", 38, FontStyles.Bold, TextAlignmentOptions.Center);
            scoreTxt.color = Color.white;
            SetPositionAndSize(scoreTxt.rectTransform, new Vector2(0f, -20f), new Vector2(200f, 50f));

            GameObject highScoreCardObj = CreateUIObject("HighScoreCard", root);
            Image hsBg = highScoreCardObj.AddComponent<Image>();
            hsBg.sprite = roundBox;
            hsBg.type = Image.Type.Sliced;
            hsBg.color = new Color32(0x18, 0x22, 0x36, 0xFA);
            RectTransform hsRt = highScoreCardObj.GetComponent<RectTransform>();
            SetPositionAndSize(hsRt, new Vector2(180f, 400f), new Vector2(240f, 110f));

            TextMeshProUGUI hsTitle = CreateText("HighScoreTitle", highScoreCardObj.transform, "High Score", 18, FontStyles.Normal, TextAlignmentOptions.Center);
            hsTitle.color = new Color32(0x9E, 0xAF, 0xCE, 0xFF);
            SetPositionAndSize(hsTitle.rectTransform, new Vector2(0f, 25f), new Vector2(200f, 30f));

            TextMeshProUGUI hsTxt = CreateText("HighScoreVal", highScoreCardObj.transform, "0", 38, FontStyles.Bold, TextAlignmentOptions.Center);
            hsTxt.color = Color.white;
            SetPositionAndSize(hsTxt.rectTransform, new Vector2(0f, -20f), new Vector2(200f, 50f));

            // Close Button [X]
            GameObject closeBtnObj = CreateUIObject("CloseButton", root);
            Image closeBtnBg = closeBtnObj.AddComponent<Image>();
            closeBtnBg.sprite = roundBox;
            closeBtnBg.type = Image.Type.Sliced;
            closeBtnBg.color = new Color32(0xFF, 0x47, 0x6E, 0xFF);
            RectTransform closeBtnRt = closeBtnObj.GetComponent<RectTransform>();
            SetPositionAndSize(closeBtnRt, new Vector2(490f, 430f), new Vector2(48f, 48f));
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            TextMeshProUGUI closeTxt = CreateText("CloseTxt", closeBtnObj.transform, "X", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            closeTxt.color = Color.white;
            SetStretch(closeTxt.rectTransform);

            // Timer Pill
            GameObject timerObj = CreateUIObject("TimerPill", root);
            Image timerBg = timerObj.AddComponent<Image>();
            timerBg.sprite = roundBox;
            timerBg.type = Image.Type.Sliced;
            timerBg.color = new Color32(0xFF, 0x57, 0x73, 0xFF);
            SetPositionAndSize(timerObj.GetComponent<RectTransform>(), new Vector2(0f, 270f), new Vector2(200f, 65f));

            TextMeshProUGUI timerTxt = CreateText("TimerVal", timerObj.transform, "01:30", 32, FontStyles.Bold, TextAlignmentOptions.Center);
            timerTxt.color = Color.white;
            SetStretch(timerTxt.rectTransform);

            // Progress Text
            TextMeshProUGUI progTxt = CreateText("ProgressText", root, "Correct: 0 / 5", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            progTxt.color = new Color32(0x00, 0xE5, 0xFF, 0xFF);
            SetPositionAndSize(progTxt.rectTransform, new Vector2(0f, 205f), new Vector2(400f, 32f));

            // Question Box
            GameObject questionBox = CreateUIObject("QuestionBox", root);
            Image qBg = questionBox.AddComponent<Image>();
            qBg.sprite = roundBox;
            qBg.type = Image.Type.Sliced;
            qBg.color = new Color32(0x13, 0x1A, 0x2A, 0xFC);
            RectTransform qRt = questionBox.GetComponent<RectTransform>();
            SetPositionAndSize(qRt, new Vector2(0f, 75f), new Vector2(740f, 170f));

            TextMeshProUGUI qTxt = CreateText("QuestionText", questionBox.transform, "2 x 7 =", 56, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            qTxt.color = Color.white;
            SetPositionAndSize(qTxt.rectTransform, new Vector2(-130f, 0f), new Vector2(360f, 100f));

            TMP_InputField ansInput = CreateInputField("AnswerInput", questionBox.transform, new Vector2(160f, 0f), new Vector2(200f, 85f), "?", roundBox);
            ansInput.textComponent.fontSize = 44;
            ansInput.textComponent.alignment = TextAlignmentOptions.Center;
            ansInput.textComponent.fontStyle = FontStyles.Bold;
            ansInput.placeholder.GetComponent<TextMeshProUGUI>().fontSize = 44;
            ansInput.placeholder.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Feedback banner & tick
            GameObject fbObj = CreateUIObject("FeedbackBanner", root);
            TextMeshProUGUI fbTxt = fbObj.AddComponent<TextMeshProUGUI>();
            fbTxt.text = "Type your answer and press Enter or Go!";
            fbTxt.fontSize = 20;
            fbTxt.fontStyle = FontStyles.Bold;
            fbTxt.alignment = TextAlignmentOptions.Center;
            fbTxt.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(fbObj.GetComponent<RectTransform>(), new Vector2(0f, -40f), new Vector2(600f, 36f));

            GameObject tickObj = CreateUIObject("FeedbackTick", root);
            Image tickImg = tickObj.AddComponent<Image>();
            tickImg.sprite = roundBox;
            tickImg.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetPositionAndSize(tickObj.GetComponent<RectTransform>(), new Vector2(260f, -40f), new Vector2(36f, 36f));
            TextMeshProUGUI tickTxt = CreateText("TickTxt", tickObj.transform, "OK", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            tickTxt.color = Color.white;
            SetStretch(tickTxt.rectTransform);
            tickObj.SetActive(false);

            // Number Pad Area
            GameObject numpadArea = CreateUIObject("NumpadArea", root);
            RectTransform npRt = numpadArea.GetComponent<RectTransform>();
            SetPositionAndSize(npRt, new Vector2(0f, -250f), new Vector2(400f, 260f));

            Button[] numpadBtns = new Button[10];
            int[] keyLayout = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };
            for (int k = 0; k < keyLayout.Length; k++)
            {
                int digit = keyLayout[k];
                int row = k / 5;
                int col = k % 5;
                float x = (col - 2) * 75f;
                float y = 50f - row * 75f;

                GameObject numBtnObj = CreateUIObject($"NumBtn_{digit}", numpadArea.transform);
                Image nBg = numBtnObj.AddComponent<Image>();
                nBg.sprite = roundBox;
                nBg.type = Image.Type.Sliced;
                nBg.color = new Color32(0x27, 0x33, 0x4E, 0xFF);
                SetPositionAndSize(numBtnObj.GetComponent<RectTransform>(), new Vector2(x, y), new Vector2(65f, 65f));

                Button nb = numBtnObj.AddComponent<Button>();
                TextMeshProUGUI nbTxt = CreateText("Digit", numBtnObj.transform, digit.ToString(), 28, FontStyles.Bold, TextAlignmentOptions.Center);
                nbTxt.color = Color.white;
                SetStretch(nbTxt.rectTransform);
                numpadBtns[digit] = nb;
            }

            // Backspace button
            GameObject bkObj = CreateUIObject("BackspaceButton", numpadArea.transform);
            Image bkBg = bkObj.AddComponent<Image>();
            bkBg.sprite = roundBox;
            bkBg.type = Image.Type.Sliced;
            bkBg.color = new Color32(0x3E, 0x48, 0x60, 0xFF);
            SetPositionAndSize(bkObj.GetComponent<RectTransform>(), new Vector2(-80f, -100f), new Vector2(140f, 55f));
            Button bkBtn = bkObj.AddComponent<Button>();
            TextMeshProUGUI bktxt = CreateText("BkTxt", bkObj.transform, "CLEAR", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            bktxt.color = Color.white;
            SetStretch(bktxt.rectTransform);

            // Submit button
            GameObject subObj = CreateUIObject("SubmitButton", numpadArea.transform);
            Image subBg = subObj.AddComponent<Image>();
            subBg.sprite = roundBox;
            subBg.type = Image.Type.Sliced;
            subBg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            SetPositionAndSize(subObj.GetComponent<RectTransform>(), new Vector2(80f, -100f), new Vector2(140f, 55f));
            Button subBtn = subObj.AddComponent<Button>();
            TextMeshProUGUI subTxt = CreateText("SubTxt", subObj.transform, "GO", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            subTxt.color = Color.white;
            SetStretch(subTxt.rectTransform);

            // Victory Panel
            GameObject victoryPanelObj = CreateUIObject("VictoryPanel", root);
            Image vicOverlay = victoryPanelObj.AddComponent<Image>();
            vicOverlay.color = new Color32(0x00, 0x00, 0x00, 0xD0);
            SetStretch(vicOverlay.rectTransform);

            GameObject vicCard = CreateUIObject("VicCard", victoryPanelObj.transform);
            Image vicCardBg = vicCard.AddComponent<Image>();
            vicCardBg.sprite = roundBox;
            vicCardBg.type = Image.Type.Sliced;
            vicCardBg.color = new Color32(0x13, 0x22, 0x3E, 0xFD);
            SetPositionAndSize(vicCard.GetComponent<RectTransform>(), Vector2.zero, new Vector2(560f, 380f));

            TextMeshProUGUI vicTitle = CreateText("VicTitle", vicCard.transform, "SPEED MASTER!", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            vicTitle.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(vicTitle.rectTransform, new Vector2(0f, 120f), new Vector2(500f, 40f));

            TextMeshProUGUI vicMsg = CreateText("VicMsg", vicCard.transform, "Fantastic calculations!\nYou completed today's math challenge!\n+50 Score Earned!", 20, FontStyles.Normal, TextAlignmentOptions.Center);
            vicMsg.color = Color.white;
            SetPositionAndSize(vicMsg.rectTransform, new Vector2(0f, 30f), new Vector2(500f, 90f));

            GameObject finBtnObj = CreateUIObject("FinishButton", vicCard.transform);
            Image finBtnBg = finBtnObj.AddComponent<Image>();
            finBtnBg.sprite = roundBox;
            finBtnBg.type = Image.Type.Sliced;
            finBtnBg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            SetPositionAndSize(finBtnObj.GetComponent<RectTransform>(), new Vector2(0f, -100f), new Vector2(260f, 56f));
            Button finBtn = finBtnObj.AddComponent<Button>();
            TextMeshProUGUI finTxt = CreateText("FinTxt", finBtnObj.transform, "Return to Town", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            finTxt.color = Color.white;
            SetStretch(finTxt.rectTransform);

            victoryPanelObj.SetActive(false);

            MathsGameMiniGame mg = canvas.gameObject.AddComponent<MathsGameMiniGame>();
            mg.scoreText = scoreTxt;
            mg.highScoreText = hsTxt;
            mg.timerText = timerTxt;
            mg.progressText = progTxt;
            mg.questionText = qTxt;
            mg.answerInput = ansInput;
            mg.submitButton = subBtn;
            mg.feedbackText = fbTxt;
            mg.feedbackTick = tickImg;
            mg.numpadButtons = numpadBtns;
            mg.numpadBackspaceButton = bkBtn;
            mg.numpadEnterButton = subBtn;
            mg.victoryPanel = victoryPanelObj;
            mg.victoryMessage = vicMsg;
            mg.finishButton = finBtn;
            mg.closeButton = closeBtn;

            string path = $"{SCENE_DIR}/MiniGame_MathSpeed.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildMathPuzzleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color32(0x68, 0x82, 0x1E, 0xFF));
            // EventSystem handled by Town / standalone Awake()

            Canvas canvas = CreateCanvas();
            canvas.sortingOrder = 50;
            Transform root = canvas.transform;
            Sprite roundBox = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box.png");

            Image bg = CreateImage("Background", root, null, new Color32(0x68, 0x82, 0x1E, 0xFF));
            SetStretch(bg.rectTransform);

            // Close Button [X] at top right
            GameObject closeBtnObj = CreateUIObject("CloseButton", root);
            Image closeBtnBg = closeBtnObj.AddComponent<Image>();
            closeBtnBg.sprite = roundBox;
            closeBtnBg.type = Image.Type.Sliced;
            closeBtnBg.color = new Color32(0xFF, 0x47, 0x6E, 0xFF);
            RectTransform closeBtnRt = closeBtnObj.GetComponent<RectTransform>();
            SetPositionAndSize(closeBtnRt, new Vector2(490f, 430f), new Vector2(48f, 48f));
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            TextMeshProUGUI closeTxt = CreateText("CloseTxt", closeBtnObj.transform, "X", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            closeTxt.color = Color.white;
            SetStretch(closeTxt.rectTransform);

            // Navy Frame for 3x3 Grid
            GameObject navyFrame = CreateUIObject("GridNavyFrame", root);
            Image frameBg = navyFrame.AddComponent<Image>();
            frameBg.sprite = roundBox;
            frameBg.type = Image.Type.Sliced;
            frameBg.color = new Color32(0x1B, 0x1B, 0x58, 0xFF);
            RectTransform frameRt = navyFrame.GetComponent<RectTransform>();
            SetPositionAndSize(frameRt, new Vector2(0f, 130f), new Vector2(580f, 580f));

            GameObject gridObj = CreateUIObject("GridContainer", navyFrame.transform);
            RectTransform gridRt = gridObj.GetComponent<RectTransform>();
            SetStretchWithMargin(gridRt, 20f, 20f, 20f, 20f);

            // Create 9 sum card cells
            float cellSize = 165f;
            float gap = 15f;
            float startX = -180f;
            float startY = 180f;

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int index = r * 3 + c;
                    GameObject cellCard = CreateUIObject($"Cell_{index}", gridObj.transform);
                    Image cardBg = cellCard.AddComponent<Image>();
                    cardBg.sprite = roundBox;
                    cardBg.type = Image.Type.Sliced;
                    cardBg.color = new Color32(0xFD, 0xFD, 0xF5, 0xFF);
                    RectTransform cardRt = cellCard.GetComponent<RectTransform>();
                    SetPositionAndSize(cardRt, new Vector2(startX + c * (cellSize + gap), startY - r * (cellSize + gap)), new Vector2(cellSize, cellSize));

                    TextMeshProUGUI topNum = CreateText("TopNumText", cellCard.transform, "2", 28, FontStyles.Bold, TextAlignmentOptions.Right);
                    topNum.color = new Color32(0x1A, 0x1A, 0x1A, 0xFF);
                    SetPositionAndSize(topNum.rectTransform, new Vector2(-15f, 50f), new Vector2(80f, 32f));

                    TextMeshProUGUI opNum = CreateText("OpNumText", cellCard.transform, "+ 5", 28, FontStyles.Bold, TextAlignmentOptions.Right);
                    opNum.color = new Color32(0x1A, 0x1A, 0x1A, 0xFF);
                    SetPositionAndSize(opNum.rectTransform, new Vector2(-15f, 20f), new Vector2(100f, 32f));

                    GameObject lineObj = CreateUIObject("LineSep", cellCard.transform);
                    Image lineImg = lineObj.AddComponent<Image>();
                    lineImg.color = new Color32(0x1A, 0x1A, 0x1A, 0xFF);
                    SetPositionAndSize(lineObj.GetComponent<RectTransform>(), new Vector2(0f, 2f), new Vector2(130f, 3f));

                    GameObject ansBox = CreateUIObject("AnswerBox", cellCard.transform);
                    Image ansBg = ansBox.AddComponent<Image>();
                    ansBg.sprite = roundBox;
                    ansBg.type = Image.Type.Sliced;
                    ansBg.color = new Color32(0xEE, 0xEE, 0xEE, 0xFF);
                    SetPositionAndSize(ansBox.GetComponent<RectTransform>(), new Vector2(0f, -24f), new Vector2(110f, 36f));

                    TextMeshProUGUI ansDisplay = CreateText("AnswerText", ansBox.transform, "", 22, FontStyles.Bold, TextAlignmentOptions.Center);
                    ansDisplay.color = Color.black;
                    SetStretch(ansDisplay.rectTransform);

                    GameObject goBtnObj = CreateUIObject("GoButton", cellCard.transform);
                    Image goBg = goBtnObj.AddComponent<Image>();
                    goBg.sprite = roundBox;
                    goBg.type = Image.Type.Sliced;
                    goBg.color = new Color32(0x5E, 0x94, 0x2C, 0xFF);
                    SetPositionAndSize(goBtnObj.GetComponent<RectTransform>(), new Vector2(0f, -58f), new Vector2(90f, 28f));

                    Button goBtn = goBtnObj.AddComponent<Button>();
                    TextMeshProUGUI goTxt = CreateText("GoText", goBtnObj.transform, "Go", 17, FontStyles.Bold, TextAlignmentOptions.Center);
                    goTxt.color = Color.white;
                    SetStretch(goTxt.rectTransform);

                    GameObject checkObj = CreateUIObject("Checkmark", cellCard.transform);
                    Image checkBg = checkObj.AddComponent<Image>();
                    checkBg.sprite = roundBox;
                    checkBg.color = new Color32(0x4C, 0xAF, 0x50, 0xDD);
                    SetStretch(checkObj.GetComponent<RectTransform>());
                    TextMeshProUGUI checkTxt = CreateText("CheckTxt", checkObj.transform, "OK", 50, FontStyles.Bold, TextAlignmentOptions.Center);
                    checkTxt.color = Color.white;
                    SetStretch(checkTxt.rectTransform);
                    checkObj.SetActive(false);
                }
            }

            // Oval Banner below grid
            GameObject bannerObj = CreateUIObject("InstructionBanner", root);
            Image bannerBg = bannerObj.AddComponent<Image>();
            bannerBg.sprite = roundBox;
            bannerBg.type = Image.Type.Sliced;
            bannerBg.color = new Color32(0x4B, 0x62, 0x12, 0xF5);
            RectTransform bannerRt = bannerObj.GetComponent<RectTransform>();
            SetPositionAndSize(bannerRt, new Vector2(0f, -200f), new Vector2(580f, 56f));

            TextMeshProUGUI bannerTxt = CreateText("BannerText", bannerObj.transform, "Pixel, solve 3 sums to make your mark!", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            bannerTxt.color = Color.white;
            SetStretch(bannerTxt.rectTransform);

            TextMeshProUGUI progTxt = CreateText("ProgressText", root, "Solved: 0 / 3", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            progTxt.color = Color.white;
            SetPositionAndSize(progTxt.rectTransform, new Vector2(0f, -245f), new Vector2(400f, 28f));

            TextMeshProUGUI fbTxt = CreateText("FeedbackText", root, "Tap a card, enter the answer, and press Go!", 18, FontStyles.Normal, TextAlignmentOptions.Center);
            fbTxt.color = new Color32(0xFF, 0xF5, 0x9D, 0xFF);
            SetPositionAndSize(fbTxt.rectTransform, new Vector2(0f, -275f), new Vector2(600f, 28f));

            GameObject tickObj = CreateUIObject("FeedbackTick", root);
            Image tickImg = tickObj.AddComponent<Image>();
            tickImg.sprite = roundBox;
            tickImg.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetPositionAndSize(tickObj.GetComponent<RectTransform>(), new Vector2(240f, -275f), new Vector2(28f, 28f));
            TextMeshProUGUI tickTxt = CreateText("TickTxt", tickObj.transform, "OK", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            tickTxt.color = Color.white;
            SetStretch(tickTxt.rectTransform);
            tickObj.SetActive(false);

            // Number Pad
            GameObject numpadObj = CreateUIObject("NumberPad", root);
            RectTransform npRt = numpadObj.GetComponent<RectTransform>();
            SetPositionAndSize(npRt, new Vector2(0f, -370f), new Vector2(560f, 150f));

            int[] row1 = { 1, 2, 3, 4, 5 };
            int[] row2 = { 6, 7, 8, 9, 0 };

            List<Button> keypadBtns = new List<Button>();

            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 85f;
                int d1 = row1[i];
                GameObject b1Obj = CreateUIObject($"Btn_{d1}", numpadObj.transform);
                Image b1Bg = b1Obj.AddComponent<Image>();
                b1Bg.sprite = roundBox;
                b1Bg.type = Image.Type.Sliced;
                b1Bg.color = Color.white;
                SetPositionAndSize(b1Obj.GetComponent<RectTransform>(), new Vector2(x, 35f), new Vector2(72f, 65f));

                Button b1 = b1Obj.AddComponent<Button>();
                TextMeshProUGUI t1 = CreateText("Txt", b1Obj.transform, d1.ToString(), 32, FontStyles.Bold, TextAlignmentOptions.Center);
                t1.color = new Color32(0x1B, 0x1B, 0x58, 0xFF);
                SetStretch(t1.rectTransform);
                keypadBtns.Add(b1);

                int d2 = row2[i];
                GameObject b2Obj = CreateUIObject($"Btn_{d2}", numpadObj.transform);
                Image b2Bg = b2Obj.AddComponent<Image>();
                b2Bg.sprite = roundBox;
                b2Bg.type = Image.Type.Sliced;
                b2Bg.color = Color.white;
                SetPositionAndSize(b2Obj.GetComponent<RectTransform>(), new Vector2(x, -40f), new Vector2(72f, 65f));

                Button b2 = b2Obj.AddComponent<Button>();
                TextMeshProUGUI t2 = CreateText("Txt", b2Obj.transform, d2.ToString(), 32, FontStyles.Bold, TextAlignmentOptions.Center);
                t2.color = new Color32(0x1B, 0x1B, 0x58, 0xFF);
                SetStretch(t2.rectTransform);
                keypadBtns.Add(b2);
            }

            GameObject bkObj = CreateUIObject("Btn_Bk", numpadObj.transform);
            Image bkBg = bkObj.AddComponent<Image>();
            bkBg.sprite = roundBox;
            bkBg.type = Image.Type.Sliced;
            bkBg.color = new Color32(0xFF, 0xD5, 0x4F, 0xFF);
            SetPositionAndSize(bkObj.GetComponent<RectTransform>(), new Vector2(255f, -2f), new Vector2(70f, 135f));
            Button bkBtn = bkObj.AddComponent<Button>();
            TextMeshProUGUI bkTxt = CreateText("Txt", bkObj.transform, "DEL", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            bkTxt.color = new Color32(0x1B, 0x1B, 0x58, 0xFF);
            SetStretch(bkTxt.rectTransform);

            // Victory Panel
            GameObject victoryPanelObj = CreateUIObject("VictoryPanel", root);
            Image vicOverlay = victoryPanelObj.AddComponent<Image>();
            vicOverlay.color = new Color32(0x00, 0x00, 0x00, 0xD0);
            SetStretch(vicOverlay.rectTransform);

            GameObject vicCard = CreateUIObject("VicCard", victoryPanelObj.transform);
            Image vicCardBg = vicCard.AddComponent<Image>();
            vicCardBg.sprite = roundBox;
            vicCardBg.type = Image.Type.Sliced;
            vicCardBg.color = new Color32(0x1B, 0x1B, 0x58, 0xFD);
            SetPositionAndSize(vicCard.GetComponent<RectTransform>(), Vector2.zero, new Vector2(560f, 380f));

            TextMeshProUGUI vicTitle = CreateText("VicTitle", vicCard.transform, "YOU MADE YOUR MARK!", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            vicTitle.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(vicTitle.rectTransform, new Vector2(0f, 120f), new Vector2(500f, 40f));

            TextMeshProUGUI vicMsg = CreateText("VicMsg", vicCard.transform, "Outstanding!\nYou solved the required math grid sums!\n+50 Score Earned!", 20, FontStyles.Normal, TextAlignmentOptions.Center);
            vicMsg.color = Color.white;
            SetPositionAndSize(vicMsg.rectTransform, new Vector2(0f, 30f), new Vector2(500f, 90f));

            GameObject finBtnObj = CreateUIObject("FinishButton", vicCard.transform);
            Image finBtnBg = finBtnObj.AddComponent<Image>();
            finBtnBg.sprite = roundBox;
            finBtnBg.type = Image.Type.Sliced;
            finBtnBg.color = new Color32(0x5E, 0x94, 0x2C, 0xFF);
            SetPositionAndSize(finBtnObj.GetComponent<RectTransform>(), new Vector2(0f, -100f), new Vector2(260f, 56f));
            Button finBtn = finBtnObj.AddComponent<Button>();
            TextMeshProUGUI finTxt = CreateText("FinTxt", finBtnObj.transform, "Return to Town", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            finTxt.color = Color.white;
            SetStretch(finTxt.rectTransform);

            victoryPanelObj.SetActive(false);

            MathsPuzzleMiniGame mg = canvas.gameObject.AddComponent<MathsPuzzleMiniGame>();
            mg.gridContainer = gridRt;
            mg.instructionBannerText = bannerTxt;
            mg.progressText = progTxt;
            mg.feedbackText = fbTxt;
            mg.feedbackTick = tickImg;
            mg.victoryPanel = victoryPanelObj;
            mg.victoryMessage = vicMsg;
            mg.finishButton = finBtn;
            mg.closeButton = closeBtn;

            mg.keypadButtons = keypadBtns.ToArray();
            mg.keypadBackspaceButton = bkBtn;

            string path = $"{SCENE_DIR}/MiniGame_MathPuzzle.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildMemoryMatchScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color32(0x0C, 0x14, 0x24, 0xFF));

            Canvas canvas = CreateCanvas();
            canvas.sortingOrder = 50;
            Transform root = canvas.transform;
            Sprite roundBox = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box.png");

            Image bg = CreateImage("Background", root, null, new Color32(0x0A, 0x11, 0x20, 0xFF));
            SetStretch(bg.rectTransform);

            // Header Bar
            GameObject headerObj = CreateUIObject("HeaderBar", root);
            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color32(0x0F, 0x1B, 0x30, 0xFA);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0f, 75f);

            TextMeshProUGUI titleTxt = CreateText("Title", headerObj.transform, "FINANCIAL MEMORY MATCH", 24, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            titleTxt.color = new Color32(0xFF, 0x00, 0x7F, 0xFF);
            SetPositionAndSize(titleTxt.rectTransform, new Vector2(-420f, 0f), new Vector2(380f, 40f));

            TextMeshProUGUI subTitleTxt = CreateText("SubTitle", headerObj.transform, "Flip cards to match financial pairs!", 17, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            subTitleTxt.color = new Color32(0x90, 0xCA, 0xF9, 0xFF);
            SetPositionAndSize(subTitleTxt.rectTransform, new Vector2(20f, 0f), new Vector2(380f, 32f));

            TextMeshProUGUI statsTxt = CreateText("StatsText", headerObj.transform, "Pairs: 0/6 | Moves: 0 | Time: 90s", 18, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            statsTxt.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetPositionAndSize(statsTxt.rectTransform, new Vector2(340f, 0f), new Vector2(300f, 32f));

            // Close Button [X]
            GameObject closeBtnObj = CreateUIObject("CloseButton", headerObj.transform);
            Image closeBtnBg = closeBtnObj.AddComponent<Image>();
            closeBtnBg.sprite = roundBox;
            closeBtnBg.type = Image.Type.Sliced;
            closeBtnBg.color = new Color32(0xFF, 0x47, 0x6E, 0xFF);
            RectTransform closeBtnRt = closeBtnObj.GetComponent<RectTransform>();
            SetPositionAndSize(closeBtnRt, new Vector2(480f, 0f), new Vector2(40f, 40f));
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            TextMeshProUGUI closeTxt = CreateText("CloseTxt", closeBtnObj.transform, "X", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            closeTxt.color = Color.white;
            SetStretch(closeTxt.rectTransform);

            // Feedback Banner
            GameObject fbObj = CreateUIObject("FeedbackBanner", root);
            TextMeshProUGUI fbTxt = fbObj.AddComponent<TextMeshProUGUI>();
            fbTxt.text = "Click any card to flip it face up!";
            fbTxt.fontSize = 20;
            fbTxt.fontStyle = FontStyles.Bold;
            fbTxt.alignment = TextAlignmentOptions.Center;
            fbTxt.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(fbObj.GetComponent<RectTransform>(), new Vector2(0f, 220f), new Vector2(800f, 36f));

            // Cards Grid Container Card
            GameObject gridCard = CreateUIObject("GridCard", root);
            Image gridCardBg = gridCard.AddComponent<Image>();
            gridCardBg.sprite = roundBox;
            gridCardBg.type = Image.Type.Sliced;
            gridCardBg.color = new Color32(0x0E, 0x18, 0x2A, 0xF5);
            RectTransform gridCardRt = gridCard.GetComponent<RectTransform>();
            SetPositionAndSize(gridCardRt, new Vector2(0f, -20f), new Vector2(560f, 460f));

            GameObject gridContainer = CreateUIObject("CardsGridContainer", gridCard.transform);
            RectTransform gridContainerRt = gridContainer.GetComponent<RectTransform>();
            SetStretch(gridContainerRt);
            gridContainerRt.offsetMin = new Vector2(15f, 15f);
            gridContainerRt.offsetMax = new Vector2(-15f, -15f);

            // Victory Panel
            GameObject victoryPanelObj = CreateUIObject("VictoryPanel", root);
            Image vicOverlay = victoryPanelObj.AddComponent<Image>();
            vicOverlay.color = new Color32(0x00, 0x00, 0x00, 0xD0);
            SetStretch(vicOverlay.rectTransform);

            GameObject vicCard = CreateUIObject("VicCard", victoryPanelObj.transform);
            Image vicCardBg = vicCard.AddComponent<Image>();
            vicCardBg.sprite = roundBox;
            vicCardBg.type = Image.Type.Sliced;
            vicCardBg.color = new Color32(0x13, 0x22, 0x3E, 0xFD);
            SetPositionAndSize(vicCard.GetComponent<RectTransform>(), Vector2.zero, new Vector2(560f, 380f));

            TextMeshProUGUI vicTitle = CreateText("VicTitle", vicCard.transform, "PERFECT MEMORY!", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            vicTitle.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(vicTitle.rectTransform, new Vector2(0f, 120f), new Vector2(500f, 40f));

            TextMeshProUGUI vicMsg = CreateText("VicMsg", vicCard.transform, "Awesome work!\nYou matched all financial pairs!\n+50 Score Earned!", 20, FontStyles.Normal, TextAlignmentOptions.Center);
            vicMsg.color = Color.white;
            SetPositionAndSize(vicMsg.rectTransform, new Vector2(0f, 30f), new Vector2(500f, 90f));

            GameObject finBtnObj = CreateUIObject("FinishButton", vicCard.transform);
            Image finBtnBg = finBtnObj.AddComponent<Image>();
            finBtnBg.sprite = roundBox;
            finBtnBg.type = Image.Type.Sliced;
            finBtnBg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            SetPositionAndSize(finBtnObj.GetComponent<RectTransform>(), new Vector2(0f, -100f), new Vector2(260f, 56f));
            Button finBtn = finBtnObj.AddComponent<Button>();
            TextMeshProUGUI finTxt = CreateText("FinTxt", finBtnObj.transform, "Return to Town", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            finTxt.color = Color.white;
            SetStretch(finTxt.rectTransform);

            victoryPanelObj.SetActive(false);

            MemoryMatchMiniGame mg = canvas.gameObject.AddComponent<MemoryMatchMiniGame>();
            mg.titleText = titleTxt;
            mg.subTitleText = subTitleTxt;
            mg.statsText = statsTxt;
            mg.feedbackText = fbTxt;
            mg.cardsGridContainer = gridContainerRt;
            mg.victoryPanel = victoryPanelObj;
            mg.victoryMessage = vicMsg;
            mg.finishButton = finBtn;
            mg.closeButton = closeBtn;

            string path = $"{SCENE_DIR}/MiniGame_MemoryMatch.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildQuizMasterScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color32(0x0F, 0x17, 0x2A, 0xFF));

            Canvas canvas = CreateCanvas();
            canvas.sortingOrder = 50;
            Transform root = canvas.transform;
            Sprite roundBox = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/rounded_box.png");

            Image bg = CreateImage("Background", root, null, new Color32(0x0B, 0x13, 0x2B, 0xFF));
            SetStretch(bg.rectTransform);

            // Header Bar
            GameObject headerObj = CreateUIObject("HeaderBar", root);
            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color32(0x13, 0x1E, 0x36, 0xFA);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0f, 75f);

            TextMeshProUGUI titleTxt = CreateText("Title", headerObj.transform, "FINANCIAL QUIZ MASTER", 24, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            titleTxt.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(titleTxt.rectTransform, new Vector2(-420f, 0f), new Vector2(380f, 40f));

            TextMeshProUGUI catTxt = CreateText("CategoryText", headerObj.transform, "Category: Saving Basics", 17, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            catTxt.color = new Color32(0xC0, 0xD4, 0xFA, 0xFF);
            SetPositionAndSize(catTxt.rectTransform, new Vector2(20f, 0f), new Vector2(380f, 32f));

            TextMeshProUGUI progTxt = CreateText("ProgressText", headerObj.transform, "Question 1 of 4 | Score: 0/3", 18, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            progTxt.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            SetPositionAndSize(progTxt.rectTransform, new Vector2(340f, 0f), new Vector2(300f, 32f));

            // Close Button [X]
            GameObject closeBtnObj = CreateUIObject("CloseButton", headerObj.transform);
            Image closeBtnBg = closeBtnObj.AddComponent<Image>();
            closeBtnBg.sprite = roundBox;
            closeBtnBg.type = Image.Type.Sliced;
            closeBtnBg.color = new Color32(0xFF, 0x47, 0x6E, 0xFF);
            RectTransform closeBtnRt = closeBtnObj.GetComponent<RectTransform>();
            SetPositionAndSize(closeBtnRt, new Vector2(480f, 0f), new Vector2(40f, 40f));
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            TextMeshProUGUI closeTxt = CreateText("CloseTxt", closeBtnObj.transform, "X", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            closeTxt.color = Color.white;
            SetStretch(closeTxt.rectTransform);

            // Question Card
            GameObject qCard = CreateUIObject("QuestionCard", root);
            Image qCardBg = qCard.AddComponent<Image>();
            qCardBg.sprite = roundBox;
            qCardBg.type = Image.Type.Sliced;
            qCardBg.color = new Color32(0x15, 0x22, 0x3D, 0xF5);
            RectTransform qCardRt = qCard.GetComponent<RectTransform>();
            SetPositionAndSize(qCardRt, new Vector2(0f, 130f), new Vector2(740f, 140f));

            TextMeshProUGUI qTxt = CreateText("QuestionText", qCard.transform, "Loading financial question...", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            qTxt.color = Color.white;
            SetStretch(qTxt.rectTransform);
            qTxt.rectTransform.offsetMin = new Vector2(25f, 15f);
            qTxt.rectTransform.offsetMax = new Vector2(-25f, -15f);

            // Option Buttons (4 options)
            List<Button> optBtns = new List<Button>();
            List<TMP_Text> optTexts = new List<TMP_Text>();
            float optStartY = 20f;
            float optGap = 62f;

            for (int i = 0; i < 4; i++)
            {
                GameObject optObj = CreateUIObject($"Option_{i}", root);
                Image optBg = optObj.AddComponent<Image>();
                optBg.sprite = roundBox;
                optBg.type = Image.Type.Sliced;
                optBg.color = new Color32(0x13, 0x38, 0x6E, 0xFF);
                RectTransform optRt = optObj.GetComponent<RectTransform>();
                SetPositionAndSize(optRt, new Vector2(0f, optStartY - i * optGap), new Vector2(700f, 52f));

                Button optBtn = optObj.AddComponent<Button>();
                TextMeshProUGUI optLabel = CreateText("Label", optObj.transform, $"Option {i + 1}", 18, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
                optLabel.color = Color.white;
                SetStretch(optLabel.rectTransform);
                optLabel.rectTransform.offsetMin = new Vector2(25f, 0f);
                optLabel.rectTransform.offsetMax = new Vector2(-25f, 0f);

                optBtns.Add(optBtn);
                optTexts.Add(optLabel);
            }

            // Feedback Banner
            GameObject fbObj = CreateUIObject("FeedbackBanner", root);
            TextMeshProUGUI fbTxt = fbObj.AddComponent<TextMeshProUGUI>();
            fbTxt.text = "Select the best answer:";
            fbTxt.fontSize = 18;
            fbTxt.fontStyle = FontStyles.Bold;
            fbTxt.alignment = TextAlignmentOptions.Center;
            fbTxt.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(fbObj.GetComponent<RectTransform>(), new Vector2(0f, -240f), new Vector2(740f, 40f));

            // Victory Panel
            GameObject victoryPanelObj = CreateUIObject("VictoryPanel", root);
            Image vicOverlay = victoryPanelObj.AddComponent<Image>();
            vicOverlay.color = new Color32(0x00, 0x00, 0x00, 0xD0);
            SetStretch(vicOverlay.rectTransform);

            GameObject vicCard = CreateUIObject("VicCard", victoryPanelObj.transform);
            Image vicCardBg = vicCard.AddComponent<Image>();
            vicCardBg.sprite = roundBox;
            vicCardBg.type = Image.Type.Sliced;
            vicCardBg.color = new Color32(0x13, 0x22, 0x3E, 0xFD);
            SetPositionAndSize(vicCard.GetComponent<RectTransform>(), Vector2.zero, new Vector2(560f, 380f));

            TextMeshProUGUI vicTitle = CreateText("VicTitle", vicCard.transform, "QUIZ MASTER CHAMPION!", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            vicTitle.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(vicTitle.rectTransform, new Vector2(0f, 120f), new Vector2(500f, 40f));

            TextMeshProUGUI vicMsg = CreateText("VicMsg", vicCard.transform, "Awesome work!\nYou mastered the Library Quiz!\n+50 Score Earned!", 20, FontStyles.Normal, TextAlignmentOptions.Center);
            vicMsg.color = Color.white;
            SetPositionAndSize(vicMsg.rectTransform, new Vector2(0f, 30f), new Vector2(500f, 90f));

            GameObject finBtnObj = CreateUIObject("FinishButton", vicCard.transform);
            Image finBtnBg = finBtnObj.AddComponent<Image>();
            finBtnBg.sprite = roundBox;
            finBtnBg.type = Image.Type.Sliced;
            finBtnBg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            SetPositionAndSize(finBtnObj.GetComponent<RectTransform>(), new Vector2(0f, -100f), new Vector2(260f, 56f));
            Button finBtn = finBtnObj.AddComponent<Button>();
            TextMeshProUGUI finTxt = CreateText("FinTxt", finBtnObj.transform, "Return to Town", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            finTxt.color = Color.white;
            SetStretch(finTxt.rectTransform);

            victoryPanelObj.SetActive(false);

            QuizMasterMiniGame mg = canvas.gameObject.AddComponent<QuizMasterMiniGame>();
            mg.titleText = titleTxt;
            mg.categoryText = catTxt;
            mg.questionText = qTxt;
            mg.progressText = progTxt;
            mg.feedbackText = fbTxt;
            mg.optionButtons = optBtns.ToArray();
            mg.optionTexts = optTexts.ToArray();
            mg.victoryPanel = victoryPanelObj;
            mg.victoryMessage = vicMsg;
            mg.finishButton = finBtn;
            mg.closeButton = closeBtn;

            string path = $"{SCENE_DIR}/MiniGame_QuizMaster.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        #endregion

        private static void UpdateBuildSettings(string[] scenePaths)
        {
            var scenes = new EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; i++)
            {
                scenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            }
            EditorBuildSettings.scenes = scenes;
        }

        #endregion

        #region UI Creation Helpers

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject obj = CreateUIObject(name, parent);
            Image img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string content,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions align)
        {
            GameObject obj = CreateUIObject(name, parent);
            TextMeshProUGUI txt = obj.AddComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.raycastTarget = false;
            return txt;
        }

        private static TMP_InputField CreateInputField(
            string name,
            Transform parent,
            Vector2 pos,
            Vector2 size,
            string placeholderText,
            Sprite roundBox)
        {
            GameObject inputObj = CreateUIObject(name, parent);
            Image bg = inputObj.AddComponent<Image>();
            bg.sprite = roundBox;
            bg.type = Image.Type.Sliced;
            bg.color = new Color32(0x12, 0x11, 0x2C, 0xFF);
            SetPositionAndSize(inputObj.GetComponent<RectTransform>(), pos, size);

            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();

            GameObject textArea = CreateUIObject("Text Area", inputObj.transform);
            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            SetStretchWithMargin(textAreaRect, 18, 6, 18, 6);
            textArea.AddComponent<RectMask2D>();

            GameObject placeholderObj = CreateUIObject("Placeholder", textArea.transform);
            RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
            SetStretch(placeholderRect);
            TextMeshProUGUI placeholderComp = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderComp.text = placeholderText;
            placeholderComp.fontSize = 20;
            placeholderComp.fontStyle = FontStyles.Normal;
            placeholderComp.color = new Color(1f, 1f, 1f, 0.35f);
            placeholderComp.alignment = TextAlignmentOptions.MidlineLeft;

            GameObject textObj = CreateUIObject("Text", textArea.transform);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            SetStretch(textRect);
            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
            textComp.fontSize = 20;
            textComp.fontStyle = FontStyles.Normal;
            textComp.color = Color.white;
            textComp.alignment = TextAlignmentOptions.MidlineLeft;

            inputField.textViewport = textAreaRect;
            inputField.placeholder = placeholderComp;
            inputField.textComponent = textComp;
            inputField.lineType = TMP_InputField.LineType.SingleLine;

            return inputField;
        }

        private static Slider CreateSlider(
            string name,
            Transform parent,
            Vector2 pos,
            Vector2 size,
            Sprite trackSprite,
            Sprite knobSprite,
            Color32 trackColor,
            Color32 fillColor,
            Color32 knobColor)
        {
            GameObject sliderObj = CreateUIObject(name, parent);
            SetPositionAndSize(sliderObj.GetComponent<RectTransform>(), pos, size);
            Slider slider = sliderObj.AddComponent<Slider>();

            // 1. Background Track
            GameObject bgObj = CreateUIObject("Background", sliderObj.transform);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = trackSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = trackColor;
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(0f, 8f);

            // 2. Fill Area
            GameObject fillAreaObj = CreateUIObject("Fill Area", sliderObj.transform);
            RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRect.anchoredPosition = Vector2.zero;
            fillAreaRect.sizeDelta = new Vector2(-20f, 8f);

            GameObject fillObj = CreateUIObject("Fill", fillAreaObj.transform);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = trackSprite;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = fillColor;
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            // 3. Handle Slide Area
            GameObject handleAreaObj = CreateUIObject("Handle Slide Area", sliderObj.transform);
            RectTransform handleAreaRect = handleAreaObj.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.pivot = new Vector2(0.5f, 0.5f);
            handleAreaRect.offsetMin = new Vector2(14f, 0f);
            handleAreaRect.offsetMax = new Vector2(-14f, 0f);

            GameObject handleObj = CreateUIObject("Handle", handleAreaObj.transform);
            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.sprite = knobSprite;
            handleImg.color = knobColor;
            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(26f, 26f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        private static void CreateCamera(Color bgColor)
        {
            GameObject camObj = new GameObject("Main Camera", typeof(Camera));
            Camera cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bgColor;
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.tag = "MainCamera";
        }

        private static void CreateEventSystem()
        {
            GameObject esObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            
            var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                esObj.AddComponent(inputModuleType);
            }
            else
            {
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void EnsureGameManager()
        {
            GameObject gm = new GameObject("GameManager", typeof(GameManager));
        }

        private static void SetPositionAndSize(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetStretchWithMargin(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        #endregion
    }
}
