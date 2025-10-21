// Assets/OdinEvents/Editor/OdinEventDrawer.cs
#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using OdinEvents;

namespace OdinEvents.Editor._Assets.Assets.OdinEvents.Editor
{
    // Draws any OdinEventBase field to look/feel like UnityEvent.
    public class OdinEventBaseDrawer : OdinValueDrawer<OdinEventBase>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            CallNextDrawer(label);
        }
    }
}
#endif
