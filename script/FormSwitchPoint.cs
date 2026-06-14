using UnityEngine;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using DG.Tweening;
using PP;
using UnityEngine.Events;
namespace pp
{
	/// <summary>
	/// 将此脚本放在场景中的 GameObject 上（例如一个"形态切换点"）。
	/// 当玩家靠近并按下交互键时，玩家会切换为指定的目标形态。
	/// 每个此脚本实例对应一个特定的目标形态。
	/// 
	/// 工作原理：
	/// 1. 使用 Trigger 检测玩家进入/离开范围
	/// 2. 玩家在范围内按下交互键时，将当前玩家替换为目标形态 Prefab
	/// 3. 参考 CharacterSwitchManager 的实现，但简化为一个点对应一个形态
	/// </summary>
	public class FormSwitchPoint : CorgiMonoBehaviour
	{
		[Header("Display Sprite")]
		[MMInformation("将需要做缩放动画的 SpriteRenderer 拖到这里，玩家靠近时 scale 从 0 到 1，离开时回到 0。", MMInformationAttribute.InformationType.Info, false)]

		/// 显示用的 SpriteRenderer（玩家靠近时做缩放动画）
		[Tooltip("显示用的 SpriteRenderer（玩家靠近时做缩放动画）")]
		public SpriteRenderer DisplaySprite;

		/// 缩放动画时长（秒）
		[Tooltip("缩放动画时长（秒）")]
		public float AnimationDuration = 0.3f;

		/// 提示 Sprite 显示时的目标缩放
		[Tooltip("提示 Sprite 显示时的目标缩放")]
		public Vector3 DisplaySpriteTargetScale = new Vector3(2f, 2f, 2f);

		[Header("Target Form")]
		[MMInformation("将目标角色 Prefab 拖到这里，当玩家在此范围内交互时，会切换为该形态。", MMInformationAttribute.InformationType.Info, false)]

		/// 目标形态的 Character Prefab（玩家将切换成的形态）
		[Tooltip("目标形态的 Character Prefab（玩家将切换成的形态）")]
		public Character TargetCharacterPrefab;

		/// 要控制的玩家 ID
		[Tooltip("要控制的玩家 ID")]
		public string PlayerID = "Player1";

		/// 要控制的玩家在 LevelManager 中的索引
		[Tooltip("要控制的玩家在 LevelManager 中的索引")]
		public int PlayerIndex = 0;

		[Header("Interaction Settings")]

		/// 交互键
		[Tooltip("交互键")]
		public KeyCode InteractKey = KeyCode.E;

		/// 交互提示文本（可选，用于 UI 显示）
		[Tooltip("交互提示文本（可选，用于 UI 显示）")]
		public string InteractionPrompt = "按 E 切换形态";

		/// 是否在切换后禁用此切换点（一次性形态切换点）
		[Tooltip("是否在切换后禁用此切换点（一次性形态切换点）")]
		public bool DisableAfterSwitch = true;

		/// 切换后新角色的额外向上偏移，避免卡进平台
		[Tooltip("切换后新角色的额外向上偏移，避免卡进平台")]
		public float SwitchSpawnYOffset = 1f;

		[Header("Sync Settings")]

		/// 是否同步速度和跳跃高度到新形态
		[Tooltip("开启时，切换形态后会同步旧角色的移动速度和跳跃高度到新角色；关闭时则使用新 Prefab 的默认值")]
		public bool SyncSpeedAndJump = true;

		[Header("Visual Effects")]

		/// 切换时播放的粒子特效
		[Tooltip("切换时播放的粒子特效")]
		public ParticleSystem SwitchVFX;

		[Header("Debug")]

		/// 测试用按钮，强制触发切换
		[MMInspectorButton("ForceSwitch")]
		public bool ForceSwitchButton;
		[Header("Events")]
		public UnityEvent OnFormSwitched;

		/// 玩家是否在触发器范围内
		public bool PlayerInRange { get; protected set; }

