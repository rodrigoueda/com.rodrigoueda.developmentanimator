using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Text.RegularExpressions;
using UnityEditor.Animations;
using System.Collections.Generic;

namespace DevelopmentAnimator
{
    public class MainWindow : EditorWindow
    {
        public enum SelectedController
        {
            Original,
            Development
        }

        private const string FOLDER =
            "Assets/" + Constants.PACKAGES_SETTINGS_FOLDER + "/" + Constants.SETTINGS_FOLDER;
        private const string ANIMATOR_FOLDER = FOLDER + "/" + Constants.ANIMATORS_FOLDER;

        private const string SKIN_TOOLBAR = "Toolbar";
        private const string SKIN_BUTTON = "ToolbarButton";
        private const float SWITCH_BUTTON_WIDTH = 120f;
        private const string ORIGINAL_CONTROLLER_ICON = "sv_icon_dot11_sml";
        private const string DEVELOPMENT_CONTROLLER_ICON = "sv_icon_dot12_sml";

        private const string SKIN_SEARCH = "ToolbarSeachTextField";
        private const string SKIN_CANCELBUTTON = "ToolbarSeachCancelButton";
        private const string SELECT_ICON = "Animation.FilterBySelection";

        private const string ADD_CLIP_ICON = "CreateAddNew";
        private const string REMOVE_CLIP_ICON = "Toolbar Minus";
        private const string DELETE_CLIP_ICON = "TreeEditor.Trash";
        private const string SYNC_CLIP_ICON = "RotateTool";

        private const string SYNCED_CLIP_ICON = "sv_icon_dot3_sml";
        private const string NOT_SYNCED_CLIP_ICON = "sv_icon_dot0_sml";
        private const string NOT_SYNCED_DEVCLIP_ICON = "Warning";

        private static readonly Vector2 _minSize = new Vector2(200, 150);
        private static readonly Rect _startRect = new Rect(50, 50, 250, 250);

        private DevelopmentAnimatorObject.DevelopmentAnimatorItem _developmentAnimatorItem;
        private static DevelopmentAnimatorObject _developmentAnimatorObject;

        private static AnimationClip[] _originalAnimatorClips;
        private static AnimationClip[] _developmentAnimatorClips;

        private static bool _isAnimator = false;
        private static SelectedController _selectedController;

        private AnimatorController _devAnimatorController;
        private AnimatorController _ogAnimatorController;

        private string _ogSearchText = "";
        private string _devSearchText = "";

        private Vector2 _scrollPos = Vector2.zero;

        [MenuItem(Constants.WINDOW_PATH)]
        private static void ShowWindow()
        {
            MainWindow window = CreateInstance<MainWindow>();

            window.titleContent = new GUIContent(
                Constants.ASSET_NAME,
                EditorGUIUtility.FindTexture(Constants.ASSET_ICON)
            );

            window.minSize = _minSize;

            EditorPrefs.SetFloat("position.x", _startRect.x);
            EditorPrefs.SetFloat("position.y", _startRect.y);
            EditorPrefs.SetFloat("position.width", _startRect.width);
            EditorPrefs.SetFloat("position.height", _startRect.height);

            window.Show();
        }

        private void LoadResources()
        {
            GameObject selection = Selection.activeGameObject;

            _developmentAnimatorObject = DevelopmentAnimatorObject.Load();

            _developmentAnimatorItem =
                _developmentAnimatorObject.GetSelectedAnimator(selection);

            if (_developmentAnimatorItem == null)
            {
                _isAnimator = false;
                return;
            }

            _isAnimator = true;

            int animatorInstanceID =
                selection.GetComponent<Animator>().runtimeAnimatorController.GetInstanceID();

            int ogInstanceID = _developmentAnimatorItem.originalController.GetInstanceID();

            _selectedController =
                (ogInstanceID == animatorInstanceID) ?
                    SelectedController.Original : SelectedController.Development;

            Assert.IsNotNull(_developmentAnimatorItem.originalController);

            _originalAnimatorClips = _developmentAnimatorItem.originalController.animationClips;

            _ogAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                AssetDatabase.GetAssetPath(
                    _developmentAnimatorItem.originalController.GetInstanceID()
                )
            );

