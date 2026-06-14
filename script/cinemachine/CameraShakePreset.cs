using DG.Tweening;
using UnityEngine;

namespace PP {
  [CreateAssetMenu(fileName = "shake_", menuName = "Geminum/Scene/Cinemachine Shake Preset")]
  public class CameraShakePreset : ScriptableObject {

    public float Duration;
    public float StrengthFloat;
    [Tooltip("Strengh of shaking in each axis")]
    public Vector3 StrengthVector;
    [Tooltip("Frequency of shaking")]
    public int Vibrato;
    [Tooltip("When checked, fade out will not be applied")]
    public bool ManuallyFadeout;
    [Tooltip("When checked, shaking will also affact camera Z axis")]
    public bool IncludeZAxis;
    [Tooltip("randomness of shaking, over 90-180 not used here")]
    [Range(0, 90)]
    public float Randomness;
    [Tooltip("Harmonic is more balanced and visually more pleasant")]
    public ShakeRandomnessMode randomnessMode;

    [Header("Tweener setting")]
    [Tooltip("Delay of seconds to start the tweener")]
    public float Delay;
    [Tooltip("Ease type")]
    public Ease EaseType;
    [Tooltip("Loop count, -1 is infinity")]
    public int TweenLoopCycle;
    [Tooltip("Loop type")]
    public LoopType TweenLoopType;
  }
}