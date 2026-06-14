using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
namespace PP
{
public class GlobalManager : MonoBehaviour
{
    public GameManager _gameManager=>gameManager;
    [SerializeField] public GlobalAudioManager globalAudioManager;
    private GameManager gameManager;
    [Header("Events")]
    [SerializeField] public UnityEvent onEnterScene = new UnityEvent();

    public static GlobalManager Instance { get; private set; }
    public SceneManager sceneManager;

    public void Awake()
    {
        if (Instance != null && Instance != this) {
            Debug.LogWarning("GlobalManager 已存在，销毁重复实例！");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        TriggerEnterScene();
    }

    public void registManager(GameManager manager)
    {
        gameManager = manager;
    }

        public void QuitGame()
    {
        //onQuitGame?.Invoke();
        QuitApplication();
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TriggerEnterScene();
    }

    private void TriggerEnterScene()
    {
        if (onEnterScene == null)
        {
            onEnterScene = new UnityEvent();
        }

        onEnterScene.Invoke();
    }
}
}
