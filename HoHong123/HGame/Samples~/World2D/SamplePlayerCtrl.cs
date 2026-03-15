#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * SWUInputAction의 Player 액션 맵을 사용하는 2D 사이드스크롤 플레이어 이동 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 현재 Player 액션 맵에는 Jump 액션과 Space 바인딩이 없습니다.
 * 2. 따라서 W / UpArrow의 Move.y 입력을 점프로 해석합니다.
 * 3. 진짜 점프 입력 분리를 원하면 InputActions에 Jump 액션을 추가해야 합니다.
 * =========================================================
 */
#endif

using UnityEngine;
using UnityEngine.Assertions;
using HUtil.Inspector;

namespace SWU.Player {
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class SamplePlayerCtrl : MonoBehaviour {
        #region Fields
        [HTitle("Physics")]
        [SerializeField]
        Rigidbody2D rigidBody;
        [SerializeField]
        Transform groundCheck;
        [SerializeField]
        LayerMask groundLayer;
        [SerializeField]
        float groundCheckRadius = 0.15f;

        [HTitle("Movement")]
        [SerializeField]
        float moveSpeed = 5f;
        [SerializeField]
        float jumpForce = 10f;
        [SerializeField]
        bool allowAirControl = true;

        SWUInputAction input;
        Vector2 moveInput;
        bool isGrounded;
        bool wasJumpHeld;
        bool jumpQueued;
        #endregion

        #region Properties
        public Vector2 MoveInput => moveInput;
        public bool IsGrounded => isGrounded;
        #endregion

        #region Getter / Setter
        public void SetMoveSpeed(float value) => moveSpeed = value;
        public void SetJumpForce(float value) => jumpForce = value;
        #endregion

        #region Initialization
        private void Reset() {
            rigidBody = GetComponent<Rigidbody2D>();
        }
        #endregion

        #region Unity Life Cycle
        private void Awake() {
            Assert.IsNotNull(rigidBody, $"[{nameof(SamplePlayerCtrl)}] Rigidbody2D is required.");
            Assert.IsNotNull(groundCheck, $"[{nameof(SamplePlayerCtrl)}] GroundCheck is required.");

            input = new SWUInputAction();
        }

        private void OnEnable() {
            Assert.IsNotNull(input, $"[{nameof(SamplePlayerCtrl)}] InputAction is required.");
            input.Player.Enable();
        }

        private void OnDisable() {
            if (input == null) return;
            input.Player.Disable();
        }

        private void Update() {
            _UpdateGrounded();
            _UpdateMoveInput();
            _UpdateJumpInput();
        }

        private void FixedUpdate() {
            _ApplyMove();
            _ApplyJump();
        }
        #endregion

        #region === Move Feature ===
        #region Private - Move
        private void _UpdateMoveInput() {
            moveInput = input.Player.Move.ReadValue<Vector2>();
        }

        private void _ApplyMove() {
            if (!isGrounded && !allowAirControl) return;
            rigidBody.linearVelocityX = moveInput.x * moveSpeed;
        }
        #endregion
        #endregion

        #region === Jump Feature ===
        #region Private - Jump
        private void _UpdateJumpInput() {
            bool isJumpHeld = moveInput.y > 0.5f;

            if (isJumpHeld && !wasJumpHeld) {
                jumpQueued = true;
            }

            wasJumpHeld = isJumpHeld;
        }

        private void _ApplyJump() {
            if (!jumpQueued) return;

            jumpQueued = false;

            if (!isGrounded) return;

            rigidBody.linearVelocityY = 0;

            rigidBody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        #endregion
        #endregion

        #region === Ground Feature ===
        #region Private - Ground
        private void _UpdateGrounded() {
            Assert.IsNotNull(groundCheck, $"[{nameof(SamplePlayerCtrl)}] GroundCheck is required.");
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        #endregion
        #endregion

#if UNITY_EDITOR
        #region === Debug Feature ===
        #region Private - Debug
        private void OnDrawGizmosSelected() {
            if (groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        #endregion
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. Player 액션 맵의 Move(Vector2)를 사용한 좌우 이동입니다.
 * 2. Move.y > 0 입력을 점프로 해석합니다.
 * 3. GroundCheck를 통한 바닥 판정 후 점프를 수행합니다.
 * 
 * 사용법 ::
 * 1. Player 오브젝트에 Rigidbody2D와 Collider2D를 추가합니다.
 * 2. 발 아래 빈 오브젝트를 만들고 groundCheck에 연결합니다.
 * 3. 바닥 오브젝트를 Ground 레이어로 지정하고 groundLayer에 연결합니다.
 * 4. SWUInputAction.inputactions가 Generate C# Class 되어 있어야 합니다.
 * 
 * 변수 설명 ::
 * rigidBody : 플레이어 물리 이동에 사용되는 Rigidbody2D
 * groundCheck : 바닥 판정 위치
 * groundLayer : 바닥으로 간주할 레이어
 * moveSpeed : 좌우 이동 속도
 * jumpForce : 점프 힘
 * groundCheckRadius : 바닥 판정 반경
 * allowAirControl : 공중에서 좌우 제어 허용 여부
 * 
 * 이벤트 ::
 * 없음
 * 
 * 기타 ::
 * 1. 현재 Player 액션 맵에는 Space 점프가 없습니다.
 * 2. Space 점프가 필요하면 InputActions 수정이 반드시 필요합니다.
 * =========================================================
 */
#endif