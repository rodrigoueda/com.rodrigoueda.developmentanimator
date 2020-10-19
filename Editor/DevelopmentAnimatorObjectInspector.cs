using UnityEngine;
using System.Collections;
using UnityEditor;

namespace DevelopmentAnimator
{
    [CustomEditor(typeof(DevelopmentAnimatorObject))]
    public class DevelopmentAnimatorObjectInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Label("Development Animator Data");
        }
    }
}
