using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

[CustomPropertyDrawer(typeof(Sound))]
/** 
 * Custom Property Drawer for Sound class. Creates two buttons, Play and Stop which create preview sound objects in edit mode.
 * Displays buttons when property is expanded.
 * Gets the path of the Sound object to play or stop Preview sound(s).
 */
public class CustomSoundInspector : PropertyDrawer
{
    /**
     * OnGUI draws custome property drawer for Sound property in AudioManageWindow.
     * When expanded displays Play and Stop buttons for used in edit mode.
     */
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label, true);
        if (property.isExpanded)
        {
            if (GUI.Button(new Rect(position.xMin + 10, position.yMax - 25f, (position.width / 2f) - 20, 20f), "Play"))
            {
                AudioManager soundarr = property.serializedObject.targetObject as AudioManager;
                string number = property.propertyPath.Split('[', ']')[1];
                int index = int.Parse(number);
                Sound targetSound = soundarr.sounds[index];
                targetSound.PlayPreviewSound();
            }
            if (GUI.Button(new Rect(position.xMin + (position.width / 2f) + 10, position.yMax - 25f, (position.width / 2f) - 20, 20f), "Stop"))
            {
                AudioManager soundarr = property.serializedObject.targetObject as AudioManager;
                string number = property.propertyPath.Split('[', ']')[1];
                int index = int.Parse(number);
                Sound targetSound = soundarr.sounds[index];
                targetSound.StopPreviewSound();
            }
        }
    }

    /**
     * GetPropertyHeight adjusts the propertyheight when expanded to allow for buttons.
     */
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded)
            return EditorGUI.GetPropertyHeight(property, label) + 30f;
        return EditorGUI.GetPropertyHeight(property, label);
    }
}
