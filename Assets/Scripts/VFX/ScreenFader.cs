using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Mirror;
using UnityEngine.Events;
using UnityEngine.UI;


public class ScreenFader : MonoBehaviour
{
    private static ScreenFader _instance;
    public TextMeshProUGUI text;

    public static ScreenFader instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Instantiate(Resources.Load("ScreenTransitions/ScreenFadeCanvas") as GameObject).GetComponentInChildren<ScreenFader>();
                DontDestroyOnLoad(_instance);
            }
            return _instance;
        }
    }

    private string stringToFade;
    private int indexToFade = -1;
    private bool asyncLoad = false;

    public float fadeInDelay = 0f;

    public RawImage blackOutImage;

    [SerializeField] private Animator animator;

    [System.NonSerialized]
    public UnityEvent fadeOutOperation = new UnityEvent();

    public bool stayDarkUntilConnect;
    private bool initialFadeCompleted = false;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            //DontDestroyOnLoad(gameObject);
        }

        blackOutImage.enabled = true;
        animator.Play("BlackScreen");
    }

    public void Update()
    {
        if (!initialFadeCompleted && (!stayDarkUntilConnect || (GameManager.instance != null && GameManager.instance.localPlayerCharacter != null)))
        {
            blackOutImage.enabled = false;
            initialFadeCompleted = true;
            animator.Play("FadeIn");
        }
    }

    public void Start()
    {
        StartCoroutine(DelayedFadeIn(fadeInDelay));
    }

    public IEnumerator DelayedFadeIn(float timer)
    {
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        animator.SetTrigger("FadeIn");
    }

    private UnityAction fadeInOutAction;

    public void FadeInOut(UnityAction midFadeAction)
    {
        fadeInOutAction = midFadeAction;
        animator.Play("FadeInOutStart");
    }

    public void MidFade()
    {
        fadeInOutAction.Invoke();
    }

    public void FadeOutCompleted()
    {
        if (fadeOutOperation != null)
        {
            fadeOutOperation.Invoke();
            fadeOutOperation.RemoveAllListeners();
        }
    }

    public void SceneFade()
    {
        if (asyncLoad)
        {
            readyToLoadAsyncScene = true;
        }
        else
        {
            int tempIndex = indexToFade;
            indexToFade = -1;
            if (tempIndex >= 0)
            {
                SceneManager.LoadScene(tempIndex);
            }
            else SceneManager.LoadScene(stringToFade);
        }
    }

    public void FadeInCompleted()
    {

    }

    public void FadeToSceneAsync(string _sceneName, UnityAction<AsyncOperation> loadStartedAction)
    {
        stringToFade = _sceneName;
        FadeToSceneAsync(loadStartedAction);
    }

    public void FadeToSceneAsync(int _buildIndex, UnityAction<AsyncOperation> loadStartedAction)
    {
        indexToFade = _buildIndex;
        FadeToSceneAsync(loadStartedAction);
    }

    public void FadeToScene(string _sceneName)
    {
        stringToFade = _sceneName;
        FadeToScene();
    }

    public void FadeToScene(int _buildIndex)
    {
        indexToFade = _buildIndex;
        FadeToScene();
    }

    public void FadeToSceneAsync(UnityAction<AsyncOperation> loadStartedAction)
    {
        asyncLoad = true;
        fadeOutOperation.AddListener(SceneFade);
        animator.Play("FadeOut");
        StartCoroutine(IFadeToSceneAsync(loadStartedAction));
    }

    public void FadeToScene()
    {
        asyncLoad = false;
        fadeOutOperation.AddListener(SceneFade);
        animator.Play("FadeOut");
    }

    private bool readyToLoadAsyncScene = false;
    public IEnumerator IFadeToSceneAsync(UnityAction<AsyncOperation> loadStartedAction)
    {
        asyncLoad = true;
        animator.Play("FadeOut");
        while (!readyToLoadAsyncScene)
        {
            yield return null;
        }
        readyToLoadAsyncScene = false;

        AsyncOperation operation;

        int tempIndex = indexToFade;
        indexToFade = -1;
        if (tempIndex >= 0)
        {
            operation = SceneManager.LoadSceneAsync(tempIndex);
        }
        else
        {
            operation = SceneManager.LoadSceneAsync(stringToFade);
        }

        if (loadStartedAction != null)
        {
            loadStartedAction.Invoke(operation);
        }

    }

    public void FadeOut(UnityAction fadeOutAction)
    {
        animator.Play("FadeOut");
        fadeOutOperation.AddListener(fadeOutAction);
    }
}
