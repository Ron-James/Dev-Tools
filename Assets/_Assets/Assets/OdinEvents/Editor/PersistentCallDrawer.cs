#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

namespace OdinEvents.Editor._Assets.Assets.OdinEvents.Editor
{
    public class PersistentCallDrawer : OdinValueDrawer<PersistentCall>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var call = this.ValueEntry.SmartValue;
            if (call == null)
            {
                SirenixEditorGUI.ErrorMessageBox("Null listener.");
                return;
            }

            SirenixEditorGUI.BeginBox(label);

            // Target field
            var newTarget = SirenixEditorFields.UnityObjectField("Target", call.target, typeof(UnityEngine.Object), true);
            if (newTarget != call.target)
            {
                call.target = newTarget;
                call.ClearCache();
            }

            // Method dropdown
            if (call.target != null)
            {
                var type = call.target.GetType();
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(m => !m.IsSpecialName && m.GetParameters().Length <= 4 && m.GetParameters().All(p =>
                        p.ParameterType.IsPrimitive || p.ParameterType == typeof(string) ||
                        typeof(UnityEngine.Object).IsAssignableFrom(p.ParameterType) ||
                        p.ParameterType.IsEnum || p.ParameterType.IsInterface))
                    .ToArray();
                var methodDisplayNames = methods.Select(m =>
                {
                    var args = m.GetParameters();
                    var argTypes = string.Join(", ", args.Select(a => a.ParameterType.Name));
                    return args.Length == 0 ? m.Name + "()" : $"{m.Name}({argTypes})";
                }).ToArray();
                int selectedIndex = Array.FindIndex(methods, m => m.Name == call.methodName);
                int newIndex = EditorGUILayout.Popup("Method", selectedIndex, methodDisplayNames);
                if (newIndex != selectedIndex && newIndex >= 0)
                {
                    call.methodName = methods[newIndex].Name;
                    call.ClearCache();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Select a target to choose a method.");
            }

            // Arguments
            if (call.TryGetMethod(out var method))
            {
                var parameters = method.GetParameters();
                while (call.arguments.Count < parameters.Length)
                    call.arguments.Add(new ArgBinding());
                while (call.arguments.Count > parameters.Length)
                    call.arguments.RemoveAt(call.arguments.Count - 1);
                for (int i = 0; i < parameters.Length; i++)
                {
                    var arg = call.arguments[i];
                    var paramType = parameters[i].ParameterType;
                    object currentValue = arg.constant.GetValue();
                    object newValue = currentValue;
                    if (typeof(UnityEngine.Object).IsAssignableFrom(paramType))
                    {
                        newValue = SirenixEditorFields.UnityObjectField($"Arg {i+1} ({paramType.Name})", currentValue as UnityEngine.Object, paramType, true);
                    }
                    else if (paramType == typeof(int))
                    {
                        int intValue = currentValue is int ? (int)currentValue : 0;
                        newValue = SirenixEditorFields.IntField($"Arg {i+1} (int)", intValue);
                    }
                    else if (paramType == typeof(float))
                    {
                        float floatValue = currentValue is float ? (float)currentValue : 0f;
                        newValue = SirenixEditorFields.FloatField($"Arg {i+1} (float)", floatValue);
                    }
                    else if (paramType == typeof(bool))
                    {
                        bool boolValue = currentValue is bool ? (bool)currentValue : false;
                        newValue = EditorGUILayout.Toggle($"Arg {i+1} (bool)", boolValue);
                    }
                    else if (paramType == typeof(string))
                    {
                        newValue = SirenixEditorFields.TextField($"Arg {i+1} (string)", currentValue as string ?? "");
                    }
                    else if (paramType.IsEnum)
                    {
                        var enumValue = currentValue as Enum ?? (Enum)Enum.ToObject(paramType, 0);
                        newValue = SirenixEditorFields.EnumDropdown($"Arg {i+1} ({paramType.Name})", enumValue);
                    }
                    else if (paramType.IsInterface)
                    {
                        newValue = SirenixEditorFields.UnityObjectField($"Arg {i+1} (interface)", currentValue as UnityEngine.Object, typeof(UnityEngine.Object), true);
                    }
                    // Store the new value
                    if (!Equals(newValue, currentValue))
                    {
                        arg.constant.SetValue(newValue);
                    }
                }
            }
            else
            {
                SirenixEditorGUI.InfoMessageBox("Pick a target and method to bind arguments.");
            }

            SirenixEditorGUI.EndBox();
        }
    }
    
}
#endif