		protected Character _instantiatedCharacter;
		protected ParticleSystem _instantiatedVFX;
		protected CorgiEngineEvent _switchEvent = new CorgiEngineEvent(CorgiEngineEventTypes.CharacterSwitch, null);
		protected Tween _scaleTween;

		/// <summary>
		/// Awake：初始化 DisplaySprite 的 scale 为 0
		/// </summary>
		protected virtual void Awake()
		{
			if (DisplaySprite != null)
			{
				DisplaySprite.transform.localScale = Vector3.zero;
			}
		}

		/// <summary>
		/// 实例化目标角色并禁用
		/// 注意：延迟到切换时才实例化，避免 Prefab 在禁用状态下收到 LevelStart 等事件导致初始化错误
		/// </summary>
		protected virtual void InstantiateTargetCharacter()
		{
			if (TargetCharacterPrefab == null)
			{
				Debug.LogError("[FormSwitchPoint] TargetCharacterPrefab 未设置！", this);
				return;
			}

			_instantiatedCharacter = Instantiate(TargetCharacterPrefab);
			_instantiatedCharacter.name = "FormSwitch_" + TargetCharacterPrefab.name;
			_instantiatedCharacter.gameObject.SetActive(false);
			_instantiatedCharacter.transform.position = this.transform.position + Vector3.up * Mathf.Max(0f, SwitchSpawnYOffset);
		}

