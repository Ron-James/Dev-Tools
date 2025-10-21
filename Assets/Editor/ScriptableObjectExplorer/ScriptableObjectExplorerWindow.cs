using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Editor.ScriptableObjectExplorer;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;
using UnityEditor.Experimental.GraphView;


namespace ScriptsbleObjectExplorer
{
    /// <summary>
    /// The main Editor Window that shows all ScriptableObjects in the project. It provides:
    /// 
    /// Features:
    /// 1) Organizes ScriptableObjects by inheritance and groups them by namespace.
    /// 2) Provides a dropdown for namespace filtering (to reduce clutter by excluding unwanted namespaces).
    /// 3) Namespaces are shown as root nodes, with types listed underneath.
    /// 4) Displays a foldout-based inspector panel for each ScriptableObject instance, showing:
    ///    - The full inline inspector for the object.
    ///    - A table of serialized fields for quick reference and editing.
    /// 5) Allows easy asset navigation with Ping and Open buttons.
    /// 6) Auto-refreshes when project changes are detected, or manually via a refresh button.
    /// 7) Includes a ScriptableObject creator with:
    ///    - A dropdown to select a ScriptableObject type.
    ///    - A folder path selector for the save location.
    ///    - Input for the asset name.
    ///    - Button to generate the asset.
    /// </summary>
    public class ScriptableObjectExplorerWindow : OdinMenuEditorWindow
    {
        /// <summary>
        /// Dictionary of { Type -> List of ScriptableObject instances }.
        /// Populated by scanning the project for all ScriptableObjects.
        /// </summary>
        private Dictionary<Type, List<ScriptableObject>> soDictionary;

        /// <summary>
        /// Root of our Type hierarchy (a "virtual" node for the base ScriptableObject type).
        /// </summary>
        private TypeNode rootNode;

        /// <summary>
        /// Tracks which namespaces are included or excluded in the tree.
        /// Saved using EditorPrefs for persistence across sessions.
        /// </summary>
        private Dictionary<string, bool> namespaceFilters;

        /// <summary>
        /// Stores the user-typed search term for filtering ScriptableObject instance names.
        /// </summary>
        [Tooltip("Type here to filter ScriptableObject instances by name.")] [SerializeField]
        private string instanceSearchTerm = "";

        /// <summary>
        /// Tracks the selected ScriptableObject type for the asset creator.
        /// </summary>
        [Tooltip("The type of ScriptableObject to create.")]
        private Type selectedSOType;

        /// <summary>
        /// The folder path where the new ScriptableObject will be saved.
        /// </summary>
        [FolderPath(RequireExistingPath = true),
         Tooltip("Select the folder path where the new ScriptableObject will be saved.")]
        [BoxGroup("Conditions")]
        private string savePath = "Assets/Resources";

        /// <summary>
        /// The name of the created ScriptableObject.
        /// </summary>
        [Tooltip("The name of the new ScriptableObject.")] [SerializeField]
        private string assetName = "NewScriptableObject";

        private const string WINDOW_PATH = "Window/Scriptable Object/Scriptable Object Explorer";





        /// <summary>
        /// Opens the ScriptableObject Explorer window via the Unity menu.
        /// </summary>
        [MenuItem(WINDOW_PATH)]
        private static void OpenWindow()
        {
            var window = GetWindow<ScriptableObjectExplorerWindow>();
            window.titleContent = new GUIContent("Scriptable Object Explorer");
            window.Show();
        }

        /// <summary>
        /// Called when the window is enabled. Subscribes to project changes and initializes namespace filters.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.projectChanged += ForceRefresh;
            InitializeNamespaceFilters();
        }

