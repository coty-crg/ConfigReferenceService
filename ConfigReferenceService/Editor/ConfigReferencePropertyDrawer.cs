#if UNITY_EDITOR
namespace ConfigRefs
{
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(ConfigReference<>), true)]
    public class ConfigReferencePropertyDrawer : PropertyDrawer
    {
        private SerializedProperty _activeProperty;

        void Revert() 
        { 
            _activeProperty.prefabOverride = false; 
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 1 && position.Contains(e.mousePosition))
            {
                _activeProperty = property;

                GenericMenu context = new GenericMenu();
                if (property.prefabOverride)
                {
                    context.AddItem(new GUIContent("Revert"), false, Revert);
                }
                else
                {
                    context.AddDisabledItem(new GUIContent("Revert"));
                }
                context.ShowAsContext();
            }

            // fetch the generic type of the actual property 
            // this generic is used for the object selection (limits type, to avoid user error) 
            var type = fieldInfo.FieldType;

            // if we're a property of a T[], then yoink out the element type 
            if (type.IsArray)
            {
                type = type.GetElementType();
            }

            // if we're a property of a List<T>, then rip out the element type 
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var genericListElementTypes = type.GetGenericArguments();
                if (genericListElementTypes != null && genericListElementTypes.Length > 0)
                {
                    type = genericListElementTypes[0];
                }
            }

            // try to figure out what Object we're allowed to use in the object picker, based on the <T> 
            var genericType = typeof(Object);
            var typeGenericArguments = type.GetGenericArguments();
            if (typeGenericArguments != null && typeGenericArguments.Length > 0)
            {
                genericType = typeGenericArguments[0];
            }

            var guidProperty = property.FindPropertyRelative("Guid");
            var objNameProperty = property.FindPropertyRelative("ObjName");

            // using 3x the rect size, so each half can be used 
            position.width /= 3f;

            var labelPosition = position;

            var objectFieldPos = labelPosition;
                objectFieldPos.x += position.width;

            var guidPosition = objectFieldPos;
                guidPosition.x += guidPosition.width;

            // draw the name of this property 
            EditorGUI.LabelField(labelPosition, label);

            var multipleValues = property.hasMultipleDifferentValues;
            if (multipleValues)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.TextField(objectFieldPos, "[multiple values]");
                EditorGUI.EndDisabledGroup();
                return;
            }

            ScriptableObject _referencedObject = null;

            // fast guid lookup in editor 
            var guidString = guidProperty.stringValue;
            if (!string.IsNullOrEmpty(guidString))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guidString);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    _referencedObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                }
            }

            // fallback lookup by name 
            if (_referencedObject == null)
            {
                var stringName = objNameProperty.stringValue;
                if (!string.IsNullOrEmpty(stringName))
                {
                    AssetDatabaseE.EditorFindByNameHash(stringName.GetHashCode(), genericType, out object result);
                    if (result != null)
                    {
                        _referencedObject = (ScriptableObject)result;
                    }
                }
            }

            // draw the object field 
            var prevReferenceObject = _referencedObject;
            _referencedObject = (ScriptableObject)EditorGUI.ObjectField(objectFieldPos, _referencedObject, genericType, false);

            if (_referencedObject != null)
            {
                objNameProperty.stringValue = _referencedObject.name;

                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_referencedObject, out string newGuid, out long newLocalId))
                {
                    guidProperty.stringValue = newGuid;
                }
            }
            else
            {
                objNameProperty.stringValue = string.Empty;
                guidProperty.stringValue = string.Empty;
            }

            if (prevReferenceObject != _referencedObject && _referencedObject != null)
            {
                var baseConfigService = AssetDatabaseE.FindSingletonAsset<ConfigReferenceService>();
                baseConfigService.EditorEnsureConfig(new ConfigReference<ScriptableObject>()
                {
                    Guid = guidProperty.stringValue
                });
            }

            // draw the actual property
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(guidPosition, guidProperty, new GUIContent(), true);
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif