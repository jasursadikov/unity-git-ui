using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Abuksigun.MRGitUI
{
    public class SplitterState
    {
        object splitterStateInstance;
        static Type splitterStateType;
        static FieldInfo realSizesField;

        internal object SplitterStateInstance => splitterStateInstance;
        public IReadOnlyList<float> RealSizes => realSizesField.GetValue(splitterStateInstance) as float[];

        static SplitterState()
        {
            var unityEditorAssembly = Assembly.Load("UnityEditor");
            splitterStateType = unityEditorAssembly.GetType("UnityEditor.SplitterState");
            realSizesField = splitterStateType.GetField("realSizes", BindingFlags.Public | BindingFlags.Instance);
        }

        public SplitterState(params float[] relativeSizes)
        {
            splitterStateInstance = Activator.CreateInstance(splitterStateType, new object[] { relativeSizes });
        }
    }


    public class SplitterGUILayout
    {
        static Type splitterGUILayoutType;
        static MethodInfo beginHorizontalSplitMethod;
        static MethodInfo endHorizontalSplitMethod;
        static MethodInfo beginVerticalSplitMethod;
        static MethodInfo endVerticalSplitMethod;

        static SplitterGUILayout()
        {
            var unityEditorAssembly = Assembly.Load("UnityEditor");
            splitterGUILayoutType = unityEditorAssembly.GetType("UnityEditor.SplitterGUILayout");
            var splitterStateType = unityEditorAssembly.GetType("UnityEditor.SplitterState");

            beginHorizontalSplitMethod = splitterGUILayoutType.GetMethod("BeginHorizontalSplit", new[] { splitterStateType, typeof(GUILayoutOption[]) });
            endHorizontalSplitMethod = splitterGUILayoutType.GetMethod("EndHorizontalSplit", BindingFlags.Public | BindingFlags.Static);
            beginVerticalSplitMethod = splitterGUILayoutType.GetMethod("BeginVerticalSplit", new[] { splitterStateType, typeof(GUILayoutOption[]) });
            endVerticalSplitMethod = splitterGUILayoutType.GetMethod("EndVerticalSplit", BindingFlags.Public | BindingFlags.Static);
        }

        public static void BeginHorizontalSplit(SplitterState state, params GUILayoutOption[] options)
        {
            beginHorizontalSplitMethod.Invoke(null, new object[] { state.SplitterStateInstance, options });
        }

        public static void EndHorizontalSplit()
        {
            endHorizontalSplitMethod.Invoke(null, null);
        }

        public static void BeginVerticalSplit(SplitterState state, params GUILayoutOption[] options)
        {
            beginVerticalSplitMethod.Invoke(null, new object[] { state.SplitterStateInstance, options });
        }

        public static void EndVerticalSplit()
        {
            endVerticalSplitMethod.Invoke(null, null);
        }
    }
}