            if (_developmentAnimatorItem.developmentController != null)
            {
                _developmentAnimatorClips =
                    _developmentAnimatorItem.developmentController.animationClips;

                _devAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    AssetDatabase.GetAssetPath(
                        _developmentAnimatorItem.developmentController.GetInstanceID()
                    )
                );
            }
        }

        private static void CheckAnimatorsFolder()
        {
            if (!AssetDatabase.IsValidFolder(ANIMATOR_FOLDER))
            {
                AssetDatabase.CreateFolder(FOLDER, Constants.ANIMATORS_FOLDER);
            }
        }

        private void CreateDevelopmentController(RuntimeAnimatorController ogController)
        {
            CheckAnimatorsFolder();

            string ogPath = AssetDatabase.GetAssetPath(ogController.GetInstanceID());
            string ogName = Regex.Replace(ogPath, @".*[\\\/](.*)$", "$1");

            string fullPath = AssetDatabase.GenerateUniqueAssetPath(ANIMATOR_FOLDER + "/" + ogName);

            _developmentAnimatorItem.developmentController =
                UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(fullPath);

            _devAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                AssetDatabase.GetAssetPath(
                    _developmentAnimatorItem.developmentController.GetInstanceID()
                )
            );

            _developmentAnimatorObject.SaveDevelopmentAnimatorItem(_developmentAnimatorItem);
        }

        private void SwitchControllers()
        {
            RuntimeAnimatorController controller;
            if (_selectedController == SelectedController.Original)
            {
                controller = _developmentAnimatorItem.developmentController;
                _selectedController = SelectedController.Development;
            }
            else
            {
                controller = _developmentAnimatorItem.originalController;
                _selectedController = SelectedController.Original;
            }

            GameObject selection = Selection.activeGameObject;

            selection.GetComponent<Animator>().runtimeAnimatorController = controller;

            LoadResources();
        }

        private void DrawSearchBar()
        {
            bool isOriginal = (_selectedController == SelectedController.Original);

            GUILayout.BeginHorizontal(SKIN_TOOLBAR);

            if (isOriginal)
            {
                _ogSearchText = GUILayout.TextField(_ogSearchText, SKIN_SEARCH);
            }
            else
            {
                _devSearchText = GUILayout.TextField(_devSearchText, SKIN_SEARCH);
            }
            if (GUILayout.Button("", SKIN_CANCELBUTTON) || Event.current.keyCode == KeyCode.Escape)
            {
                if (isOriginal)
                {
                    _ogSearchText = "";
                }
                else
                {
                    _devSearchText = "";
                }
                GUI.FocusControl(null);
                Repaint();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawHeaderToolbar()
        {
            GUILayout.BeginHorizontal(SKIN_TOOLBAR);
            GUIContent labelContent;
            RuntimeAnimatorController controller;

            if (_selectedController == SelectedController.Original)
            {
                controller = _developmentAnimatorItem.originalController;
                labelContent = new GUIContent()
                {
                    text = "Main Controller",
                    image = EditorGUIUtility.FindTexture(ORIGINAL_CONTROLLER_ICON)
                };
            }
            else
            {
                controller = _developmentAnimatorItem.developmentController;
                labelContent = new GUIContent()
                {
                    text = "Development Controller",
                    image = EditorGUIUtility.FindTexture(DEVELOPMENT_CONTROLLER_ICON)
                };
            }

            GUILayout.Label(labelContent);
            GUIContent btnContent = new GUIContent("Switch Controllers");

            if (GUILayout.Button(new GUIContent()
            {
                image = EditorGUIUtility.FindTexture(SELECT_ICON)
            }, SKIN_BUTTON, GUILayout.MaxWidth(30f)))
            {
                EditorGUIUtility.PingObject(controller);
            }

            if (GUILayout.Button(btnContent, SKIN_BUTTON, GUILayout.MaxWidth(SWITCH_BUTTON_WIDTH)))
            {
                SwitchControllers();
            }
            GUILayout.EndHorizontal();
        }

        private bool CheckExists(AnimationClip clip, AnimationClip[] clipList)
        {
            for (int i = 0; i < clipList.Length; i++)
            {
                if (clipList[i].GetInstanceID() == clip.GetInstanceID())
                {
                    return true;
                }
            }
            return false;
        }

        private void ReplaceLayer(AnimatorController controller, AnimationClip[] clipList = null)
        {
            controller.RemoveLayer(0);
            controller.AddLayer("Base Layer");

            if (clipList != null)
            {
                for (int i = 0; i < clipList.Length; i++)
                {
                    controller.AddMotion(clipList[i]);
                }
            }
        }

        private AnimationClip[] RemoveMotion(AnimationClip clip)
        {
            List<AnimationClip> keepList = new List<AnimationClip>();

            for (int i = 0; i < _developmentAnimatorClips.Length; i++)
            {
                if (_developmentAnimatorClips[i].GetInstanceID() != clip.GetInstanceID())
                {
                    keepList.Add(_developmentAnimatorClips[i]);
                }
            }

            return keepList.ToArray();
        }

        private void DrawClipsList()
        {
            bool isOriginal = (_selectedController == SelectedController.Original);

            AnimationClip[] disabledList;
            AnimationClip[] enabledList;

            if (isOriginal)
            {
                enabledList = _originalAnimatorClips;
                disabledList = _developmentAnimatorClips;
            }
            else
            {
                disabledList = _originalAnimatorClips;
                enabledList = _developmentAnimatorClips;
            }

            for (int i = 0; i < enabledList.Length; i++)
            {
                if (isOriginal)
                {
                    if (enabledList[i]
                        .name.IndexOf(_ogSearchText, System.StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }
                else
                {
                    if (enabledList[i]
                        .name.IndexOf(_devSearchText, System.StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }

                bool exists = CheckExists(enabledList[i], disabledList);

                GUIContent labelContent = new GUIContent()
                {
                    text = enabledList[i].name
                };

                if (exists)
                {
                    labelContent.image = EditorGUIUtility.FindTexture(SYNCED_CLIP_ICON);
                }
                else
                {
                    if (isOriginal)
                    {
                        labelContent.image = EditorGUIUtility.FindTexture(NOT_SYNCED_CLIP_ICON);
                    }
                    else
                    {
                        labelContent.image = EditorGUIUtility.FindTexture(NOT_SYNCED_DEVCLIP_ICON);
                    }
                }

                GUILayout.BeginHorizontal("Box");
                GUILayout.Label(labelContent);

                if (GUILayout.Button(new GUIContent()
                {
                    image = EditorGUIUtility.FindTexture(SELECT_ICON)
                }, GUILayout.MaxWidth(30f)))
                {
                    EditorGUIUtility.PingObject(enabledList[i]);
                }

                GUIContent btnContent = new GUIContent();
                if (isOriginal)
                {
                    if (exists)
                    {
                        btnContent.tooltip = "Remove clip from developer animator controller.";
                        btnContent.image = EditorGUIUtility.FindTexture(REMOVE_CLIP_ICON);
                        if (GUILayout.Button(btnContent, GUILayout.MaxWidth(30f)))
                        {
                            ReplaceLayer(_devAnimatorController, RemoveMotion(enabledList[i]));
                            return;
                        }
                    }
                    else
                    {
                        btnContent.tooltip = "Copy clip to developer animator controller.";
                        btnContent.image = EditorGUIUtility.FindTexture(ADD_CLIP_ICON);
                        if (GUILayout.Button(btnContent, GUILayout.MaxWidth(30f)))
                        {
                            _devAnimatorController.AddMotion(enabledList[i]);
                        }
                    }
                }
                else
                {
                    if (!exists)
                    {
                        btnContent.tooltip = "Sync clip to main animator controller.";
                        btnContent.image = EditorGUIUtility.FindTexture(SYNC_CLIP_ICON);
                        if (GUILayout.Button(btnContent, GUILayout.MaxWidth(30f)))
                        {
                            _ogAnimatorController.AddMotion(enabledList[i]);
                        }
                    }
                    btnContent.tooltip = "Remove clip from developer animator controller.";
                    btnContent.image = EditorGUIUtility.FindTexture(DELETE_CLIP_ICON);
                    if (GUILayout.Button(btnContent, GUILayout.MaxWidth(30f)))
                    {
                        ReplaceLayer(_devAnimatorController, RemoveMotion(enabledList[i]));
                        return;
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        private void OnGUI()
        {
            LoadResources();

            if (!_isAnimator)
            {
                return;
            }

            if (_developmentAnimatorItem.developmentController == null)
            {
                const string btnCaption = "Create a Development Animator";
                if (GUILayout.Button(new GUIContent(btnCaption), GUILayout.MinHeight(50f)))
                {
                    CreateDevelopmentController(_developmentAnimatorItem.originalController);
                    LoadResources();
                }
                return;
            }

            DrawHeaderToolbar();
            DrawSearchBar();

            using (var scrollView = new GUILayout.ScrollViewScope(_scrollPos, false, false))
            {
                _scrollPos = scrollView.scrollPosition;

                DrawClipsList();
            }
        }
    }
}