		/// <summary>
		/// 实例化粒子特效
		/// </summary>
		protected virtual void InstantiateVFX()
		{
			if (SwitchVFX != null)
			{
				_instantiatedVFX = Instantiate(SwitchVFX);
				_instantiatedVFX.Stop();
				_instantiatedVFX.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// 每帧检测输入
		/// 注意：使用 Input.GetKeyDown 而不是 CorgiEngine 的 InputManager，
		/// 因为本项目使用自定义 PlayerInput 系统，禁用了 CorgiEngine 的自动读输入
		/// </summary>
		protected virtual void Update()
		{
			if (!PlayerInRange)
			{
				return;
			}

			if (Input.GetKeyDown(InteractKey))
			{
				PerformSwitch();
			}
		}

		/// <summary>
		/// 玩家进入触发器范围
		/// </summary>
		protected virtual void OnTriggerEnter2D(Collider2D other)
		{
			CheckPlayerEnter(other.gameObject);
		}

		protected virtual void OnTriggerEnter(Collider other)
		{
			CheckPlayerEnter(other.gameObject);
		}

		/// <summary>
		/// 玩家离开触发器范围
		/// </summary>
		protected virtual void OnTriggerExit2D(Collider2D other)
		{
			CheckPlayerExit(other.gameObject);
		}

		protected virtual void OnTriggerExit(Collider other)
		{
			CheckPlayerExit(other.gameObject);
		}

		/// <summary>
		/// 检查进入的对象是否为玩家
		/// </summary>
		protected virtual void CheckPlayerEnter(GameObject other)
		{
			Character character = other.gameObject.MMGetComponentNoAlloc<Character>();
			if (character != null && character.CharacterType == Character.CharacterTypes.Player)
			{
				PlayerInRange = true;
				PlayScaleInAnimation();
			}
		}

		/// <summary>
		/// 检查离开的对象是否为玩家
		/// </summary>
		protected virtual void CheckPlayerExit(GameObject other)
		{
			Character character = other.gameObject.MMGetComponentNoAlloc<Character>();
			if (character != null && character.CharacterType == Character.CharacterTypes.Player)
			{
				PlayerInRange = false;
				PlayScaleOutAnimation();
			}
		}

		/// <summary>
		/// 播放 Sprite 放大动画（scale 0 -> 1）
		/// </summary>
		protected virtual void PlayScaleInAnimation()
		{
			if (DisplaySprite == null) return;

			// 终止之前的动画
			_scaleTween?.Kill();
			_scaleTween = DisplaySprite.transform.DOScale(DisplaySpriteTargetScale, AnimationDuration)
				.SetEase(Ease.OutBack)
				.SetLink(gameObject);
		}

		/// <summary>
		/// 播放 Sprite 缩小动画（scale 1 -> 0）
		/// </summary>
		protected virtual void PlayScaleOutAnimation()
		{
			if (DisplaySprite == null) return;

			// 终止之前的动画
			_scaleTween?.Kill();
			_scaleTween = DisplaySprite.transform.DOScale(Vector3.zero, AnimationDuration)
				.SetEase(Ease.InBack)
				.SetLink(gameObject);
		}

		/// <summary>
		/// 强制切换（Inspector 调试用）
		/// </summary>
		public virtual void ForceSwitch()
		{
			PerformSwitch();
		}

		/// <summary>
		/// 执行形态切换
		/// </summary>
		protected virtual void PerformSwitch()
		{
			if (LevelManager.Instance == null || LevelManager.Instance.Players == null)
			{
				Debug.LogWarning("[FormSwitchPoint] LevelManager 或 Players 为空。", this);
				return;
			}

			if (LevelManager.Instance.Players.Count <= PlayerIndex)
			{
				Debug.LogWarning("[FormSwitchPoint] PlayerIndex 超出范围。", this);
				return;
			}

			// 延迟实例化：在切换时才创建目标角色，避免 Prefab 在禁用状态下收到事件报错
			if (_instantiatedCharacter == null)
			{
				InstantiateTargetCharacter();
				InstantiateVFX();
			}

			if (_instantiatedCharacter == null)
			{
				Debug.LogWarning("[FormSwitchPoint] 目标角色未实例化，无法切换。", this);
				return;
			}
			OnFormSwitched?.Invoke();

			StartCoroutine(SwitchCoroutine());
		}

		/// <summary>
		/// 切换协程：保存状态 -> 替换角色 -> 播放特效
		/// </summary>
		protected virtual System.Collections.IEnumerator SwitchCoroutine()
		{
			// 保存当前玩家的状态
			float newHealth = LevelManager.Instance.Players[PlayerIndex].gameObject.MMGetComponentNoAlloc<Health>().CurrentHealth;
			bool facingRight = LevelManager.Instance.Players[PlayerIndex].IsFacingRight;

			// 保存当前玩家的移动速度和跳跃高度（经过 LearningDegree 调整后的值）
			CharacterHorizontalMovement oldHorizontalMovement = null;
			CharacterJump oldCharacterJump = null;
			float oldWalkSpeed = 0f;
			float oldJumpHeight = 0f;

			if (SyncSpeedAndJump)
			{
				oldHorizontalMovement = LevelManager.Instance.Players[PlayerIndex].gameObject.MMGetComponentNoAlloc<CharacterHorizontalMovement>();
				oldCharacterJump = LevelManager.Instance.Players[PlayerIndex].gameObject.MMGetComponentNoAlloc<CharacterJump>();
				oldWalkSpeed = oldHorizontalMovement != null ? oldHorizontalMovement.WalkSpeed : 0f;
				oldJumpHeight = oldCharacterJump != null ? oldCharacterJump.JumpHeight : 0f;
			}

			// 保存当前玩家的 AI 相关数值（对应 GameManager.updateLevel 中设置的参数）
			PlayerManager oldPlayerManager = LevelManager.Instance.Players[PlayerIndex].gameObject.GetComponent<PlayerManager>();
			float oldAiDuration = oldPlayerManager != null ? oldPlayerManager.aiDuration : 1f;

			AIActionPPIdle oldAIActionIdle = LevelManager.Instance.Players[PlayerIndex].gameObject.GetComponent<AIActionPPIdle>();
			float oldMinHesitateDuration = oldAIActionIdle != null ? oldAIActionIdle.MinHesitateDuration : 0.5f;
			float oldMaxHesitateDuration = oldAIActionIdle != null ? oldAIActionIdle.MaxHesitateDuration : 2f;

			// 禁用旧玩家，启用新角色
			LevelManager.Instance.Players[PlayerIndex].gameObject.SetActive(false);
			_instantiatedCharacter.SetPlayerID(PlayerID);
			_instantiatedCharacter.gameObject.SetActive(true);

			// 将新角色移动到旧玩家位置
			_instantiatedCharacter.transform.position = LevelManager.Instance.Players[PlayerIndex].transform.position + Vector3.up * Mathf.Max(0f, SwitchSpawnYOffset);
			_instantiatedCharacter.transform.rotation = LevelManager.Instance.Players[PlayerIndex].transform.rotation;

			// 保持相同的移动状态和条件状态
			_instantiatedCharacter.MovementState.ChangeState(LevelManager.Instance.Players[PlayerIndex].MovementState.CurrentState);
			_instantiatedCharacter.ConditionState.ChangeState(LevelManager.Instance.Players[PlayerIndex].ConditionState.CurrentState);

			// 设置为当前玩家
			LevelManager.Instance.Players[PlayerIndex] = _instantiatedCharacter;

			// 更新 PP.GameManager 的 playerManager 引用为新角色的 PlayerManager
			PlayerManager newPlayerManager = _instantiatedCharacter.GetComponent<PlayerManager>();
			if (newPlayerManager != null)
			{
				PP.GameManager.Instance.playerManager = newPlayerManager;
			}
			else
			{
				Debug.LogWarning("[FormSwitchPoint] 新角色没有 PlayerManager 组件，请检查 Prefab 配置。", this);
			}

			// 播放 VFX
			if (_instantiatedVFX != null)
			{
				_instantiatedVFX.gameObject.SetActive(true);
				_instantiatedVFX.transform.position = _instantiatedCharacter.transform.position;
				_instantiatedVFX.Play();
			}

			// 触发切换事件（通知摄像机等）
			MMEventManager.TriggerEvent(_switchEvent);

			yield return null;

			// 传递生命值
			LevelManager.Instance.Players[PlayerIndex].gameObject.MMGetComponentNoAlloc<Health>().SetHealth(newHealth, this.gameObject);

			// 传递行走速度和跳跃高度（根据 SyncSpeedAndJump 开关决定是否同步）
			if (SyncSpeedAndJump)
			{
				CharacterHorizontalMovement newHorizontalMovement = LevelManager.Instance.Players[PlayerIndex].gameObject.MMGetComponentNoAlloc<CharacterHorizontalMovement>();
				if (newHorizontalMovement != null)
				{
					newHorizontalMovement.WalkSpeed = oldWalkSpeed;
				}

				CharacterJump newCharacterJump = LevelManager.Instance.Players[PlayerIndex].gameObject.MMGetComponentNoAlloc<CharacterJump>();
				if (newCharacterJump != null)
				{
					newCharacterJump.JumpHeight = oldJumpHeight;
				}
			}

			// 传递 AI 控制持续时间
			PlayerManager newPM = LevelManager.Instance.Players[PlayerIndex].gameObject.GetComponent<PlayerManager>();
			if (newPM != null)
			{
				newPM.aiDuration = oldAiDuration;
			}

			// 传递 AI 犹豫时间范围
			AIActionPPIdle newAIActionIdle = LevelManager.Instance.Players[PlayerIndex].gameObject.GetComponent<AIActionPPIdle>();
			if (newAIActionIdle != null)
			{
				newAIActionIdle.MinHesitateDuration = oldMinHesitateDuration;
				newAIActionIdle.MaxHesitateDuration = oldMaxHesitateDuration;
			}

			// 保持面朝方向
			Character.FacingDirections facingDirection = facingRight ? Character.FacingDirections.Right : Character.FacingDirections.Left;
			LevelManager.Instance.Players[PlayerIndex].Face(facingDirection);

			// 如果是一次性的，切换后禁用自己
			if (DisableAfterSwitch)
			{
				this.enabled = false;
				PlayerInRange = false;
			}
		}

		/// <summary>
		/// 在场景中绘制 Gizmos 辅助线
		/// </summary>
		protected virtual void OnDrawGizmos()
		{
			// 绘制一个图标表示这是一个形态切换点
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(this.transform.position, 0.5f);

			// 如果有目标 Prefab，画一条线指向它
			if (TargetCharacterPrefab != null)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawLine(this.transform.position, this.transform.position + Vector3.up * 1.5f);
			}
		}
	}
}
