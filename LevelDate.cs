using UnityEngine;

namespace PP
{
[CreateAssetMenu(fileName = "LevelData", menuName = "ScriptableObjects/LevelData", order = 1)]
public class LevelDate : ScriptableObject
{
    public float ResistanceDegree = 0f;
    [Header("AI Settings")]
    [Tooltip("AI 维持的时间（秒）")]
    public float aiDuration = 10f;

    [Tooltip("AI犹豫时间（秒）")]
    public float minaiHesitation = 0f;
    public float maxaiHesitation = 1f;

    [Header("Movement Settings")]
    [Tooltip("移动速度")]
    public float moveSpeed = 5f;

    [Tooltip("跳跃力度")]
    public float jumpForce = 12f;
}
}