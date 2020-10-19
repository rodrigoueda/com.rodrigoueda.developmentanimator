using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DevelopmentAnimator
{
    public class DevelopmentAnimatorObject : ScriptableObject
    {
        private const string PATH =
            "Assets/" + Constants.PACKAGES_SETTINGS_FOLDER + "/" +
            Constants.SETTINGS_FOLDER + "/" + Constants.SETTINGS_FILE;
        private const string FOLDER =
            "Assets/" + Constants.PACKAGES_SETTINGS_FOLDER + "/" + Constants.SETTINGS_FOLDER;

        [System.Serializable]
        public class DevelopmentAnimatorItem
        {
            public RuntimeAnimatorController originalController = null;
            public RuntimeAnimatorController developmentController = null;
            public List<AnimationClip> clipList = new List<AnimationClip>();
        }

        public List<DevelopmentAnimatorItem> animatorsList = new List<DevelopmentAnimatorItem>();

        public static DevelopmentAnimatorObject Load()
        {
            DevelopmentAnimatorObject settings =
                AssetDatabase.LoadAssetAtPath<DevelopmentAnimatorObject>(PATH);

            if (settings != null)
            {
                return settings;
            }

            if (!AssetDatabase.IsValidFolder(FOLDER))
            {
                if (!AssetDatabase.IsValidFolder("Assets/" + Constants.PACKAGES_SETTINGS_FOLDER))
                {
                    AssetDatabase.CreateFolder("Assets", Constants.PACKAGES_SETTINGS_FOLDER);
                }
                AssetDatabase.CreateFolder(
                    "Assets/" + Constants.PACKAGES_SETTINGS_FOLDER, Constants.SETTINGS_FOLDER);
            }

            settings = CreateInstance<DevelopmentAnimatorObject>();

            AssetDatabase.CreateAsset(settings, PATH);
            AssetDatabase.SaveAssets();

            return settings;
        }

        public DevelopmentAnimatorItem GetSelectedAnimator(GameObject selected)
        {
            if (selected == null)
            {
                return null;
            }

            DevelopmentAnimatorItem item = null;
            Animator animator = selected.GetComponent<Animator>();

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return null;
            }


            for (int i = 0; i < animatorsList.Count; i++)
            {
                int _originalID;
                int _developID;

                if (animatorsList[i].originalController != null)
                {
                    _originalID = animatorsList[i].originalController.GetInstanceID();
                    if (_originalID == animator.runtimeAnimatorController.GetInstanceID())
                    {
                        return animatorsList[i];
                    }
                }

                if (animatorsList[i].developmentController != null)
                {
                    _developID = animatorsList[i].developmentController.GetInstanceID();
                    if (_developID == animator.runtimeAnimatorController.GetInstanceID())
                    {
                        return animatorsList[i];
                    }
                }
            }

            if (item == null)
            {
                item = new DevelopmentAnimatorItem
                {
                    originalController = animator.runtimeAnimatorController
                };

                animatorsList.Add(item);
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }

            return item;
        }

        public void SaveDevelopmentAnimatorItem(DevelopmentAnimatorItem item)
        {
            for (int i = 0; i < animatorsList.Count; i++)
            {
                if (item.originalController == animatorsList[i].originalController)
                {
                    animatorsList[i].developmentController = item.developmentController;
                    animatorsList[i].clipList = item.clipList;

                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();

                    return;
                }
            }

        }
    }
}