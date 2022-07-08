using UnityEngine;
using UnityEditor;

/** 
 * Editor window to manage and edit AudioManager scriptable object instances and Sound instances in AudioManager scriptable object.
 * Automatically creates AudioManager scriptable object if one is not present in Resources/Audiomanger names AudioManagerData.
 * Displays Editor Window.
 */
public class AudioManagerWindow : EditorWindow
{
    protected AudioManager audioManagerData;/*!< Reference to AudioManager scriptable Object */
    protected SerializedObject serializedObject;/*!< Reference to serialized AudioManager. Used to get serialized property to create relevant property fields */
    protected SerializedProperty serializedProperty;/*!< Reference to serialized property. Changes between each property in serializedObject. Used to display PropertyFields*/

    Vector2 scrollPosition;/*!< The scroll position in the window*/

    [MenuItem("Window/Audio Manager")]
    /** 
     * Displays the Editor Window. Specifies the window location under Window in editor
     */
    public static void ShowWindow()
    {
        GetWindow<AudioManagerWindow>("Audio Manager"); 
    }

    /** 
     * Displays the editor window.
     * Gets the serialized AudioManager scriptable object if not already set.
     * Used to display all properties contained within AudioManager scriptable object.
     * Allows AudioManager data to be edited and sounds to be previewed.
     */
    void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, true, true, GUILayout.Width(position.width), GUILayout.Height(position.height));
        GetSerializedObject();
        
        if(serializedObject != null)
        {
            serializedProperty = serializedObject.GetIterator();
            serializedProperty.NextVisible(true);
            DisplayProperties(serializedProperty);
        }
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();
    }

    /** 
     * Update function called each tick. Calls AudioManager ManagePreviewObjects() function to update preview objects
     */
    private void Update()
    {
        audioManagerData.ManagePreviewObjects();
    }

    /** 
     * Displays all properties fields of SerializedObject
     */
    protected void DisplayProperties(SerializedProperty p)
    {
        while (p.NextVisible(false))
        {
            EditorGUILayout.PropertyField(p, true);
        } 
    }

    /** 
     * Function to get and set audioManagerData and serializableObject. If audioManagerData instance cannot be located under Resources/AudioManager one is created and set.
     */
    protected void GetSerializedObject()
    {
        if (!audioManagerData)
        {
            audioManagerData = Resources.Load<AudioManager>("AudioManager/AudioManagerData");
            if (!audioManagerData)
            {
                audioManagerData = CreateInstance<AudioManager>();
                AssetDatabase.CreateAsset(audioManagerData, "Assets/Resources/AudioManager/AudioManagerData.asset");
            }
        }

        if(serializedObject == null)
        {
            serializedObject = new SerializedObject(audioManagerData);
        }
    }
}
