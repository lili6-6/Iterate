using UnityEngine;
using UnityEngine.Events;

namespace PP
{
public class Property_Switch : Property_base
{
    [SerializeField]private bool autoTrigger = false;//自动保持开启状态
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource enterAudioSource;
    [SerializeField] private AudioSource exitAudioSource;
    [SerializeField] private float audioVolume = 1.0f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
        
        // Setup audio source if assigned
        if (enterAudioSource != null)
        {
            enterAudioSource.volume = audioVolume;
            enterAudioSource.playOnAwake = false;
        }
        if (exitAudioSource != null)
        {
            exitAudioSource.volume = audioVolume;
            exitAudioSource.playOnAwake = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    protected override void OnTriggerEnter2D(Collider2D others)
    {
        if (((1 << others.gameObject.layer) & targetLayer) != 0)
        {
            if (currentState == State.Idle)
            {
                ChangeState(State.Awake);
                AwakeEvent.Invoke();
                
                // Play enter sound
                if (enterAudioSource != null)
                {
                    enterAudioSource.Play();
                }
            }
        }
    }
    protected override void OnTriggerExit2D(Collider2D others)
    {
        if (!autoTrigger)//离开后不自动保持开启状态
        {
            if (((1 << others.gameObject.layer) & targetLayer) != 0)
            {
                if (currentState == State.Awake)
                {
                    ChangeState(State.Idle);
                    IdleEvent.Invoke();
                    
                    // Play exit sound
                    if (exitAudioSource != null)
                    {
                        exitAudioSource.Play();
                    }
                }
            }
        }
    }
}
}