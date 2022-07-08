using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Singleton Class
 * Object Pool that creates a specified number of GameObjects with audioSource and SoundSourceLogic components on Play. 
 * Manages all created objects being added and removed from the pool.
 * Parent Object does not destroy on load to enhance performance and scene change start times.
 * Don't destroy on load to persists between scenes for performance and scene load times
 */
public class ObjectPooler : MonoBehaviour
{
    GameObject prefab;/*!< Reference to prefab object to be pooled*/
    public int SoundPoolSize = 20;/*!< Number of pooled objects to create*/

    private static ObjectPooler _instance;/*!< Reference to the instance for Singleton*/

    /** 
    * Gets the instance of the object, if not found creates and sets a new instance.
    */
    public static ObjectPooler instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Instantiate(Resources.Load("AudioManager/SoundPool") as GameObject).GetComponent<ObjectPooler>();
                DontDestroyOnLoad(_instance);
            }
            return _instance;
        }
    }

    List<GameObject> PooledObjects;/*!< List of pooled Prefab objects to be managed*/

    /** 
    * Function called on awake. Checks if another instance exists and removes itself if one is found. 
    * sets PooledObjects list.
    * Locates and sets prefab Under Resource/AudioManage/SoundSource
    * Creates specified number of Prefab objects in the pool based on SoundPoolSize
    */
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        PooledObjects = new List<GameObject>();

        prefab = Resources.Load("AudioManager/SoundSource") as GameObject;
        if(prefab == null)
        {
            Debug.LogError("Object Pool Prefab missing under Resource/AudioManager/SoundSource");
        }

        for (int i = 0; i < SoundPoolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.transform.parent = gameObject.transform;
            obj.SetActive(false);
            PooledObjects.Add(obj);
        }
    }

    /** 
     * Function to get and activate the first object in the pool. Sets the positon and rotation of the object. Returns GameObject.
     */
    public GameObject SpawFromPool(Vector3 position, Quaternion rotation)
    {
        GameObject objectToSpawn = PooledObjects[0];
        PooledObjects.Remove(objectToSpawn);

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        PooledObjects.Add(objectToSpawn);

        return objectToSpawn;
    }

    /** 
     * Function to disable pool object and return it to the back of the list to be reused.
     */
    public void ReturnToPool(GameObject pooledObject)
    {
        GameObject ObjectToBeDeactivated = PooledObjects[PooledObjects.IndexOf(pooledObject)];
        pooledObject.transform.parent = gameObject.transform;
        ObjectToBeDeactivated.SetActive(false);
        PooledObjects.Remove(ObjectToBeDeactivated);
        PooledObjects.Add(ObjectToBeDeactivated);
    }
}
