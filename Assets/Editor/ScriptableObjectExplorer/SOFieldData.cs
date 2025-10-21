using System.Reflection;
using Sirenix.OdinInspector;

namespace ScriptsbleObjectExplorer
{


    /// <summary>
    /// Represents a serialized field for display in the field table.
    /// </summary>
    public class SOFieldData
    {
        private object target;
        private FieldInfo fieldInfo;

        public SOFieldData(object target, FieldInfo field)
        {
            this.target = target;
            this.fieldInfo = field;
        }

        [TableColumnWidth(150, Resizable = false)]
        [ShowInInspector, DisplayAsString, LabelText("Field")]
        public string FieldName => fieldInfo.Name;

        [ShowInInspector, DisplayAsString, LabelText("Type")]
        public string FieldType => fieldInfo.FieldType.Name;

        [ShowInInspector, LabelText("Value")]
        public object Value
        {
            get => fieldInfo.GetValue(target);
            set => fieldInfo.SetValue(target, value);
        }
    }
}