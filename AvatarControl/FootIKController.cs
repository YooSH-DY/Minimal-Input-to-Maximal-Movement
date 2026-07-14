using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class FootGlideController : MonoBehaviour
{
    private Animator animator;

    private enum Dance { Walking, Runningman, Spongebob }
    private Dance lastDance = Dance.Runningman;

    private float liftYVelocity = 0f;
    private float footGlideOffsetZ = 0f;
    private float footGlideVelocity = 0f;
    private Vector3 leftHintVel = Vector3.zero;
    private Vector3 rightHintVel = Vector3.zero;
    private Vector3 smoothLeftFootTarget;
    private Vector3 smoothRightFootTarget;

    [Header("Speed Control (속도 제어)")]
    [Range(-2.0f, 2.0f)] public float animationSpeed = 1f; // 애니메이션 속도 제어 (음수=역재생)
    [Range(0.1f, 3.0f)] public float movementSpeed = 2f;  // 이동 속도 제어
    
    // 역재생 관련 변수
    private bool isReversePlayback = false;
    private float reverseTime = 0f;
    
    [Header("Movement Settings")]
    public float spongebobMoveFactor = 0.5f; // 스폰지밥 동작 시 이동 속도 배율 (0.5 = 50% 속도)
    public float turnSpeed = 100f;
    
    [Header(" Spongebob Kick Out Settings (NEW IK System)")]
    [Tooltip("킥아웃 모드를 활성화/비활성화 합니다. K키로도 토글 가능")]
    public bool isKickOutMode = false; // 킥아웃 모드 활성화
    
    [Header("📏 킥 거리 및 높이 설정")]
    [Range(0f, 1.5f)] [Tooltip("발이 위로 올라가는 높이를 조절합니다 (0=바닥, 1.5=높게)")]
    public float kickOutHeight = 0.3f; // 킥아웃 발 높이
    
    [Range(0f, 2f)] [Tooltip("발이 좌우로 뻗어나가는 거리를 조절합니다 (0=제자리, 2=멀리)")]
    public float kickOutDistance = 0.8f; // 킥아웃 발 거리 
    
    [Range(-45f, 45f)] [Tooltip("킥 각도를 조절합니다. 음수=아래쪽, 양수=위쪽 각도")]
    public float kickOutAngle = 0f; // 킥 각도 (새로 추가)
    
    [Range(1f, 10f)] [Tooltip("발차기 뻗는 속도/강도를 조절합니다 (1=부드럽게, 10=매우빠르게)")]
    public float kickBurstIntensity = 4f; // 킥 버스트 강도
    
    [Header("⚡ 킥 속도 및 타이밍")]
    [Range(0.1f, 3f)] [Tooltip("킥 애니메이션의 속도를 조절합니다 (0.1=매우느림, 3=매우빠름)")]
    public float kickOutSpeed = 1.5f; // 킥아웃 속도
    
    [Range(0.5f, 3f)] [Tooltip("애니메이션과 킥의 동기화 비율을 조절합니다. 1=애니메이션과 동일, 2=2배 빠름")]
    public float kickOutPulseRate = 1.5f; // 킥아웃 펄스 주기 (애니메이션 동기화)
    
    [Header("🔄 킥 타이밍 설정 (%)")]
    [Range(5f, 20f)] [Tooltip("한 번의 킥이 지속되는 시간 (%")]
    public float kickDuration = 10f; // 킥 지속 시간 (기본 10%)
    
    [Range(5f, 20f)] [Tooltip("킥과 킥 사이의 휴식 시간 (%")]
    public float kickRestTime = 10f; // 킥 간 휴식 시간 (기본 10%)
    
    [Range(0f, 20f)] [Tooltip("사이클과 사이클 사이의 추가 휴식 시간 (%")]
    public float cycleBreakTime = 8f; // 사이클 간 휴식 시간 (기본 8%)
    
    [Header("🐰 Spongebob Hop Settings (Bunny Jump Mode)")]
    [Tooltip("토끼처럼 통통 튀는 Hop 모드를 활성화합니다. H키로도 토글 가능")]
    public bool isHopMode = false; // Hop 모드 활성화
    
    [Header("📐 Hop 높이 및 스타일 설정")]
    [Range(0f, 1.5f)] [Tooltip("양발이 함께 위로 튀는 높이를 조절합니다 (0=바닥, 1.5=높게)")]
    public float hopHeight = 0.5f; // Hop 높이
    
    [Range(0f, 0.3f)] [Tooltip("Hop할 때 발이 안쪽으로 모이는 정도 (토끼 자세) - 너무 크면 발이 관통할 수 있음")]
    public float hopFootGather = 0.1f; // 발 모으기 (기본값 낮춤)
    
    [Range(0f, 0.3f)] [Tooltip("Hop할 때 약간 앞으로 기울어지는 정도 (점프 자세)")]
    public float hopForwardLean = 0.1f; // 앞기울임
    
    [Header("⏱️ Hop 타이밍 및 강도")]
    [Range(0.5f, 3f)] [Tooltip("Hop 주기를 조절합니다. 작을수록 빠른 주기로 반복")]
    public float hopPulseRate = 1.2f; // Hop 펄스 주기
    
    [Header("🔄 Hop 사이클 타이밍 설정 (%)")]
    [Range(5f, 20f)] [Tooltip("한 번의 Hop이 지속되는 시간 (%)")]
    public float hopDuration = 10f; // Hop 지속 시간 (기본 10%)
    
    [Range(5f, 20f)] [Tooltip("Hop과 Hop 사이의 휴식 시간 (%)")]
    public float hopRestTime = 10f; // Hop 간 휴식 시간 (기본 10%)
    
    [Range(0f, 20f)] [Tooltip("사이클과 사이클 사이의 추가 휴식 시간 (%)")]
    public float hopCycleBreakTime = 8f; // Hop 사이클 간 휴식 시간 (기본 8%)
    
    [Range(1f, 8f)] [Tooltip("Hop 강도/높이 배율을 조절합니다 (1=부드럽게, 8=강하게)")]
    public float hopIntensity = 4f; // Hop 강도
    
    [Header("🐰2 Spongebob Hop2 Settings (Bunny Jump Mode 2)")]
    [Tooltip("토끼처럼 통통 튀는 Hop2 모드를 활성화합니다. J키로도 토글 가능")]
    public bool isHop2Mode = false; // Hop2 모드 활성화
    
    [Header("📐 Hop2 높이 및 스타일 설정")]
    [Range(0f, 1.5f)] [Tooltip("양발이 함께 위로 튀는 높이를 조절합니다 (0=바닥, 1.5=높게)")]
    public float hop2Height = 1.5f; // Hop2 높이 (더 눈에 띄게 증가)
    
    [Range(0f, 0.3f)] [Tooltip("Hop2할 때 발이 안쪽으로 모이는 정도 (토끼 자세) - 너무 크면 발이 관통할 수 있음")]
    public float hop2FootGather = 0.1f; // 발 모으기 (기본값 낮춤)
    
    [Range(0f, 0.3f)] [Tooltip("Hop2할 때 약간 앞으로 기울어지는 정도 (점프 자세)")]
    public float hop2ForwardLean = 0.1f; // 앞기울임
    
    [Header("⏱️ Hop2 타이밍 및 강도")]
    [Range(0.5f, 3f)] [Tooltip("Hop2 주기를 조절합니다. 작을수록 빠른 주기로 반복")]
    public float hop2PulseRate = 1.2f; // Hop2 펄스 주기
    
    [Header("🔄 Hop2 사이클 타이밍 설정 (%)")]
    [Range(5f, 20f)] [Tooltip("한 번의 Hop2이 지속되는 시간 (%)")]
    public float hop2Duration = 10f; // Hop2 지속 시간 (기본 10%)
    
    [Range(5f, 20f)] [Tooltip("Hop2과 Hop2 사이의 휴식 시간 (%)")]
    public float hop2RestTime = 10f; // Hop2 간 휴식 시간 (기본 10%)
    
    [Range(0f, 20f)] [Tooltip("사이클과 사이클 사이의 추가 휴식 시간 (%)")]
    public float hop2CycleBreakTime = 8f; // Hop2 사이클 간 휴식 시간 (기본 8%)
    
    [Range(1f, 8f)] [Tooltip("Hop2 강도/높이 배율을 조절합니다 (1=부드럽게, 8=강하게)")]
    public float hop2Intensity = 8f; // Hop2 강도 (더 강하게)
    
    [Header("🦵 Spongebob Shuffle Forward Cross Settings")]
    [Tooltip("스폰지밥 셔플 포워드 크로스 모드를 활성화합니다. F키로도 토글 가능")]
    public bool isShuffleCrossMode = false; // 셔플 크로스 모드 활성화
    
    [Header("📐 크로스 발차기 거리 및 높이 설정")]
    [Range(0f, 2f)] [Tooltip("발이 대각선 앞으로 뻗어나가는 거리를 조절합니다 (0=제자리, 2=멀리)")]
    public float crossDistance = 0.8f; // 크로스 발차기 거리 (기본값 조정)
    
    [Range(0f, 1.5f)] [Tooltip("발이 위로 올라가는 높이를 조절합니다 (0=바닥, 1.5=높게)")]
    public float crossHeight = 0.3f; // 크로스 발차기 높이 (기본값 조정)
    
    [Range(-45f, 45f)] [Tooltip("크로스 발차기 각도를 조절합니다. 음수=아래쪽, 양수=위쪽 각도")]
    public float crossAngle = 10f; // 크로스 발차기 각도 (기본값 10도 - 자연스러운 대각선)
    
    [Header("⚡ 크로스 발차기 속도 및 타이밍")]
    [Range(0.5f, 3f)] [Tooltip("크로스 발차기 주기를 조절합니다. 작을수록 빠른 주기로 반복")]
    public float crossPulseRate = 1.0f; // 크로스 발차기 펄스 주기
    
    [Header("🔄 크로스 발차기 사이클 타이밍 설정 (%)")]
    [Range(5f, 20f)] [Tooltip("한 번의 크로스 발차기가 지속되는 시간 (%)")]
    public float crossDuration = 12f; // 크로스 발차기 지속 시간 (기본 12%)
    
    [Range(5f, 20f)] [Tooltip("크로스 발차기와 발차기 사이의 휴식 시간 (%)")]
    public float crossRestTime = 8f; // 크로스 발차기 간 휴식 시간 (기본 8%)
    
    [Range(0f, 20f)] [Tooltip("사이클과 사이클 사이의 추가 휴식 시간 (%)")]
    public float crossCycleBreakTime = 10f; // 크로스 발차기 사이클 간 휴식 시간 (기본 10%)
    
    [Range(1f, 8f)] [Tooltip("크로스 발차기 강도/속도를 조절합니다 (1=부드럽게, 8=강하게)")]
    public float crossIntensity = 5f; // 크로스 발차기 강도
    
    // 킥아웃 IK 제어 변수
    private float kickOutIntensity = 0f; // 현재 킥아웃 강도 (0~1)
    private float leftKickIntensity = 0f; // 왼발 킥아웃 강도
    private float rightKickIntensity = 0f; // 오른발 킥아웃 강도
    private Vector3 leftFootKickOffset = Vector3.zero;
    private Vector3 rightFootKickOffset = Vector3.zero;
    private float kickStartTime = 0f; // 킥 시작 시간
    
    // Hop IK 제어 변수
    private float hopCurrentIntensity = 0f; // 현재 Hop 강도 (0~1)
    private Vector3 leftFootHopOffset = Vector3.zero;
    private Vector3 rightFootHopOffset = Vector3.zero;
    
    // Hop2 IK 제어 변수
    private float hop2CurrentIntensity = 0f; // 현재 Hop2 강도 (0~1)
    private Vector3 leftFootHop2Offset = Vector3.zero;
    private Vector3 rightFootHop2Offset = Vector3.zero;
    
    // 크로스 발차기 IK 제어 변수
    private float crossCurrentIntensity = 0f; // 현재 크로스 발차기 강도 (0~1)
    private float leftCrossIntensity = 0f; // 왼발 크로스 강도
    private float rightCrossIntensity = 0f; // 오른발 크로스 강도
    private Vector3 leftFootCrossOffset = Vector3.zero;
    private Vector3 rightFootCrossOffset = Vector3.zero;
    
    // === 모드 전환 대기 시스템 ===
    private bool isPendingModeSwitch = false; // 모드 전환 대기 중
    private int pendingISValue = -1; // 대기 중인 IS 값
    private bool isCurrentCycleCompleting = false; // 현재 사이클 완료 중
    
    // === 몸통 Y축 위치 관리 ===
    private float currentBodyY = 0f; // 현재 몸통 Y축 위치
    private float targetBodyY = 0f; // 목표 몸통 Y축 위치
    private float bodyYTransitionSpeed = 8f; // 몸통 Y축 전환 속도
    
    [Header("Foot IK Targets")]
    public Transform leftFootTarget;
    public Transform rightFootTarget;

    [Header("Manual Control Parameters")]
    [Range(-1f, 1f)] public float spacingRatio = 0f;
    [Range(-1f, 1f)] public float depthRatio = 0f;
    [Range(-30f, 30f)] public float leanAngle = 0f;

    [Header("QX 통합모드 설정 (V키로 토글)")]
    // qxRange 관련 변수들 제거됨 - 방향전환 기능 없음

    [Header("Speed Control Settings (공통 속도 설정)")]
    [Range(0.5f, 1.0f)] public float minSpeedMultiplier = 0.7f; // 최소 속도 배율
    [Range(1.0f, 2.0f)] public float maxSpeedMultiplier = 1.3f; // 최대 속도 배율
    
    [Header("Body Lean Parameters (상체 굽히기)")]
    [Range(-0.5f, 1f)] public float bodyLeanAmount = 0f;  // A값/qX로 제어되는 상체 굽히기 강도 (-0.5: 뒤로 젖히기, 0: 중립, 1: 가슴 숙이기)
    [Range(0.1f, 5f)] public float bodyLeanEase = 2f;
    [Range(0f, 90f)] public float maxBodyLeanForwardAngle = 45f;   // 최대 앞쪽 굽히기 각도 (가슴 숙이기)
    [Range(0f, 90f)] public float maxBodyLeanBackAngle = 30f;     // 최대 뒤쪽 젖히기 각도
    
    [Header("Legacy Glide Parameters (발 글라이딩 - 비활성화됨)")]
    [Range(0f, 0.6f)] public float glideAmount = 0f;  // 더 이상 사용되지 않음 (상체 굽히기로 대체)
    [Range(0.1f, 5f)] public float glideEase = 2f;
    public float maxLiftY = 0.12f; // 기존 0.15f에서 0.12f로 더욱 감소 (발 떠있는 느낌 완전 최소화)
    public float maxKneeLiftZ = 0.25f;  // 0.2f에서 0.25f로 증가 - 무릎 굽힘 효과 가시성 강화
    public float maxFootGlideBackZ = -0.15f;
    // glideMaxThreshold를 0.6f에서 0.58f로 약간 줄여서 최대값에서도 애니메이션이 계속되도록 함
    private const float glideMaxThreshold = 0.58f;

    [Header("UI Sliders (0~1)")]
    public Slider blendSlider;
    public Slider bodyLeanSlider;  // 기존 glideSlider에서 변경
    public Slider depthSlider;
    public Slider speedSlider; // 속도 조절용 슬라이더 추가



    [Header("Jump Settings")]
    public Rigidbody rb; // 점프용 Rigidbody
    public bool isJumpInProgress = false; // 점프 중임을 나타내는 플래그
    
    [Header("Jump Charging")]
    public float jumpHeightMin = 2.0f;   // 최소 점프 높이 (0.8f에서 상향 조정)
    public float jumpHeightMax = 5.0f;   // 최대 점프 높이를 5.0m
    public float maxJumpChargeTime = 1.5f; // 최대 충전 시간 (초)
    private float jumpChargeTimer = 0f;    // 현재 점프 충전 시간
    
    [Header("Jump Crouch Settings")]
    public float maxCrouchDepth = 0.3f;    // 최대 웅크림 깊이 (무릎 높이 감소량)
    public float crouchResponseSpeed = 5f; // 웅크림 반응 속도
    public float crouchSmoothTime = 0.2f;  // 웅크림 부드러운 전환 시간
    public float maxCrouchChargeTime = 0.5f; // 최대 웅크림 충전 시간 이거 조절해서 웅크리는속도 조절가능
    private float currentCrouchDepth = 0f;  // 현재 웅크림 깊이
    private float targetCrouchDepth = 0f;   // 목표 웅크림 깊이
    private float crouchVelocity = 0f;      // 웅크림 속도 (SmoothDamp용)
    private float crouchChargeTimer = 0f;   // 현재 웅크림 충전 시간
    
    // 점프 상태 관리 열거형
    private enum JumpState
    {
        None,           // 점프 상태 아님
        WaitingForDown, // Base 상태에서 대기 중 (아래로 기울임 대기)
        WaitingForUp,   // Ready 상태에서 대기 중 (위로 기울임 대기)
        Jumping,        // Jumping 상태 (도약)
        JumpAir,        // JumpAir 상태 (체공)
        JumpingDown,    // JumpingDown 상태 (착지)
        Completing      // 점프 완료 중
    }
    
    // 점프 관련 변수들
    private JumpState jumpState = JumpState.None;
    private bool isKeyboardJump = false;
    private float currentJumpHeight = 1.5f; // 기본 점프 높이를 1.5m로 감소
    private bool wasBackwalkingBeforeJump = false;
    
    // 애니메이션 제어 상수
    private const float JUMP_PREP_FRAME = 11.0f;
    private const float JUMP_TOTAL_FRAMES = 55.0f;
    private const float JUMP_PREP_NORMALIZED = JUMP_PREP_FRAME / JUMP_TOTAL_FRAMES;
    
    // 롤(Roll) 각도 관련 변수들
    private float currentRoll = 0f;
    private float minRoll = 0f;  // 점프 준비 중 저장된 최소 롤 값
    private float maxRoll = 0f;  // 점프 실행 시 저장된 최대 롤 값
    private bool isWaitingForRollUp = false;
    private bool isCollectingMaxRoll = false;
    private float maxRollCollectStartTime = 0f;
    private const float maxRollCollectDuration = 0.2f;
    
    // 롤 각도 임계값 설정
    private const float ROLL_DOWN_THRESHOLD = -10f;  // 웅크림 시작 임계값
    private const float ROLL_UP_THRESHOLD = 10f;     // 점프 시작 임계값
    
    // 점프 높이 설정 관련 변수
    private const float MIN_JUMP_HEIGHT = 1.0f;
    private const float MAX_JUMP_HEIGHT = 6.0f;
    private const float MIN_ROLL_SUM = 20f;
    private const float MAX_ROLL_SUM = 160f;
    private float calculatedJumpHeight = 0f;
    
    // 점프 상태 플래그들
    private bool jumpBase = false;
    private bool jumpReady = false;
    private bool jumping = false;
    private bool jumpDown = false;
    
    // 애니메이션 잠금 관련 변수
    private bool isAnimationLocked = false;
    private float animationLockEndTime = 0f;
    private string currentAnimationName = "";
    private float readyAnimStartTime = 0f;
    
    // 자이로 값 관련 변수
    private float currentGyroY = 0f;
    private float lowestGyroY = 0f;
    private const float GYRO_DOWN_THRESHOLD = -60f;
    private const float GYRO_UP_THRESHOLD = 60f;
    private const float AUTO_ADVANCE_TIME = 0.3f;
    
    // 내부 연산용
    private float footSpacingX, footSpacingZ, liftY;
    private Transform spine;
    private Quaternion spineInitLocalRot;
    
    // 상체 굽히기 시스템 변수들
    private float currentBodyLeanAngle = 0f;  // 현재 상체 굽히기 각도
    private float bodyLeanVelocity = 0f;     // 상체 굽히기 보간 속도

    private float currentQy = 0f;
    private float currentQz = 0f;
    private float currentQx = 0f;
    private float targetYRotation;
    private float initialYRotation;
    
    // 방향 고정 시스템을 위한 변수들
    private bool isDirectionLocked = false;
    private float lockedDirection;
    private float maxReachedAngle = 0f;   // 현재 회전에서 도달한 최고 각도
    private float angleHoldTime = 0f;     // 현재 각도를 유지한 시간
    private float currentAngle = 0f;      // 현재 계산된 각도
    private bool isRotating = false;      // 현재 회전 중인지 여부
    private const float HOLD_TIME_THRESHOLD = 1f; // 1초
    
    // 스핀 제어를 위한 변수들 (qx 기반 제거)
    private bool spinTriggered = false;   // 스핀이 이미 트리거되었는지 확인
    
    // 방향전환 관련 변수들 제거됨
    
    // Spin 상태 모니터링을 위한 변수들
    private bool wasInSpin = false;       // 이전 프레임에 Spin 상태였는지
    private bool spinCompleted = false;   // 스핀 완료 처리 여부
    
    // 애니메이션 일시정지/재생을 위한 변수
    private bool isPaused = true;         // 시작 시 일시정지 상태로 시작
    
    // 다른 스크립트에서 일시정지 상태를 확인할 수 있는 정적 속성
    public static bool IsPaused { get; private set; } = true;

    private int lastMValue = -1;  // 이전 m 값을 저장 (초기값 -1로 설정)
    private int currentAValue = 120;  // 현재 a 값 (초기값 120으로 설정하여 glideAmount가 0부터 시작)
    private int currentDValue = 50;   // 현재 d 값 (초기값 50으로 설정)
    private int currentIBValue = 50;  // 현재 IB 값 (Inner Bend - 내측 굽힘)
    private int currentFBValue = 50;  // 현재 FB 값 (Forward Bend - 전방 굽힘)
    private int currentISValue = 50;  // 현재 IS 값 (Inner Stretch - 내측 스트레치)
    private int lastFBValue = -1;  // 이전 FB 값을 저장 (초기값 -1로 설정)
    
    // === DOT 센서 기반 통합 제어 시스템 ===
    [Header(" DOT 센서 기반 통합 제어")]
    
    // 가속도 기반 움직임 강도 제어
    [Tooltip("가속도 감지 민감도")]
    [Range(0.1f, 5f)] public float accelerationSensitivity = 1f;
    
    [Tooltip("가속도 → 인텐시티/버스트 매핑 강도")]
    [Range(0.1f, 3f)] public float accelerationToIntensityMultiplier = 1f;
    
    // qx 기반 다리 높이 제어
    [Tooltip("qx 기반 다리 높이 조절 강도")]
    [Range(0f, 2f)] public float qxHeightMultiplier = 1f;
    
    // 가속도 계산을 위한 변수들
    private Vector3 lastVelocity = Vector3.zero;
    private Vector3 currentAcceleration = Vector3.zero;
    private float currentAccelerationMagnitude = 0f;
    
    // qx 기반 높이 계산 결과
    private float currentQxHeightMultiplier = 1f;

    // FB 전용 회전 변수들
    private bool isFBRotating = false;  // FB 회전 진행 여부
    private float fbTargetYRotation = 0f;  // FB 회전 목표 각도
    private float lastFBRotationTime = 0f;  // 마지막 FB 회전 완료 시간

    // 웹소켓 연결 상태 관리
    private bool isWebSocketConnected = false;
    private bool isWebSocketDataEnabled = false;  // 스페이스바로 활성화되는 웹소켓 데이터 반영 여부
    private bool isQxIntegratedMode = false;      // V키로 토글되는 qX 통합 제어 모드
    
    // 키보드로 설정한 값들 저장 (웹소켓 연결 해제 시 유지용)
    private float keyboardSpacingRatio = 0f;
    private float keyboardDepthRatio = 0f;
    private float keyboardBodyLeanAmount = 0f;  // Z/X 키로 제어되는 상체 굽히기/젖히기 (-0.5~1: 뒤로젖히기~가슴숙이기)
    private float keyboardQz = 0f;  // 키보드로 조작되는 qz 값
    
    // 개별 키 프레스 감지를 위한 변수들
    private bool lastAKeyState = false;
    private bool lastDKeyState = false;
    private bool lastQKeyState = false;
    private bool lastEKeyState = false;
    private bool lastUpArrowState = false;
    private bool lastDownArrowState = false;
    private bool lastLeftArrowState = false;
    private bool lastRightArrowState = false;
    private float maxReachedCrouchDepth = 0f; // 현재 점프에서 도달한 최대 웅크림 깊이
    private bool isCrouchDepthLocked = false; // 웅크림 깊이 고정 여부
    private float lastInspectorAnimationSpeed = 1f; // 이전 프레임의 인스펙터 animationSpeed 값 저장
    // 웅크림 상태 관리를 위한 변수 추가
    private bool isCrouchingMaintained = false; // 웅크림 자세 유지 여부
    public GameObject footTrailObject; // 인스펙터에서 Trail Renderer가 붙은 GameObject를 할당
    void Awake()
    {
        animator = GetComponent<Animator>();
        if (leftFootTarget != null)
            smoothLeftFootTarget = leftFootTarget.position;
        if (rightFootTarget != null)
            smoothRightFootTarget = rightFootTarget.position;

        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        if (spine != null)
            spineInitLocalRot = spine.localRotation;

        if (blendSlider != null)
            blendSlider.onValueChanged.AddListener(v => animator.SetFloat("Blend", v));
        if (bodyLeanSlider != null)
            bodyLeanSlider.onValueChanged.AddListener(v => {
                // UI 슬라이더 값 0~1을 그대로 사용 (0=중립, 1=최대 가슴 숙이기)
                keyboardBodyLeanAmount = v;  // 키보드 값에 저장하여 WebSocket 해제 시에도 유지
                if (!isWebSocketConnected) {
                    bodyLeanAmount = v;  // WebSocket 연결 해제 시에만 즉시 적용
                }
                Debug.Log($"UI로 상체 굽히기 설정: {v:F2} (가슴 숙이기) - 각도: {v * maxBodyLeanForwardAngle:F1}도");
            });
        if (depthSlider != null)
            depthSlider.onValueChanged.AddListener(v => {
                depthRatio = Mathf.Lerp(-0.5f, 0.2f, v);
            });
        if (speedSlider != null)
            speedSlider.onValueChanged.AddListener(v => {
                // 0~1 슬라이더를 minSpeedMultiplier ~ maxSpeedMultiplier로 매핑 (애니메이션 속도)
                animationSpeed = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, v);
                animator.speed = animationSpeed;
                // 이동속도는 별도로 movementSpeed 변수로 제어
            });
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
        // 점프 높이 초기화
        currentJumpHeight = 1.5f; // 기본값으로 재설정
        DataTransBehavior.OnNewQy += v => currentQy = v; // qy 값을 직접 사용
        DataTransBehavior.OnNewQz += v => currentQz = v; // qz 값을 직접 사용
        DataTransBehavior.OnNewQx += HandleQxData;       // qx 값 처리
        DataTransBehavior.OnNewM += HandleMData;         // m 값 처리 (애니메이션 전환)
        DataTransBehavior.OnNewA += HandleAData;         // a 값 처리 (glideAmount 제어)
        DataTransBehavior.OnNewD += HandleDData;         // d 값 처리 (spacingRatio, depthRatio 제어)
        DataTransBehavior.OnNewIB += HandleIBData;       // IB 값 처리 (Inner Bend)
        DataTransBehavior.OnNewFB += HandleFBData;       // FB 값 처리 (Forward Bend)
        DataTransBehavior.OnNewIS += HandleISData;       // IS 값 처리 (Inner Stretch)
        
        // DOT 센서 데이터 이벤트 구독
        DataTransBehavior.OnNewAcceleration += HandleAccelerationData;  // 3축 가속도 통합
        DataTransBehavior.OnNewAccelX += HandleAccelXData;             // X축 가속도
        DataTransBehavior.OnNewAccelY += HandleAccelYData;             // Y축 가속도
        DataTransBehavior.OnNewAccelZ += HandleAccelZData;             // Z축 가속도
        
        // WebSocket 연결 상태 변경 이벤트 구독
        DataTransBehavior.OnConnectionStateChanged += HandleConnectionStateChanged;

        // 씬에 여러 개의 오디오 리스너가 있을 경우, 이들을 찾아 하나만 남기고 비활성화하여 경고를 제거합니다.
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners.Length > 1)
        {
            // 사용자의 요청에 따라 경고 로그가 출력되지 않도록 주석 처리합니다.
            // Debug.LogWarning($"[AudioListener] 씬에 {listeners.Length}개의 오디오 리스너가 있습니다. 하나만 남기고 비활성화합니다.");
            for (int i = 1; i < listeners.Length; i++)
            {
                listeners[i].enabled = false;
            }
        }
    }

    void Start()
    {
        // 시작 시 Rigidbody를 Kinematic으로 설정하여 물리/애니메이션 충돌로 인한 떨림 현상을 방지합니다.
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // 애니메이터 파라미터 존재 확인
        //Debug.Log("=== 애니메이터 파라미터 확인 ===");
        var parameters = animator.parameters;
        // foreach (var param in parameters)
        // {
        //     Debug.Log($"파라미터: {param.name}, 타입: {param.type}");
        // }
        
        // 현재 애니메이션 상태 정보 출력
        var currentState = animator.GetCurrentAnimatorStateInfo(0);
        //Debug.Log($"시작 시 애니메이션 상태: Hash={currentState.shortNameHash}, FullPathHash={currentState.fullPathHash}");
        
        // 실제 상태 이름들을 확인하기 위한 로그
        // Debug.Log($"상태 체크 - RunningmanRecentSensing_jin: {currentState.IsName("RunningmanRecentSensing_jin")}");
        // Debug.Log($"상태 체크 - SpongebobRecentsension_jin: {currentState.IsName("SpongebobRecentsension_jin")}");
        // Debug.Log($"상태 체크 - Spin: {currentState.IsName("Spin")}");
        // Debug.Log($"상태 체크 - walking: {currentState.IsName("walking")}");
        
        // 시작 시 애니메이션을 일시정지 상태로 설정
        animator.speed = 0f;
        
        // FB 기반 회전을 위한 초기 각도 설정
        initialYRotation = transform.eulerAngles.y;
        targetYRotation = initialYRotation;
        isPaused = true;
        IsPaused = true; // 정적 속성도 설정
        
        // 웹소켓 데이터는 비활성화 상태로 시작 (스페이스바로 활성화)
        isWebSocketDataEnabled = false;
        
        Debug.Log("=== 초기 상태 설정 완료 ===");
        Debug.Log("애니메이션이 일시정지된 상태로 시작합니다.");
        Debug.Log("스페이스바를 눌러 웹소켓 데이터를 활성화하고 애니메이션을 시작하세요.");
        Debug.Log("0키를 누르면 언제든지 완전 초기화할 수 있습니다.");
    }

    // FootIKController.cs

    void Update()
    {
        // === 모드 전환 대기 시스템 체크 ===
        if (isPendingModeSwitch && IsCurrentModeCycleComplete())
        {
            ExecutePendingModeSwitch();
        }
        
        // 대기 상태 디버그 (1초마다)
        if (isPendingModeSwitch && Time.time % 1f < Time.deltaTime)
        {
            Debug.Log($"⏳ 모드 전환 대기 중... 대상: IS={pendingISValue}, 현재사이클완료={IsCurrentModeCycleComplete()}");
        }
        
        // [수정] 점프 중이 아닐 때 지면에 있다면, Rigidbody를 Kinematic으로 강제 설정하여
        // 떨림 현상을 방지하고 안정적인 상태를 유지하는 안전장치를 추가합니다.
        if (!isJumpInProgress && IsGrounded())
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                Debug.Log("지면에서 비정상적인 물리 상태 감지. Kinematic으로 강제 전환하여 안정화합니다.");
            }
        }

        // Update() 메서드의 웅크림 처리 부분을 수정
        if (isJumpInProgress)
        {
            HandleJumpStateMachineInput();

            // === 웅크림 애니메이션 지속적 업데이트 강화 ===
            if ((jumpState == JumpState.WaitingForUp || (isJumpInProgress && targetCrouchDepth > 0f))
                && !jumping && jumpState != JumpState.Jumping && jumpState != JumpState.JumpAir)
            {
                // 부드러운 웅크림 전환 처리 (SmoothDamp 사용)
                currentCrouchDepth = Mathf.SmoothDamp(currentCrouchDepth, targetCrouchDepth, ref crouchVelocity, crouchSmoothTime);

                // 현재 웅크림 깊이를 기반으로 JumpPhase 계산
                float crouchRatio = currentCrouchDepth / maxCrouchDepth;
                float jumpPhase = Mathf.Lerp(0f, 1f, crouchRatio);

                // === 웹소켓 유지 모드일 때 강제 애니메이션 적용 ===
                if (isCrouchingMaintained || targetCrouchDepth > 0.01f)
                {
                    // 애니메이션에 강제로 적용 (매 프레임)
                    animator.SetFloat("JumpPhase", jumpPhase);

                    // Jump 애니메이션 상태 강제 유지
                    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"))
                    {
                        animator.Play("Jump", 0, 0);
                        Debug.Log("웹소켓 웅크림: Jump 애니메이션 강제 복구");
                    }
                }

                // 디버그 로그 (0.5초마다)
                if (Time.time % 0.5f < Time.deltaTime)
                {
                    Debug.Log($"[웅크림 지속] targetDepth={targetCrouchDepth:F3}, currentDepth={currentCrouchDepth:F3}, jumpPhase={jumpPhase:F3}, 유지모드={isCrouchingMaintained}");
                }
            }
            return;
        }

        // 인스펙터 슬라이더 조작 감지 및 적용 (Play 모드에서도 동작)
        if (Mathf.Abs(animationSpeed - lastInspectorAnimationSpeed) > 0.001f)
        {
            // 인스펙터에서 animationSpeed가 변경된 경우
            
            // 역재생 모드 체크
            bool wasReversePlayback = isReversePlayback;
            isReversePlayback = animationSpeed < 0;
            
            // 역재생 모드로 전환될 때 reverseTime 초기화
            if (isReversePlayback && !wasReversePlayback)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                reverseTime = stateInfo.normalizedTime % 1f; // 현재 재생 위치에서 시작
                if (reverseTime == 0f) reverseTime = 1f; // 0이면 1로 설정
            }
            
            // 인스펙터 조작 효과 적용
            if (!isPaused) {
                if (isReversePlayback) {
                    animator.speed = 0f; // 역재생 시 애니메이터 속도는 0으로 설정
                } else {
                    animator.speed = animationSpeed;
                }
            }

            // 값 변경 로깅
            Debug.Log($"인스펙터에서 속도 조절: 애니메이션 속도={animationSpeed:F2}, 이동 속도={movementSpeed:F2}, 역재생={isReversePlayback}");

            // 이전 인스펙터 값 저장
            lastInspectorAnimationSpeed = animationSpeed;
            
            // UI 슬라이더 값 동기화 (인스펙터 슬라이더와 UI 슬라이더 연동)
            if (speedSlider != null)
            {
                // 인스펙터의 animationSpeed가 변경되면 UI 슬라이더에 반영
                speedSlider.value = Mathf.InverseLerp(minSpeedMultiplier, maxSpeedMultiplier, animationSpeed);
            }
        }

        // 스페이스바로 웹소켓 데이터 활성화 및 애니메이션 일시정지/재생 전환
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 첫 스페이스바 입력 시 웹소켓 데이터 활성화
            if (!isWebSocketDataEnabled)
            {
                isWebSocketDataEnabled = true;
                Debug.Log("웹소켓 데이터 활성화! 이제 센서 데이터가 애니메이션에 반영됩니다.");
            }
            
            isPaused = !isPaused;
            IsPaused = isPaused; // 정적 속성 업데이트

            if (isPaused)
            {
                // 애니메이션 일시정지
                animator.speed = 0f;
                Debug.Log("애니메이션 일시정지");
            }
            else
            {
                // 애니메이션 재생 (현재 설정된 animationSpeed 값 사용)
                animator.speed = animationSpeed;
                Debug.Log("애니메이션 재생");
                
                // 일시정지 상태에서 받은 최신 데이터 적용
                ApplyCachedWebSocketData();
            }
        }
                
        // V키로 qX 통합 제어 모드 토글 (일시정지 상태에서도 동작)
        if (Input.GetKeyDown(KeyCode.V))
        {
            isQxIntegratedMode = !isQxIntegratedMode;
            if (isQxIntegratedMode)
            {
                Debug.Log("V키: QX 통합 제어 모드 ON - qX로 상체 굽히기/젖히기(-0.2~0.4)와 보폭(SpacingRatio/DepthRatio)을 통합 제어합니다");
                Debug.Log($"현재 설정: qxRange 관련 기능 제거됨");
            }
            else
            {
                Debug.Log("V키: QX 통합 제어 모드 OFF - A/D 데이터를 개별적으로 사용합니다 (기본 모드)");
            }
        }
        
        // QX 통합모드 설정 실시간 조절 (Shift + 숫자키)
        if (isQxIntegratedMode)
        {
            // Shift + 3/4: 범위 조절
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                                    Debug.Log($"QX 입력 범위 조절 기능 제거됨");
                }
                if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                                    Debug.Log($"QX 입력 범위 조절 기능 제거됨");
                }
                
                // Shift + 5/6: 전환 속도 조절
                if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    Debug.Log($"QX 전환 속도 조절 기능 제거됨");
                }
                if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    Debug.Log($"QX 전환 속도 조절 기능 제거됨");
                }
            }
        }
        
        // J키 입력으로 점프 준비 상태 진입
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (jumpState == JumpState.None && !isJumpInProgress)
            {
                StartJumpPreparation();
            }
        }

        // L키로 카메라 회전 (기존 기능 유지)
        if (Input.GetKey(KeyCode.L))
            transform.Rotate(0, turnSpeed * Time.deltaTime, 0);
        
        // K키로 킥아웃 모드 토글 (IS 모드 제어와 통합)
        if (Input.GetKeyDown(KeyCode.K))
        {
            isKickOutMode = !isKickOutMode;
            if (isKickOutMode)
            {
                isHopMode = false;
                isShuffleCrossMode = false;
                isHop2Mode = false; // Hop2 모드도 비활성화
                Debug.Log($"🦵 킥아웃 모드: {(isKickOutMode ? "ON" : "OFF")} (IS 모드 제어와 통합)");
            }
            else
            {
            Debug.Log($"🦵 킥아웃 모드: {(isKickOutMode ? "ON" : "OFF")}");
            }
        }
        
        // H키로 Hop 모드 토글 (IS 모드 제어와 통합)
        if (Input.GetKeyDown(KeyCode.H))
        {
            isHopMode = !isHopMode;
            if (isHopMode)
            {
                isKickOutMode = false;
                isShuffleCrossMode = false;
                isHop2Mode = false; // Hop2 모드도 비활성화
                Debug.Log($"🐰 Hop 모드: {(isHopMode ? "ON" : "OFF")} (IS 모드 제어와 통합)");
            }
            else
            {
                Debug.Log($"🐰 Hop 모드: {(isHopMode ? "ON" : "OFF")}");
            }
        }
        
        // F키로 셔플 크로스 모드 토글 (IS 모드 제어와 통합)
        if (Input.GetKeyDown(KeyCode.F))
        {
            isShuffleCrossMode = !isShuffleCrossMode;
            if (isShuffleCrossMode)
            {
                isKickOutMode = false;
                isHopMode = false;
                isHop2Mode = false; // Hop2 모드도 비활성화
                Debug.Log($"🦵 셔플 크로스 모드: {(isShuffleCrossMode ? "ON" : "OFF")} (IS 모드 제어와 통합)");
            }
            else
            {
                Debug.Log($"🦵 셔플 크로스 모드: {(isShuffleCrossMode ? "ON" : "OFF")}");
            }
        }
        
        // B키로 Hop2 모드 토글 (다른 모드와 상호 배타적)
        if (Input.GetKeyDown(KeyCode.B))
        {
            isHop2Mode = !isHop2Mode;
            if (isHop2Mode)
            {
                // 다른 모드들 비활성화
                isKickOutMode = false;
                isHopMode = false;
                isShuffleCrossMode = false;
                Debug.Log("🐰2 Hop2 모드 활성화! (다른 모드 비활성화)");
                Debug.Log($"🎯 Hop2 파라미터 확인 - Height:{hop2Height}, Intensity:{hop2Intensity}, PulseRate:{hop2PulseRate}");
            }
            else
            {
                Debug.Log("🐰2 Hop2 모드 비활성화!");
            }
        }

        // 현재 animator 상태
        var state = animator.GetCurrentAnimatorStateInfo(0);
        bool inWalking = state.IsName("walking");
        bool inRunningman = state.IsName("RunningmanRecentSensing_jin");
        bool inSpongebob = state.IsName("SpongebobRecentsension_jin");
        bool inSpin = state.IsName("Spin");

        // === DOT 센서 기반 통합 제어 시스템 업데이트 ===
        if (isWebSocketConnected && isWebSocketDataEnabled)
        {
            // qx 기반 높이 계산
            float qxHeight = CalculateQxBasedHeight();
            
            // 가속도 기반 움직임 강도 계산
            float accelerationIntensity = CalculateAccelerationBasedIntensity();
            
            // 가속도 기반 Kick Burst Intensity 계산 (3.0 ~ 8.0)
            kickBurstIntensity = CalculateKickBurstIntensity();
            
            // 🔥 Hop2 시스템 진입 직전 체크
            Debug.Log($"🎯 Hop2 시스템 진입 전 체크: isHop2Mode={isHop2Mode}");
            
            // === Hop2 시스템 (원래 Hop 시스템 완전 복사) ===
            if (isHop2Mode)
            {
                Debug.Log($"🔥 Hop2 시스템 진입! isHop2Mode={isHop2Mode}");
                
                // 애니메이션과 동기화된 Hop2 타이밍 계산 (원래 Hop과 동일)
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                float animNormalizedTime = stateInfo.normalizedTime % 1f; // 0~1 정규화된 애니메이션 시간
                
                // hop2PulseRate로 Hop2 주기 조절 (애니메이션 기반)
                float hop2Cycle = (animNormalizedTime * hop2PulseRate) % 1f; // 0~1 정규화
                
                // Hop2 강도 계산 (원래 Hop과 동일한 방식) - 각 발별 독립 제어
                float leftHop2Intensity = 0f;
                float rightHop2Intensity = 0f;
                
                // 백분율을 소수로 변환
                float hop2Dur = hop2Duration / 100f; // 예: 10% → 0.1
                float hop2Rest = hop2RestTime / 100f; // 예: 10% → 0.1
                float hop2Break = hop2CycleBreakTime / 100f; // 예: 8% → 0.08
                
                // 한 사이클 길이 계산: 왼발Hop2 + 휴식 + 오른발Hop2 + 휴식 + 사이클간휴식
                float oneHop2CycleLength = hop2Dur + hop2Rest + hop2Dur + hop2Rest + hop2Break;
                
                // 사이클 반복 계산 (원래 Hop과 동일한 방식)
                float repeatHop2Cycle = hop2Cycle % oneHop2CycleLength;
                
                // 각 구간의 경계점 계산
                float leftHop2End = hop2Dur; // 왼발Hop2 끝
                float firstRest2End = leftHop2End + hop2Rest; // 첫 휴식 끝
                float rightHop2End = firstRest2End + hop2Dur; // 오른발Hop2 끝
                float secondRest2End = rightHop2End + hop2Rest; // 두 번째 휴식 끝
                // 사이클 간 휴식: secondRest2End ~ oneHop2CycleLength
                
                // 왼발 Hop2 타이밍 (0 ~ hop2Duration% 구간)
                if (repeatHop2Cycle >= 0f && repeatHop2Cycle < leftHop2End)
                {
                    // 토끼 점프처럼 빠르게 올라갔다가 부드럽게 내려오는 Hop2 (포물선 모양)
                    float hop2Progress = repeatHop2Cycle / hop2Dur; // 0~1로 정규화
                    leftHop2Intensity = hop2Intensity * hop2Progress * (1f - hop2Progress); // 포물선 함수
                }
                
                // 오른발 Hop2 타이밍 (firstRest2End ~ rightHop2End 구간)
                if (repeatHop2Cycle >= firstRest2End && repeatHop2Cycle < rightHop2End)
                {
                    // 토끼 점프처럼 빠르게 올라갔다가 부드럽게 내려오는 Hop2 (포물선 모양)
                    float hop2Progress = (repeatHop2Cycle - firstRest2End) / hop2Dur; // 0~1로 정규화
                    rightHop2Intensity = hop2Intensity * hop2Progress * (1f - hop2Progress); // 포물선 함수
                }
                
                // 양발 Hop2 오프셋 (각 발별 독립 제어) - 토끼처럼 안쪽으로 모으면서 높이 점프
                leftFootHop2Offset = new Vector3(
                    hop2FootGather * leftHop2Intensity, // X축: 안쪽으로 모으기 (토끼 자세)
                    hop2Height * leftHop2Intensity, // Y축: 위로 높이 점프
                    hop2ForwardLean * leftHop2Intensity // Z축: 약간 앞으로 기울이기 (점프 자세)
                );
                
                rightFootHop2Offset = new Vector3(
                    -hop2FootGather * rightHop2Intensity, // X축: 안쪽으로 모으기 (토끼 자세)
                    hop2Height * rightHop2Intensity, // Y축: 위로 높이 점프
                    hop2ForwardLean * rightHop2Intensity // Z축: 약간 앞으로 기울이기 (점프 자세)
                );
                
                // 디버그 로그 (더 자주)
                if (Time.time % 0.5f < Time.deltaTime)
                {
                    Debug.Log($"🐰2⚡ Hop2 - 왼발Hop2={leftHop2Intensity:F2}, 오른발Hop2={rightHop2Intensity:F2}, 주기={repeatHop2Cycle:F2}");
                    Debug.Log($"� Hop2 오프셋 - 왼발: {leftFootHop2Offset}, 오른발: {rightFootHop2Offset}");
                }
            }
            else
            {
                // 🔥 Hop2 비활성화 상태 디버그
                Debug.Log($"❌ Hop2 else 블록 실행 - 오프셋 초기화됨! isHop2Mode={isHop2Mode}");
                
                // Hop2 해제 시 오프셋 부드럽게 초기화
                leftFootHop2Offset = Vector3.Lerp(leftFootHop2Offset, Vector3.zero, Time.deltaTime * 8f);
                rightFootHop2Offset = Vector3.Lerp(rightFootHop2Offset, Vector3.zero, Time.deltaTime * 8f);
                
                Debug.Log($"🔍 초기화 후 오프셋 - 왼발: {leftFootHop2Offset}, 오른발: {rightFootHop2Offset}");
            }
            
            // 디버그 로그 (1초마다 - 더 자주 표시)
            if (Time.time % 1f < Time.deltaTime)
            {
                Debug.Log($"🎯[통합 제어] qx높이:{qxHeight:F2}, 가속도강도:{accelerationIntensity:F2}, 킥버스트:{kickBurstIntensity:F2}");
            }
        }

        // === 웹소켓 및 키보드 입력을 통합 처리하는 새로운 로직 ===
        if (isWebSocketConnected && isWebSocketDataEnabled && !isPaused)
        {
            // --- 웹소켓 연결 시 ---
            if (isQxIntegratedMode)
            {
                // V키 ON: qX 통합 제어 모드 - 기존 파라미터를 직접 조절
                
                // qX 값 범위 매핑 (방향전환 기능 제거됨)
                float normalizedQx = Mathf.Clamp(currentQx, -0.6f, 0.6f) / 0.6f; // -1 ~ 1 범위로 정규화 (고정값 사용)
                
                // 상체 굽히기/젖히기 제어 (-0.2 ~ 0.4 범위)
                float targetBodyLeanAmount;
                if (normalizedQx > 0)
                {
                    // qX 양수: 빨라질 때 상체 숙이기 (0 ~ 0.4)
                    targetBodyLeanAmount = normalizedQx * 0.5f;
                }
                else
                {
                    // qX 음수: 느려질 때 상체 뒤로 젖히기 (0 ~ -0.2)
                    targetBodyLeanAmount = normalizedQx * 0.2f; // normalizedQx가 음수이므로 결과는 음수
                }
                bodyLeanAmount = Mathf.Lerp(bodyLeanAmount, targetBodyLeanAmount, Time.deltaTime * bodyLeanEase);
                
                // 보폭 제어 - qx로 SpacingRatio/DepthRatio 통합 제어 (개별제어와 동일한 범위 사용)
                float targetSpacingRatio = 0f;
                float targetDepthRatio = 0f;
                
                // qX 값에 따른 보폭 제어 (-1 ~ 1 범위)
                if (inRunningman)
                {
                    // 러닝맨: DepthRatio로 앞뒤 보폭 조절 (개별제어와 동일: -0.4 ~ 0.1)
                    if (normalizedQx > 0)
                    {
                        // qX 양수: 빨라질 때 보폭 좁히기 (0 -> -0.4)
                        targetDepthRatio = -normalizedQx * 0.4f;
                    }
                    else
                    {
                        // qX 음수: 느려질 때 보폭 넓히기 (0 -> 0.1)
                        targetDepthRatio = -normalizedQx * 0.1f;
                    }
                }
                else if (inSpongebob)
                {
                    // 스폰지밥: 기본 보폭은 센서 데이터로 제어
                    float baseRange = 0.2f; // 기본 범위
                    
                    if (normalizedQx > 0)
                    {
                        // qX 양수: 빨라질 때 보폭 좁히기
                        targetSpacingRatio = -normalizedQx * baseRange;
                    }
                    else
                    {
                        // qX 음수: 느려질 때 보폭 넓히기  
                        targetSpacingRatio = -normalizedQx * baseRange;
                    }
                    
                    // 킥아웃 모드가 활성화되어 있으면 스폰지밥용 추가 효과 (메인 IK는 Update에서 처리)
                    if (isKickOutMode)
                    {
                        // 스폰지밥에서 킥아웃 시 SpacingRatio도 약간 조정
                        float kickTime = Time.time * kickOutPulseRate;
                        float kickDirection = Mathf.Sin(kickTime) > 0 ? 1f : -1f;
                        
                        // 약간의 추가 spacing 효과 (IK가 메인이므로 작게)
                        float extraSpacing = kickDirection * kickOutDistance * 0.1f;
                        
                        // 기존 spacing에 추가 효과 적용
                        targetSpacingRatio += extraSpacing;
                        
                        // 디버그 로그
                        if (Time.time % 2f < Time.deltaTime)
                        {
                            Debug.Log($"🔥🦵 킥아웃 SpacingRatio! 방향={kickDirection:F0}, 추가={extraSpacing:F2}, 최종={targetSpacingRatio:F2}");
                        }
                    }
                    else
                    {
                        // 킥 모드 꺼짐 로그
                        if (Time.time % 2f < Time.deltaTime)
                        {
                            Debug.Log($"� 킥아웃 꺼짐 - 일반 스폰지밥 모드, SpacingRatio={targetSpacingRatio:F2}");
                        }
                    }
                    
                    // 최종 SpacingRatio 설정 (일반 범위)
                    targetSpacingRatio = Mathf.Clamp(targetSpacingRatio, -1.0f, 1.0f);
                }
                
                // 보폭 변화를 부드럽게 적용하여 급격한 변화 방지
                float lerpSpeed = 2.5f; // 고정값 사용 (방향전환 기능 제거됨)
                
                // 애니메이션 상태에 따라 기존 파라미터에 부드럽게 적용
                if (inRunningman)
                {
                    depthRatio = Mathf.Lerp(depthRatio, targetDepthRatio, Time.deltaTime * lerpSpeed);
                    spacingRatio = Mathf.Lerp(spacingRatio, 0f, Time.deltaTime * lerpSpeed);
                }
                else if (inSpongebob)
                {
                    spacingRatio = Mathf.Lerp(spacingRatio, targetSpacingRatio, Time.deltaTime * lerpSpeed);
                    depthRatio = Mathf.Lerp(depthRatio, 0f, Time.deltaTime * lerpSpeed);
                }
                else
                {
                    depthRatio = Mathf.Lerp(depthRatio, 0f, Time.deltaTime * lerpSpeed);
                    spacingRatio = Mathf.Lerp(spacingRatio, 0f, Time.deltaTime * lerpSpeed);
                }
                
                // 통합 제어 디버깅 (2초마다)
                if (Time.time % 2f < Time.deltaTime)
                {
                    string animState = inRunningman ? "러닝맨" : (inSpongebob ? "스폰지밥" : "기타");
                    float displayTargetRatio = inRunningman ? targetDepthRatio : targetSpacingRatio;
                    Debug.Log($"[QX통합제어] 애니메이션={animState}, qx={currentQx:F3}, 정규화={normalizedQx:F3}");
                    Debug.Log($"[QX통합설정] 방향전환 기능 제거됨");
                    Debug.Log($"[QX통합결과] target={displayTargetRatio:F3}, depth={depthRatio:F3}, spacing={spacingRatio:F3}, bodyLean={bodyLeanAmount:F3}");
                }
            }
            else
            {
                // V키 OFF: 기본 모드 (A/D 개별 제어)
                // A 값 (상체 굽힘) 적용
                float targetBodyLeanAmount = CalculateBodyLeanFromA(currentAValue);
                bodyLeanAmount = Mathf.Lerp(bodyLeanAmount, targetBodyLeanAmount, Time.deltaTime * bodyLeanEase);

                // D 값 (보폭) 적용 - 현재 애니메이션 상태에 따라 분기
                if (inRunningman)
                {
                    // [수정] 러닝맨일 경우, isForRunningman을 true로 전달하여 DepthRatio 계산
                    depthRatio = CalculateRatioFromD(currentDValue, true);
                    spacingRatio = 0f;
                }
                else if (inSpongebob)
                {
                    // [수정] 스폰지밥일 경우, isForRunningman을 false로 전달하여 SpacingRatio 계산
                    spacingRatio = CalculateRatioFromD(currentDValue, false);
                    depthRatio = 0f;
                }
                else
                {
                    depthRatio = 0f;
                    spacingRatio = 0f;
                }
            }

            // 기존 글라이딩 시스템 비활성화 (상체 굽히기로 대체)
            glideAmount = 0f;
            animator.SetFloat("Glide", glideAmount);
        }
        else if (!isPaused)
        {
            // --- 웹소켓 미연결 시 (키보드 제어) ---
            // Z/X 키로 상체 굽힘/젖힘 제어 (-0.5 ~ 1.0 범위)
            if (Input.GetKeyDown(KeyCode.Z))
            {
                keyboardBodyLeanAmount -= 0.2f;
                keyboardBodyLeanAmount = Mathf.Clamp(keyboardBodyLeanAmount, -0.5f, 1f);
                if (keyboardBodyLeanAmount > 0)
                {
                    float angle = keyboardBodyLeanAmount * maxBodyLeanForwardAngle;
                    Debug.Log($"Z키 입력: 상체 굽히기 = {keyboardBodyLeanAmount:F1} ({angle:F1}도 가슴 숙이기)");
                }
                else
                {
                    float angle = keyboardBodyLeanAmount * maxBodyLeanBackAngle;
                    Debug.Log($"Z키 입력: 상체 젖히기 = {keyboardBodyLeanAmount:F1} ({angle:F1}도 뒤로 젖히기)");
                }
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                keyboardBodyLeanAmount += 0.2f;
                keyboardBodyLeanAmount = Mathf.Clamp(keyboardBodyLeanAmount, -0.5f, 1f);
                if (keyboardBodyLeanAmount > 0)
                {
                    float angle = keyboardBodyLeanAmount * maxBodyLeanForwardAngle;
                    Debug.Log($"X키 입력: 상체 굽히기 = {keyboardBodyLeanAmount:F1} ({angle:F1}도 가슴 숙이기)");
                }
                else if (keyboardBodyLeanAmount < 0)
                {
                    float angle = keyboardBodyLeanAmount * maxBodyLeanBackAngle;
                    Debug.Log($"X키 입력: 상체 젖히기 = {keyboardBodyLeanAmount:F1} ({angle:F1}도 뒤로 젖히기)");
                }
                else
                {
                    Debug.Log($"X키 입력: 상체 중립 = {keyboardBodyLeanAmount:F1}");
                }
            }
            bodyLeanAmount = Mathf.Lerp(bodyLeanAmount, keyboardBodyLeanAmount, Time.deltaTime * bodyLeanEase);

            // A/D, Q/E 키로 보폭 제어
            if (Input.GetKeyDown(KeyCode.A))
            {
                keyboardSpacingRatio = Mathf.Clamp(keyboardSpacingRatio - 0.2f, -1f, 1f);
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                keyboardSpacingRatio = Mathf.Clamp(keyboardSpacingRatio + 0.2f, -1f, 1f);
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                keyboardDepthRatio = Mathf.Clamp(keyboardDepthRatio - 0.2f, -1f, 1f);
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                keyboardDepthRatio = Mathf.Clamp(keyboardDepthRatio + 0.2f, -1f, 1f);
            }
            
            // 현재 상태에 따라 키보드 값 적용
            if (inRunningman)
            {
                depthRatio = keyboardDepthRatio;
                spacingRatio = 0f;
            }
            else if (inSpongebob)
            {
                spacingRatio = keyboardSpacingRatio;
                depthRatio = 0f;
            }
            else
            {
                depthRatio = 0f;
                spacingRatio = 0f;
            }
            
            // 기존 글라이딩 시스템 비활성화
            glideAmount = 0f;
            animator.SetFloat("Glide", glideAmount);
        }
        
        // C키로 최대 각도 조정 (테스트용)
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (maxBodyLeanForwardAngle == 45f)
                maxBodyLeanForwardAngle = 30f;
            else if (maxBodyLeanForwardAngle == 30f)
                maxBodyLeanForwardAngle = 60f;
            else
                maxBodyLeanForwardAngle = 45f;
            Debug.Log($"C키: 최대 굽히기 각도 변경! {maxBodyLeanForwardAngle:F1}도");
        }

        // 0키로 완전 초기화
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            CompleteReset();
        }

        // 현재 상태 디버깅 (5초마다 출력)
        if (Time.time % 5f < Time.deltaTime)
        {
            Debug.Log($"현재 애니메이션 상태: {state.shortNameHash}, 상태명 체크 - Walking: {inWalking}, Running: {inRunningman}, Spongebob: {inSpongebob}, Spin: {inSpin}");
            Debug.Log($"[웹소켓 상태] 연결: {isWebSocketConnected}, 데이터활성: {isWebSocketDataEnabled}, 일시정지: {isPaused}");
            Debug.Log($"[FB 상태] 현재FB: {currentFBValue}, 이전FB: {lastFBValue}, FB회전중: {isFBRotating}");
        }

        // Spin 상태 종료 감지 (개선된 방법)
        if (wasInSpin && !inSpin && !spinCompleted)
        {
            Debug.Log("Spin 상태 종료 감지! OnSpinComplete 호출");
            OnSpinComplete();
        }
        else if (!inSpin && !wasInSpin)
        {
            // 스핀이 아닌 상태가 지속될 때만 완료 플래그 리셋
            if (spinCompleted && Time.time % 1f < Time.deltaTime)
            {
                spinCompleted = false; // 1초마다 리셋 (안전장치)
                Debug.Log("스핀 완료 플래그 리셋");
            }
        }
        wasInSpin = inSpin;

        // === DOT 센서 값 디버깅 (5초마다 출력) ===
        // if (Time.time % 5f < Time.deltaTime)
        // {
        //     Debug.Log($"[DOT 센서 디버깅] qX={currentQx:F3}, qY={currentQy:F3}, qZ={currentQz:F3}");
        // }

        // qY/qZ 기반 스핀 기능 제거 - 스핀은 다른 방식으로 제어

        // qX 기반 속도 제어 제거 - 기본 속도 사용
        if (!isJumpInProgress) // 점프 중이 아닐 때만 속도 제어
        {
            // 기본 속도 사용
            movementSpeed = 2.0f;   // 기본 이동 속도

            if (!isPaused)
            {
                // 애니메이션 속도 계산
                float finalAnimSpeed = animationSpeed;
                
                // 킥아웃 모드일 때 속도 부스트
                if (isKickOutMode)
                {
                    finalAnimSpeed *= kickOutSpeed;
                    
                    if (!isReversePlayback && Time.time % 2f < Time.deltaTime)
                    {
                        Debug.Log($"🚀⚡ 킥아웃 스피드 부스트: {animationSpeed:F2} → {finalAnimSpeed:F2} (x{kickOutSpeed})");
                    }
                }
                
                // 최종 애니메이션 속도 적용
                if (!isReversePlayback)
                {
                    animator.speed = finalAnimSpeed;
                }
            }
        }

        // 이동 처리 (일시정지 상태가 아닐 때만 이동)
        if (!isPaused && !inSpin)
        {
            if ((inWalking || inRunningman || inSpongebob) || glideAmount > 0f)
            {
                // === 발 미끄러짐 보정 시스템 ===
                float speedCompensation = 1.0f;
                if (animationSpeed > 1.2f)
                {
                    speedCompensation = Mathf.Lerp(1.0f, 0.85f, (animationSpeed - 1.2f) / (1.8f - 1.2f));
                }
                else if (animationSpeed < 0.8f)
                {
                    speedCompensation = Mathf.Lerp(1.0f, 1.15f, (0.8f - animationSpeed) / (0.8f - 0.5f));
                }

                // === [수정된 부분] ===
                // 기본 이동 속도에 animationSpeed(애니메이션 속도)를 먼저 곱합니다.
                float currentMoveVelocity = movementSpeed;

                // 스폰지밥 애니메이션일 경우, 이동 속도에만 추가 배율을 적용합니다.
                if (inSpongebob)
                {
                    currentMoveVelocity *= spongebobMoveFactor;
                }
                // ======================

                // 최종 이동 거리에 보상 값을 곱해줍니다.
                float finalMoveDistance = currentMoveVelocity * speedCompensation * Time.deltaTime;
                transform.position += transform.forward * finalMoveDistance;
            }
        }

        // ================== [수정된 부분 시작] ==================
        // 웹소켓 qz 또는 키보드 입력에 따른 방향 제어 시스템 (스핀, 점프, FB 회전 중이 아닐 때만 적용)
        // FB 회전 중이거나 FB 회전이 최근에 완료된 경우 다른 회전 시스템 비활성화
        if (!inSpin && !isJumpInProgress && !isFBRotating && !HasRecentFBRotation())
        {
            // 웹소켓 연결 상태에 따라 제어 방식을 분기
            if (isWebSocketConnected && isWebSocketDataEnabled)
            {
                if (isQxIntegratedMode)
                {
                    // 방향전환 기능 제거됨
                    // 방향전환 기능 제거됨 - qx는 점프 모드에서만 사용
                    
                    // 방향전환 기능 제거됨
                    {
                        // 방향전환 기능 제거됨

                        // 방향전환 기능 제거됨

                        // 방향전환 기능 제거됨

                        // 방향전환 기능 제거됨

                        // 방향전환 기능 제거됨
                    }
                    // 방향전환 기능 제거됨

                    // 방향전환 기능 제거됨
                    }
                    else
                    {
                    // qZ 기반 회전 기능 제거됨
                }
            }
            
            else // 웹소켓이 연결되지 않았을 때 (키보드 제어)
            {
                // 왼쪽 화살표 키를 누르면 왼쪽으로 회전
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    transform.Rotate(0, -turnSpeed * Time.deltaTime, 0);
                }
                // 오른쪽 화살표 키를 누르면 오른쪽으로 회전
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    transform.Rotate(0, turnSpeed * Time.deltaTime, 0);
                }
            }
        }
        // =================== [수정된 부분 끝] ===================

        // === FB 기반 회전 처리 (완전히 독립적으로 동작, 걷기 중에도 가능) ===
        if (isFBRotating && isWebSocketDataEnabled && !inSpin && !isJumpInProgress)
        {
            float currentY = transform.eulerAngles.y;
            float smoothedY = Mathf.LerpAngle(currentY, fbTargetYRotation, turnSpeed * Time.deltaTime * 0.02f);
            transform.rotation = Quaternion.Euler(0f, smoothedY, 0f);
            
            // 회전 진행 상황 로그 (1초마다)
            if (Time.time % 1f < Time.deltaTime)
            {
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(currentY, fbTargetYRotation));
                Debug.Log($"[FB 회전진행] 현재: {currentY:F1}°, 목표: {fbTargetYRotation:F1}°, 차이: {angleDiff:F1}°");
            }
            
            // 목표 각도에 거의 도달했으면 회전 완료
            if (Mathf.Abs(Mathf.DeltaAngle(currentY, fbTargetYRotation)) < 2f)
            {
                isFBRotating = false;
                lastFBRotationTime = Time.time;  // 완료 시간 기록
                // 회전 완료 시 현재 각도를 새로운 기준으로 설정 (다른 시스템이 되돌리지 않도록)
                transform.rotation = Quaternion.Euler(0f, fbTargetYRotation, 0f);
                Debug.Log($"[FB 회전완료] 목표 각도 도달하고 방향 고정: {fbTargetYRotation:F1}°, 보호시간 3초 시작");
            }
        }
        else if (isFBRotating && !isWebSocketDataEnabled)
        {
            Debug.Log("[FB 회전대기] 웹소켓 데이터가 비활성화되어 FB 회전 대기 중");
        }
        // FB 회전이 아닐 때는 현재 방향 유지 (다른 시스템이 개입하지 않도록)

        // 수동 애니메이션 속도 조절 (상태에 따라 동작 분리)
        bool upArrowPressed = Input.GetKey(KeyCode.UpArrow);
        bool downArrowPressed = Input.GetKey(KeyCode.DownArrow);

        // 점프 모드일 때는 화살표 키가 점프 제어로 사용됨
        if (isJumpInProgress)
        {
            // HandleJumpStateMachineInput()에서 이미 처리됨
        }
        else
        {
            // 기존 속도 제어 로직 (점프 중이 아닐 때만)
            if (upArrowPressed)
            {
                if (!isWebSocketConnected)
                {
                    movementSpeed = Mathf.Min(movementSpeed + Time.deltaTime * 1.0f, 4.0f); // 이동속도 증가
                    if (!isPaused) {
                        animator.speed = animationSpeed; // 애니메이션 속도는 그대로 유지
                    }
                }
            }
            else if (downArrowPressed)
            {
                if (!isWebSocketConnected)
                {
                    movementSpeed = Mathf.Max(movementSpeed - Time.deltaTime * 1.0f, 0.2f); // 이동속도 감소
                    if (!isPaused) {
                        animator.speed = animationSpeed; // 애니메이션 속도는 그대로 유지
                    }
                }
            }
            else
            {
                if (!isWebSocketConnected)
                {
                    if (!isPaused) {
                        animator.speed = animationSpeed;
                    }
                }
            }
        }

        // 댄스 전환 키들 (R, S, W, T)
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R 키 입력 감지! 러닝맨으로 전환 시도");
            // 러닝맨 전환 로직...
            var currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.IsName("RunningmanRecentSensing_jin"))
            {
                Debug.Log("이미 러닝맨 상태입니다.");
                return;
            }
            lastDance = Dance.Runningman;
            animator.SetBool("isRunningmanLast", true);
            animator.SetBool("isSpongebobLast", false);
            animator.SetBool("isWalkingLast", false);
            animator.ResetTrigger("ToSpongebob");
            animator.ResetTrigger("ToSpin");
            animator.ResetTrigger("ToWalking");
            animator.ResetTrigger("ToRunningman");
            animator.SetTrigger("ToRunningman");
            Debug.Log("ToRunningman 트리거 설정 완료");
            ResetDirection();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("S 키 입력 감지! 스폰지밥으로 전환 시도");
            // 스폰지밥 전환 로직...
            var currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.IsName("SpongebobRecentsension_jin"))
            {
                Debug.Log("이미 스폰지밥 상태입니다.");
                return;
            }
            lastDance = Dance.Spongebob;
            animator.SetBool("isSpongebobLast", true);
            animator.SetBool("isRunningmanLast", false);
            animator.SetBool("isWalkingLast", false);
            animator.ResetTrigger("ToRunningman");
            animator.ResetTrigger("ToSpin");
            animator.ResetTrigger("ToWalking");
            animator.ResetTrigger("ToSpongebob");
            animator.SetTrigger("ToSpongebob");
            Debug.Log("ToSpongebob 트리거 설정 완료");
            ResetDirection();
        }
        
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("W 키 입력 감지! 걷기로 전환 시도");
            // 걷기 전환 로직...
            var currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.IsName("walking"))
            {
                Debug.Log("이미 걷기 상태입니다.");
                return;
            }
            lastDance = Dance.Walking;
            animator.SetBool("isWalkingLast", true);
            animator.SetBool("isRunningmanLast", false);
            animator.SetBool("isSpongebobLast", false);
            animator.ResetTrigger("ToRunningman");
            animator.ResetTrigger("ToSpongebob");
            animator.ResetTrigger("ToSpin");
            animator.ResetTrigger("ToWalking");
            animator.SetTrigger("ToWalking");
            Debug.Log("ToWalking 트리거 설정 완료");
            ResetDirection();
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            // T키 수동 스핀 로직...
            var currentState = animator.GetCurrentAnimatorStateInfo(0);
            animator.SetBool("isWalkingLast", false);
            animator.SetBool("isRunningmanLast", false);
            animator.SetBool("isSpongebobLast", false);

            if (currentState.IsName("RunningmanRecentSensing_jin"))
            {
                lastDance = Dance.Runningman;
                Debug.Log("러닝맨에서 수동 스핀 트리거");
            }
            else if (currentState.IsName("SpongebobRecentsension_jin"))
            {
                lastDance = Dance.Spongebob;
                Debug.Log("스폰지밥에서 수동 스핀 트리거");
            }
            else if (currentState.IsName("walking"))
            {
                lastDance = Dance.Walking;
                Debug.Log("걷기에서 수동 스핀 트리거");
            }
            
            animator.ResetTrigger("ToRunningman");
            animator.ResetTrigger("ToSpongebob");
            animator.ResetTrigger("ToWalking");
            animator.ResetTrigger("ToSpin");
            animator.SetTrigger("ToSpin");
            Debug.Log($"수동 ToSpin 트리거! 이전 댄스: {lastDance}");
        }

        // --- 디버그 로그 (상태 확인용) ---
        // if (Time.time % 3f < Time.deltaTime) // 3초에 한 번
        // {
        //     Debug.Log($"---[상태 리포트]--- WebSocket: {isWebSocketConnected}, Paused: {isPaused}");
        //     Debug.Log($"A값: {currentAValue} -> 상체굽힘: {bodyLeanAmount:F2}");
        //     Debug.Log($"D값: {currentDValue} | 러닝맨({inRunningman}): DepthRatio={depthRatio:F2} | 스폰지밥({inSpongebob}): SpacingRatio={spacingRatio:F2}");
        // }
        
        // 역재생 처리
        if (isReversePlayback && !isPaused)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            // 역재생 속도 매핑: -1이면 0.6배속으로 역재생 (보기 좋은 속도)
            float reverseSpeed;
            if (animationSpeed <= -1.0f)
            {
                // -1.0 이하: 0.6배속 기준으로 스케일링
                reverseSpeed = 0.6f * Mathf.Abs(animationSpeed);
            }
            else
            {
                // -0.1 ~ -0.9: 직접 절댓값 사용 (더 세밀한 제어)
                reverseSpeed = Mathf.Abs(animationSpeed);
            }
            
            // 현재 애니메이션의 normalizedTime 가져오기
            float currentNormalizedTime = stateInfo.normalizedTime % 1f;
            
            // 역방향으로 시간 업데이트
            float newTime = currentNormalizedTime - (Time.deltaTime * reverseSpeed);
            
            // 시간이 0 이하로 가면 1로 래핑
            if (newTime <= 0f)
            {
                newTime = 1f + newTime; // 음수를 1에서 빼서 부드러운 루프
            }
            
            // 애니메이터 속도를 0으로 유지하고 시간만 직접 제어
            animator.speed = 0f;
            animator.Play(stateInfo.shortNameHash, 0, newTime);
            
            // 디버그 로그 (매 초마다)
            if (Time.time % 1f < Time.deltaTime)
            {
                Debug.Log($"[역재생] animSpeed={animationSpeed:F2} → reverseSpeed={reverseSpeed:F2}, time={newTime:F3}");
            }
        }
        
        // === 새로운 킥아웃 시스템 (IK 직접 제어) ===
        if (isKickOutMode)
        {
            // 애니메이션과 동기화된 킥 타이밍 계산
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animNormalizedTime = stateInfo.normalizedTime % 1f; // 0~1 정규화된 애니메이션 시간

            // kickOutPulseRate로 킥 주기 조절 (애니메이션 기반)
            float kickCycle = (animNormalizedTime * kickOutPulseRate) % 1f; // 0~1 정규화
            float leftKickCycle = kickCycle;
            float rightKickCycle = (kickCycle + 0.5f) % 1f; // 오른발은 180도 지연 - 교차 작동
            
            // 킥 강도 계산 (동적 타이밍: 인스펙터에서 조절 가능)
            leftKickIntensity = 0f;
            rightKickIntensity = 0f;
            
            // 백분율을 소수로 변환
            float kickDur = kickDuration / 100f; // 예: 10% → 0.1
            float restTime = kickRestTime / 100f; // 예: 10% → 0.1
            float breakTime = cycleBreakTime / 100f; // 예: 8% → 0.08
            
            // 한 사이클 길이 계산: 왼발킥 + 휴식 + 오른발킥 + 휴식 + 사이클간휴식
            float oneCycleLength = kickDur + restTime + kickDur + restTime + breakTime;
            
            // 사이클 반복 계산
            float repeatCycle = leftKickCycle % oneCycleLength;
            
            // 각 구간의 경계점 계산 - 굽힘 → 차기 순서로 변경
            float leftBendEnd = kickDur; // 왼발 굽힘 끝
            float leftKickEnd = leftBendEnd + kickDur; // 왼발 킥 끝
            float rightBendEnd = leftKickEnd + kickDur; // 오른발 굽힘 끝
            float rightKickEnd = rightBendEnd + kickDur; // 오른발 킥 끝
            // 사이클 간 휴식: rightKickEnd ~ oneCycleLength
            
            // 왼발 굽힘 → 킥아웃 연속 동작 (0 ~ leftKickEnd 구간) - 굽힐 때는 바로 위로, 킥아웃할 때만 벌어지기
            if (repeatCycle >= 0f && repeatCycle < leftKickEnd)
            {
                // 굽힘 → 킥아웃을 하나의 연속된 동작으로
                float totalProgress = repeatCycle / leftKickEnd; // 0~1로 정규화
                if (totalProgress < 0.5f)
                {
                    // 전반부 (0~50%): 굽히기 - 바로 위로만 굽히기 (X축 벌어짐 없음)
                    float bendProgress = totalProgress * 2f; // 0~1로 정규화
                    leftKickIntensity = kickBurstIntensity * 0.5f * bendProgress * (1f - bendProgress); // 굽힘 강도
                }
                else
                {
                    // 후반부 (50~100%): 차기 - 킥아웃할 때만 X축으로 벌어지기
                    float kickProgress = (totalProgress - 0.5f) * 2f; // 0~1로 정규화
                    leftKickIntensity = kickBurstIntensity * kickProgress * (1f - kickProgress); // 킥아웃 강도
                }
            }
            
            // 오른발 굽힘 → 킥아웃 연속 동작 (leftKickEnd ~ rightKickEnd 구간) - 굽힐 때는 바로 위로, 킥아웃할 때만 벌어지기
            else if (repeatCycle >= leftKickEnd && repeatCycle < rightKickEnd)
            {
                // 굽힘 → 킥아웃을 하나의 연속된 동작으로
                float totalProgress = (repeatCycle - leftKickEnd) / (rightKickEnd - leftKickEnd); // 0~1로 정규화
                if (totalProgress < 0.5f)
                {
                    // 전반부 (0~50%): 굽히기 - 바로 위로만 굽히기 (X축 벌어짐 없음)
                    float bendProgress = totalProgress * 2f; // 0~1로 정규화
                    rightKickIntensity = kickBurstIntensity * 0.5f * bendProgress * (1f - bendProgress); // 굽힘 강도
                }
                else
                {
                    // 후반부 (50~100%): 차기 - 킥아웃할 때만 X축으로 벌어지기
                    float kickProgress = (totalProgress - 0.5f) * 2f; // 0~1로 정규화
                    rightKickIntensity = kickBurstIntensity * kickProgress * (1f - kickProgress); // 킥아웃 강도
                }
            }
            
            // 킥 각도를 라디안으로 변환
            float angleRad = kickOutAngle * Mathf.Deg2Rad;
            float angleOffset = Mathf.Sin(angleRad); // 각도에 따른 높이 보정
            
            // 왼발 킥 오프셋 (킥할 때만 적용) - 각도 조절 반영
            leftFootKickOffset = new Vector3(
                -kickOutDistance * leftKickIntensity, // X축: 왼쪽으로 (음수)
                (kickOutHeight + angleOffset * 0.3f) * leftKickIntensity, // Y축: 위로 킥 + 각도 보정
                kickOutDistance * 0.2f * leftKickIntensity // Z축: 약간 앞으로
            );
            
            // 오른발 킥 오프셋 (킥할 때만 적용) - 각도 조절 반영
            rightFootKickOffset = new Vector3(
                kickOutDistance * rightKickIntensity, // X축: 오른쪽으로 (양수)
                (kickOutHeight + angleOffset * 0.3f) * rightKickIntensity, // Y축: 위로 킥 + 각도 보정
                kickOutDistance * 0.2f * rightKickIntensity // Z축: 약간 앞으로
            );
            
            // 디버그 로그 (덜 자주)
            if (Time.time % 1f < Time.deltaTime)
            {
                bool isCycleComplete = IsKickOutCycleComplete();
                float debugKickDur = kickDuration / 100f;
                float debugRestTime = kickRestTime / 100f;
                float debugBreakTime = cycleBreakTime / 100f;
                float oneKickCycleLength = debugKickDur + debugRestTime + debugKickDur + debugRestTime + debugBreakTime;
                float repeatKickCycle = kickCycle % oneKickCycleLength;
                float debugSecondRestEnd = debugKickDur + debugRestTime + debugKickDur + debugRestTime;
                bool inRestPeriod = (repeatKickCycle >= debugSecondRestEnd) || 
                                   (repeatKickCycle >= debugKickDur && repeatKickCycle < debugKickDur + debugRestTime) || 
                                   (repeatKickCycle >= debugKickDur + debugRestTime + debugKickDur && repeatKickCycle < debugSecondRestEnd);
                Debug.Log($"🦵⚡ 킥아웃 IK 교차 - 왼발킥={leftKickIntensity:F2}, 오른발킥={rightKickIntensity:F2}, 사이클={kickCycle:F2}, 사이클완료={isCycleComplete}, 휴식구간={inRestPeriod}, 양발내려옴={leftKickIntensity < 0.01f && rightKickIntensity < 0.01f}");
            }
        }
        else
        {
            // 킥아웃 해제 시 기존 애니메이션의 자연스러운 도착지점으로 부드럽게 돌아가기
            // Vector3.zero 대신 기존 애니메이션의 자연스러운 위치로 돌아가기
            Vector3 naturalLeftReturn = new Vector3(
                -kickOutDistance * 0.1f, // X축: 왼쪽으로 살짝만 (자연스러운 균형)
                kickOutHeight * 0.05f, // Y축: 살짝만 위로 (자연스러운 자세)
                kickOutDistance * 0.05f // Z축: 살짝만 앞으로 (자연스러운 자세)
            );
            
            Vector3 naturalRightReturn = new Vector3(
                kickOutDistance * 0.1f, // X축: 오른쪽으로 살짝만 (자연스러운 균형)
                kickOutHeight * 0.05f, // Y축: 살짝만 위로 (자연스러운 자세)
                kickOutDistance * 0.05f // Z축: 살짝만 앞으로 (자연스러운 자세)
            );
            
            leftFootKickOffset = Vector3.Lerp(leftFootKickOffset, naturalLeftReturn, Time.deltaTime * 8f);
            rightFootKickOffset = Vector3.Lerp(rightFootKickOffset, naturalRightReturn, Time.deltaTime * 8f);
        }
        
        // === 스폰지밥 Hop 시스템 (토끼처럼 통통 튀기) ===
        if (isHopMode)
        {
            // 애니메이션과 동기화된 Hop 타이밍 계산 (킥아웃과 동일한 방식)
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animNormalizedTime = stateInfo.normalizedTime % 1f; // 0~1 정규화된 애니메이션 시간
            
            // hopPulseRate로 Hop 주기 조절 (애니메이션 기반)
            float hopCycle = (animNormalizedTime * hopPulseRate) % 1f; // 0~1 정규화
            
            // Hop 강도 계산 (킥아웃과 동일한 % 기반 사이클 방식) - 각 발별 독립 제어
            float leftHopIntensity = 0f;
            float rightHopIntensity = 0f;
            
            // 백분율을 소수로 변환
            float hopDur = hopDuration / 100f; // 예: 10% → 0.1
            float hopRest = hopRestTime / 100f; // 예: 10% → 0.1
            float hopBreak = hopCycleBreakTime / 100f; // 예: 8% → 0.08
            
            // 한 사이클 길이 계산: 왼발Hop + 휴식 + 오른발Hop + 휴식 + 사이클간휴식
            float oneHopCycleLength = hopDur + hopRest + hopDur + hopRest + hopBreak;
            
            // 사이클 반복 계산 (킥아웃과 동일한 방식)
            float repeatHopCycle = hopCycle % oneHopCycleLength;
            
            // 각 구간의 경계점 계산
            float leftHopEnd = hopDur; // 왼발Hop 끝
            float firstRestEnd = leftHopEnd + hopRest; // 첫 휴식 끝
            float rightHopEnd = firstRestEnd + hopDur; // 오른발Hop 끝
            float secondRestEnd = rightHopEnd + hopRest; // 두 번째 휴식 끝
            // 사이클 간 휴식: secondRestEnd ~ oneHopCycleLength
            
            // 왼발 Hop 타이밍 (0 ~ hopDuration% 구간)
            if (repeatHopCycle >= 0f && repeatHopCycle < leftHopEnd)
            {
                // 토끼 점프처럼 빠르게 올라갔다가 부드럽게 내려오는 Hop (포물선 모양)
                float hopProgress = repeatHopCycle / hopDur; // 0~1로 정규화
                leftHopIntensity = hopIntensity * hopProgress * (1f - hopProgress); // 포물선 함수
            }
            
            // 오른발 Hop 타이밍 (firstRestEnd ~ rightHopEnd 구간)
            if (repeatHopCycle >= firstRestEnd && repeatHopCycle < rightHopEnd)
            {
                // 토끼 점프처럼 빠르게 올라갔다가 부드럽게 내려오는 Hop (포물선 모양)
                float hopProgress = (repeatHopCycle - firstRestEnd) / hopDur; // 0~1로 정규화
                rightHopIntensity = hopIntensity * hopProgress * (1f - hopProgress); // 포물선 함수
            }
            
            // 양발 Hop 오프셋 (각 발별 독립 제어) - 1번 이미지처럼 자연스럽게 (다리를 들어올렸다가 살짝 뻗기)
            leftFootHopOffset = new Vector3(
                -hopFootGather * leftHopIntensity * 1.2f, // X축: 왼쪽으로 살짝 뻗기 (1번 이미지처럼 자연스럽게)
                (hopHeight - 0.15f) * leftHopIntensity, // Y축: 위로 높이 점프 - 0.15f로 높이 낮춤
                hopForwardLean * leftHopIntensity * 2.5f // Z축: 앞으로 더 많이 뻗기 (1번 이미지처럼 앞으로 뻗기)
            );
            
            rightFootHopOffset = new Vector3(
                hopFootGather * rightHopIntensity * 1.2f, // X축: 오른쪽으로 살짝 뻗기 (1번 이미지처럼 자연스럽게)
                (hopHeight - 0.15f) * rightHopIntensity, // Y축: 위로 높이 점프 - 오른발만의 강도 사용
                hopForwardLean * rightHopIntensity * 2.5f // Z축: 앞으로 더 많이 뻗기 (1번 이미지처럼 앞으로 뻗기)
            );
            
            // 전신 점프 효과 (왼발/오른발 Hop할 때만 올라가고, 휴식할 때는 내려옴)
            float bodyHopIntensity = 0f;
            
            // 왼발 Hop할 때만 점프 (0 ~ hopDuration% 구간) - 더 높고 빠른 깡총 뛰기
            if (repeatHopCycle >= 0f && repeatHopCycle < leftHopEnd)
            {
                // 왼발 Hop과 함께 점프 - 더 높고 빠른 깡총 뛰기
                float hopProgress = repeatHopCycle / hopDur; // 0~1로 정규화
                bodyHopIntensity = hopHeight * 3.5f * hopProgress * (1f - hopProgress); // 포물선 점프 - 높이 대폭 증가 (2.0f → 3.5f)
            }
            // 오른발 Hop할 때만 점프 (firstRestEnd ~ rightHopEnd 구간) - 각 구간을 독립적으로 계산해서 왼발과 동일한 그래프 + 최고점 도달 빠르게
            else if (repeatHopCycle >= firstRestEnd && repeatHopCycle < rightHopEnd)
            {
                // 오른발 Hop과 함께 점프 - 각 구간을 독립적으로 계산
                // 오른발 구간 내에서 0~1로 정규화 (왼발과 동일한 방식)
                float rightProgress = (repeatHopCycle - firstRestEnd) / hopDur; // 0~1로 정규화
                // 오른발만 최고점에 더 빠르게 도달하도록 조정 (1.5배 빨리)
                float fastRightProgress = Mathf.Clamp01(rightProgress * 1.5f); // 1.5배 빨리 최고점 도달
                // 왼발과 완전히 동일한 포물선 공식 사용 (독립적인 0~1 그래프 + 빠른 최고점 도달)
                bodyHopIntensity = hopHeight * 3.5f * fastRightProgress * (1f - fastRightProgress); // 포물선 점프 - 왼발과 동일 + 빠른 최고점 도달
            }
            // 휴식 구간에서는 점프하지 않음 (bodyHopIntensity = 0)
            
            // 몸통을 Y축으로 부드럽게 설정 (Hop할 때만 점프)
            targetBodyY = bodyHopIntensity; // 목표 Y축 위치 설정
            currentBodyY = Mathf.Lerp(currentBodyY, targetBodyY, Time.deltaTime * bodyYTransitionSpeed);
            
            Vector3 currentPosition = transform.position;
            currentPosition.y = currentBodyY;
            transform.position = currentPosition;
            
            // 디버그 로그 (더 자주 - 점프 느낌 차이 진단용)
            if (Time.time % 0.5f < Time.deltaTime)
            {
                bool isCycleComplete = IsHopCycleComplete();
                float hopSecondRestEnd = hopDur + hopRest + hopDur + hopRest;
                bool inRestPeriod = (repeatHopCycle >= hopSecondRestEnd) || 
                                   (repeatHopCycle >= leftHopEnd && repeatHopCycle < firstRestEnd) || 
                                   (repeatHopCycle >= rightHopEnd && repeatHopCycle < secondRestEnd);
                Debug.Log($"🐰⚡ 토끼 Hop - 왼발Hop={leftHopIntensity:F2}, 오른발Hop={rightHopIntensity:F2}, 주기={repeatHopCycle:F2}, 사이클완료={isCycleComplete}, 휴식구간={inRestPeriod}, 양발내려옴={leftHopIntensity < 0.1f && rightHopIntensity < 0.1f}");
            }
        }
        else
        {
            // Hop 해제 시 오프셋 부드럽게 초기화
            leftFootHopOffset = Vector3.Lerp(leftFootHopOffset, Vector3.zero, Time.deltaTime * 8f);
            rightFootHopOffset = Vector3.Lerp(rightFootHopOffset, Vector3.zero, Time.deltaTime * 8f);
        }
    
        // === 스폰지밥 Hop2 시스템 (토끼처럼 통통 튀기 - Hop 완전 복사) ===
        if (isHop2Mode)
        {
            // 애니메이션과 동기화된 Hop2 타이밍 계산 (원래 Hop과 동일한 방식)
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animNormalizedTime = stateInfo.normalizedTime % 1f; // 0~1 정규화된 애니메이션 시간
            
            // hop2PulseRate로 Hop2 주기 조절 (애니메이션 기반)
            float hop2Cycle = (animNormalizedTime * hop2PulseRate) % 1f; // 0~1 정규화
            
            // Hop2 강도 계산 (원래 Hop과 동일한 % 기반 사이클 방식) - 각 발별 독립 제어
            float leftHop2Intensity = 0f;
            float rightHop2Intensity = 0f;
            
            // 백분율을 소수로 변환
            float hop2Dur = hop2Duration / 100f; // 예: 10% → 0.1
            float hop2Rest = hop2RestTime / 100f; // 예: 10% → 0.1
            float hop2Break = hop2CycleBreakTime / 100f; // 예: 8% → 0.08
            
            // 한 사이클 길이 계산: 왼발Hop2 + 휴식 + 오른발Hop2 + 휴식 + 사이클간휴식
            float oneHop2CycleLength = hop2Dur + hop2Rest + hop2Dur + hop2Rest + hop2Break;
            
            // 사이클 반복 계산 (원래 Hop과 동일한 방식)
            float repeatHop2Cycle = hop2Cycle % oneHop2CycleLength;
            
            // 각 구간의 경계점 계산
            float leftHop2End = hop2Dur; // 왼발Hop2 끝
            float firstRest2End = leftHop2End + hop2Rest; // 첫 휴식 끝
            float rightHop2End = firstRest2End + hop2Dur; // 오른발Hop2 끝
            float secondRest2End = rightHop2End + hop2Rest; // 두 번째 휴식 끝
            // 사이클 간 휴식: secondRest2End ~ oneHop2CycleLength
            
            // 왼발 Hop2 타이밍 (0 ~ hop2Duration% 구간)
            if (repeatHop2Cycle >= 0f && repeatHop2Cycle < leftHop2End)
            {
                // 토끼 점프처럼 빠르게 올라갔다가 부드럽게 내려오는 Hop2 (포물선 모양)
                float hop2Progress = repeatHop2Cycle / hop2Dur; // 0~1로 정규화
                leftHop2Intensity = hop2Intensity * hop2Progress * (1f - hop2Progress); // 포물선 함수
            }
            
            // 오른발 Hop2 타이밍 (firstRest2End ~ rightHop2End 구간)
            if (repeatHop2Cycle >= firstRest2End && repeatHop2Cycle < rightHop2End)
            {
                // 토끼 점프처럼 빠르게 올라갔다가 부드럽게 내려오는 Hop2 (포물선 모양)
                float hop2Progress = (repeatHop2Cycle - firstRest2End) / hop2Dur; // 0~1로 정규화
                rightHop2Intensity = hop2Intensity * hop2Progress * (1f - hop2Progress); // 포물선 함수
            }
            
            // 양발 Hop2 오프셋 (각 발별 독립 제어) - 토끼처럼 안쪽으로 모으면서 높이 점프
            leftFootHop2Offset = new Vector3(
                hop2FootGather * leftHop2Intensity, // X축: 안쪽으로 모으기 (토끼 자세)
                hop2Height * leftHop2Intensity, // Y축: 위로 높이 점프
                hop2ForwardLean * leftHop2Intensity // Z축: 약간 앞으로 기울이기 (점프 자세)
            );
            
            rightFootHop2Offset = new Vector3(
                -hop2FootGather * rightHop2Intensity, // X축: 안쪽으로 모으기 (토끼 자세)
                hop2Height * rightHop2Intensity, // Y축: 위로 높이 점프
                hop2ForwardLean * rightHop2Intensity // Z축: 약간 앞으로 기울이기 (점프 자세)
            );
            
            // 디버그 로그 (더 자주)
            if (Time.time % 0.5f < Time.deltaTime)
            {
                bool isCycleComplete = IsHop2CycleComplete();
                float hop2SecondRestEnd = hop2Dur + hop2Rest + hop2Dur + hop2Rest;
                bool inRestPeriod = (repeatHop2Cycle >= hop2SecondRestEnd) || 
                                   (repeatHop2Cycle >= leftHop2End && repeatHop2Cycle < firstRest2End) || 
                                   (repeatHop2Cycle >= rightHop2End && repeatHop2Cycle < secondRest2End);
                Debug.Log($"🐰2⚡ 토끼 Hop2 - 왼발Hop2={leftHop2Intensity:F2}, 오른발Hop2={rightHop2Intensity:F2}, 주기={repeatHop2Cycle:F2}, 사이클완료={isCycleComplete}, 휴식구간={inRestPeriod}, 양발내려옴={leftHop2Intensity < 0.1f && rightHop2Intensity < 0.1f}");
            }
        }
        else
        {
            // Hop2 해제 시 오프셋 부드럽게 초기화
            leftFootHop2Offset = Vector3.Lerp(leftFootHop2Offset, Vector3.zero, Time.deltaTime * 8f);
            rightFootHop2Offset = Vector3.Lerp(rightFootHop2Offset, Vector3.zero, Time.deltaTime * 8f);
        }
        
            // === 스폰지밥 셔플 크로스 시스템 (왼발→오른발 순서, 교차 대각선 앞으로 뻗기) ===
        if (isShuffleCrossMode)
        {
            // 애니메이션과 동기화된 크로스 발차기 타이밍 계산 (킥아웃과 동일한 방식)
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animNormalizedTime = stateInfo.normalizedTime % 1f; // 0~1 정규화된 애니메이션 시간
            
            // crossPulseRate로 크로스 발차기 주기 조절 (애니메이션 기반)
            float crossCycle = (animNormalizedTime * crossPulseRate) % 1f; // 0~1 정규화
            
            // 크로스 발차기 강도 계산 (킥아웃과 동일한 % 기반 사이클 방식)
            leftCrossIntensity = 0f;
            rightCrossIntensity = 0f;
            
            // 백분율을 소수로 변환
            float crossDur = crossDuration / 100f; // 예: 12% → 0.12
            float crossRest = crossRestTime / 100f; // 예: 8% → 0.08
            float crossBreak = crossCycleBreakTime / 100f; // 예: 10% → 0.1
            
            // 한 사이클 길이 계산: 왼발크로스 + 휴식 + 오른발크로스 + 휴식 + 사이클간휴식
            float oneCrossCycleLength = crossDur + crossRest + crossDur + crossRest + crossBreak;
            
            // 사이클 반복 계산 (킥아웃과 동일한 방식)
            float repeatCrossCycle = crossCycle % oneCrossCycleLength;
            
            // 각 구간의 경계점 계산
            float leftCrossEnd = crossDur; // 왼발크로스 끝
            float firstRestEnd = leftCrossEnd + crossRest; // 첫 휴식 끝
            float rightCrossEnd = firstRestEnd + crossDur; // 오른발크로스 끝
            float secondRestEnd = rightCrossEnd + crossRest; // 두 번째 휴식 끝
            // 사이클 간 휴식: secondRestEnd ~ oneCrossCycleLength
            
            // 왼발 크로스 발차기 타이밍 (0 ~ crossDuration% 구간)
            if (repeatCrossCycle >= 0f && repeatCrossCycle < leftCrossEnd)
            {
                // 빠르게 올라갔다가 빠르게 내려오는 크로스 발차기 (포물선 모양)
                float crossProgress = repeatCrossCycle / crossDur; // 0~1로 정규화
                leftCrossIntensity = crossIntensity * crossProgress * (1f - crossProgress); // 포물선 함수
            }
            
            // 오른발 크로스 발차기 타이밍 (firstRestEnd ~ rightCrossEnd 구간)
            if (repeatCrossCycle >= firstRestEnd && repeatCrossCycle < rightCrossEnd)
            {
                // 빠르게 올라갔다가 빠르게 내려오는 크로스 발차기 (포물선 모양)
                float crossProgress = (repeatCrossCycle - firstRestEnd) / crossDur; // 0~1로 정규화
                rightCrossIntensity = crossIntensity * crossProgress * (1f - crossProgress); // 포물선 함수
            }
            
            // 크로스 발차기 각도를 라디안으로 변환
            float crossAngleRad = crossAngle * Mathf.Deg2Rad;
            float crossAngleOffset = Mathf.Sin(crossAngleRad); // 각도에 따른 높이 보정
            
            // 왼발 크로스 발차기 오프셋 (왼발 차례일 때 오른쪽 대각선 앞으로 뻗기)
            leftFootCrossOffset = new Vector3(
                crossDistance * leftCrossIntensity * 0.6f, // X축: 오른쪽으로 (몸 중앙을 가로질러)
                (crossHeight + crossAngleOffset * 0.2f) * leftCrossIntensity, // Y축: 위로 + 각도 보정 (0.15f 제거)
                crossDistance * leftCrossIntensity * 0.5f // Z축: 앞으로 (적당한 거리)
            );
            
            // 오른발 크로스 발차기 오프셋 (오른발 차례일 때 왼쪽 대각선 앞으로 뻗기)
            rightFootCrossOffset = new Vector3(
                -crossDistance * rightCrossIntensity * 0.6f, // X축: 왼쪽으로 (몸 중앙을 가로질러) - 부호만 반대
                (crossHeight + crossAngleOffset * 0.2f) * rightCrossIntensity, // Y축: 위로 + 각도 보정
                crossDistance * rightCrossIntensity * 0.5f // Z축: 앞으로 (적당한 거리)
            );
            
            // 디버그 로그 (더 자주 - 문제 진단용)
            if (Time.time % 0.5f < Time.deltaTime)
            {
                bool isCycleComplete = IsCrossCycleComplete();
                float debugSecondRestEnd = crossDur + crossRest + crossDur + crossRest;
                bool inRestPeriod = (repeatCrossCycle >= debugSecondRestEnd) || 
                                   (repeatCrossCycle >= leftCrossEnd && repeatCrossCycle < firstRestEnd) || 
                                   (repeatCrossCycle >= rightCrossEnd && repeatCrossCycle < debugSecondRestEnd);
                Debug.Log($"🦵⚡ 셔플 크로스 - 왼발크로스={leftCrossIntensity:F2}, 오른발크로스={rightCrossIntensity:F2}, 주기={repeatCrossCycle:F2}, 사이클완료={isCycleComplete}, 휴식구간={inRestPeriod}, 양발내려옴={leftCrossIntensity < 0.1f && rightCrossIntensity < 0.1f}");
            }
        }
        else
        {
            // 크로스 발차기 해제 시 오프셋 부드럽게 초기화
            leftFootCrossOffset = Vector3.Lerp(leftFootCrossOffset, Vector3.zero, Time.deltaTime * 8f);
            rightFootCrossOffset = Vector3.Lerp(rightFootCrossOffset, Vector3.zero, Time.deltaTime * 8f);
        }
    }



    void LateUpdate()
    {
         // 점프 중에는 웅크림 애니메이션 완전 비활성화
        if (jumping || jumpState == JumpState.Jumping || jumpState == JumpState.JumpAir || jumpState == JumpState.JumpingDown)
        {
            // 점프 중에는 웅크림 시스템 완전 비활성화
            return;
        }
        
        // 웅크림 상태 유지 시 애니메이션 강제 적용 (점프 중이 아닐 때만)
        if (isCrouchingMaintained && targetCrouchDepth > 0f)
        {
            float crouchRatio = currentCrouchDepth / maxCrouchDepth;
            float jumpPhase = Mathf.Lerp(0f, 1f, crouchRatio);
            animator.SetFloat("JumpPhase", jumpPhase);
            
            // Jump 애니메이션 상태 강제 유지
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"))
            {
                animator.Play("Jump", 0, 0);
            }
        }
        // 점프 중에는 상체 굽히기 시스템 비활성화 (물리와 충돌 방지)
        if (isJumpInProgress)
        {
            // 반복적인 로그 출력을 막기 위해 주석 처리
            // Debug.Log("점프 중: 상체 굽히기 시스템 비활성화");
            return;
        }
         if (footTrailObject != null)
        {
            // 디버그 로그 추가하여 실제로 실행되는지 확인
            //Debug.Log($"FootTrail 위치 업데이트: {transform.position}");
            
            if (footTrailObject != null)
            {
                // 위치 설정
                Vector3 characterBasePos = transform.position;
                Vector3 trailAnchorPoint = characterBasePos;
                trailAnchorPoint -= transform.forward * 0.1f;
                trailAnchorPoint.y = 0.01f;
                
                footTrailObject.transform.position = trailAnchorPoint;
                
                // [수정] Transform Z 사용 시 트레일이 바닥에 평평하게 보이도록 회전 설정
                footTrailObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // X축으로 90도 회전
            }
        }
        else
        {
            Debug.LogWarning("footTrailObject가 할당되지 않았습니다!");
        }
        // if (spine != null)
        // {
        //     // 기존 leanAngle (수동 조정) + A값 기반 상체 굽히기 결합
        //     // bodyLeanAmount는 0~1 범위: 0=중립, 1=최대 가슴 숙이기
        //     float targetBodyLeanAngle = bodyLeanAmount * maxBodyLeanForwardAngle;
            
        //     currentBodyLeanAngle = Mathf.SmoothDamp(currentBodyLeanAngle, targetBodyLeanAngle, ref bodyLeanVelocity, 0.1f);
            
        //     // 최종 상체 각도 = 기존 lean + 새로운 상체 굽히기
        //     float finalLeanAngle = leanAngle + currentBodyLeanAngle;
        //     spine.localRotation = spineInitLocalRot * Quaternion.Euler(finalLeanAngle, 0f, 0f);
            
        //     // 디버그 로그 (2초마다)
        //     if (Time.time % 2f < Time.deltaTime)
        //     {
        //         Debug.Log($"상체 굽히기: bodyLeanAmount={bodyLeanAmount:F2}, targetAngle={targetBodyLeanAngle:F1}°, currentAngle={currentBodyLeanAngle:F1}°, finalLeanAngle={finalLeanAngle:F1}°");
        //         Debug.Log($"Spine 현재 회전: {spine.localRotation.eulerAngles}, 초기 회전: {spineInitLocalRot.eulerAngles}");
        //     }
        // }
        
        // === 몸통 Y축 위치 통합 관리 ===
        // 홉 모드가 아닐 때는 바닥으로 부드럽게 내리기
        if (!isHopMode)
        {
            targetBodyY = 0f;
            currentBodyY = Mathf.Lerp(currentBodyY, targetBodyY, Time.deltaTime * bodyYTransitionSpeed);
            
            Vector3 currentPosition = transform.position;
            currentPosition.y = currentBodyY;
            transform.position = currentPosition;
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        // Hop2 모드 상태 디버깅
        if (isHop2Mode && Time.time % 0.5f < Time.deltaTime)
        {
            Debug.Log($"🎯 OnAnimatorIK - Hop2 모드 활성! isJumpInProgress={isJumpInProgress}, rb.isKinematic={rb?.isKinematic}");
        }
        
        // [수정] Rigidbody가 물리 제어를 받고 있을 때(isKinematic == false)는 IK 시스템이 절대 개입하지 못하도록
        // 방어 조건을 강화하여, 물리 엔진과의 충돌을 원천적으로 차단합니다.
        if (isJumpInProgress || (rb != null && !rb.isKinematic))
        {
            if (isHop2Mode && Time.time % 0.5f < Time.deltaTime)
            {
                Debug.Log($"❌ Hop2 IK 차단됨! isJumpInProgress={isJumpInProgress}, rb.isKinematic={rb?.isKinematic}");
            }
            // 만약의 경우를 대비해 모든 IK 가중치를 0으로 확실하게 리셋합니다.
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0f);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            return;
        }

        // 점프 중에는 IK 시스템 완전 비활성화
        if (isJumpInProgress)
        {
            // 모든 IK 가중치를 0으로 설정하여 완전히 비활성화
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0f);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);

            // 반복적인 로그 출력을 막기 위해 주석 처리
            // Debug.Log("점프 중: IK 시스템 비활성화");
            return;
        }

        // 현재 상태 재확인
        var state = animator.GetCurrentAnimatorStateInfo(0);
        bool inRunning = state.IsName("RunningmanRecentSensing_jin");
        bool inSpongebob = state.IsName("SpongebobRecentsension_jin");

        // IK 기본 위치 - 발목과 발가락 끝 모두 가져오기
        Vector3 lBase = animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        Vector3 rBase = animator.GetIKPosition(AvatarIKGoal.RightFoot);

        // ToeBase 위치 가져오기 (더 정확한 발 끝 제어를 위해)
        Transform leftToeBase = animator.GetBoneTransform(HumanBodyBones.LeftToes);
        Transform rightToeBase = animator.GetBoneTransform(HumanBodyBones.RightToes);

        Vector3 lToeBase = leftToeBase != null ? leftToeBase.position : lBase;
        Vector3 rToeBase = rightToeBase != null ? rightToeBase.position : rBase;

        // 발목과 발가락 끝 사이의 오프셋 계산 (발 길이)
        Vector3 leftFootOffset = lToeBase - lBase;
        Vector3 rightFootOffset = rToeBase - rBase;

        // 평균 발 길이 계산 (앞쪽 방향 기준)
        float avgFootLength = (Vector3.Dot(leftFootOffset, transform.forward) + Vector3.Dot(rightFootOffset, transform.forward)) * 0.5f;
        avgFootLength = Mathf.Max(avgFootLength, 0.05f); // 최소 5cm 보장

        // 디버그 정보 (5초마다)
        // if (Time.time % 5f < Time.deltaTime)
        // {
        //     Debug.Log($"ToeBase 시스템: 왼발 오프셋={leftFootOffset.magnitude:F3}, 오른발 오프셋={rightFootOffset.magnitude:F3}, 평균 발길이={avgFootLength:F3}");
        // }

        // 1) 실제 X축·Z축 분리 거리 구하기
        float footSepX = Vector3.Dot(rBase - lBase, transform.right);
        float halfSepX = footSepX * 0.5f;
        float footSepZ = Vector3.Dot(rBase - lBase, transform.forward);
        float halfSepZ = footSepZ * 0.5f;

        // 2) 모든 애니메이션 상태에서 좌우 보폭 적용
        // 글라이딩 시 좌우 보폭 자동 확장 (무릎이 낮아질수록 발이 벌어짐)
        // 글라이딩 보폭 확장 시스템 비활성화 (상체 굽히기로 대체됨)
        // 보폭 확장: 스폰지밥(X축), 러닝맨(Z축) - 더 이상 사용하지 않음
        float glideSpacingBonus = 0f;  // 항상 0으로 고정
        float glideDepthBonus = 0f;    // 항상 0으로 고정

        // 기존 글라이딩 로직 주석 처리
        /*
        if (inSpongebob) {
            // 스폰지밥: X축(좌우) 보폭 확장
            if (glideAmount > 0.05f) {
                if (glideAmount <= 0.25f) {
                    glideSpacingBonus = Mathf.Lerp(0f, 0.4f, Mathf.Pow((glideAmount - 0.05f) / 0.2f, 0.7f));
                } else {
                    glideSpacingBonus = Mathf.Lerp(0.4f, 0.6f, (glideAmount - 0.25f) / 0.35f);
                }
            }
        } else if (inRunning) {
            // 러닝맨: Z축(앞뒤) 보폭 확장
            if (glideAmount > 0.1f) {
                if (glideAmount <= 0.4f) {
                    glideDepthBonus = Mathf.Lerp(0.5f, 1.5f, (glideAmount - 0.1f) / 0.3f);
                } else {
                    glideDepthBonus = Mathf.Lerp(1.5f, 3.0f, (glideAmount - 0.4f) / 0.2f);
                }
            }
        }
        */
        // 애니메이션별 보폭 제어 분리
        // Spacing Ratio: 스폰지밥에만 적용 (X축, 좌우 보폭)
        // Depth Ratio: 러닝맨에만 적용 (Z축, 앞뒤 보폭)
        float scaledSpacingForPosition = 0f; // 기본값으로 초기화
        float effectiveSpacingRatio = 0f;
        float effectiveDepthRatio = 0f;

        if (inSpongebob)
        {
            // 스폰지밥: spacingRatio만 적용 (좌우 보폭)
            scaledSpacingForPosition = spacingRatio * 0.6f;
            effectiveSpacingRatio = scaledSpacingForPosition + glideSpacingBonus;
            // depthRatio는 무시 (Z축 보폭 변화 없음)
            effectiveDepthRatio = 0f + glideDepthBonus;
        }
        else if (inRunning)
        {
            // 러닝맨: depthRatio만 적용 (앞뒤 보폭)
            effectiveSpacingRatio = 0f + glideSpacingBonus;
            // spacingRatio는 무시 (X축 보폭 변화 없음)
            effectiveDepthRatio = depthRatio + glideDepthBonus;
            scaledSpacingForPosition = 0f; // 러닝맨에서는 좌우 보폭 변화 없음
        }
        else
        {
            // 걷기 등 다른 애니메이션: 둘 다 무시
            effectiveSpacingRatio = 0f + glideSpacingBonus;
            effectiveDepthRatio = 0f + glideDepthBonus;
            scaledSpacingForPosition = 0f;
        }

        // 최종 발 간격 계산
        footSpacingX = effectiveSpacingRatio * halfSepX;
        footSpacingZ = effectiveDepthRatio * halfSepZ;

        // 오프셋 적용
        Vector3 lateralOffset = transform.right * footSpacingX;
        Vector3 forwardOffset = transform.forward * footSpacingZ;
        Vector3 liftOffset = transform.up * liftY;
        Vector3 glideOffset = Vector3.zero;  // 글라이딩 오프셋 비활성화 (상체 굽히기로 대체)

        // 발이 바닥에 닿도록 보폭이 커질 때 발이 뜨는 현상 방지 (자연스럽게 개선)
        // depthRatio가 음수일 때(발이 앞으로 오는 경우) 수직 높이 조정을 줄임
        float verticalFactor = 1.0f;
        if (depthRatio < 0f)
        {
            // depthRatio가 -1에 가까울수록 수직 높이를 줄임 (최소값을 0.3으로 상향 조정)
            verticalFactor = Mathf.Lerp(1.0f, 0.3f, Mathf.Abs(depthRatio));
        }
 
        // 수직 높이 조정
        liftOffset = transform.up * (liftY * verticalFactor);

        // ToeBase 기반 발 타겟 계산 (더 정확한 발 끝 제어)
        Vector3 lTarget = lBase - lateralOffset + liftOffset - forwardOffset + glideOffset;
        Vector3 rTarget = rBase + lateralOffset + liftOffset + forwardOffset + glideOffset;

        // 발가락 끝 기준으로 최종 타겟 조정 (발 끝이 지면에 정확히 닿도록)
        Vector3 lToeTarget = lTarget + transform.forward * avgFootLength * 0.8f; // 발 길이의 80% 앞으로
        Vector3 rToeTarget = rTarget + transform.forward * avgFootLength * 0.8f;

        // 발목 위치는 발가락 기준으로 역산 (자연스러운 발 각도 유지)
        lTarget = lToeTarget - transform.forward * avgFootLength * 0.8f;
        rTarget = rToeTarget - transform.forward * avgFootLength * 0.8f;

        // 보폭 조정 시 극강 바닥 뚫기 방지 시스템 (25%부터 보호)
        float strideFloorProtection = 0f;

        // 25% 이상의 보폭에서 보호 시작 (-0.4 보폭 완전 대응)
        float maxStrideRatio = Mathf.Max(Mathf.Abs(scaledSpacingForPosition), Mathf.Abs(effectiveDepthRatio));
        if (maxStrideRatio > 0.25f)
        {
            // 보폭이 25% 이상일 때부터 극강 보호 시작
            // 25%~100% 범위에서 극강 보호, 최대 10mm
            strideFloorProtection = Mathf.Lerp(0f, 0.01f, Mathf.Clamp01((maxStrideRatio - 0.25f) / 0.75f));

            // 98% 이상에서 추가 극소 보호 (기존 로직 유지)
            if (maxStrideRatio > 0.98f)
            {
                float extraProtection = Mathf.Lerp(0f, 0.0002f, Mathf.Clamp01((maxStrideRatio - 0.98f) / 0.02f));
                strideFloorProtection = Mathf.Max(strideFloorProtection, extraProtection);
            }
        }

        // 글라이딩 보호 완전 제거 (이미 처리됨)

        // 통합 바닥 보호 높이 계산 (완전 극소화)
        float totalFloorProtection = strideFloorProtection;
        float basicMinHeight = transform.position.y + 0.0001f + totalFloorProtection; // 기본 높이를 0.1mm로 완전 극소화

        // 발 타겟에 즉시 바닥 보호 적용
        lTarget.y = Mathf.Max(lTarget.y, basicMinHeight);
        rTarget.y = Mathf.Max(rTarget.y, basicMinHeight);

        // 글라이딩 시 발 위치 보간 속도 조정 (더 안정적으로)
        float footTransitionSpeed = 40f; // 기존 10~6에서 40으로 대폭 증가
        if (glideAmount > 0.3f)
        {
            footTransitionSpeed = Mathf.Lerp(40f, 30f, (glideAmount - 0.3f) / 0.3f); // 더 빠르게 보간
        }

        smoothLeftFootTarget = Vector3.Lerp(smoothLeftFootTarget, lTarget, Time.deltaTime * footTransitionSpeed);
        smoothRightFootTarget = Vector3.Lerp(smoothRightFootTarget, rTarget, Time.deltaTime * footTransitionSpeed);

        // 지면에 맞추기 - 강화된 버전 (레이어 마스크 추가)
        float raycastHeight = 1.2f; // 더 높게 시작
        float raycastDistance = 3.0f; // 더 멀리 탐색
        float footPadding = 0.01f; // 지면 위 여유 공간을 1cm로 감소 (기존 2cm에서)

        // 레이어 마스크 설정 (바닥/지면 레이어만 감지)
        int groundLayerMask = LayerMask.GetMask("Default", "Ground"); // 필요시 "Ground" 레이어 추가

        // 다중 지점에서 지면 감지 (더 정확한 바닥 감지)
        Vector3[] groundCheckPoints = {
            transform.position + Vector3.up * 0.5f,
            transform.position + Vector3.up * 0.5f + transform.right * 0.3f,
            transform.position + Vector3.up * 0.5f - transform.right * 0.3f,
            transform.position + Vector3.up * 0.5f + transform.forward * 0.3f,
            transform.position + Vector3.up * 0.5f - transform.forward * 0.3f
        };

        float groundLevel = transform.position.y - 0.01f; // 기본값을 1cm 아래로 설정 (더 자연스럽게)
        bool centerGroundDetected = false;

        foreach (Vector3 checkPoint in groundCheckPoints)
        {
            if (Physics.Raycast(checkPoint, Vector3.down, out var hit, 3.0f, groundLayerMask))
            {
                groundLevel = Mathf.Max(groundLevel, hit.point.y);
                centerGroundDetected = true;
            }
        }

        // 강화된 발 지면 감지 시스템
        Vector3 leftOrigin = transform.position + Vector3.up * raycastHeight;
        Vector3 rightOrigin = transform.position + Vector3.up * raycastHeight;

        // 발 위치에 따라 레이캐스트 위치 조정 (측면 offset 적용)
        leftOrigin += transform.right * -0.3f; // 왼쪽으로 더 넓게 오프셋
        rightOrigin += transform.right * 0.3f; // 오른쪽으로 더 넓게 오프셋

        // 왼발 지면 감지 - 다중 시도 (3번 시도)
        bool leftGroundDetected = false;
        RaycastHit hitL = new RaycastHit();

        // 1차: 발 근처에서 감지
        leftGroundDetected = Physics.Raycast(leftOrigin, Vector3.down, out hitL, raycastDistance, groundLayerMask);

        // 2차: 현재 발 위치에서 감지
        if (!leftGroundDetected)
        {
            leftGroundDetected = Physics.Raycast(smoothLeftFootTarget + Vector3.up * raycastHeight,
                                              Vector3.down, out hitL, raycastDistance, groundLayerMask);
        }

        // 3차: 발가락 끝 위치에서 감지 (ToeBase 기준)
        if (!leftGroundDetected)
        {
            Vector3 leftToePosition = smoothLeftFootTarget + transform.forward * avgFootLength * 0.8f;
            leftGroundDetected = Physics.Raycast(leftToePosition + Vector3.up * raycastHeight,
                                              Vector3.down, out hitL, raycastDistance, groundLayerMask);
        }

        // 4차: 캐릭터 중심에서 감지 (최후 수단)
        if (!leftGroundDetected && centerGroundDetected)
        {
            hitL.point = new Vector3(smoothLeftFootTarget.x, groundLevel, smoothLeftFootTarget.z);
            leftGroundDetected = true;
        }

        // 오른발 지면 감지 - 다중 시도 (4번 시도)
        bool rightGroundDetected = false;
        RaycastHit hitR = new RaycastHit();

        // 1차: 발 근처에서 감지
        rightGroundDetected = Physics.Raycast(rightOrigin, Vector3.down, out hitR, raycastDistance, groundLayerMask);

        // 2차: 현재 발 위치에서 감지
        if (!rightGroundDetected)
        {
            rightGroundDetected = Physics.Raycast(smoothRightFootTarget + Vector3.up * raycastHeight,
                                               Vector3.down, out hitR, raycastDistance, groundLayerMask);
        }

        // 3차: 발가락 끝 위치에서 감지 (ToeBase 기준)
        if (!rightGroundDetected)
        {
            Vector3 rightToePosition = smoothRightFootTarget + transform.forward * avgFootLength * 0.8f;
            rightGroundDetected = Physics.Raycast(rightToePosition + Vector3.up * raycastHeight,
                                               Vector3.down, out hitR, raycastDistance, groundLayerMask);
        }

        // 4차: 캐릭터 중심에서 감지 (최후 수단)
        if (!rightGroundDetected && centerGroundDetected)
        {
            hitR.point = new Vector3(smoothRightFootTarget.x, groundLevel, smoothRightFootTarget.z);
            rightGroundDetected = true;
        }

        // ToeBase 기반 발 위치 최종 조정 (발가락 끝이 지면에 닿도록)
        if (leftGroundDetected)
        {
            // 발가락 끝이 지면에 닿도록 발목 위치를 역산
            Vector3 desiredToePosition = new Vector3(
                smoothLeftFootTarget.x + transform.forward.x * avgFootLength * 0.8f,
                hitL.point.y + footPadding + totalFloorProtection,
                smoothLeftFootTarget.z + transform.forward.z * avgFootLength * 0.8f
            );

            // 발목 위치는 발가락 위치에서 발 길이만큼 뒤로
            smoothLeftFootTarget = desiredToePosition - transform.forward * avgFootLength * 0.8f;

            // 최소 높이 보장
            float leftMinHeight = hitL.point.y + footPadding + totalFloorProtection;
            smoothLeftFootTarget.y = Mathf.Max(smoothLeftFootTarget.y, leftMinHeight);
        }
        else
        {
            // 감지 실패 시 안전한 높이 설정 + 보폭 보호 적용  
            float leftSafeHeight = groundLevel + footPadding + totalFloorProtection;
            smoothLeftFootTarget.y = Mathf.Max(smoothLeftFootTarget.y, leftSafeHeight);
        }

        if (rightGroundDetected)
        {
            // 발가락 끝이 지면에 닿도록 발목 위치를 역산
            Vector3 desiredToePosition = new Vector3(
                smoothRightFootTarget.x + transform.forward.x * avgFootLength * 0.8f,
                hitR.point.y + footPadding + totalFloorProtection,
                smoothRightFootTarget.z + transform.forward.z * avgFootLength * 0.8f
            );

            // 발목 위치는 발가락 위치에서 발 길이만큼 뒤로
            smoothRightFootTarget = desiredToePosition - transform.forward * avgFootLength * 0.8f;

            // 최소 높이 보장
            float rightMinHeight = hitR.point.y + footPadding + totalFloorProtection;
            smoothRightFootTarget.y = Mathf.Max(smoothRightFootTarget.y, rightMinHeight);
        }
        else
        {
            // 감지 실패 시 안전한 높이 설정 + 보폭 보호 적용
            float rightSafeHeight = groundLevel + footPadding + totalFloorProtection;
            smoothRightFootTarget.y = Mathf.Max(smoothRightFootTarget.y, rightSafeHeight);
        }

        // 보폭별 추가 바닥 보호 (15%부터 극강 보호 시작 - 0.2 보폭도 완전 대응)
        if (Mathf.Abs(scaledSpacingForPosition) > 0.15f || Mathf.Abs(effectiveDepthRatio) > 0.15f)
        {
            // 15% 이상 보폭에서 극강 보호 시작 (0.2 보폭 완전 대응)
            float bigStrideProtection = Mathf.Lerp(0f, 0.015f,
                Mathf.Clamp01((Mathf.Max(Mathf.Abs(scaledSpacingForPosition), Mathf.Abs(effectiveDepthRatio)) - 0.15f) / 0.85f));

            // 98% 이상 극한 깊이에서 추가 보호 (기존 로직 유지)
            if (Mathf.Abs(scaledSpacingForPosition) > 0.98f || Mathf.Abs(effectiveDepthRatio) > 0.98f)
            {
                float extraProtection = Mathf.Lerp(0f, 0.005f,
                    Mathf.Clamp01((Mathf.Max(Mathf.Abs(scaledSpacingForPosition), Mathf.Abs(effectiveDepthRatio)) - 0.98f) / 0.02f));
                bigStrideProtection = Mathf.Max(bigStrideProtection, extraProtection);
            }

            float strideMinHeight = groundLevel + footPadding + totalFloorProtection + bigStrideProtection;
            smoothLeftFootTarget.y = Mathf.Max(smoothLeftFootTarget.y, strideMinHeight);
            smoothRightFootTarget.y = Mathf.Max(smoothRightFootTarget.y, strideMinHeight);
        }

        // 러닝맨 특화 깊이 보폭 보호 (glideDepthBonus 고려) - 15% 부터 극강 보호 시작
        if (inRunning && Mathf.Abs(effectiveDepthRatio) > 0.15f)
        {
            // 러닝맨에서 깊이 보폭이 15% 이상일 때부터 보호 시작 (0.2 보폭 완전 대응)
            float runningDepthProtection = 0f;
            float absEffectiveDepth = Mathf.Abs(effectiveDepthRatio);

            // 15%~100% 범위에서 극강 보호 적용 (최대 15mm)
            if (absEffectiveDepth > 0.15f)
            {
                runningDepthProtection = Mathf.Lerp(0f, 0.015f, (absEffectiveDepth - 0.15f) / 0.85f);
            }

            // 98% 이상 극한 깊이에서 추가 보호 (기존 로직 유지)
            if (absEffectiveDepth > 0.98f)
            {
                float extraProtection = Mathf.Lerp(0f, 0.005f, (absEffectiveDepth - 0.98f) / 0.02f);
                runningDepthProtection = Mathf.Max(runningDepthProtection, extraProtection);
            }

            // 러닝맨에서 음수 depthRatio (발이 앞으로 나오는 경우) 특별 극강 보호
            if (effectiveDepthRatio < -0.15f)
            {
                // 깊이 보폭이 -15% 이하일 때부터 극강 보호 시작 (0.2 보폭 완전 대응)
                float negativeDepthProtection = Mathf.Lerp(0f, 0.02f, (Mathf.Abs(effectiveDepthRatio) - 0.15f) / 0.85f);
                runningDepthProtection = Mathf.Max(runningDepthProtection, negativeDepthProtection);
            }

            if (runningDepthProtection > 0f)
            {
                float runningMinHeight = groundLevel + footPadding + totalFloorProtection + runningDepthProtection;
                smoothLeftFootTarget.y = Mathf.Max(smoothLeftFootTarget.y, runningMinHeight);
                smoothRightFootTarget.y = Mathf.Max(smoothRightFootTarget.y, runningMinHeight);

                // 러닝맨 깊이 보폭 디버그 (1초마다)
                if (Time.time % 1f < Time.deltaTime)
                {
                    Debug.Log($"러닝맨 깊이 보폭 보호 (15%부터 극강): effectiveDepthRatio={effectiveDepthRatio:F2}, 보호높이={runningDepthProtection:F4}, 글라이드보너스={glideDepthBonus:F2}");
                }
            }
        }

        // 절대 최소 높이 강제 적용 (물리 콜라이더 고려) - 15%부터 극강 보호
        float absoluteMinHeight = groundLevel + 0.001f + totalFloorProtection; // 기본 1mm + 보폭 보호만

        // 15% 이상 보폭에서 극강 안전 장치
        if (Mathf.Abs(scaledSpacingForPosition) > 0.15f || Mathf.Abs(effectiveDepthRatio) > 0.15f)
        {
            // 15% 이상 보폭에서 극강 안전 장치를 극강으로 설정
            float extraSafetyHeight = groundLevel + 0.02f + totalFloorProtection; // 20mm 극강 보호
            absoluteMinHeight = Mathf.Max(absoluteMinHeight, extraSafetyHeight);

            // 디버그 로그 (1초마다)
            if (Time.time % 1f < Time.deltaTime)
            {
                Debug.Log($"25% 이상 보폭 절대 안전 보호 (극강): spacing={scaledSpacingForPosition:F2}, depth={effectiveDepthRatio:F2}, 추가보호높이={extraSafetyHeight:F4}");
            }
        }

        smoothLeftFootTarget.y = Mathf.Max(smoothLeftFootTarget.y, absoluteMinHeight);
        smoothRightFootTarget.y = Mathf.Max(smoothRightFootTarget.y, absoluteMinHeight);

        // 캐릭터 콜라이더 하단 기준 최소 높이 (CapsuleCollider 고려) - 여유공간 더욱 감소
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        float colliderBottomY = transform.position.y - 0.9f; // 기본값
        if (capsule != null)
        {
            // 실제 CapsuleCollider 높이 사용 (여유공간 1cm로 감소)
            colliderBottomY = transform.position.y + capsule.center.y - (capsule.height * 0.5f) + 0.01f; // 1cm 여유
        }
        smoothLeftFootTarget.y = Mathf.Max(smoothLeftFootTarget.y, colliderBottomY);
        smoothRightFootTarget.y = Mathf.Max(smoothRightFootTarget.y, colliderBottomY);

        // 극한 보폭 시 추가 보호 (95% 이상에서만 작동하도록 완전 극소화)
        if (Mathf.Abs(scaledSpacingForPosition) > 0.95f || Mathf.Abs(effectiveDepthRatio) > 0.95f)
        {
            float extremeProtection = groundLevel + 0.02f; // 2cm 강제 보호 (극강 보호)
            smoothLeftFootTarget.y = Mathf.Max(smoothLeftFootTarget.y, extremeProtection);
            smoothRightFootTarget.y = Mathf.Max(smoothRightFootTarget.y, extremeProtection);

            // 디버그 로그
            if (Time.time % 1f < Time.deltaTime)
            {
                Debug.Log($"극한 보폭 보호 적용: spacing={scaledSpacingForPosition:F2}, depth={effectiveDepthRatio:F2}, 보호높이={extremeProtection:F2}");
            }
        }

        // 러닝맨 글라이딩+깊이 보폭 결합 특별 보호 (15% 수준부터 극강)
        if (inRunning && glideAmount > 0.15f && Mathf.Abs(effectiveDepthRatio) > 0.15f)
        {
            // 러닝맨에서 글라이딩 15% 이상 + 깊이 보폭 15% 이상일 때부터 보호 시작 (0.2 보폭 완전 대응)
            float combinedProtection = 0f;
            float glideDepthCombined = glideAmount + (Mathf.Abs(effectiveDepthRatio) * 0.2f); // 깊이 보폭의 20%만 고려

            // 15% 수준에서 극강 보호 시작 (0.2 보폭 완전 대응)
            if (glideDepthCombined > 0.15f)
            {
                // 15%~150% 범위에서 극강 보호 적용 (최대 20mm)
                combinedProtection = Mathf.Lerp(0f, 0.02f, Mathf.Clamp01((glideDepthCombined - 0.15f) / 1.35f));
            }

            // 100% 이상에서 추가 보호 (기존 로직 유지)
            if (glideDepthCombined > 1.0f)
            {
                // 결합값이 100% 이상일 때 추가 극강 보호 (최대 5mm)
                float extraProtection = Mathf.Lerp(0f, 0.005f, Mathf.Clamp01((glideDepthCombined - 1.0f) / 0.5f));
                combinedProtection = Mathf.Max(combinedProtection, extraProtection);
            }

            if (combinedProtection > 0f)
            {
                float combinedMinHeight = groundLevel + footPadding + totalFloorProtection + combinedProtection;
                smoothLeftFootTarget.y = Mathf.Max(smoothLeftFootTarget.y, combinedMinHeight);
                smoothRightFootTarget.y = Mathf.Max(smoothRightFootTarget.y, combinedMinHeight);

                // 러닝맨 결합 보호 디버그 (1초마다)
                if (Time.time % 1f < Time.deltaTime)
                {
                    Debug.Log($"러닝맨 글라이딩+깊이 결합 보호 (15%부터 극강): glide={glideAmount:F2}, depth={effectiveDepthRatio:F2}, 결합값={glideDepthCombined:F2}, 보호높이={combinedProtection:F4}");
                }
            }
        }

        // 디버그 레이 표시 (Scene 뷰에서 확인 가능)
        Debug.DrawRay(leftOrigin, Vector3.down * raycastDistance, leftGroundDetected ? Color.green : Color.red, 0.1f);
        Debug.DrawRay(rightOrigin, Vector3.down * raycastDistance, rightGroundDetected ? Color.green : Color.red, 0.1f);

        // 발목 위치 (노란색)
        Debug.DrawLine(smoothLeftFootTarget, smoothLeftFootTarget + Vector3.up * 0.1f, Color.yellow, 0.1f);
        Debug.DrawLine(smoothRightFootTarget, smoothRightFootTarget + Vector3.up * 0.1f, Color.yellow, 0.1f);

        // 발가락 끝 위치 (파란색) - ToeBase 기준
        Vector3 leftToePos = smoothLeftFootTarget + transform.forward * avgFootLength * 0.8f;
        Vector3 rightToePos = smoothRightFootTarget + transform.forward * avgFootLength * 0.8f;
        Debug.DrawLine(leftToePos, leftToePos + Vector3.up * 0.15f, Color.blue, 0.1f);
        Debug.DrawLine(rightToePos, rightToePos + Vector3.up * 0.15f, Color.blue, 0.1f);

        // 발 윤곽선 표시 (발목과 발가락 끝 연결)
        Debug.DrawLine(smoothLeftFootTarget, leftToePos, Color.cyan, 0.1f);
        Debug.DrawLine(smoothRightFootTarget, rightToePos, Color.cyan, 0.1f);

        // 보폭 디버그 정보 (2초마다 출력) - 애니메이션별 적용 상태 표시
        if (Time.time % 2f < Time.deltaTime && (Mathf.Abs(scaledSpacingForPosition) > 0.15f || Mathf.Abs(effectiveDepthRatio) > 0.15f))
        {
            string animationState = inSpongebob ? "스폰지밥" : (inRunning ? "러닝맨" : "기타");
            string activeParameter = "";

            if (inSpongebob && Mathf.Abs(scaledSpacingForPosition) > 0.01f)
            {
                activeParameter = $"Spacing Ratio: {spacingRatio:F2} (좌우 보폭)";
            }
            else if (inRunning && Mathf.Abs(effectiveDepthRatio) > 0.01f)
            {
                activeParameter = $"Depth Ratio: {depthRatio:F2} (앞뒤 보폭)";
            }
            else
            {
                activeParameter = "없음 (해당 애니메이션에서 비활성)";
            }

            Debug.Log($"애니메이션별 보폭 제어 [{animationState}]: {activeParameter}, " +
                     $"보호높이={totalFloorProtection:F3}, 왼발Y={smoothLeftFootTarget.y:F3}, 오른발Y={smoothRightFootTarget.y:F3}");
        }

        // 최종 절대 안전 장치 - 15%부터 바닥 뚫기 완전 차단 (0.2 보폭 완전 대응)
        float finalMinHeight = groundLevel + 0.001f + totalFloorProtection; // 기본 1mm + 보폭 보호

        // 15% 이상 보폭에서 최종 안전 높이 극강 보호
        if (Mathf.Abs(scaledSpacingForPosition) > 0.15f || Mathf.Abs(effectiveDepthRatio) > 0.15f)
        {
            // 15% 이상 보폭에서 최종 안전 장치를 극강으로 설정
            float finalSafetyHeight = groundLevel + 0.025f + totalFloorProtection; // 25mm 최종 극강 보호
            finalMinHeight = Mathf.Max(finalMinHeight, finalSafetyHeight);
        }

        smoothLeftFootTarget.y = Mathf.Max(smoothLeftFootTarget.y, finalMinHeight);
        smoothRightFootTarget.y = Mathf.Max(smoothRightFootTarget.y, finalMinHeight);

        // IK 가중치 계산: 보폭 조정 또는 glide 효과가 있을 때 적용
        float rawGlideWeight = Mathf.Clamp01((glideAmount / glideMaxThreshold) * 1.2f);

        // 글라이딩 시 IK 가중치 조정 (리니어하게 수정)
        float glideWeight;
        if (glideAmount <= 0.2f)
        {
            // 0.2까지는 정상 가중치 (원본 애니메이션 효과 유지)
            glideWeight = rawGlideWeight;
        }
        else
        {
            // 0.2~0.6 구간: 리니어하게 가중치 감소 (100%→50%)
            // 0.2일 때 100%, 0.4일 때 75%, 0.6일 때 50%
            float t = (glideAmount - 0.2f) / 0.4f; // 0.2에서 0.6까지의 정규화된 값
            float reductionFactor = Mathf.Lerp(1.0f, 0.5f, t);
            glideWeight = rawGlideWeight * reductionFactor;
        }

        // 로그 추가 - 값 변화 디버깅 (0.1 단위로 값이 변경될 때만 출력)
        if (Mathf.Abs(glideAmount - Mathf.Round(glideAmount * 10) / 10) < 0.01f && Time.time % 0.5f < Time.deltaTime)
        {
            Debug.Log($"글라이딩 IK 가중치: glideAmount={glideAmount:F2}, rawGlideWeight={rawGlideWeight:F2}, glideWeight={glideWeight:F2}");
        }

        // 보폭 가중치 계산 - 애니메이션별로 적용
        float spacingWeightFactor = 0f;
        float depthWeightFactor = 0f;

        if (inSpongebob)
        {
            // 스폰지밥: spacingRatio만 고려 (좌우 보폭)
            spacingWeightFactor = Mathf.Clamp01(Mathf.Abs(scaledSpacingForPosition) * 1.0f);
            depthWeightFactor = 0f; // depthRatio 무시
        }
        else if (inRunning)
        {
            // 러닝맨: depthRatio만 고려 (앞뒤 보폭)
            spacingWeightFactor = 0f; // spacingRatio 무시
            depthWeightFactor = Mathf.Clamp01(Mathf.Abs(depthRatio) * 0.6f);
        }
        else
        {
            // 기타 애니메이션: 둘 다 무시
            spacingWeightFactor = 0f;
            depthWeightFactor = 0f;
        }

        float spacingWeight = Mathf.Max(spacingWeightFactor, depthWeightFactor);

        // 전체 IK 가중치 계산
        float ikWeight = Mathf.Max(glideWeight, spacingWeight);

        // 디버그: IK 가중치 계산 정보 출력 (1초마다) - 애니메이션별 상태 표시
        if (Time.time % 1f < Time.deltaTime)
        {
            string animationState = inSpongebob ? "스폰지밥" : (inRunning ? "러닝맨" : "기타");
            string activeControl = "";

            if (inSpongebob)
            {
                activeControl = $"Spacing만 적용: spacingWeight={spacingWeightFactor:F3}";
            }
            else if (inRunning)
            {
                activeControl = $"Depth만 적용: depthWeight={depthWeightFactor:F3}";
            }
            else
            {
                activeControl = "보폭 제어 비활성";
            }

            Debug.Log($"IK Weight 계산 [{animationState}]: {activeControl}, glideWeight={glideWeight:F3}, 최종 ikWeight={ikWeight:F3}");
        }

        // ToeBase 기반 발 위치 최종 적용 + 킥아웃 오프셋 + Hop 오프셋 + Hop2 오프셋 + 크로스 오프셋
        Vector3 finalLeftFootPos = smoothLeftFootTarget + leftFootKickOffset + leftFootHopOffset + leftFootHop2Offset + leftFootCrossOffset;
        Vector3 finalRightFootPos = smoothRightFootTarget + rightFootKickOffset + rightFootHopOffset + rightFootHop2Offset + rightFootCrossOffset;
        
        // Hop2 모드일 때 강제 디버그
        if (isHop2Mode)
        {
            Debug.Log($"🔍 Final Position 계산:");
            Debug.Log($"   smoothLeftFootTarget: {smoothLeftFootTarget}");
            Debug.Log($"   leftFootHop2Offset: {leftFootHop2Offset}");
            Debug.Log($"   finalLeftFootPos: {finalLeftFootPos}");
            Debug.Log($"   차이: {(finalLeftFootPos - smoothLeftFootTarget).magnitude:F3}");
        }
        
        // Hop2 오프셋 적용 (다른 모드가 활성화되지 않았을 때 추가 적용)
        if (isHop2Mode && !isKickOutMode && !isHopMode && !isShuffleCrossMode)
        {
            // Hop2 IK 적용 디버깅
            if (Time.time % 1f < Time.deltaTime)
            {
                Debug.Log($"🐰2[IK 적용] Hop2 IK 적용됨! 왼발오프셋:{leftFootHop2Offset:F3}, 오른발오프셋:{rightFootHop2Offset:F3}");
            }
        }
        
        // 킥아웃, Hop, Hop2, 크로스 모드일 때 IK 가중치를 강제로 최대로 설정
        float finalIkWeight = (isKickOutMode || isHopMode || isHop2Mode || isShuffleCrossMode) ? 1.0f : ikWeight;
        
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, finalIkWeight);
        animator.SetIKPosition(AvatarIKGoal.LeftFoot, finalLeftFootPos);

        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, finalIkWeight);
        animator.SetIKPosition(AvatarIKGoal.RightFoot, finalRightFootPos);
        
        // 킥아웃 활성 시 로그 숨김 (가속도 계산 로그로 대체)
        if (isKickOutMode)
        {
            // 기본발위치 vs 최종위치 로그 숨김
        }
        
        // Hop 활성 시 디버그 로그
        if (isHopMode)
        {
            Debug.Log($"🚀🐰⚡ Hop IK 적용! IK가중치={finalIkWeight:F2}, 왼발최종위치={finalLeftFootPos}, 오른발최종위치={finalRightFootPos}");
            Debug.Log($"🎯 기본발위치 vs 최종위치 - 왼발: {smoothLeftFootTarget} → {finalLeftFootPos}, 차이: {leftFootHopOffset}");
        }
        
        // Hop2 활성 시 디버그 로그
        if (isHop2Mode)
        {
            Debug.Log($"🚀🐰2⚡ Hop2 IK 적용! IK가중치={finalIkWeight:F2}, 왼발최종위치={finalLeftFootPos}, 오른발최종위치={finalRightFootPos}");
            Debug.Log($"🎯 기본발위치 vs 최종위치 - 왼발: {smoothLeftFootTarget} → {finalLeftFootPos}, 차이: {leftFootHop2Offset}");
            Debug.Log($"🔍 Hop2 오프셋 상세 - 왼발오프셋:{leftFootHop2Offset:F3}, 오른발오프셋:{rightFootHop2Offset:F3}");
            if (leftFootHop2Offset.magnitude > 0.001f || rightFootHop2Offset.magnitude > 0.001f)
            {
                Debug.Log($"✅ Hop2 오프셋이 활성화되어 있음! 왼발크기:{leftFootHop2Offset.magnitude:F3}, 오른발크기:{rightFootHop2Offset.magnitude:F3}");
            }
            else
            {
                Debug.Log($"❌ Hop2 오프셋이 0임! 계산 로직 확인 필요");
            }
        }
        
        // 크로스 활성 시 디버그 로그
        if (isShuffleCrossMode)
        {
            Debug.Log($"🚀🦵⚡ 셔플 크로스 IK 적용! IK가중치={finalIkWeight:F2}, 왼발최종위치={finalLeftFootPos}, 오른발최종위치={finalRightFootPos}");
            Debug.Log($"🎯 기본발위치 vs 최종위치 - 왼발: {smoothLeftFootTarget} → {finalLeftFootPos}, 차이: {leftFootCrossOffset}");
        }

        // ToeBase 기반 발 회전 조정 (지면에 더 자연스럽게 맞도록)
        if (leftGroundDetected || rightGroundDetected)
        {
            // 발 회전 조정을 위한 IK 회전 가중치 설정 
            float footRotationWeight = ikWeight * 0.5f; // ToeBase 고려로 회전 가중치 약간 증가

            if (leftGroundDetected)
            {
                // 발가락 끝 기준으로 지면 각도 계산
                Vector3 leftToePosition = smoothLeftFootTarget + transform.forward * avgFootLength * 0.8f;
                Vector3 footDirection = (leftToePosition - smoothLeftFootTarget).normalized;

                // 지면 법선에 맞춰 왼발 회전 조정 (발가락 끝이 지면에 닿도록)
                Quaternion leftFootRotation = Quaternion.FromToRotation(Vector3.up, hitL.normal)
                                           * Quaternion.LookRotation(footDirection, hitL.normal);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, footRotationWeight);
                animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftFootRotation);
            }

            if (rightGroundDetected)
            {
                // 발가락 끝 기준으로 지면 각도 계산
                Vector3 rightToePosition = smoothRightFootTarget + transform.forward * avgFootLength * 0.8f;
                Vector3 footDirection = (rightToePosition - smoothRightFootTarget).normalized;

                // 지면 법선에 맞춰 오른발 회전 조정 (발가락 끝이 지면에 닿도록)
                Quaternion rightFootRotation = Quaternion.FromToRotation(Vector3.up, hitR.normal)
                                            * Quaternion.LookRotation(footDirection, hitR.normal);
                animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, footRotationWeight);
                animator.SetIKRotation(AvatarIKGoal.RightFoot, rightFootRotation);
            }
        }
        else
        {
            // 지면이 감지되지 않으면 회전은 적용하지 않음
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
        }

        // 무릎 힌트 - 상체 굽히기와 완전 분리하여 처리
        // glideAmount는 0으로 고정되었으므로, 무릎 IK는 기본적인 애니메이션 기반으로만 동작
        // 기본 무릎 굽힘 팩터 (애니메이션 타입별로 다르게 설정)
        float baseKneeFactor = 0f;
        if (inSpongebob)
        {
            baseKneeFactor = 0.3f; // 스폰지밥: 기본 30% 무릎 굽힘
        }
        else if (inRunning)
        {
            baseKneeFactor = 0.2f; // 러닝맨: 기본 20% 무릎 굽힘  
        }
        else
        {
            baseKneeFactor = 0.1f; // 걷기: 기본 10% 무릎 굽힘
        }

        float adjustedGlideKneeFactor = baseKneeFactor;

        // 점프 준비 상태에서 웅크림 깊이 적용
        if (jumpState == JumpState.WaitingForUp && isJumpInProgress)
        {
            // 웅크림 깊이에 비례하여 무릎 굽힘 강화
            float crouchKneeBoost = currentCrouchDepth / maxCrouchDepth; // 0~1 범위
            adjustedGlideKneeFactor = Mathf.Lerp(baseKneeFactor, 1.0f, crouchKneeBoost);

            // 웅크림 상태 디버그 로그
            if (Time.time % 0.5f < Time.deltaTime)
            {
                Debug.Log($"[점프 준비 웅크림] currentCrouchDepth={currentCrouchDepth:F3}, crouchKneeBoost={crouchKneeBoost:F2}, adjustedKneeFactor={adjustedGlideKneeFactor:F2}");
            }
        }

        // 로그 추가 - 무릎 IK 상태 디버깅 (상체 굽히기와 분리 확인)
        if (Time.time % 2f < Time.deltaTime && (adjustedGlideKneeFactor > 0.01f || bodyLeanAmount > 0.01f))
        {
            Debug.Log($"무릎 IK (상체굽히기 분리): baseKneeFactor={baseKneeFactor:F2}, adjustedGlideKneeFactor={adjustedGlideKneeFactor:F2}, bodyLeanAmount={bodyLeanAmount:F2}");
        }

        // 보폭에 따른 무릎 굽힘 제거 - 상체 굽히기와 분리하여 기본 무릎 처리
        float combinedKneeFactor = adjustedGlideKneeFactor;

        // 기본 kneeLiftZ 값 (상체 굽히기와 무관하게 애니메이션별 기본값 사용)
        float kneeLiftZ = Mathf.Lerp(0.05f, maxKneeLiftZ, combinedKneeFactor);

        // 무릎 높이 조정 - 상체 굽히기와 분리하여 애니메이션 타입에 따라 설정
        float minKneeHeight = 0.1f;  // 최소 무릎 높이
        float maxKneeHeight = 0.45f; // 최대 무릎 높이

        // 상체 굽히기와 관계없이 기본 무릎 높이 계산
        float baseKneeHeight = Mathf.Lerp(minKneeHeight, maxKneeHeight, combinedKneeFactor);
        float kneeHeight = baseKneeHeight; // glideKneeHeightFactor 제거로 단순화

        // 점프 준비 상태에서 웅크림 깊이에 따른 무릎 높이 조정
        if (jumpState == JumpState.WaitingForUp && isJumpInProgress)
        {
            // 웅크림 깊이만큼 무릎 높이 감소
            kneeHeight -= currentCrouchDepth;
            // 최소 높이 보장
            kneeHeight = Mathf.Max(kneeHeight, 0.05f);

            // 웅크림 시 무릎을 더 앞쪽으로 이동 (더 깊은 웅크림)
            float crouchForwardBoost = currentCrouchDepth / maxCrouchDepth;
            kneeLiftZ += crouchForwardBoost * 0.2f; // 최대 20cm 추가 전진
        }

        // 최소 높이 보장 (바닥 뚫기 방지)
        kneeHeight = Mathf.Max(kneeHeight, 0.08f);

        // 로그 추가 - 무릎 상태 디버깅 (상체 굽히기와 분리 확인)
        if (Time.time % 2f < Time.deltaTime && (adjustedGlideKneeFactor > 0.01f || bodyLeanAmount > 0.01f))
        {
            Debug.Log($"무릎 처리 (상체굽히기 분리): kneeLiftZ={kneeLiftZ:F2}, kneeHeight={kneeHeight:F2}, bodyLeanAmount={bodyLeanAmount:F2}");
        }

        // 발 위치 기준으로 무릎 힌트 계산 (바닥 뚫기 현상 방지)
        // depthRatio에 따른 무릎 위치 조정 - 보폭이 크거나 감소할 때 높이와 전방 이동 조절
        float kneeForwardOffset = kneeLiftZ;

        // depthRatio가 음수일 때(발이 앞으로 오는 경우) 무릎을 더 앞쪽으로 이동
        if (depthRatio < 0)
        {
            // 보폭이 작아질숝 무릎이 더 앞으로 위치하도록 함
            kneeForwardOffset = Mathf.Lerp(kneeLiftZ, kneeLiftZ * 2.0f, Mathf.Abs(depthRatio));
        }

        // 발 위치를 기반으로 무릎 힌트 설정
        Vector3 lHintTarget = smoothLeftFootTarget + transform.up * kneeHeight + transform.forward * kneeForwardOffset;
        Vector3 rHintTarget = smoothRightFootTarget + transform.up * kneeHeight + transform.forward * kneeForwardOffset;

        // 무릎 힌트가 지면/발보다 항상 높게 유지되도록 높이 체크
        float baseHeight = transform.position.y;
        float feetHeight = Mathf.Max(smoothLeftFootTarget.y, smoothRightFootTarget.y);
        float minKneeY = Mathf.Max(baseHeight + 0.1f, feetHeight + 0.15f);

        // 무릎이 바닥을 뚫지 않도록 최소 높이 강제 적용
        if (lHintTarget.y < minKneeY) lHintTarget.y = minKneeY;
        if (rHintTarget.y < minKneeY) rHintTarget.y = minKneeY;

        // depthRatio에 따른 추가 조정 - 보폭이 커질수록 무릎이 더 앞쪽으로
        if (depthRatio < -0.3f)
        {
            float depthFactor = Mathf.Clamp01((-depthRatio - 0.3f) / 0.7f); // 0.3-1.0 범위 정규화
            float extraForward = depthFactor * 0.2f; // 최대 20cm 추가 전진

            lHintTarget += transform.forward * extraForward;
            rHintTarget += transform.forward * extraForward;
        }

        // 무릎 굽힘 가시성 개선을 위한 응답성 조정
        // 글라이딩 시 무릎 효과를 더 뚜렷하게 표현하기 위해 빠른 반응속도 유지
        float kneeResponseTime = 0.01f; // 기존 0.03~0.04에서 0.01로 더 빠르게
        Vector3 lHint = Vector3.SmoothDamp(animator.GetIKHintPosition(AvatarIKHint.LeftKnee), lHintTarget, ref leftHintVel, kneeResponseTime);
        Vector3 rHint = Vector3.SmoothDamp(animator.GetIKHintPosition(AvatarIKHint.RightKnee), rHintTarget, ref rightHintVel, kneeResponseTime);

        // IK 힌트 가중치 조정 - 상체 굽히기와 분리하여 기본 IK 가중치만 사용
        float kneeHintWeight = ikWeight;

        // 상체 굽히기와 무관하게 기본 무릎 힌트 가중치 사용 (glideBoost 제거)
        // 애니메이션 타입에 따른 기본 무릎 굽힘만 적용

        animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, kneeHintWeight);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, kneeHintWeight);
        animator.SetIKHintPosition(AvatarIKHint.LeftKnee, lHint);
        animator.SetIKHintPosition(AvatarIKHint.RightKnee, rHint);

        // === 상체 굽히기 IK 시스템 (Spine 회전만 사용) ===
        // bodyLeanAmount가 있을 때만 추가 효과 적용 (Body Position은 건드리지 않음)
        if (bodyLeanAmount > 0.01f && animator.isHuman)
        {
            // 상체 굽히기는 LateUpdate의 Spine 회전으로만 처리
            // 여기서는 팔 위치만 약간 조정 (선택사항)
            if (bodyLeanAmount > 0.5f) // 50% 이상 굽힐 때만 팔 위치 조정
            {
                // 팔 IK 가중치 설정 (매우 가벼운 조정)
                float handIkWeight = (bodyLeanAmount - 0.5f) * 0.2f; // 최대 10% 가중치
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, handIkWeight);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, handIkWeight);

                // 팔을 약간 앞으로 내리는 자연스러운 자세 (매우 미세한 조정)
                Vector3 leftHandPos = animator.GetIKPosition(AvatarIKGoal.LeftHand);
                Vector3 rightHandPos = animator.GetIKPosition(AvatarIKGoal.RightHand);

                leftHandPos += transform.forward * bodyLeanAmount * 0.05f; // 앞으로 (매우 작게)
                leftHandPos += Vector3.down * bodyLeanAmount * 0.03f; // 아래로 (매우 작게)
                rightHandPos += transform.forward * bodyLeanAmount * 0.05f; // 앞으로 (매우 작게)
                rightHandPos += Vector3.down * bodyLeanAmount * 0.03f; // 아래로 (매우 작게)

                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPos);
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPos);
            }
            else
            {
                // 상체 굽히기가 적을 때는 팔 IK 비활성화
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            }

            // 디버그 정보 (2초마다) - Body Position 조정 제거
            if (Time.time % 2f < Time.deltaTime)
            {
                Debug.Log($"상체 IK 적용 (Spine 회전만): bodyLeanAmount={bodyLeanAmount:F2}, 팔 IK만 적용");
            }
        }
        else
        {
            // 상체 굽히기가 없을 때는 팔 IK 완전 비활성화
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
        }
        // === [새로 추가] 상체 굽히기/젖히기(A값/qX) 최종 적용 시스템 ===
        // OnAnimatorIK의 마지막 단계에서 실행하여 다른 IK 효과와 충돌하지 않도록 합니다.
        if (spine != null && Mathf.Abs(bodyLeanAmount) > 0.01f)
        {
            // 1. 목표 각도 계산
            // bodyLeanAmount는 Update()에서 실시간으로 계산됩니다 (-0.5: 뒤로 젖히기, 0: 중립, 1: 앞으로 굽히기)
            float targetBodyLeanAngle;
            if (bodyLeanAmount > 0)
            {
                // 양수: 앞으로 굽히기
                targetBodyLeanAngle = bodyLeanAmount * maxBodyLeanForwardAngle;
            }
            else
            {
                // 음수: 뒤로 젖히기
                targetBodyLeanAngle = bodyLeanAmount * maxBodyLeanBackAngle; // bodyLeanAmount가 음수이므로 결과는 음수
            }

            // 2. 현재 각도를 목표 각도로 부드럽게 보간
            // currentBodyLeanAngle은 클래스 변수로 선언되어 있어야 합니다.
            currentBodyLeanAngle = Mathf.SmoothDamp(currentBodyLeanAngle, targetBodyLeanAngle, ref bodyLeanVelocity, 0.1f);
            
            // 3. 최종 회전값 계산
            // spineInitLocalRot는 Awake()에서 저장된 초기 회전값입니다.
            // Quaternion 곱셈을 사용하여 초기 회전에 추가적인 굽힘을 적용합니다.
            Quaternion leanRotation = Quaternion.Euler(currentBodyLeanAngle, 0f, 0f);
            
            // 4. 최종 계산된 회전값을 Spine 뼈에 직접 적용
            animator.SetBoneLocalRotation(HumanBodyBones.Spine, spineInitLocalRot * leanRotation);
        }
        else if (spine != null)
        {
            // bodyLeanAmount가 0에 가까우면 초기 상태로 복원
            currentBodyLeanAngle = Mathf.SmoothDamp(currentBodyLeanAngle, 0f, ref bodyLeanVelocity, 0.1f);
            Quaternion leanRotation = Quaternion.Euler(currentBodyLeanAngle, 0f, 0f);
            animator.SetBoneLocalRotation(HumanBodyBones.Spine, spineInitLocalRot * leanRotation);
        }
    }

    // FootIKController.cs - OnSpinComplete() 메서드

    public void OnSpinComplete()
    {
        if (spinCompleted)
        {
            Debug.Log("스핀 완료 이미 처리됨 - 중복 호출 무시");
            return;
        }

        Debug.Log("스핀 애니메이션 완료!");
        spinCompleted = true; // 중복 방지 플래그

        // === 모든 애니메이터 파라미터 완전 초기화 ===
        // Bool 파라미터 모두 리셋 (여기서 리셋하고 아래에서 다시 설정)
        animator.SetBool("isWalkingLast", false);
        animator.SetBool("isRunningmanLast", false);
        animator.SetBool("isSpongebobLast", false);

        // 모든 트리거 완전 리셋 (안전장치)
        animator.ResetTrigger("ToWalking");
        animator.ResetTrigger("ToRunningman");
        animator.ResetTrigger("ToSpongebob");
        animator.ResetTrigger("ToSpin");

        // 회전 상태만 초기화
        isDirectionLocked = false;
        angleHoldTime = 0f;
        isRotating = false;

        // 애니메이션이 끝난 후의 실제 Y 회전값을 새로운 기준으로 설정
        float finalRotation = transform.eulerAngles.y;
        targetYRotation = finalRotation;
        initialYRotation = finalRotation;
        maxReachedAngle = finalRotation;

        Debug.Log($"스핀 완료 - 애니메이션 후 방향: {finalRotation:F1}도를 새 기준으로 설정");

        // === 지연 후 정확한 상태로 복귀 ===
        StartCoroutine(DelayedReturnToPreviousDance());
    }



   // FootIKController.cs - DelayedReturnToPreviousDance 코루틴

    private System.Collections.IEnumerator DelayedReturnToPreviousDance()
    {
        // 스핀 애니메이션이 완전히 끝날 때까지 충분히 대기
        // (스핀 애니메이션의 실제 재생 시간보다 약간 길게 설정)
        yield return new WaitForSeconds(0.1f); // 0.1초 대기 (더 짧게 시도)

        // 모든 Bool 파라미터 초기화 (안전장치)
        animator.SetBool("isWalkingLast", false);
        animator.SetBool("isRunningmanLast", false);
        animator.SetBool("isSpongebobLast", false);
        
        // 모든 트리거 리셋 (안전장치)
        animator.ResetTrigger("ToRunningman");
        animator.ResetTrigger("ToSpongebob");
        animator.ResetTrigger("ToWalking");
        animator.ResetTrigger("ToSpin");

        // 애니메이션 전환 시간 (매우 짧게 설정)
        float transitionDuration = 0.05f; // 아주 짧은 시간 (50ms)

        // 이전 댄스 상태로 복귀 (CrossFade 사용)
        switch (lastDance)
        {
            case Dance.Walking:
                animator.SetBool("isWalkingLast", true); // 해당 Bool만 true로 설정
                animator.CrossFade("walking", transitionDuration);
                Debug.Log($"스핀 완료 - '걷기'로 즉시 CrossFade (지연 {transitionDuration}s)");
                break;

            case Dance.Runningman:
                animator.SetBool("isRunningmanLast", true); // 해당 Bool만 true로 설정
                animator.CrossFade("RunningmanRecentSensing_jin", transitionDuration);
                Debug.Log($"스핀 완료 - '러닝맨'으로 즉시 CrossFade (지연 {transitionDuration}s)");
                break;

            case Dance.Spongebob:
                animator.SetBool("isSpongebobLast", true); // 해당 Bool만 true로 설정
                animator.CrossFade("SpongebobRecentsension_jin", transitionDuration);
                Debug.Log($"스핀 완료 - '스폰지밥'으로 즉시 CrossFade (지연 {transitionDuration}s)");
                break;

            default: // 기본 걷기
                animator.SetBool("isWalkingLast", true);
                animator.CrossFade("walking", transitionDuration);
                Debug.Log($"스핀 완료 - 기본 '걷기'로 즉시 CrossFade (지연 {transitionDuration}s)");
                break;
        }

        // 다음 스핀을 위해 spinCompleted 플래그 리셋 (애니메이션 전환 후)
        yield return new WaitForSeconds(transitionDuration + 0.05f); // 전환이 완전히 끝날 때까지 대기
        spinCompleted = false;
        Debug.Log("스핀 완료 플래그 리셋");
    }


    private void ResetDirection()
    {
        // [수정] 현재 캐릭터의 실제 Y축 회전 각도를 가져옵니다.
        float currentRotationY = transform.eulerAngles.y;

        // [수정] 모든 회전 관련 변수를 현재 각도를 기준으로 재설정합니다.
        initialYRotation = currentRotationY;  // 새로운 기준점을 현재 각도로 설정
        targetYRotation = currentRotationY;   // 목표 각도도 현재 각도로 설정하여 움직이지 않게 함
        maxReachedAngle = currentRotationY;   // 최대 도달 각도도 현재 각도로 재설정

        // 나머지 상태 변수들은 새로운 제스처를 위해 초기화합니다.
        isDirectionLocked = false;
        lockedDirection = 0f;
        angleHoldTime = 0f;
        currentAngle = 0f;
        isRotating = false;
        keyboardQz = 0f; // 키보드 입력 값 초기화

        Debug.Log($"방향 시스템 재설정 완료! 새로운 기준 각도: {initialYRotation:F1}도");
    }


    // qx 데이터 처리 메서드 - 점프 중일 때만 기능 사용
    private void HandleQxData(float qxValue)
    {
        currentQx = qxValue;
        
        // 일시정지 상태일 때는 데이터만 저장
        if (isPaused)
        {
            return;
        }
        
        // === 점프 모드일 때만 qX 데이터 사용 ===
        if (isJumpInProgress)
        {
            HandleJumpModeQxData(qxValue);
            return;
        }
        
        // === 일반 모드일 때는 qX 데이터 무시 ===
        // qX는 점프 모드에서만 사용 (웅크림/도약)
        //Debug.Log($"[일반 모드 qX] qX={qxValue:F3} - 점프 모드에서만 사용");
    }

    // 점프 모드에서 qX 데이터를 점프 기능으로 재매핑하는 메서드
    private void HandleJumpModeQxData(float qxValue)
    {
        // qX 양수 (기존 스피드업) → 도약 트리거
        if (qxValue >= 0.3f && jumpState == JumpState.WaitingForUp)
        {
            // 강한 양수 신호를 도약 트리거로 사용
            Debug.Log($"[점프 모드] qX 도약 트리거: {qxValue:F3} - 점프 실행!");
            
            // 웅크림 깊이에 따라 점프 높이 계산
            float crouchRatio = targetCrouchDepth / maxCrouchDepth;
            currentJumpHeight = Mathf.Lerp(jumpHeightMin, jumpHeightMax, crouchRatio);
            
            // 점프 실행
            jumpBase = false;
            jumpReady = false;
            jumping = true;
            
            StopAllCoroutines();
            StartCoroutine(ImprovedJump());
        }
        // qX 음수 (기존 스피드다운) → 웅크림 제어 (깊이 고정 시스템 적용)
        else if (qxValue <= -0.15f)
        {
            // === 수정된 웅크림 깊이 제어 시스템 ===
            
            // 웅크림 준비 상태로 전환 (한 번만)
            if (jumpState == JumpState.WaitingForDown && !jumpReady)
            {
                jumpBase = false;
                jumpReady = true;
                jumping = false;
                
                // 새로운 점프 시작 시 웅크림 관련 변수 초기화
                maxReachedCrouchDepth = 0f;
                isCrouchDepthLocked = false;
                isCrouchingMaintained = false;
                
                Debug.Log("웹소켓 qX로 웅크림 모드 진입 - 웅크림 깊이 추적 시작");
                StopAllCoroutines();
                StartCoroutine(ImprovedJump());
            }
            
            // 웅크림 깊이가 고정되지 않은 상태에서만 실시간 조정
            if (!isCrouchDepthLocked && (jumpState == JumpState.WaitingForUp || jumpState == JumpState.WaitingForDown))
            {
                // 음수 강도에 따라 웅크림 깊이 조절
                float crouchIntensity = Mathf.Abs(qxValue); // 0.15 ~ 1.0 범위
                float normalizedIntensity = Mathf.Clamp01((crouchIntensity - 0.15f) / 0.85f); // 0~1로 정규화
                float currentCalculatedDepth = normalizedIntensity * maxCrouchDepth;
                
                // 최대 웅크림 깊이 업데이트 (더 깊게 웅크릴 때만)
                if (currentCalculatedDepth > maxReachedCrouchDepth)
                {
                    maxReachedCrouchDepth = currentCalculatedDepth;
                    targetCrouchDepth = maxReachedCrouchDepth;
                    Debug.Log($"[웅크림 깊이 증가] qX={qxValue:F3} → 새로운 최대 깊이: {maxReachedCrouchDepth:F3}");
                }
                else
                {
                    // 이미 더 깊게 웅크린 적이 있다면 그 깊이 유지
                    targetCrouchDepth = maxReachedCrouchDepth;
                    Debug.Log($"[웅크림 깊이 유지] qX={qxValue:F3}, 현재계산={currentCalculatedDepth:F3}, 유지깊이={maxReachedCrouchDepth:F3}");
                }
            }
            else if (isCrouchDepthLocked)
            {
                // 깊이가 고정된 상태에서는 변경하지 않음
                Debug.Log($"[웅크림 깊이 고정됨] qX={qxValue:F3}, 고정깊이={targetCrouchDepth:F3} 유지");
            }
        }
        // === 중립 범위에서 웅크림 깊이 고정 ===
        else // qX 중립 범위 (-0.15 ~ 0.3)
        {
            // 웅크림 상태였고 현재 웅크림 깊이가 있으면서 아직 고정되지 않았을 때 깊이 고정
            if (jumpState == JumpState.WaitingForUp && targetCrouchDepth > 0.01f && !isCrouchDepthLocked)
            {
                // 웹소켓 웅크림 깊이 고정
                isCrouchDepthLocked = true;
                isCrouchingMaintained = true;
                
                Debug.Log($"[웹소켓 웅크림 고정] qX 중립 진입 → 깊이 고정: {targetCrouchDepth:F3}");
            }
            
            // 고정 모드 중일 때 로그 (2초마다)
            if (isCrouchDepthLocked && Time.time % 2f < Time.deltaTime)
            {
                Debug.Log($"[웅크림 고정 유지] 고정깊이={targetCrouchDepth:F3}, qX={qxValue:F3}");
            }
        }
    }

    private void HandleMData(int mValue)
    {
        // 점프 중에는 애니메이션 전환 차단
        if (isJumpInProgress)
        {
            Debug.Log($"[점프 모드] m 값 차단: {mValue} (점프 완료 후 적용 대기)");
            return;
        }

        Debug.Log($"[M 수신] M 값: {mValue}, 이전값: {lastMValue}, 점프중: {isJumpInProgress}, 웹소켓활성: {isWebSocketDataEnabled}");
        
        // 일시정지 상태일 때는 데이터를 저장만 하고 실제 처리는 하지 않음
        if (isPaused)
        {
            Debug.Log($"일시정지 상태: m 값 {mValue} 저장됨 (스페이스바를 눌러 재생하면 적용됨)");
            lastMValue = mValue;
            return;
        }

        // m 값이 이전과 다르거나, 현재 애니메이션 상태가 걷기가 아니라면 반드시 트리거 실행
        var currentState = animator.GetCurrentAnimatorStateInfo(0);
        bool alreadyInTargetState = false;
        if (mValue == 0 && currentState.IsName("walking")) alreadyInTargetState = true;
        if (mValue == 1 && currentState.IsName("RunningmanRecentSensing_jin")) alreadyInTargetState = true;
        if (mValue == 2 && currentState.IsName("SpongebobRecentsension_jin")) alreadyInTargetState = true;

        if (mValue == lastMValue && alreadyInTargetState)
        {
            Debug.Log($"동일한 m 값 무시: {mValue} (이미 해당 상태)");
            return;
        }
        // === 새로 추가: m=5일 때 점프 시작 ===
        if (mValue == 5)
        {
            // 웹소켓으로 점프 시작
            if (jumpState == JumpState.None && !isJumpInProgress)
            {
                StartJumpPreparation();
                Debug.Log("m=5 수신 -> 웹소켓으로 점프 동작 시작");
            }
            else
            {
                Debug.Log($"m=5 수신했지만 점프 시작 불가: jumpState={jumpState}, isJumpInProgress={isJumpInProgress}");
            }
            // m=5는 lastMValue를 업데이트하지 않음 (반복 점프 허용)
            return;
        }
        // m=0이면 걷기, m=1이면 러닝맨, m=2면 스폰지밥, m=5면 점프로 전환
        else if (mValue == 0)
        {
            // 걷기로 전환
            lastDance = Dance.Walking;
            animator.SetBool("isWalkingLast", true);
            animator.SetBool("isRunningmanLast", false);
            animator.SetBool("isSpongebobLast", false);
            //animator.CrossFade("walking", 0.15f);
            animator.SetTrigger("ToWalking");
            Debug.Log("m=0 수신 -> 걷기 애니메이션으로 전환");
            
            // 댄스 전환 시 방향 초기화
            ResetDirection();
        }
        else if (mValue == 1)
        {
            // 러닝맨으로 전환
            lastDance = Dance.Runningman;
            animator.SetBool("isWalkingLast", false);
            animator.SetBool("isRunningmanLast", true);
            animator.SetBool("isSpongebobLast", false);
            animator.CrossFade("RunningmanRecentSensing_jin", 0.2f);
            //animator.SetTrigger("ToRunningman");
            Debug.Log("m=1 수신 -> 러닝맨 댄스로 전환");
            
            // 댄스 전환 시 방향 초기화
            ResetDirection();
        }
        else if (mValue == 2)
        {
            // 스폰지밥으로 전환
            lastDance = Dance.Spongebob;
            animator.SetBool("isWalkingLast", false);
            animator.SetBool("isSpongebobLast", true);
            animator.SetBool("isRunningmanLast", false);
            animator.CrossFade("SpongebobRecentsension_jin", 0.3f);
            Debug.Log("m=2 수신 -> 스폰지밥 댄스로 전환");
            
            // 댄스 전환 시 방향 초기화
            ResetDirection();
        }
        else
        {
            Debug.Log($"알 수 없는 m 값: {mValue} (m=0: 걷기, m=1: 러닝맨, m=2: 스폰지밥)");
        }
    }

        // d 값에 따른 처리 메서드 (단순화)
    private void HandleDData(int dValue)
    {
        // 점프 중에는 값 업데이트만 하고 즉시 리턴
        if (isJumpInProgress)
        {
            currentDValue = dValue;
            return;
        }
        currentDValue = dValue;
        // 디버그 로그 추가
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[HandleDData] d 값 수신: {dValue}");
        }
    }



    // d 값을 ratio로 변환하는 메서드 (spacingRatio, depthRatio 공통 사용)
    private float CalculateRatioFromD(int dValue, bool isForRunningman)
    {
        float minTarget, maxTarget;
        float inputMin = 20f;
        float inputMax = 80f;

        // [수정] isForRunningman 플래그에 따라 다른 최소/최대 목표값을 설정합니다.
        if (isForRunningman)
        {
            // 러닝맨(Depth Ratio)의 목표 범위 - 러닝맨에 최적화된 범위
            minTarget = -0.4f; // D값 20일 때: 발을 뒤로 (기본 러닝맨 스타일)
            maxTarget = 0.1f;  // D값 80일 때: 발을 약간 앞으로 (변형된 스타일)
            // D값 50(기본값)일 때는 depthRatio ≈ -0.15 (자연스러운 러닝맨 동작)
        }
        else // 스폰지밥(Spacing Ratio)의 경우
        {
            // 스폰지밥(Spacing Ratio)의 목표 범위
            minTarget = -0.2f; // 예: 스폰지밥은 한쪽으로 덜 벌어지도록 -0.5
            maxTarget = 0.2f;  // 예: 스폰지밥은 다른 쪽으로도 덜 벌어지도록 0.2
        }

        // dValue가 유효 범위를 벗어날 때 처리
        if (dValue <= inputMin) return minTarget;
        if (dValue >= inputMax) return maxTarget;

        // inputMin ~ inputMax 범위에서 minTarget ~ maxTarget으로 리니어 보간
        float normalizedD = (dValue - inputMin) / (inputMax - inputMin);
        return Mathf.Lerp(minTarget, maxTarget, normalizedD);
    }

    
    // 디버깅용 메서드 (필요시 호출)
    private void DebugFootPositions()
    {
        Debug.Log($"Foot Positions - Left: {smoothLeftFootTarget.y:F3}, Right: {smoothRightFootTarget.y:F3}, " +
                  $"DepthRatio: {depthRatio:F3}, SpacingRatio: {spacingRatio:F3}");
    }

    /// a 값에 따른 상체 굽히기 처리 메서드 (단순화)
    private void HandleAData(int aValue)
    {
        // 점프 중에는 값 업데이트만 하고 즉시 리턴
        if (isJumpInProgress)
        {
            currentAValue = aValue;
            return;
        }
        currentAValue = aValue;
        // 디버그 로그 추가 (값이 제대로 들어오는지 확인)
        if (Time.frameCount % 60 == 0) // 1초에 한 번 정도 로그
        {
            Debug.Log($"[HandleAData] a 값 수신: {aValue}");
        }
    }


    // a 값을 bodyLeanAmount로 변환하는 메서드 (범위 확인)
    private float CalculateBodyLeanFromA(int aValue)
    {
        // === 매핑 범위 확인 및 로그 ===
        //Debug.Log($"[A값 변환] 입력: {aValue}");
        
        // a=120 이상 -> 0 (중립)
        // a=80 이하 -> 1.0 (최대 가슴 숙이기)
        // 80~120 범위에서 리니어 보간
        if (aValue >= 120) 
        {
            //Debug.Log($"[A값 변환] {aValue} >= 120 -> 중립 (0)");
            return 0f; // 중립
        }
        if (aValue <= 85) 
        {
            //Debug.Log($"[A값 변환] {aValue} <= 80 -> 최대 굽힘 (1.0)");
            return 1.0f; // 최대 가슴 숙이기
        }
        
        // 80~120 범위: 리니어 보간
        float normalizedA = (120f - aValue) / (120f - 85f); // 120->0, 80->1
        Debug.Log($"[A값 변환] {aValue} (85~120 범위) -> {normalizedA:F3}");
        return normalizedA; // 0~1.0 범위로 매핑
    }

    /// IB 값 처리 메서드 (Inner Bend - 내측 굽힘)
    private void HandleIBData(int ibValue)
    {
        // 점프 중에는 값 업데이트만 하고 즉시 리턴
        if (isJumpInProgress)
        {
            currentIBValue = ibValue;
            return;
        }
        
        currentIBValue = ibValue;
        
        // 디버그 로그 추가
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[HandleIBData] IB 값 수신: {ibValue}");
        }
        
        // IB 값에 따른 처리 로직 (예: 팔꿈치, 무릎 굽힘 등)
        // 필요에 따라 IK 스타일에 적용하거나 별도 처리
        // 예: globalIKStyle.leftArmBendStrength = CalculateFromIB(ibValue);
        
        // IB 값에 따른 속도 조절 처리 (FB가 2 또는 3일 때만)
        ProcessIBSpeedControl(ibValue);
    }

    /// FB 값 처리 메서드 (Forward Bend - 전방 굽힘)
    private void HandleFBData(int fbValue)
    {
        // FB 값 수신 로그 (항상 출력)
        Debug.Log($"[FB 수신] FB 값: {fbValue}, 이전값: {lastFBValue}, 점프중: {isJumpInProgress}, 웹소켓활성: {isWebSocketDataEnabled}");
        
        // 점프 중에는 값 업데이트만 하고 즉시 리턴
        if (isJumpInProgress)
        {
            currentFBValue = fbValue;
            Debug.Log("[FB 처리] 점프 중이므로 FB 처리 스킵");
            return;
        }
        
        // 같은 FB 값이면 무시 (무한 회전 방지) - 단, 1과 4는 중간값(2,3) 후에 재실행 허용
        if (fbValue == lastFBValue && (fbValue == 1 || fbValue == 4))
        {
            Debug.Log($"[FB 처리] 연속된 회전 명령 {fbValue} 무시 (무한회전 방지)");
            return;
        }
        
        // 2,3 같은 중간값을 받으면 lastFBValue를 리셋하여 1,4 재실행 허용
        if (fbValue == 2 || fbValue == 3)
        {
            lastFBValue = fbValue;
            currentFBValue = fbValue;
            Debug.Log($"[FB 처리] 중간값 {fbValue} 수신 - 다음 1,4 명령 허용 (회전하지 않음)");
            
            // 2,3은 속도 제어만 처리하고 회전은 하지 않음
            // ProcessFBDirectionControl 호출하지 않음
            return;
        }
        
        // 이전 값 저장 및 현재 값 업데이트
        lastFBValue = currentFBValue;
        currentFBValue = fbValue;
        
        Debug.Log($"[FB 처리] FB 값 변경: {lastFBValue} → {fbValue}");
        
        // FB 값에 따른 방향 조절 처리
        ProcessFBDirectionControl(fbValue);
    }

    /// FB 값에 따른 방향 조절 처리 메서드
    private void ProcessFBDirectionControl(int fbValue)
    {
        Debug.Log($"[FB 방향조절] FB값: {fbValue} 처리 시작");
        
        switch (fbValue)
        {
            case 1: // 왼쪽으로 90도까지 회전
                Debug.Log("[FB 방향조절] FB=1 → 왼쪽 90도 회전 시작");
                SetTargetRotation(-90f);
                break;
            case 4: // 오른쪽으로 90도까지 회전
                Debug.Log("[FB 방향조절] FB=4 → 오른쪽 90도 회전 시작");
                SetTargetRotation(90f);
                break;
            default:
                Debug.Log($"[FB 방향조절] FB={fbValue} → 회전하지 않음");
                // 다른 값일 때는 회전하지 않음 (또는 중립으로 복귀)
                break;
        }
    }

    /// IS 값 처리 메서드 (Inner Stretch - 내측 스트레치)
    private void HandleISData(int isValue)
    {
        // IS 값 수신 로그 (항상 출력)
        Debug.Log($"[IS 수신] IS 값: {isValue}, 이전값: {currentISValue}, 점프중: {isJumpInProgress}, 웹소켓활성: {isWebSocketDataEnabled}");
        
        // 점프 중에는 값 업데이트만 하고 즉시 리턴
        if (isJumpInProgress)
        {
            currentISValue = isValue;
            Debug.Log("[IS 처리] 점프 중이므로 IS 처리 스킵");
            return;
        }
        
        // 현재 값 업데이트
        currentISValue = isValue;
        
        // IS 값에 따른 처리 (내측 스트레치 강도 조절)
        // IS 값이 높을수록 내측 스트레치 효과 강화
        float isRatio = isValue / 100f; // 0~1 범위로 정규화
        
        // IS 값에 따른 실제 동작 처리
        ProcessISValueEffect(isValue, isRatio);
        
        Debug.Log($"[IS 처리] IS 값 업데이트: {isValue}, 비율: {isRatio:F2}");
    }
    
    /// IS 값에 따른 실제 동작 처리 메서드
    private void ProcessISValueEffect(int isValue, float isRatio)
    {
        // === 사이클 완료 후 모드 전환 시스템 ===
        // 현재 모드에서 다른 모드로 전환하려고 할 때, 사이클이 완료되지 않았다면 대기
        bool isCurrentlyInActiveMode = isKickOutMode || isHopMode || isHop2Mode || isShuffleCrossMode;
        bool isTargetModeChange = false;
        
        // 대상 모드가 현재 모드와 다른지 확인
        switch (isValue)
        {
            case 0: isTargetModeChange = !isHop2Mode; break;
            case 1: isTargetModeChange = !isKickOutMode; break;
            case 2: isTargetModeChange = !isShuffleCrossMode; break;
            case 3: isTargetModeChange = !isHopMode; break;
            default: isTargetModeChange = isCurrentlyInActiveMode; break;
        }
        
        // 현재 활성 모드가 있고, 다른 모드로 전환하려는 경우, 사이클 완료를 기다림
        if (isCurrentlyInActiveMode && isTargetModeChange && !IsCurrentModeCycleComplete())
        {
            isPendingModeSwitch = true;
            pendingISValue = isValue;
            isCurrentCycleCompleting = true;
            
            string currentMode = isKickOutMode ? "킥아웃" : 
                               isHopMode ? "홉" : 
                               isHop2Mode ? "홉2" : 
                               isShuffleCrossMode ? "크로스" : "기타";
            Debug.Log($"🔄 모드 전환 대기: {currentMode} → IS={isValue} (사이클완료={IsCurrentModeCycleComplete()})");
            return;
        }
        
        // 즉시 전환되는 경우 디버그
        if (isCurrentlyInActiveMode && isTargetModeChange)
        {
            string currentMode = isKickOutMode ? "킥아웃" : 
                               isHopMode ? "홉" : 
                               isHop2Mode ? "홉2" : 
                               isShuffleCrossMode ? "크로스" : "기타";
            Debug.Log($"⚡ 즉시 모드 전환: {currentMode} → IS={isValue} (사이클완료={IsCurrentModeCycleComplete()})");
        }
        
        // IS 값에 따른 스펀지밥 애니메이션 모드 제어
        switch (isValue)
        {
            case 0: // Hop2 모드
                if (!isHop2Mode)
                {
                    isHop2Mode = true;
                    isKickOutMode = false;
                    isHopMode = false;
                    isShuffleCrossMode = false;
                    Debug.Log($"[IS 모드] IS={isValue} → Hop2 모드 활성화");
                }
                break;
                
            case 1: // 킥아웃 모드
                if (!isKickOutMode)
                {
                    isKickOutMode = true;
                    isHopMode = false;
                    isHop2Mode = false;
                    isShuffleCrossMode = false;
                    Debug.Log($"[IS 모드] IS={isValue} → 킥아웃 모드 활성화");
                }
                break;
                
            case 2: // 프론트 크로스 모드
                if (!isShuffleCrossMode)
                {
                    isShuffleCrossMode = true;
                    isKickOutMode = false;
                    isHopMode = false;
                    isHop2Mode = false;
                    Debug.Log($"[IS 모드] IS={isValue} → 프론트 크로스 모드 활성화");
                }
                break;
                
            case 3: // 홉 모드
                if (!isHopMode)
                {
                    isHopMode = true;
                    isKickOutMode = false;
                    isHop2Mode = false;
                    isShuffleCrossMode = false;
                    Debug.Log($"[IS 모드] IS={isValue} → 홉 모드 활성화");
                }
                break;
                
            case 4: // 기본 애니메이션
                if (isKickOutMode || isHopMode || isHop2Mode || isShuffleCrossMode)
                {
                    // 모든 모드 비활성화
                    isKickOutMode = false;
                    isHopMode = false;
                    isHop2Mode = false;
                    isShuffleCrossMode = false;
                    Debug.Log($"[IS 모드] IS={isValue} → 기본 애니메이션 (모든 모드 비활성화)");
                }
                break;
                
            default: // 기타 값 (5~100)
                if (isValue >= 5)
                {
                    Debug.Log($"[IS 모드] IS={isValue} → 기본 스펀지밥 애니메이션 (기타 값)");
                    // 모든 모드 비활성화
                    isKickOutMode = false;
                    isHopMode = false;
                    isHop2Mode = false;
                    isShuffleCrossMode = false;
                }
                break;
        }
        
        // 현재 활성화된 모드 상태 로그
        string activeMode = "기본";
        if (isKickOutMode) activeMode = "킥아웃";
        else if (isHopMode) activeMode = "홉";
        else if (isHop2Mode) activeMode = "홉2";
        else if (isShuffleCrossMode) activeMode = "프론트 크로스";
        
        Debug.Log($"[IS 모드] 현재 활성 모드: {activeMode}");
    }
    
    // === DOT 센서 기반 통합 제어 함수들 ===
    
    /// DOT 센서 가속도 데이터 처리 (3축 통합)
    private void HandleAccelerationData(Vector3 acceleration)
    {
        // 가속도 데이터 저장
        currentAcceleration = acceleration;
        
        // 중력 제거: 8~12 범위를 0으로 치환하여 중력 성분 제거 (범위 확대)
        Vector3 motionAcceleration = new Vector3(
            acceleration.x, 
            (acceleration.y >= 8f && acceleration.y <= 12f) ? 0f : acceleration.y,  // Y축 8~12 범위를 0으로
            (acceleration.z >= 8f && acceleration.z <= 12f) ? 0f : acceleration.z   // Z축 8~12 범위를 0으로
        );
        currentAccelerationMagnitude = motionAcceleration.magnitude;
        
        // 디버그 로그 (1초마다) - 8~12 범위 치환 과정 자세히 표시
        if (Time.time % 1f < Time.deltaTime)
        {
            Debug.Log($"[DOT 가속도] 원본:{acceleration} → Y축:{acceleration.y:F2}→{motionAcceleration.y:F2}, Z축:{acceleration.z:F2}→{motionAcceleration.z:F2} → 중력제거후:{motionAcceleration} → 크기:{currentAccelerationMagnitude:F3}");
        }
    }
    
    /// X축 가속도 데이터 처리
    private void HandleAccelXData(float accelX)
    {
        // X축 가속도는 좌우 움직임 강도에 영향
        if (Time.time % 2f < Time.deltaTime)
        {
            Debug.Log($"[DOT 가속도 X] {accelX:F3} (좌우 움직임)");
        }
    }
    
    /// Y축 가속도 데이터 처리
    private void HandleAccelYData(float accelY)
    {
        // Y축 가속도는 위아래 움직임 강도에 영향
        if (Time.time % 2f < Time.deltaTime)
        {
            Debug.Log($"[DOT 가속도 Y] {accelY:F3} (위아래 움직임)");
        }
    }
    
    /// Z축 가속도 데이터 처리
    private void HandleAccelZData(float accelZ)
    {
        // Z축 가속도는 앞뒤 움직임 강도에 영향
        if (Time.time % 2f < Time.deltaTime)
        {
            Debug.Log($"[DOT 가속도 Z] {accelZ:F3} (앞뒤 움직임)");
        }
    }
    
    /// qx 기반 다리 높이 계산
    private float CalculateQxBasedHeight()
    {
        // qx: -1 ~ 1 범위를 0.5 ~ 1.5 높이 배율로 매핑
        // qx가 클수록 (양수) 높이 증가, qx가 작을수록 (음수) 높이 감소
        float heightMultiplier = Mathf.Lerp(0.5f, 1.5f, (currentQx + 1f) * 0.5f);
        
        // 최종 높이 배율 계산
        currentQxHeightMultiplier = heightMultiplier * qxHeightMultiplier;
        
        // 디버그 로그 (2초마다)
        if (Time.time % 2f < Time.deltaTime)
        {
            Debug.Log($"[qx 높이] qx:{currentQx:F3}, 높이배율:{currentQxHeightMultiplier:F3}");
        }
        
        return currentQxHeightMultiplier;
    }
    
    /// 가속도 기반 통합 움직임 강도 계산
    private float CalculateAccelerationBasedIntensity()
    {
        // 가속도 크기를 0~1 범위로 정규화
        // 일반적으로 가속도는 0~10 범위이므로 10으로 나누어 정규화
        float normalizedAcceleration = Mathf.Clamp01(currentAccelerationMagnitude / 10f);
        
        // 가속도 → 인텐시티/버스트 강도로 매핑
        float intensity = normalizedAcceleration * accelerationToIntensityMultiplier;
        
        // 디버그 로그 (1초마다) - ax, ay, az 원본 값과 함께 표시
        if (Time.time % 1f < Time.deltaTime)
        {
            Debug.Log($"[가속도 강도] ax:{currentAcceleration.x:F2}, ay:{currentAcceleration.y:F2}, az:{currentAcceleration.z:F2} → 크기:{currentAccelerationMagnitude:F3} → 정규화:{normalizedAcceleration:F3} → 강도:{intensity:F3}");
        }
        
        return intensity;
    }
    
    /// 가속도 기반 Kick Burst Intensity 계산 (기본 3.0 ~ 최대 8.0)
    private float CalculateKickBurstIntensity()
    {
        // 가속도가 0이거나 매우 작을 때는 기본값 3.0 사용 (중력 제거된 순수 움직임 가속도)
        if (currentAccelerationMagnitude < 0.5f)
        {
            if (Time.time % 1f < Time.deltaTime)
            {
                Debug.Log($"⚡[가속도 킥버스트] 가속도:{currentAccelerationMagnitude:F2} → 기본값 3.0 (가만히 있음)");
            }
            return 3.0f;
        }
        
        // 가속도 크기를 0~1 범위로 정규화 (0.5 이상일 때만, 중력 제거된 값이므로 임계값 낮춤)
        float normalizedAcceleration = Mathf.Clamp01((currentAccelerationMagnitude - 0.5f) / 4.5f);
        
        // 기본값 3.0에서 최대값 8.0까지 가속도에 따라 증가
        float kickBurst = Mathf.Lerp(3.0f, 8.0f, normalizedAcceleration);
        
        // 디버그 로그 (0.5초마다 - 더 자주 표시) - ax, ay, az 원본 값과 함께 표시
        if (Time.time % 0.5f < Time.deltaTime)
        {
            Debug.Log($"⚡[가속도 킥버스트] ax:{currentAcceleration.x:F2}, ay:{currentAcceleration.y:F2}, az:{currentAcceleration.z:F2} → 크기:{currentAccelerationMagnitude:F2} → 킥버스트:{kickBurst:F2} (범위:3.0~8.0)");
        }
        
        return kickBurst;
    }
    

    /// IB 값에 따른 속도 조절 처리 메서드 (FB가 2 또는 3일 때만 동작, 모드2에서만 활성화)
    private void ProcessIBSpeedControl(int ibValue)
    {
        // 모드1 (통합모드)에서는 FB+IB 속도조절 비활성화
        if (isQxIntegratedMode)
        {
            if (Time.frameCount % 120 == 0) // 2초마다 로그
            {
                Debug.Log($"[속도조절] 모드1(통합모드)에서는 FB+IB 속도조절 비활성화");
            }
            return;
        }
        
        // 모드2 (개별모드)에서만 FB+IB 속도조절 활성화
        if (currentFBValue == 2) // FB가 2인 상태
        {
            switch (ibValue)
            {
                case 1:
                    movementSpeed = 2.8f; // 1.4배속 (2.0 * 1.4)
                    break;
                case 2:
                    movementSpeed = 2.4f; // 1.2배속 (2.0 * 1.2)
                    break;
                case 3:
                    movementSpeed = 2.0f; // 기존 속도
                    break;
            }
            
            if (Time.frameCount % 120 == 0) // 2초마다 로그
            {
                Debug.Log($"[모드2-FB=2] IB={ibValue} → 이동속도: {movementSpeed}");
            }
        }
        else if (currentFBValue == 3) // FB가 3인 상태
        {
            switch (ibValue)
            {
                case 1:
                    movementSpeed = 1.2f; // 0.6배속 (2.0 * 0.6)
                    break;
                case 2:
                    movementSpeed = 1.6f; // 0.8배속 (2.0 * 0.8)
                    break;
                case 3:
                    movementSpeed = 2.0f; // 기존 속도
                    break;
            }
            
            if (Time.frameCount % 120 == 0) // 2초마다 로그
            {
                Debug.Log($"[모드2-FB=3] IB={ibValue} → 이동속도: {movementSpeed}");
            }
        }
    }

    /// FB 회전이 최근에 완료되었는지 확인 (3초 이내)
    private bool HasRecentFBRotation()
    {
        return Time.time - lastFBRotationTime < 3f;
    }

    /// FB 회전 목표값 설정 메서드
    private void SetTargetRotation(float targetAngle)
    {
        // 현재 위치를 기준으로 상대적 회전 계산
        float currentY = transform.eulerAngles.y;
        fbTargetYRotation = currentY + targetAngle;
        
        // 360도 범위 내로 정규화
        if (fbTargetYRotation >= 360f) fbTargetYRotation -= 360f;
        if (fbTargetYRotation < 0f) fbTargetYRotation += 360f;
        
        isFBRotating = true;
        
        Debug.Log($"[FB 방향조절] 현재: {currentY:F1}°, 상대회전: {targetAngle:F1}°, FB목표: {fbTargetYRotation:F1}°");
    }


    // 트리거 설정에 지연을 추가하기 위한 코루틴
    private System.Collections.IEnumerator SetTriggerWithDelay(string triggerName)
    {
        yield return null; // 한 프레임 대기
        
        Debug.Log($"지연 후 트리거 설정: {triggerName}");
        animator.SetTrigger(triggerName);
        
        // 트리거 설정 후 상태 확인
        yield return new WaitForSeconds(0.1f);
        var newState = animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"트리거 설정 후 상태: Hash={newState.shortNameHash}");
        Debug.Log($"상태 체크 - Running: {newState.IsName("RunningmanRecentSensing_jin")}, Spongebob: {newState.IsName("SpongebobRecentsension_jin")}");
    }
    
    // WebSocket 연결 상태 변경 핸들러
    private void HandleConnectionStateChanged(bool connected)
    {
        isWebSocketConnected = connected;
        Debug.Log($"WebSocket 연결 상태 변경: {connected}");
        
        if (!connected)
        {
            // WebSocket 연결이 끊어졌을 때, 현재 값들을 키보드 값으로 저장
            keyboardSpacingRatio = spacingRatio;
            keyboardDepthRatio = depthRatio;
            keyboardBodyLeanAmount = bodyLeanAmount;
            
            Debug.Log($"WebSocket 연결 해제 - 현재 값들을 키보드 값으로 저장:");
            Debug.Log($"  spacing={keyboardSpacingRatio:F3}, depth={keyboardDepthRatio:F3}");
            Debug.Log($"  speed={animationSpeed:F3}, bodyLean={keyboardBodyLeanAmount:F3}");
        }
        else
        {
            // WebSocket 연결 시 초기화
            Debug.Log("WebSocket 연결됨 - 웹소켓 데이터로 제어 시작");
        }
    }

    
    // 일시정지 상태에서 저장된 웹소켓 데이터를 재생 시 적용하는 메서드
     private void ApplyCachedWebSocketData()
    {
        // qX는 새로운 매핑에서 속도 제어만 담당하므로 별도 처리 불필요
        // (Update() 함수에서 자동으로 currentQx 값에 따라 속도가 조절됨)
        Debug.Log($"[새로운 qX 매핑] 일시정지 후 재생: 저장된 qX 값 {currentQx:F3} (속도 제어 전용)");
        
        // m 값에 대한 처리 (애니메이션 모드)
        Debug.Log($"일시정지 후 재생: 저장된 m 값 {lastMValue} 적용");
        
        // 마지막으로 저장된 m 값을 바탕으로 애니메이션 모드 설정
        switch (lastMValue)
        {
            case 0:  // 걷기
                Debug.Log("걷기 모드 적용");
                // 걷기 상태가 아니라면 전환
                var currentState = animator.GetCurrentAnimatorStateInfo(0);
                if (!currentState.IsName("walking"))
                {
                    // 모든 관련 Bool 파라미터 설정
                    animator.SetBool("isWalkingLast", true);
                    animator.SetBool("isRunningmanLast", false);
                    animator.SetBool("isSpongebobLast", false);
                    
                    // 모든 트리거 리셋 후 새 트리거 설정
                    animator.ResetTrigger("ToRunningman");
                    animator.ResetTrigger("ToSpongebob");
                    animator.ResetTrigger("ToWalking");
                    animator.ResetTrigger("ToSpin");
                    
                    animator.SetTrigger("ToWalking");
                    
                    lastDance = Dance.Walking;
                }
                break;
                
            case 1:  // 러닝맨
                Debug.Log("러닝맨 모드 적용");
                // 러닝맨 상태가 아니라면 전환
                currentState = animator.GetCurrentAnimatorStateInfo(0);
                if (!currentState.IsName("RunningmanRecentSensing_jin"))
                {
                    // 모든 관련 Bool 파라미터 설정
                    animator.SetBool("isRunningmanLast", true);
                    animator.SetBool("isSpongebobLast", false);
                    animator.SetBool("isWalkingLast", false);
                    
                    // 모든 트리거 리셋 후 새 트리거 설정
                    animator.ResetTrigger("ToSpongebob");
                    animator.ResetTrigger("ToSpin");
                    animator.ResetTrigger("ToWalking");
                    animator.ResetTrigger("ToRunningman");
                    
                    animator.SetTrigger("ToRunningman");
                    
                    lastDance = Dance.Runningman;
                }
                break;
                
            case 2:  // 스폰지밥
                Debug.Log("스폰지밥 모드 적용");
                // 스폰지밥 상태가 아니라면 전환
                currentState = animator.GetCurrentAnimatorStateInfo(0);
                if (!currentState.IsName("SpongebobRecentsension_jin"))
                {
                    // 모든 관련 Bool 파라미터 설정
                    animator.SetBool("isSpongebobLast", true);
                    animator.SetBool("isRunningmanLast", false);
                    animator.SetBool("isWalkingLast", false);
                    
                    // 모든 트리거 리셋 후 새 트리거 설정
                    animator.ResetTrigger("ToRunningman");
                    animator.ResetTrigger("ToSpin");
                    animator.ResetTrigger("ToWalking");
                    animator.ResetTrigger("ToSpongebob");
                    
                    animator.SetTrigger("ToSpongebob");
                    
                    lastDance = Dance.Spongebob;
                }
                break;
        }
        
        // a 값 처리 (상체 굽히기로 변경)
        Debug.Log($"일시정지 후 재생: 저장된 a 값 {currentAValue} 적용");
        float targetBodyLeanAmount = CalculateBodyLeanFromA(currentAValue);
        bodyLeanAmount = targetBodyLeanAmount;
        
        // 기존 글라이딩 시스템 비활성화
        glideAmount = 0f;
        animator.SetFloat("Glide", glideAmount);
        
        // d 값에 따른 처리 (보폭)
        Debug.Log($"일시정지 후 재생: 저장된 d 값 {currentDValue} 적용");
        float calculatedRatio;
        // 모드에 따라 적용할 비율 결정 (m값 기반)
        if (lastMValue == 0 || lastMValue == 1) { // 걷기 또는 러닝맨
            // [수정] 러닝맨용 비율 계산
            calculatedRatio = CalculateRatioFromD(currentDValue, true); 
            depthRatio = calculatedRatio;
            Debug.Log($"저장된 d 값 {currentDValue} - mode {lastMValue}에서 depthRatio에 적용: {depthRatio:F3}");
        } else if (lastMValue == 2) { // 스폰지밥
            // [수정] 스폰지밥용 비율 계산
            calculatedRatio = CalculateRatioFromD(currentDValue, false);
            spacingRatio = calculatedRatio;
            Debug.Log($"저장된 d 값 {currentDValue} - mode {lastMValue}에서 spacingRatio에 적용: {spacingRatio:F3}");
        }
    }
    
    // ==================== 점프 시스템 메서드들 ====================
    
    // 점프 준비 시작 메서드
    private void StartJumpPreparation()
    {
        // 물리 상태 초기화
        ResetPhysicsState();
        Debug.Log("J키: 점프 준비 시작!");
        
        // 현재 애니메이션에 따라 전환 시간 조정
        float transitionTime = 0.3f; // 기본값
        
        var currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.IsName("walking"))
        {
            transitionTime = 0.2f; // 걷기에서는 빠르게
            lastDance = Dance.Walking;
        }
        else if (currentState.IsName("RunningmanRecentSensing_jin"))
        {
            transitionTime = 0.4f; // 러닝맨에서는 조금 더 길게
            lastDance = Dance.Runningman;
        }
        else if (currentState.IsName("SpongebobRecentsension_jin"))
        {
            transitionTime = 0.35f; // 스폰지밥에서는 중간
            lastDance = Dance.Spongebob;
        }
        
        // 부드러운 애니메이션 전환 적용
        animator.CrossFade("Jump", transitionTime, 0, 0f);
        animator.SetFloat("JumpPhase", 0);
        
        Debug.Log($"점프 준비 애니메이션으로 부드럽게 전환 시작 ({transitionTime}초, {lastDance}에서)");
        
        Debug.Log($"점프 시작 전 상태 저장: {lastDance}");
        // 웅크림 관련 변수 완전 초기화
        maxReachedCrouchDepth = 0f;
        isCrouchDepthLocked = false;
        isCrouchingMaintained = false;
        currentCrouchDepth = 0f;
        targetCrouchDepth = 0f;
        crouchVelocity = 0f;
        // 점프 상태 초기화
        jumpState = JumpState.WaitingForDown;
        isJumpInProgress = true;
        isKeyboardJump = true;
        
        // 변수 초기화
        currentJumpHeight = jumpHeightMin;
        calculatedJumpHeight = jumpHeightMin;
        jumpChargeTimer = 0f;
        
        // === 부드러운 애니메이션 전환 적용 ===
        // CrossFade 사용하여 자연스럽게 전환 (0.3초 동안)
        animator.CrossFade("Jump", 0.3f, 0, 0f);
        animator.SetFloat("JumpPhase", 0);
        
        Debug.Log("점프 준비 애니메이션으로 부드럽게 전환 시작 (0.3초)");
        
        // 점프 준비 애니메이션 시작
        StartCoroutine(ImprovedJump());
        
        Debug.Log($"[점프 시작 전] currentJumpHeight={currentJumpHeight:F2}, calculatedJumpHeight={calculatedJumpHeight:F2}");
    }

    
    // 개선된 점프 코루틴 (GestureController에서 가져온 로직)
    private System.Collections.IEnumerator ImprovedJump()
    {
        // jumpBase 상태 처리
        if(jumpBase){
            // CrossFade가 이미 시작되었으므로 Play 대신 상태만 설정
            // animator.Play("Jump", 0, 0); // 이 줄 제거
            
            // 전환 완료를 위해 잠시 대기
            yield return new WaitForSeconds(0.1f);
            
            // JumpPhase 설정 확인
            animator.SetFloat("JumpPhase", 0);
            jumpState = JumpState.WaitingForDown;
            
            Debug.Log("점프 준비(JumpBase) 상태로 자연스럽게 전환 완료 - 화살표 아래 키를 눌러 웅크리기");
            yield break;
        }
        
        // jumpReady 상태 처리 (웅크림 자세) - 일회성 설정만
        if(jumpReady){
            // Jump 애니메이션을 기본 상태로 시작
            animator.Play("Jump", 0, 0);
            
            // 초기 JumpPhase 설정 (Update에서 지속적으로 업데이트됨)
            animator.SetFloat("JumpPhase", 0f);
            
            jumpState = JumpState.WaitingForUp;
            readyAnimStartTime = Time.time;
            Debug.Log("웅크림 상태 진입 - Update()에서 지속적으로 애니메이션 업데이트됨");
            yield break;
        }

        // jumping 상태 처리 (실제 점프 실행) - GestureController 방식으로 개선
        if(jumping){
            // 웅크림 상태 완전 해제 (점프 애니메이션이 제대로 나오도록)
            isCrouchingMaintained = false;
            currentCrouchDepth = 0f;
            targetCrouchDepth = 0f;
            crouchVelocity = 0f;
            
            // Jump 상태(2)로 전환 - 도약
            animator.Play("Jump", 0, 0);
            animator.SetFloat("JumpPhase", 2); // 점프 상태로 설정
            jumpState = JumpState.Jumping;
            Debug.Log("개선된 점프: Jumping 상태 전환 (웅크림 해제)");
            
            // 도약 모션 시간 대기
            yield return new WaitForSeconds(0.2f);
            
            // 점프 실행
            if (rb != null)
            {
                rb.isKinematic = false;
                JumpWithHeight(currentJumpHeight);
            }
            
            // JumpAir 상태로 전환
            yield return new WaitForSeconds(0.35f);
            animator.SetFloat("JumpPhase", 3);
            jumpState = JumpState.JumpAir;
            Debug.Log("개선된 점프: JumpAir 상태 전환");

            // JumpAir 상태에서 Y축 값을 체크
            bool hasReachedPeak = false;
            bool hasTransitionedToDown = false;
            float checkStartTime = Time.time;
            float previousHeight = transform.position.y;
            float peakHeight = 0f;
            int consecutiveDescentFrames = 0; // 연속적인 하강 프레임 카운트
            
            // 최대 시간 제한 추가
            while (Time.time - checkStartTime < 5.0f && !hasTransitionedToDown)
            {
                float currentHeight = transform.position.y;
                
                // 최고점 갱신
                if (currentHeight > peakHeight)
                    peakHeight = currentHeight;
                
                // 연속적인 하강 감지 (피크 감지용)
                if (currentHeight < previousHeight)
                {
                    consecutiveDescentFrames++;
                    if (consecutiveDescentFrames >= 1 && !hasReachedPeak)
                    {
                        hasReachedPeak = true;
                        Debug.Log($"점프 피크 감지: 최고점 {peakHeight:F2}m");
                    }
                }
                else
                {
                    consecutiveDescentFrames = 0;
                }
                
                // 피크를 지난 후 Y축이 임계값 이하로 내려오면 JumpingDown 상태로 전환
                if (hasReachedPeak && currentHeight <= 2.0f)
                {
                    // JumpingDown 상태로 전환
                    animator.Play("Jump", 0, 0);
                    animator.SetFloat("JumpPhase", 4);
                    animator.speed = animationSpeed; // 현재 animationSpeed 값 사용
                    jumpState = JumpState.JumpingDown;
                    Debug.Log($"피크 이후 높이 {currentHeight:F2}m에서 JumpingDown 상태로 전환");
                    hasTransitionedToDown = true;
                    break;
                }
                
                previousHeight = currentHeight;
                yield return null; // 매 프레임 체크
            }
            
            // 만약 높이 체크에서 전환되지 않았다면 여기서 강제로 전환
            if (!hasTransitionedToDown)
            {
                animator.Play("Jump", 0, 0);
                animator.SetFloat("JumpPhase", 4);
                jumpState = JumpState.JumpingDown;
                Debug.Log("시간 초과 또는 피크 미감지: 강제로 JumpingDown 상태로 전환");
            }
            
            // 착지 확인 부분
            float landingStartTime = Time.time;
            bool hasLanded = false;
            float initialHeight = transform.position.y;
            
            // 착지 조건이 만족될 때까지 대기 (최대 1초)
            while (Time.time - landingStartTime < 1.0f && !hasLanded)
            {
                bool isGrounded = IsGrounded();
                float currentHeight = transform.position.y;
                
                // 더 정밀한 착지 감지 - 매우 낮은 높이 또는 IsGrounded가 true일 때
                if (isGrounded || currentHeight <= 0.05f)
                {
                    hasLanded = true;
                    break;
                }
                
                yield return null; // 매 프레임 체크
            }
            
            // 부드러운 착지를 위한 보간
            if (hasLanded)
            {
                // [수정] 착지 시 속도와 중력을 포함한 모든 물리 상태를 명시적으로 초기화하여
                // 다음 점프에 영향을 주지 않도록 합니다.
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.linearVelocity = Vector3.zero;
                }

                float smoothTime = 0.15f;
                float elapsedTime = 0f;
                Vector3 startPos = transform.position;
                Vector3 targetPos = new Vector3(startPos.x, 0f, startPos.z);
                
                // 부드럽게 위치 보정
                while (elapsedTime < smoothTime)
                {
                    elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedTime / smoothTime);
                    transform.position = Vector3.Lerp(startPos, targetPos, t);
                    yield return null;
                }
                
                // 최종 위치 확정
                transform.position = targetPos;
                Debug.Log($"착지 위치 보정 완료: {transform.position}");
            }
            else
            {
                // 착지 감지 실패 시 강제로 위치 조정
                Debug.LogWarning("착지 감지 실패, 강제 위치 설정");
                Vector3 fixedPosition = transform.position;
                fixedPosition.y = 0f;
                transform.position = fixedPosition;
            }
            
            // 대기 시간
            yield return new WaitForSeconds(0.4f);
            
            // 모든 상태 변수 초기화
            jumpState = JumpState.None;
            isAnimationLocked = false;
            isKeyboardJump = false;
            currentAnimationName = "";
            
            // 점프 완료 시 웅크림 관련 변수 완전 리셋
            maxReachedCrouchDepth = 0f;
            isCrouchDepthLocked = false;
            isCrouchingMaintained = false;

            
            // 롤 관련 변수 초기화
            currentRoll = 0f;
            minRoll = 0f;
            maxRoll = 0f;
            isWaitingForRollUp = false;
            
            // 점프 완료 플래그 설정
            isJumpInProgress = false;
            
            // 점프 전 댄스 상태에 따라 적절한 애니메이션으로 복귀
            switch (lastDance)
            {
                case Dance.Walking:
                    animator.SetTrigger("ToWalking");
                    Debug.Log("개선된 점프 완료, 걷기 상태로 복귀");
                    break;
                case Dance.Runningman:
                    animator.SetTrigger("ToRunningman");
                    Debug.Log("개선된 점프 완료, 러닝맨 상태로 복귀");
                    break;
                case Dance.Spongebob:
                    animator.SetTrigger("ToSpongebob");
                    Debug.Log("개선된 점프 완료, 스폰지밥 상태로 복귀");
                    break;
                default:
                    animator.SetTrigger("ToWalking");
                    Debug.Log("개선된 점프 완료, 기본 걷기 상태로 복귀");
                    break;
            }
        }
    }
    
    // 키보드 점프용 상태 전환 메서드들
    public void TriggerJumpReady()
    {
        if (jumpState == JumpState.WaitingForDown && isKeyboardJump)
        {
            jumpBase = false;
            jumpReady = true;
            jumping = false;
            
            Debug.Log("화살표 아래 키: 웅크림 준비 시작");
            StopAllCoroutines();
            StartCoroutine(ImprovedJump());
        }
    }
    
    public void TriggerJumpExecution()
    {
        if (jumpState == JumpState.WaitingForUp && isKeyboardJump)
        {
            // 웅크림 깊이에 따라 점프 높이 계산
            float crouchRatio = targetCrouchDepth / maxCrouchDepth; // 0~1 범위
            currentJumpHeight = Mathf.Lerp(jumpHeightMin, jumpHeightMax, crouchRatio);
            
            Debug.Log($"점프 실행! 웅크림 깊이: {targetCrouchDepth:F3}, 웅크림 비율: {crouchRatio:F2}, 점프 높이: {currentJumpHeight:F2}m");
            
            // 점프 실행
            jumpChargeTimer = 0f; // 충전 타이머는 0으로 리셋 (사용되지 않음)
            jumpBase = false;
            jumpReady = false;
            jumping = true;
            
            StopAllCoroutines();
            StartCoroutine(ImprovedJump());
        }
    }

    
    // 특정 높이로 점프하는 메서드 (간소화 버전)
    private void JumpWithHeight(float targetHeight)
    {
        // 지면 감지 로직은 점프 코루틴을 시작하기 전에 처리하므로 여기서는 제거합니다.
        if (rb == null)
        {
            Debug.LogError("Rigidbody가 없어서 점프할 수 없습니다.");
            return;
        }

        // [수정] 점프 높이 제한을 인스펙터에서 설정한 변수(jumpHeightMin, jumpHeightMax)를 사용하도록 변경합니다.
        targetHeight = Mathf.Clamp(targetHeight, jumpHeightMin, jumpHeightMax);
        
        // 물리 설정
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearDamping = 0f;
        
        // [수정] 점프 직전에 현재 속도를 강제로 0으로 만들어, 이전 점프나 다른 물리 효과의 영향을 완전히 제거합니다.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 간단한 점프 속도 계산
        float gravity = Mathf.Abs(Physics.gravity.y);
        float jumpSpeed = Mathf.Sqrt(2f * gravity * targetHeight);
        
        // 현재 위치 저장
        float startY = transform.position.y;
        calculatedJumpHeight = targetHeight;
        
        // 속도 설정 (X, Z 축 속도는 유지)
        Vector3 currentVel = rb.linearVelocity;
        currentVel.y = jumpSpeed;
        rb.linearVelocity = currentVel;
        
        Debug.Log($"점프 실행! 시작높이: {startY:F2}m, 목표: {targetHeight:F1}m, 초기속도: {jumpSpeed:F2}m/s");
    }
    
    // [수정] 지면 감지 메서드 개선 (DollPlayer.cs와 동일한 방식)
    private bool IsGrounded()
    {
        // 여러 지점에서 레이캐스트를 발사하여 더 정확하게 체크
        Vector3 center = transform.position;
        Vector3 forward = center + transform.forward * 0.3f;
        Vector3 backward = center - transform.forward * 0.3f;
        Vector3 left = center - transform.right * 0.3f;
        Vector3 right = center + transform.right * 0.3f;

        float groundCheckDistance = 1.1f; // DollPlayer.cs와 동일한 거리

        // 레이캐스트로 지면 체크
        return Physics.Raycast(center, Vector3.down, groundCheckDistance) ||
            Physics.Raycast(forward, Vector3.down, groundCheckDistance) ||
            Physics.Raycast(backward, Vector3.down, groundCheckDistance) ||
            Physics.Raycast(left, Vector3.down, groundCheckDistance) ||
            Physics.Raycast(right, Vector3.down, groundCheckDistance);
    }
    
    // 점프 높이 반환 메서드 (외부에서 호출 가능)
    public float GetJumpHeight()
    {
        return calculatedJumpHeight;
    }
    
    // 점프 취소 메서드
    private void CancelJump()
    {
        Debug.Log("ESC키: 점프 취소");
        
        // 모든 코루틴 중지
        StopAllCoroutines();
        
        // [수정] 점프 취소 시 ResetPhysicsState()를 호출하여 모든 물리 상태를 완벽하게 초기화합니다.
        ResetPhysicsState();

        // 상태 초기화
        jumpState = JumpState.None;
        isJumpInProgress = false;
        isKeyboardJump = false;
        isAnimationLocked = false;
        
        // 점프 관련 변수 초기화
        jumpBase = false;
        jumpReady = false;
        jumping = false;
        jumpDown = false;
        
        currentJumpHeight = jumpHeightMin;
        calculatedJumpHeight = jumpHeightMin;
        jumpChargeTimer = 0f;
        
        // 웅크림 관련 변수 초기화
        currentCrouchDepth = 0f;
        targetCrouchDepth = 0f;
        crouchVelocity = 0f;
        crouchChargeTimer = 0f;
        
        Debug.Log("점프 취소 - 모든 점프/웅크림 변수 완전 초기화");

        // 롤 관련 변수 초기화
        currentRoll = 0f;
        minRoll = 0f;
        maxRoll = 0f;
        isWaitingForRollUp = false;
        isCollectingMaxRoll = false;
        
        // 물리 상태 정상화
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }
        
        // 점프 전 댄스 상태에 따라 적절한 애니메이션으로 복귀
        switch (lastDance)
        {
            case Dance.Walking:
                animator.SetTrigger("ToWalking");
                Debug.Log("점프 취소 완료, 걷기 상태로 복귀");
                break;
            case Dance.Runningman:
                animator.SetTrigger("ToRunningman");
                Debug.Log("점프 취소 완료, 러닝맨 상태로 복귀");
                break;
            case Dance.Spongebob:
                animator.SetTrigger("ToSpongebob");
                Debug.Log("점프 취소 완료, 스폰지밥 상태로 복귀");
                break;
            default:
                animator.SetTrigger("ToWalking");
                Debug.Log("점프 취소 완료, 기본 걷기 상태로 복귀");
                break;
        }
        animator.speed = animationSpeed; // 현재 설정된 animationSpeed 값 사용
    }

    // [신규] Rigidbody의 모든 물리 상태를 완벽하게 초기화하는 중앙 관리 함수
    private void ResetPhysicsState()
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.linearDamping = 0f; // 댐핑도 리셋
            rb.angularDamping = 0.05f; // 기본값으로 리셋
            Debug.Log("[Physics Reset] Rigidbody state has been fully reset to kinematic.");
        }
    }


    // 점프 중에만 호출되는 입력 처리 함수
    private void HandleJumpStateMachineInput()
    {
        // ↓키: 웅크림 모드 진입 (GetKeyDown 제거)
        // 기존 GetKeyDown 코드 삭제
        
        // ↓키 누르고 있으면 점진적으로 웅크림
        if (jumpState == JumpState.WaitingForDown && Input.GetKey(KeyCode.DownArrow))
        {
            // 웅크림 준비 상태로 전환 (한 번만)
            if (!jumpReady)
            {
                jumpBase = false;
                jumpReady = true;
                jumping = false;
                Debug.Log("웅크림 모드 진입");
                StopAllCoroutines();
                StartCoroutine(ImprovedJump());
            }
            
            // 웅크림 깊이 점진적 증가
            crouchChargeTimer += Time.deltaTime;
            crouchChargeTimer = Mathf.Clamp(crouchChargeTimer, 0f, maxCrouchChargeTime);
            
            float crouchRatio = crouchChargeTimer / maxCrouchChargeTime;
            targetCrouchDepth = Mathf.Lerp(0f, maxCrouchDepth, crouchRatio);
            
            Debug.Log($"[웅크림 증가] crouchTimer: {crouchChargeTimer:F2}, 목표깊이: {targetCrouchDepth:F3}");
        }
        
        // ↓키 떼면 현재 웅크림 깊이를 고정하고 지속 상태 설정
        if (jumpState == JumpState.WaitingForUp && Input.GetKeyUp(KeyCode.DownArrow))
        {
            // 웅크림 자세 유지 플래그 설정
            isCrouchingMaintained = true;
            
            // targetCrouchDepth는 그대로 유지 (현재 깊이 고정)
            // crouchChargeTimer만 리셋하여 추가 충전 중단
            crouchChargeTimer = 0f;
            
            Debug.Log($"웅크림 깊이 고정 및 지속 모드 활성화: {targetCrouchDepth:F3}");
        }

        // 새로운 즉시 트리거 방식으로 변경
        // ↑키 누르면 즉시 점프 실행 (웅크림 깊이에 따라 높이 결정)
        if (jumpState == JumpState.WaitingForUp && Input.GetKeyDown(KeyCode.UpArrow))
        {
            // 웅크림 깊이에 따라 점프 높이 계산
            float crouchRatio = targetCrouchDepth / maxCrouchDepth; // 0~1 범위
            currentJumpHeight = Mathf.Lerp(jumpHeightMin, jumpHeightMax, crouchRatio);
            
            Debug.Log($"점프 실행! 웅크림 깊이: {targetCrouchDepth:F3}, 웅크림 비율: {crouchRatio:F2}, 점프 높이: {currentJumpHeight:F2}m");
            
            // 점프 실행
            jumpChargeTimer = 0f; // 충전 타이머는 0으로 리셋 (사용되지 않음)
            jumpBase = false;
            jumpReady = false;
            jumping = true;
            
            StopAllCoroutines();
            StartCoroutine(ImprovedJump());
        }


        // ↓키 누르고 있으면 웅크림 깊이 증가
        if (jumpState == JumpState.WaitingForUp && Input.GetKey(KeyCode.DownArrow))
        {
            crouchChargeTimer += Time.deltaTime;
            crouchChargeTimer = Mathf.Clamp(crouchChargeTimer, 0f, maxCrouchChargeTime);
            
            // 웅크림 깊이 계산 (충전 시간에 비례) - targetCrouchDepth 설정
            float crouchRatio = crouchChargeTimer / maxCrouchChargeTime;
            targetCrouchDepth = Mathf.Lerp(0f, maxCrouchDepth, crouchRatio);
            
            Debug.Log($"[웅크림 충전 중] crouchChargeTimer: {crouchChargeTimer:F2}, 목표 웅크림 깊이: {targetCrouchDepth:F3}");
        }

        // ESC: 점프 취소
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelJump();
        }
    }

    // 0키로 호출되는 완전 초기화 함수
    private void CompleteReset()
    {
        Debug.Log("=== 완전 초기화 시작 ===");
        
        // 1. 모든 코루틴 중지
        StopAllCoroutines();
        
        // 2. 웹소켓 데이터 비활성화 (스페이스바를 다시 눌러야 활성화됨)
        isWebSocketDataEnabled = false;
        isQxIntegratedMode = false; // qX 통합 모드도 OFF로 초기화
        Debug.Log("웹소켓 데이터 비활성화 - 스페이스바를 눌러 다시 활성화하세요.");
        Debug.Log("qX 통합 제어 모드 OFF로 초기화 - V키로 다시 활성화할 수 있습니다.");
        
        // 3. 애니메이션 완전 초기화
        isPaused = true;
        IsPaused = true;
        
        // 애니메이터를 안전하게 초기화 (Rebind 대신 수동 초기화)
        try
        {
            animator.speed = 0f; // 일시정지 상태로 설정
            
            // 애니메이터 파라미터들을 기본값으로 설정
            animator.SetFloat("Spacing", 0f);
            animator.SetFloat("Depth", 0f);
            animator.SetFloat("BodyLean", 0f);
            animator.SetFloat("Glide", 0f);
            animator.SetFloat("LeanAngle", 0f);
            animator.SetFloat("Blend", 0f);
            
            // 상태 플래그들 초기화
            animator.SetBool("isWalkingLast", true);
            animator.SetBool("isRunningmanLast", false);
            animator.SetBool("isSpongebobLast", false);
            
            Debug.Log("애니메이터 안전 초기화 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"애니메이터 초기화 중 경고: {e.Message}");
        }
        
        // 강제로 초기 애니메이션 상태로 전환하는 코루틴 시작
        StartCoroutine(ForceInitialAnimationState());
        
        // 4. 물리 상태 완전 초기화
        ResetPhysicsState();
        
        // 5. 점프 관련 모든 변수 초기화
        jumpState = JumpState.None;
        isJumpInProgress = false;
        isKeyboardJump = false;
        isAnimationLocked = false;
        jumpBase = false;
        jumpReady = false;
        jumping = false;
        jumpDown = false;
        currentJumpHeight = 1.5f;
        calculatedJumpHeight = 1.5f;
        jumpChargeTimer = 0f;
        
        // 6. 웅크림 관련 변수 초기화
        currentCrouchDepth = 0f;
        targetCrouchDepth = 0f;
        crouchVelocity = 0f;
        crouchChargeTimer = 0f;
        maxReachedCrouchDepth = 0f;
        
        // 7. 롤 관련 변수 초기화
        currentRoll = 0f;
        minRoll = 0f;
        maxRoll = 0f;
        isWaitingForRollUp = false;
        isCollectingMaxRoll = false;
        
        // 8. 방향/회전 관련 변수 초기화
        ResetDirection();
        
        // 9. 댄스 상태를 기본 걷기로 초기화
        lastDance = Dance.Walking;
        
        // 10. 기본 파라미터 값들 초기화
        spacingRatio = 0f;
        depthRatio = 0f;
        bodyLeanAmount = 0f;
        glideAmount = 0f;
        animationSpeed = 1f;
        movementSpeed = 2f;  // 기본 이동속도로 초기화
        leanAngle = 0f;
        
        // 11. 키보드 캐시 값들 초기화
        keyboardSpacingRatio = 0f;
        keyboardDepthRatio = 0f;
        keyboardBodyLeanAmount = 0f;
        animationSpeed = 1f;
        keyboardQz = 0f;
        
        // 12. 센서 데이터 초기화
        currentQx = 0f;
        currentQy = 0f;
        currentQz = 0f;
        
        // 13. 스핀 관련 변수 초기화
        spinTriggered = false;
        spinCompleted = false;
        wasInSpin = false;
        
        // 14. UI 슬라이더 초기화 (있는 경우)
        if (blendSlider != null) blendSlider.value = 0f;
        if (bodyLeanSlider != null) bodyLeanSlider.value = 0f;
        if (depthSlider != null) depthSlider.value = 0.5f;
        if (speedSlider != null) speedSlider.value = 0.5f;
        
        // 15. 캐릭터 위치/회전 초기화 (필요한 경우)
        transform.rotation = Quaternion.identity;
        
        // 16. 타이머 초기화
        TimeDisplay timeDisplay = FindFirstObjectByType<TimeDisplay>();
        if (timeDisplay != null)
        {
            timeDisplay.ResetTimer();
            Debug.Log("타이머 초기화 완료");
        }
        else
        {
            Debug.LogWarning("TimeDisplay 컴포넌트를 찾을 수 없습니다.");
        }
        
        Debug.Log("=== 완전 초기화 완료 ===");
        Debug.Log("스페이스바를 눌러 웹소켓 데이터를 활성화하고 애니메이션을 시작하세요.");
    }

    // 강제로 초기 애니메이션 상태로 전환하는 코루틴
    private System.Collections.IEnumerator ForceInitialAnimationState()
    {
        // 한 프레임 대기
        yield return null;
        
        // 모든 파라미터를 기본값으로 설정
        animator.SetBool("isWalkingLast", true);
        animator.SetBool("isRunningmanLast", false);
        animator.SetBool("isSpongebobLast", false);
        
        // 모든 트리거 리셋
        animator.ResetTrigger("ToRunningman");
               animator.ResetTrigger("ToSpongebob");
        animator.ResetTrigger("ToWalking");
        animator.ResetTrigger("ToSpin");
        
        // 기본 걷기로 전환
        animator.SetTrigger("ToWalking");
        
        // 몇 프레임 더 대기하여 상태 전환 완료
        yield return new WaitForSeconds(0.1f);
        
        // 다시 일시정지
        animator.speed = 0f;
        
        Debug.Log("초기 애니메이션 상태 강제 적용 완료");
    }

    // qX 통합 제어 모드 처리 함수
    private void HandleQxIntegratedControl(bool inRunningman, bool inSpongebob)
    {
        // qX 값을 기반으로 통합 제어
        // qX가 양수(빨라짐): 상체 최대 굽히기 + 보폭 좁히기
        // qX가 음수(느려짐): 상체 펴기 + 보폭 넓히기
        
        // qX 값 범위 매핑 (-0.6 ~ 0.6)
        float normalizedQx = Mathf.Clamp(currentQx, -0.6f, 0.6f) / 0.6f; // -1 ~ 1 범위로 정규화
        
        // 상체 굽히기 제어 (qX가 양수일 때 최대 굽히기)
        float targetBodyLeanAmount;
        if (normalizedQx > 0)
        {
            // qX 양수: 빨라질 때 상체 굽히기 (0 ~ 1)
            targetBodyLeanAmount = normalizedQx;
        }
        else
        {
            // qX 음수: 느려질 때 상체 펴기 (0으로 수렴)
            targetBodyLeanAmount = 0f;
        }
        
        bodyLeanAmount = Mathf.Lerp(bodyLeanAmount, targetBodyLeanAmount, Time.deltaTime * bodyLeanEase);
        
        // 보폭 제어 (qX와 반비례 관계) - 범위를 줄이고 부드럽게 적용
        float targetStepRatio;
        if (normalizedQx > 0)
        {
            // qX 양수: 빨라질 때 보폭 좁히기 - 범위를 줄여서 하체 꺾임 방지
            targetStepRatio = -normalizedQx * 0.3f; // -0.3 ~ 0 범위로 축소
        }
        else
        {
            // qX 음수: 느려질 때 보폭 넓히기 - 범위를 줄여서 하체 꺾임 방지
            targetStepRatio = -normalizedQx * 0.3f; // 0 ~ 0.3 범위로 축소
        }
        
        // 보폭 변화를 부드럽게 적용하여 급격한 변화 방지
        float lerpSpeed = 3f; // 부드러운 전환 속도
        
        // 애니메이션 상태에 따라 적절한 파라미터에 부드럽게 적용
        if (inRunningman)
        {
            depthRatio = Mathf.Lerp(depthRatio, targetStepRatio, Time.deltaTime * lerpSpeed);
            spacingRatio = Mathf.Lerp(spacingRatio, 0f, Time.deltaTime * lerpSpeed);
        }
        else if (inSpongebob)
        {
            spacingRatio = Mathf.Lerp(spacingRatio, targetStepRatio, Time.deltaTime * lerpSpeed);
            depthRatio = Mathf.Lerp(depthRatio, 0f, Time.deltaTime * lerpSpeed);
        }
        else
        {
            depthRatio = Mathf.Lerp(depthRatio, 0f, Time.deltaTime * lerpSpeed);
            spacingRatio = Mathf.Lerp(spacingRatio, 0f, Time.deltaTime * lerpSpeed);
        }
        
        // 디버그 로그 (1초마다)
        if (Time.time % 1f < Time.deltaTime)
        {
            string animState = inRunningman ? "러닝맨" : (inSpongebob ? "스폰지밥" : "기타");
            Debug.Log($"[qX 통합 제어] 애니메이션={animState}, qX={currentQx:F3}, 정규화={normalizedQx:F3}, 상체굽히기={targetBodyLeanAmount:F3}, 보폭={targetStepRatio:F3}");
            Debug.Log($"[qX 통합 결과] depthRatio={depthRatio:F3}, spacingRatio={spacingRatio:F3}");
        }
    }
    
    // === 모드 전환 대기 시스템 함수들 ===
    
    /// <summary>
    /// 킥아웃 모드의 사이클이 완료되었는지 확인 (애니메이션 사이클 기준)
    /// </summary>
    private bool IsKickOutCycleComplete()
    {
        if (!isKickOutMode) return true;
        
        // 킥아웃 모드에서 애니메이션 사이클 기준으로 휴식 구간 체크
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animNormalizedTime = stateInfo.normalizedTime % 1f;
        float kickCycle = (animNormalizedTime * kickOutPulseRate) % 1f;
        
        float kickDur = kickDuration / 100f;
        float restTime = kickRestTime / 100f;
        float breakTime = cycleBreakTime / 100f;
        float oneKickCycleLength = kickDur + restTime + kickDur + restTime + breakTime;
        float repeatKickCycle = kickCycle % oneKickCycleLength;
        
        // 각 구간의 경계점 계산
        float leftKickEnd = kickDur; // 왼발킥 끝
        float firstRestEnd = leftKickEnd + restTime; // 첫 휴식 끝
        float rightKickEnd = firstRestEnd + kickDur; // 오른발킥 끝
        float secondRestEnd = rightKickEnd + restTime; // 두 번째 휴식 끝
        
        // 휴식 구간에 있으면 완료로 간주 (강도 체크 없이 순수 애니메이션 사이클 기준)
        bool inRestPeriod = (repeatKickCycle >= secondRestEnd) || // 사이클 간 휴식
                           (repeatKickCycle >= leftKickEnd && repeatKickCycle < firstRestEnd) || // 첫 번째 휴식
                           (repeatKickCycle >= rightKickEnd && repeatKickCycle < secondRestEnd); // 두 번째 휴식
        
        // 모드 전환 요청 시에만 상세 디버그
        if (isPendingModeSwitch && Time.time % 0.2f < Time.deltaTime)
        {
            Debug.Log($"🔍 킥아웃 사이클 체크: 사이클={repeatKickCycle:F3}, 휴식구간={inRestPeriod}, 완료={inRestPeriod}");
        }
        
        return inRestPeriod;
    }
    
    /// <summary>
    /// Hop 모드의 사이클이 완료되었는지 확인 (애니메이션 사이클 기준)
    /// </summary>
    private bool IsHopCycleComplete()
    {
        if (!isHopMode) return true;
        
        // Hop 모드에서 애니메이션 사이클 기준으로 휴식 구간 체크
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animNormalizedTime = stateInfo.normalizedTime % 1f;
        float hopCycle = (animNormalizedTime * hopPulseRate) % 1f;
        
        float hopDur = hopDuration / 100f;
        float hopRest = hopRestTime / 100f;
        float hopBreak = hopCycleBreakTime / 100f;
        float oneHopCycleLength = hopDur + hopRest + hopDur + hopRest + hopBreak;
        float repeatHopCycle = hopCycle % oneHopCycleLength;
        
        // 각 구간의 경계점 계산
        float leftHopEnd = hopDur; // 왼발Hop 끝
        float firstRestEnd = leftHopEnd + hopRest; // 첫 휴식 끝
        float rightHopEnd = firstRestEnd + hopDur; // 오른발Hop 끝
        float secondRestEnd = rightHopEnd + hopRest; // 두 번째 휴식 끝
        
        // 휴식 구간에 있으면 완료로 간주 (강도 체크 없이 순수 애니메이션 사이클 기준)
        bool inRestPeriod = (repeatHopCycle >= secondRestEnd) || // 사이클 간 휴식
                           (repeatHopCycle >= leftHopEnd && repeatHopCycle < firstRestEnd) || // 첫 번째 휴식
                           (repeatHopCycle >= rightHopEnd && repeatHopCycle < secondRestEnd); // 두 번째 휴식
        
        return inRestPeriod;
    }
    
    /// <summary>
    /// Hop2 모드의 사이클이 완료되었는지 확인 (애니메이션 사이클 기준)
    /// </summary>
    private bool IsHop2CycleComplete()
    {
        if (!isHop2Mode) return true;
        
        // Hop2 모드에서 애니메이션 사이클 기준으로 휴식 구간 체크
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animNormalizedTime = stateInfo.normalizedTime % 1f;
        float hop2Cycle = (animNormalizedTime * hop2PulseRate) % 1f;
        
        float hop2Dur = hop2Duration / 100f;
        float hop2Rest = hop2RestTime / 100f;
        float hop2Break = hop2CycleBreakTime / 100f;
        float oneHop2CycleLength = hop2Dur + hop2Rest + hop2Dur + hop2Rest + hop2Break;
        float repeatHop2Cycle = hop2Cycle % oneHop2CycleLength;
        
        // 각 구간의 경계점 계산
        float leftHop2End = hop2Dur; // 왼발Hop2 끝
        float firstRest2End = leftHop2End + hop2Rest; // 첫 휴식 끝
        float rightHop2End = firstRest2End + hop2Dur; // 오른발Hop2 끝
        float secondRest2End = rightHop2End + hop2Rest; // 두 번째 휴식 끝
        
        // 휴식 구간에 있으면 완료로 간주 (강도 체크 없이 순수 애니메이션 사이클 기준)
        bool inRestPeriod = (repeatHop2Cycle >= secondRest2End) || // 사이클 간 휴식
                           (repeatHop2Cycle >= leftHop2End && repeatHop2Cycle < firstRest2End) || // 첫 번째 휴식
                           (repeatHop2Cycle >= rightHop2End && repeatHop2Cycle < secondRest2End); // 두 번째 휴식
        
        return inRestPeriod;
    }
    
    /// <summary>
    /// 크로스 모드의 사이클이 완료되었는지 확인 (애니메이션 사이클 기준)
    /// </summary>
    private bool IsCrossCycleComplete()
    {
        if (!isShuffleCrossMode) return true;
        
        // 크로스 모드에서 애니메이션 사이클 기준으로 휴식 구간 체크
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animNormalizedTime = stateInfo.normalizedTime % 1f;
        float crossCycle = (animNormalizedTime * crossPulseRate) % 1f;
        
        float crossDur = crossDuration / 100f;
        float crossRest = crossRestTime / 100f;
        float crossBreak = crossCycleBreakTime / 100f;
        float oneCrossCycleLength = crossDur + crossRest + crossDur + crossRest + crossBreak;
        float repeatCrossCycle = crossCycle % oneCrossCycleLength;
        
        // 각 구간의 경계점 계산
        float leftCrossEnd = crossDur; // 왼발크로스 끝
        float firstRestEnd = leftCrossEnd + crossRest; // 첫 휴식 끝
        float rightCrossEnd = firstRestEnd + crossDur; // 오른발크로스 끝
        float secondRestEnd = rightCrossEnd + crossRest; // 두 번째 휴식 끝
        
        // 휴식 구간에 있으면 완료로 간주 (강도 체크 없이 순수 애니메이션 사이클 기준)
        bool inRestPeriod = (repeatCrossCycle >= secondRestEnd) || // 사이클 간 휴식
                           (repeatCrossCycle >= leftCrossEnd && repeatCrossCycle < firstRestEnd) || // 첫 번째 휴식
                           (repeatCrossCycle >= rightCrossEnd && repeatCrossCycle < secondRestEnd); // 두 번째 휴식
        
        // 모드 전환 요청 시에만 상세 디버그
        if (isPendingModeSwitch && Time.time % 0.2f < Time.deltaTime)
        {
            Debug.Log($"🔍 크로스 사이클 체크: 사이클={repeatCrossCycle:F3}, 휴식구간={inRestPeriod}, 완료={inRestPeriod}");
        }
        
        return inRestPeriod;
    }
    
    /// <summary>
    /// 현재 활성화된 모드의 사이클이 완료되었는지 확인
    /// </summary>
    private bool IsCurrentModeCycleComplete()
    {
        if (isKickOutMode) return IsKickOutCycleComplete();
        if (isHopMode) return IsHopCycleComplete();
        if (isHop2Mode) return IsHop2CycleComplete();
        if (isShuffleCrossMode) return IsCrossCycleComplete();
        
        return true; // 기본 모드일 때는 즉시 전환 가능
    }
    
    /// <summary>
    /// 대기 중인 모드 전환을 실행
    /// </summary>
    private void ExecutePendingModeSwitch()
    {
        if (!isPendingModeSwitch) return;
        
        Debug.Log($"🔄 사이클 완료! 대기 중인 모드 전환 실행: IS={pendingISValue}");
        
        // 홉 모드에서 다른 모드로 전환 시 몸통 위치 즉시 바닥으로 초기화
        if (isHopMode && pendingISValue != 3) // IS=3은 홉 모드
        {
            Debug.Log($"🏃 홉 모드에서 다른 모드로 전환 - 몸통 위치 바닥으로 초기화");
            targetBodyY = 0f;
            currentBodyY = 0f; // 즉시 바닥으로
            Vector3 pos = transform.position;
            pos.y = 0f;
            transform.position = pos;
        }
        
        // 대기 중인 IS 값으로 모드 전환 실행
        ProcessISValueEffect(pendingISValue, 1.0f);
        
        // 대기 상태 초기화
        isPendingModeSwitch = false;
        pendingISValue = -1;
        isCurrentCycleCompleting = false;
    }

}