        /// <summary>
        /// Called when the window is disabled. Unsubscribes from project changes and saves preferences.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            EditorApplication.projectChanged -= ForceRefresh;
            SaveNamespacePreferences();
        }

        /// <summary>
        /// Forces the OdinMenuTree to rebuild. This is called after project changes or when the user applies filters.
        /// </summary>
        private void ForceRefresh()
        {
            ForceMenuTreeRebuild();
        }

        /// <summary>
        /// Builds the menu tree for the left panel.
        /// Groups namespaces as root nodes and lists ScriptableObject types under each namespace.
        /// </summary>
        protected override OdinMenuTree BuildMenuTree()
        {
            GatherScriptableObjects();
            var tree = new OdinMenuTree(true);

            rootNode = BuildTypeHierarchy(soDictionary);

            // Add namespaces as root nodes, with their respective types underneath.
            foreach (var namespaceGroup in rootNode.Children)
            {
                string namespaceName = namespaceGroup.Type.Namespace ?? "Empty";

                // Add namespace as a root node
                var namespaceMenuItem = tree.Add(namespaceName, namespaceGroup);

                // Recursively add types and instances
                CreateMenuItemsRecursively(tree, namespaceGroup, namespaceName);
            }

            return tree;
        }





        /// <summary>
        /// Draws the top panel of the right editor, including:
        /// - Namespace dropdown for filtering.
        /// - Search bar for instance filtering.
        /// - ScriptableObject asset creator UI.
        /// - Buttons for Ping and Open in Inspector when a ScriptableObject is selected.
        /// </summary>
        protected override void OnBeginDrawEditors()
        {
            base.OnBeginDrawEditors();

            // Namespace Dropdown for Filtering
            if (EditorGUILayout.DropdownButton(new GUIContent("Namespace Filters"), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var ns in namespaceFilters.Keys.ToList())
                {
                    bool currentState = namespaceFilters[ns];
                    menu.AddItem(new GUIContent(ns), currentState, () =>
                    {
                        namespaceFilters[ns] = !currentState;
                        SaveNamespacePreferences();
                        ForceRefresh();
                    });
                }

                menu.ShowAsContext();
            }

            // Search Bar and Refresh Buttons
            EditorGUILayout.BeginHorizontal();
            instanceSearchTerm = EditorGUILayout.TextField("Search", instanceSearchTerm);
            if (GUILayout.Button("Apply Filters", GUILayout.Width(100))) ForceRefresh();
            if (GUILayout.Button("Refresh", GUILayout.Width(100))) ForceRefresh();
            EditorGUILayout.EndHorizontal();

            // ScriptableObject Creator
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Create New Asset", EditorStyles.boldLabel);

            // ScriptableObject Type Selector
            List<Type> concreteTypes = GetFilteredConcreteTypes();
            int selectedIndex = concreteTypes.IndexOf(selectedSOType);
            selectedIndex = EditorGUILayout.Popup("Type", selectedIndex, concreteTypes.Select(t => t.Name).ToArray());
            selectedSOType = selectedIndex >= 0 ? concreteTypes[selectedIndex] : null;



            // Save Path and Asset Name
            if (GUILayout.Button("Browse", GUILayout.Width(120)))
            {
                SelectFilePath();
            }
            
            
            

            void SelectFilePath()
            {
                string selectedPath = GetRelativeFolderPath();
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    savePath = selectedPath;


                }
            }

            string GetRelativeFolderPath()
            {
                // 1) Prompt the user to select a folder, starting at the Assets path
                string absolutePath = EditorUtility.OpenFolderPanel("Select Folder", savePath, "");

                // If user canceled or closed the panel, absolutePath will be an empty string
                if (string.IsNullOrEmpty(absolutePath))
                {
                    Debug.Log("No folder selected.");
                    return "";
                }

                // 2) Convert backslashes to forward slashes (for consistency across platforms)
                absolutePath = absolutePath.Replace("\\", "/");
                string assetsPath = Application.dataPath.Replace("\\", "/");

                // 3) Ensure that the selected path is within the Assets folder
                if (!absolutePath.StartsWith(assetsPath))
                {
                    Debug.LogError("Selected folder is not inside the 'Assets' folder!");
                    return "";
                }

                // 4) Create the relative path
                //    Remove the portion that matches Application.dataPath 
                //    and prepend "Assets"
                string relativePath = "Assets" + absolutePath.Substring(assetsPath.Length);


                // You can now use relativePath however you want
                return relativePath;
            }

            savePath = EditorGUILayout.TextField("Save Path", savePath);
            assetName = EditorGUILayout.TextField("Asset Name", assetName);



            // Generate Button
            if (selectedSOType != null && GUILayout.Button("Create New Asset"))
            {
                CreateScriptableObject(selectedSOType, savePath, assetName);
            }

            GUILayout.Space(10);
            // Add Ping and Open buttons if a ScriptableObject is selected
            if (this.MenuTree.Selection.Count > 0 && this.MenuTree.Selection[0].Value is ScriptableObject selectedSO)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical();
                // Ping Button
                if (GUILayout.Button("Ping", GUILayout.Width(200)))
                {
                    EditorGUIUtility.PingObject(selectedSO);
                }
                
                if(GUILayout.Button("Open as Panel", GUILayout.Width(200)))
                {
                    ScriptableObjectPanel.OpenPanelForScriptableObject(selectedSO);
                }

                // Open in Inspector Button
                if (GUILayout.Button("Open in Inspector", GUILayout.Width(200)))
                {
                    Selection.activeObject = selectedSO;
                }

                EditorGUILayout.EndVertical();
            }
        }


        /// <summary>
        /// Scans the project for all ScriptableObjects and organizes them by type and namespace.
        /// </summary>
        private void GatherScriptableObjects()
        {
            soDictionary = new Dictionary<Type, List<ScriptableObject>>();

            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (so == null) continue;

                var ns = so.GetType().Namespace ?? "Empty";
                if (!namespaceFilters.TryGetValue(ns, out bool include) || !include) continue;

                if (!string.IsNullOrEmpty(instanceSearchTerm) &&
                    !so.name.Contains(instanceSearchTerm, StringComparison.OrdinalIgnoreCase))
                    continue;

                Type type = so.GetType();
                if (!soDictionary.ContainsKey(type)) soDictionary[type] = new List<ScriptableObject>();
                soDictionary[type].Add(so);
            }
        }
        
        

        /// <summary>
        /// Initializes the namespace filter dictionary and loads preferences from EditorPrefs.
        /// </summary>
        private void InitializeNamespaceFilters()
        {
            if (namespaceFilters == null) namespaceFilters = new Dictionary<string, bool>();

            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so == null) continue;

                var namesSpace = so.GetType().Namespace ?? "Empty";
                if (!namespaceFilters.ContainsKey(namesSpace))
                    namespaceFilters[namesSpace] = EditorPrefs.GetBool($"SOExplorer_NS_{namesSpace}", true);
            }
        }

        /// <summary>
        /// Saves the namespace filter preferences using EditorPrefs.
        /// </summary>
        private void SaveNamespacePreferences()
        {
            foreach (var kvp in namespaceFilters)
            {
                EditorPrefs.SetBool($"SOExplorer_NS_{kvp.Key}", kvp.Value);
            }
        }

        /// <summary>
        /// Returns a list of filtered concrete types of ScriptableObjects.
        /// </summary>
        private List<Type> GetFilteredConcreteTypes()
        {
            return soDictionary.Keys
                .Where(t => !t.IsAbstract)
                .Where(t => namespaceFilters.TryGetValue(t.Namespace ?? "Empty", out bool include) && include)
                .ToList();
        }

        /// <summary>
        /// Creates a new ScriptableObject asset of the specified type.
        /// </summary>
        private void CreateScriptableObject(Type type, string path, string name)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(name))
            {
                Debug.LogError("Invalid save path or name.");
                return;
            }

            ScriptableObject instance = ScriptableObject.CreateInstance(type);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{name}.asset");
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = instance;
        }

        /// <summary>
        /// Builds a hierarchy of TypeNodes based on ScriptableObject inheritance.
        /// </summary>
        private TypeNode BuildTypeHierarchy(Dictionary<Type, List<ScriptableObject>> soDict)
        {
            var typeNodes = new Dictionary<Type, TypeNode>();

            // Create a node for every Type that has ScriptableObject instances
            foreach (var kvp in soDict)
            {
                typeNodes[kvp.Key] = new TypeNode(kvp.Key, kvp.Value);
            }

            // Link parent and child nodes
            foreach (var kvp in typeNodes)
            {
                var type = kvp.Key;
                var node = kvp.Value;

                // Find the parent type
                var baseType = type.BaseType;
                while (baseType != null && baseType != typeof(ScriptableObject))
                {
                    if (typeNodes.TryGetValue(baseType, out var parentNode))
                    {
                        node.Parent = parentNode;
                        parentNode.Children.Add(node);
                        break;
                    }

                    baseType = baseType.BaseType;
                }
            }

            // Create the root node for ScriptableObject
            var root = new TypeNode(typeof(ScriptableObject), null);

            // Attach orphan nodes (those without a parent) to the root
            foreach (var node in typeNodes.Values)
            {
                if (node.Parent == null)
                {
                    node.Parent = root;
                    root.Children.Add(node);
                }
            }

            return root;
        }


        /// <summary>
        /// Recursively adds TypeNodes and their children to the menu tree.
        /// </summary>
        private void CreateMenuItemsRecursively(OdinMenuTree tree, TypeNode node, string path)
        {
            // Skip adding an actual menu item for the "virtual" ScriptableObject root
            if (node.Type != typeof(ScriptableObject))
            {
                string nodeName = node.Type.Name;
                
                
                
                // Get the default icon for the type
                
                Texture2D typeIcon = IconHelper.GetDefaultIconForScriptableObject(node.Type);
                //Style laable to be bold for the type
                
                // Add the type node to the menu tree
                
                var typeMenuItem = tree.Add($"{path}/{nodeName}", node, typeIcon);
                

                // Add ScriptableObject instances under the type node
                foreach (var so in node.ScriptableObjects)
                {
                    Texture icon = IconHelper.GetDefaultIconForScriptableObject(so.GetType()); // Get the default icon for the type
                    string soPath = $"{path}/{nodeName}/{so.name}";
                    tree.Add(soPath, so, icon);
                }
            }

            // Recursively add children
            foreach (var child in node.Children)
            {
                CreateMenuItemsRecursively(tree, child, $"{path}/{node.Type.Name}");
            }
        }


        /// <summary>
        /// Represents a node in the type hierarchy.
        /// Contains the Type itself, any ScriptableObject instances of that type, a parent, and children.
        /// </summary>
        public class TypeNode
        {
            public Type Type;
            public List<ScriptableObject> ScriptableObjects;
            public TypeNode Parent;
            public List<TypeNode> Children;

            public TypeNode(Type type, List<ScriptableObject> soList)
            {
                Type = type;
                ScriptableObjects = soList ?? new List<ScriptableObject>();
                Children = new List<TypeNode>();
            }
        }

        /// <summary>
        /// Wraps a single ScriptableObject for display in the menu tree.
        /// Includes an inline inspector, serialized fields, and utility buttons.
        /// </summary>
        public class ScriptableObjectMenuItem
        {
            private ScriptableObject so;

            public ScriptableObjectMenuItem(ScriptableObject so)
            {
                this.so = so;
            }

            [FoldoutGroup("$FoldoutTitle", expanded: false)]
            [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
            [LabelText("Inspector View")]
            public ScriptableObject ScriptableObjectReference => so;

            [FoldoutGroup("$FoldoutTitle")]
            [TableList(IsReadOnly = false)]
            [LabelText("Serialized Fields Table")]
            public List<SOFieldData> Fields => CollectSerializedFields(so);

            [FoldoutGroup("$FoldoutTitle")]
            [HorizontalGroup("$FoldoutTitle/Buttons", MarginLeft = 0.2f, Order = 100)]
            [Button(ButtonSizes.Medium), GUIColor(0.7f, 1f, 0.7f)]
            private void PingAsset()
            {
                if (so != null)
                {
                    EditorGUIUtility.PingObject(so);
                    Selection.activeObject = so;
                }
            }
            
            
            

            [HorizontalGroup("$FoldoutTitle/Buttons")]
            [Button(ButtonSizes.Medium), GUIColor(0.7f, 0.9f, 1f)]
            private void OpenInEditor()
            {
                if (so != null)
                {
                    Selection.activeObject = so;
                }
            }
            

            private List<SOFieldData> CollectSerializedFields(ScriptableObject so)
            {
                if (so == null) return new List<SOFieldData>();

                var result = new List<SOFieldData>();
                var fields = so.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    bool isSerializable = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
                    if (isSerializable)
                        result.Add(new SOFieldData(so, field));
                }

                return result;
            }

            private string FoldoutTitle => so.name;
        }
    }

}