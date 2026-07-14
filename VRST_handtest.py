import json
import math
import os
import time

import cv2
import mediapipe as mp
import websocket  # 웹소켓 클라이언트


# Local Unity development is the default. Override this environment variable
# when the Unity server runs on another machine.
UNITY_WS_URL = os.getenv("UNITY_WS_URL", "ws://127.0.0.1:5678")

# Mediapipe 초기화
mp_hands = mp.solutions.hands
mp_drawing = mp.solutions.drawing_utils

# mode 확정 시스템을 위한 변수들 (캘리브레이션 시스템 초기화 아래에 추가)
mode_confirmation_count = 0  # 같은 모드가 연속으로 감지된 횟수
last_detected_mode = None  # 마지막으로 감지된 모드
last_confirmed_mode = None  # 마지막으로 확정된 모드 (3회 연속)
last_sent_mode = None  # 마지막으로 웹소켓으로 전송한 모드
last_sent_is = None  # 마지막으로 전송한 index state
MODE_CONFIRMATION_THRESHOLD = 3  # 모드 확정을 위한 임계값
mode5_counter = 0
MODE5_CONFIRM_FRAMES = 100  # 약 1초 (30fps 기준)


# 지수이동평균 스무딩 클래스
class ExponentialMovingAverage:
    def __init__(
        self, alpha=0.1
    ):  # Note: The default alpha in the class definition is not what's used for the instance.
        self.alpha = alpha
        self.last_value = None

    def smooth(self, value):
        if self.last_value is None:
            self.last_value = value
            return value
        smoothed_value = self.alpha * value + (1 - self.alpha) * self.last_value
        self.last_value = smoothed_value
        return smoothed_value

    def reset(self):
        self.last_value = None


# 전역 스무딩 객체 생성 (카메라 열기 전에 추가)
distance_smoother = ExponentialMovingAverage(
    alpha=0.2
)  # 변경: alpha 값을 0.05에서 0.03으로 수정
angle_smoother = ExponentialMovingAverage(alpha=0.1)
# 이전 모드 상태를 추적하기 위한 변수
prev_mode = None
# 이전 프레임의 검지 단계 코드 (빈구간에서 이전 값 유지용)
last_index_status_code = None
# index state confirmation
INDEX_CONFIRM_TIME = 0  # seconds required to confirm a new index state
index_candidate = None
index_candidate_start = None


def check_hand_orientation(hand_landmarks):
    """손목의 방향을 확인하여 팔이 수직인지 판단"""
    wrist = hand_landmarks.landmark[mp_hands.HandLandmark.WRIST]
    middle_mcp = hand_landmarks.landmark[mp_hands.HandLandmark.MIDDLE_FINGER_MCP]

    # 손목에서 중지 MCP로의 벡터 (손의 주축)
    hand_vector_y = middle_mcp.y - wrist.y

    # y값이 양수면 손이 아래쪽을 향함 (정상), 음수면 위쪽을 향함 (팔을 든 상태)
    # 임계값: -0.05 (약간의 여유를 둠)
    is_arm_raised = hand_vector_y < -0.05

    return is_arm_raised


def is_thumb_extended(hand_landmarks, handedness):
    mcp = hand_landmarks.landmark[2]
    tip = hand_landmarks.landmark[4]
    wrist = hand_landmarks.landmark[0]
    index_mcp = hand_landmarks.landmark[5]
    middle_mcp = hand_landmarks.landmark[9]
    ring_mcp = hand_landmarks.landmark[13]
    pinky_mcp = hand_landmarks.landmark[17]

    # 손바닥 중심 계산 (MCP 4개 평균)
    palm_cx = (index_mcp.x + middle_mcp.x + ring_mcp.x + pinky_mcp.x) / 4
    palm_cy = (index_mcp.y + middle_mcp.y + ring_mcp.y + pinky_mcp.y) / 4

    # TIP ~ 손바닥 중심 거리
    dist_tip_palm = math.hypot(tip.x - palm_cx, tip.y - palm_cy)
    # MCP ~ TIP 거리
    dist_mcp_tip = math.hypot(tip.x - mcp.x, tip.y - mcp.y)

    # 엄지 각도
    angle = calculate_angle(
        (mcp.x, mcp.y),
        (hand_landmarks.landmark[3].x, hand_landmarks.landmark[3].y),
        (tip.x, tip.y),
    )

    # TIP이 손바닥 중심에 가까우면 접힘 (더 엄격한 기준)
    if dist_tip_palm < dist_mcp_tip * 0.8:
        return False  # 접힘

    # 엄지와 검지 PIP 사이 거리 체크 (mode2에서 false positive 방지)
    index_pip = hand_landmarks.landmark[6]
    thumb_index_distance = math.hypot(tip.x - index_pip.x, tip.y - index_pip.y)
    palm_width = math.hypot(index_mcp.x - pinky_mcp.x, index_mcp.y - pinky_mcp.y)

    # 엄지가 검지에 너무 가까우면 접힌 것으로 판단 (더 엄격한 기준)
    if thumb_index_distance < palm_width * 0.8:
        return False

    # 정면 카메라를 위한 개선된 엄지 인식 로직
    # 1. 각도 기반 판단 (관대한 기준)
    if (
        angle > 140
    ):  # 각도 기준을 140도로 낮춤 (정면에서는 각도가 더 작게 측정될 수 있음)
        # 2. 거리 기반 추가 검증
        # 엄지 끝이 손바닥 중심에서 충분히 멀리 있는가?
        if dist_tip_palm > dist_mcp_tip * 0.6:  # 기준을 0.8에서 0.6으로 완화
            return True

    # 3. 거리만으로도 판단 (각도가 부정확할 때를 위한 백업)
    # 엄지가 손바닥 중심에서 멀고, MCP에서 TIP까지의 거리가 충분하면 펴진 것으로 판단
    if dist_tip_palm > palm_width * 0.4 and dist_mcp_tip > palm_width * 0.3:
        return True

    return False


# 각도 계산 함수 (세 점으로 이루어진 각도)
def calculate_angle(a, b, c):
    """a, b, c는 (x, y, z) 튜플. b는 각도의 꼭짓점"""
    ba = (a[0] - b[0], a[1] - b[1])
    bc = (c[0] - b[0], c[1] - b[1])
    cosine_angle = (ba[0] * bc[0] + ba[1] * bc[1]) / (
        math.sqrt(ba[0] ** 2 + ba[1] ** 2) * math.sqrt(bc[0] ** 2 + bc[1] ** 2) + 1e-6
    )
    angle = math.acos(cosine_angle)
    return math.degrees(angle)


# 손가락이 펴졌는지 판단 함수
def finger_angle(hand_landmarks, mcp_id, pip_id, tip_id):
    mcp = hand_landmarks.landmark[mcp_id]
    pip = hand_landmarks.landmark[pip_id]
    tip = hand_landmarks.landmark[tip_id]
    return calculate_angle((mcp.x, mcp.y), (pip.x, pip.y), (tip.x, tip.y))


# 손가락별 각도 임계값 (160도 이상이면 펴짐)
ANGLE_THRESHOLD = 150

# 캘리브레이션 시스템 초기화 - 제거됨

# 카메라 열기
cap = cv2.VideoCapture(0)
# 성능 최적화를 위해 해상도 설정 HD
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1080)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)

# 모드 상태 변수 추가
mode = None

# WebSocket 서버 연결
ws = None
try:
    # Add a timeout to the connection attempt (e.g., 2 seconds)
    ws = websocket.create_connection(UNITY_WS_URL, timeout=2)
    print("WebSocket connected.")
except websocket.WebSocketTimeoutException:
    print("WebSocket connection timed out after 2 seconds.")
    print("Continuing without WebSocket connection...")
    ws = None
except ConnectionRefusedError:
    print(
        "WebSocket connection refused by server. Check if the server is running and accessible."
    )
    print("Continuing without WebSocket connection...")
    ws = None
except Exception as e:  # Catch other potential exceptions during connection attempt
    print(f"WebSocket connection attempt failed: {e}")
    print("Continuing without WebSocket connection...")
    ws = None
last_send_time = time.time() - 1

# 메인 루프 시작 전 웹소켓 전송 타이밍 변수 초기화
last_send_time = time.time()

with mp_hands.Hands(
    max_num_hands=1, min_detection_confidence=0.7, min_tracking_confidence=0.7
) as hands:
    while cap.isOpened():
        ret, frame = cap.read()
        if not ret:
            continue

        # 이미지 처리
        image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        image.flags.writeable = False
        results = hands.process(image)

        # 결과 다시 BGR로
        image.flags.writeable = True
        image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

        # 랜드마크 처리
        if results.multi_hand_landmarks:
            for idx, hand_landmarks in enumerate(results.multi_hand_landmarks):
                # 손 방향 확인 (팔을 들었는지 체크)
                is_arm_raised = check_hand_orientation(hand_landmarks)

                # 각 손가락 각도 계산
                angles = {
                    "Thumb": finger_angle(hand_landmarks, 2, 3, 4),
                    "Index": finger_angle(hand_landmarks, 5, 6, 8),
                    "Middle": finger_angle(hand_landmarks, 9, 10, 12),
                    "Ring": finger_angle(hand_landmarks, 13, 14, 16),
                    "Pinky": finger_angle(hand_landmarks, 17, 18, 20),
                }

                # 검지 각도 스무딩 처리
                raw_index_angle = angles["Index"]
                smoothed_index_angle = angle_smoother.smooth(raw_index_angle)

                # === 검지 4단계 분류 (I 코드: 0..3) ===
                # 요구사항: I=0 -> angle >= 155
                #            I=1 -> 120 ~ 130
                #            I=2 -> 90 ~ 110
                #            I=3 -> angle < 78
                # 빈구간(어떤 범위에도 속하지 않음)일 경우 이전 값을 유지
                index_status_code = None
                a = smoothed_index_angle
                if a >= 155:
                    index_status_code = 0
                elif 110 <= a <= 138:
                    index_status_code = 1
                elif 85 <= a <= 105:
                    index_status_code = 2
                elif a < 78:
                    index_status_code = 3
                else:
                    # 빈구간: 이전 값 유지
                    index_status_code = last_index_status_code

                # 첫 프레임에서 이전 값이 없으면 가장 가까운 유효 값으로 초기화
                if index_status_code is None:
                    # choose nearest range center
                    centers = {0: 157.5, 1: 125, 2: 100, 3: 39}
                    closest = min(centers.keys(), key=lambda k: abs(a - centers[k]))
                    index_status_code = closest

                # 업데이트된 값을 일정 시간(0.4s) 안정적으로 유지했을 때 전역으로 반영
                # If the detected index_status_code differs from the last sent/stable one,
                # start/continue a candidate timer. Only update last_index_status_code when
                # the candidate has been stable for INDEX_CONFIRM_TIME seconds.
                current_time = time.time()

                if index_status_code == last_index_status_code:
                    # same as current stable state, clear candidate
                    index_candidate = None
                    index_candidate_start = None
                else:
                    # new candidate observed
                    if index_candidate != index_status_code:
                        index_candidate = index_status_code
                        index_candidate_start = current_time
                    else:
                        # candidate is continuing; check duration
                        if index_candidate_start is not None and (
                            current_time - index_candidate_start >= INDEX_CONFIRM_TIME
                        ):
                            # confirm change
                            last_index_status_code = index_candidate
                            # reset candidate
                            index_candidate = None
                            index_candidate_start = None

                # 검지 PIP(6), 중지 PIP(10) 좌표 (명시적 랜드마크 이름 사용)
                index_pip = hand_landmarks.landmark[
                    mp_hands.HandLandmark.INDEX_FINGER_PIP
                ]
                middle_pip = hand_landmarks.landmark[
                    mp_hands.HandLandmark.MIDDLE_FINGER_PIP
                ]

                # 이미지 좌표계로 변환 (픽셀 단위)
                h, w, _ = image.shape
                x1_pip, y1_pip = int(index_pip.x * w), int(index_pip.y * h)
                x2_pip, y2_pip = int(middle_pip.x * w), int(middle_pip.y * h)
                pip_distance = math.sqrt(
                    (x1_pip - x2_pip) ** 2 + (y1_pip - y2_pip) ** 2
                )

                # 기준 거리: 손목(0) ~ 검지 MCP(5)
                index_mcp = hand_landmarks.landmark[
                    mp_hands.HandLandmark.INDEX_FINGER_MCP
                ]
                wrist = hand_landmarks.landmark[mp_hands.HandLandmark.WRIST]

                x1_base, y1_base = int(index_mcp.x * w), int(index_mcp.y * h)
                x2_base, y2_base = int(wrist.x * w), int(wrist.y * h)
                base_dist_pixel = math.sqrt(
                    (x1_base - x2_base) ** 2 + (y1_base - y2_base) ** 2
                )

                # 모드별 거리 계산
                norm_dist = -1.0  # 초기값
                raw_norm_dist = -1.0  # 원본 거리값 저장용

                if base_dist_pixel > 1e-6:
                    if mode == "mode2":
                        # 검지 PIP(6), 중지 PIP(10)
                        index_pip = hand_landmarks.landmark[
                            mp_hands.HandLandmark.INDEX_FINGER_PIP
                        ]
                        middle_pip = hand_landmarks.landmark[
                            mp_hands.HandLandmark.MIDDLE_FINGER_PIP
                        ]
                        x1_pip, y1_pip = int(index_pip.x * w), int(index_pip.y * h)
                        x2_pip, y2_pip = int(middle_pip.x * w), int(middle_pip.y * h)
                        pip_distance = math.sqrt(
                            (x1_pip - x2_pip) ** 2 + (y1_pip - y2_pip) ** 2
                        )
                        raw_norm_dist = pip_distance / base_dist_pixel
                        norm_dist = distance_smoother.smooth(
                            raw_norm_dist
                        )  # 스무딩 적용
                        dist_label = "Norm Dist (indexPIP-middlePIP)"

                    elif mode == "mode1":
                        # 검지 PIP(6), 엄지 IP(3)
                        index_pip = hand_landmarks.landmark[
                            mp_hands.HandLandmark.INDEX_FINGER_PIP
                        ]
                        thumb_ip = hand_landmarks.landmark[
                            mp_hands.HandLandmark.THUMB_IP
                        ]
                        x1_pip, y1_pip = int(index_pip.x * w), int(index_pip.y * h)
                        x2_pip, y2_pip = int(thumb_ip.x * w), int(thumb_ip.y * h)
                        pip_distance = math.sqrt(
                            (x1_pip - x2_pip) ** 2 + (y1_pip - y2_pip) ** 2
                        )
                        raw_norm_dist = pip_distance / base_dist_pixel
                        norm_dist = distance_smoother.smooth(
                            raw_norm_dist
                        )  # 스무딩 적용
                        dist_label = "Norm Dist (indexPIP-thumbIP)"

                    else:
                        dist_label = "Norm Dist (wrist-idxMCP)"

                # 각도 기반 펴짐/굽힘 판단 (스무딩된 각도 사용)
                fingers = {
                    finger: (angle > ANGLE_THRESHOLD)
                    for finger, angle in angles.items()
                }

                # 검지는 스무딩된 각도로 다시 판단
                fingers["Index"] = smoothed_index_angle > ANGLE_THRESHOLD

                # 엄지손가락은 별도 판별 (handedness 정보 활용)
                handedness = None
                thumb_angle = 0
                if results.multi_handedness:
                    handedness = results.multi_handedness[idx].classification[0].label
                if handedness:
                    fingers["Thumb"] = is_thumb_extended(hand_landmarks, handedness)
                    # 엄지 각도 계산 (디버깅용)
                    mcp = hand_landmarks.landmark[2]
                    thumb_angle = calculate_angle(
                        (mcp.x, mcp.y),
                        (hand_landmarks.landmark[3].x, hand_landmarks.landmark[3].y),
                        (hand_landmarks.landmark[4].x, hand_landmarks.landmark[4].y),
                    )

                # 모드 판별 및 상태 유지 (팔을 든 상태에서는 모드 변경 방지)
                if not is_arm_raised:
                    # mode5 조건: 엄지만 굽힘, 나머지 손가락은 모두 펴짐
                    if (
                        not fingers["Thumb"]
                        and fingers["Index"]
                        and fingers["Middle"]
                        and fingers["Ring"]
                        and fingers["Pinky"]
                    ):
                        mode5_counter += 1
                        if mode5_counter >= MODE5_CONFIRM_FRAMES:
                            current_mode = "mode5"
                        else:
                            current_mode = None  # 아직 확정 아님
                    else:
                        mode5_counter = 0  # 모드5 카운터 리셋

                        # mode0 조건: 손가락 5개 모두 펴짐
                        if (
                            fingers["Thumb"]
                            and fingers["Index"]
                            and fingers["Middle"]
                            and fingers["Ring"]
                            and fingers["Pinky"]
                        ):
                            current_mode = "mode0"

                        # 기존 mode1, mode2 판별 로직...
                        elif mode == "mode1":
                            if (
                                fingers["Index"]
                                and fingers["Middle"]
                                and not fingers["Thumb"]
                                and not fingers["Ring"]
                                and not fingers["Pinky"]
                            ):
                                current_mode = "mode2"
                            else:
                                current_mode = "mode1"

                        elif mode == "mode2":
                            if (
                                fingers["Thumb"]
                                and fingers["Index"]
                                and not fingers["Middle"]
                                and not fingers["Ring"]
                                and not fingers["Pinky"]
                            ):
                                current_mode = "mode1"
                            else:
                                current_mode = "mode2"

                        else:
                            # mode2 조건: 검지와 중지만 펴짐, 나머지는 굽힘
                            if (
                                fingers["Index"]
                                and fingers["Middle"]
                                and not fingers["Thumb"]
                                and not fingers["Ring"]
                                and not fingers["Pinky"]
                            ):
                                current_mode = "mode2"
                            # mode1 조건: 엄지와 검지만 펴짐, 나머지는 굽힘
                            elif (
                                fingers["Thumb"]
                                and fingers["Index"]
                                and not fingers["Middle"]
                                and not fingers["Ring"]
                                and not fingers["Pinky"]
                            ):
                                current_mode = "mode1"
                            else:
                                current_mode = None

                    # === 새로 추가: Mode 확정 시스템 ===
                    # Ignore mode1 and mode5 entirely for confirmation
                    if current_mode in ("mode1", "mode5"):
                        current_mode = None

                    if current_mode == last_detected_mode:
                        # 같은 모드가 연속으로 감지됨
                        mode_confirmation_count += 1
                        print(
                            f"Mode confirmation: {current_mode} x{mode_confirmation_count}"
                        )
                    else:
                        # 다른 모드가 감지됨 - 카운트 리셋
                        mode_confirmation_count = 1
                        last_detected_mode = current_mode
                        print(f"Mode changed to: {current_mode} (count reset)")

                    # 모드 확정 조건: 3회 연속 동일한 모드 감지
                    if (
                        mode_confirmation_count >= MODE_CONFIRMATION_THRESHOLD
                        and current_mode != last_confirmed_mode
                    ):
                        last_confirmed_mode = current_mode
                        if current_mode:
                            mode = current_mode  # 실제 mode 변수 업데이트
                            print(f"Mode CONFIRMED: {mode} (3회 연속 감지)")

                            # 모드 변경 시 거리 스무딩 초기화
                            if prev_mode != mode:
                                distance_smoother.reset()
                        else:
                            mode = None
                    elif mode_confirmation_count >= MODE_CONFIRMATION_THRESHOLD:
                        # 이미 확정된 모드가 계속 유지되는 경우
                        mode = last_confirmed_mode

                # 이전 모드 상태 업데이트
                prev_mode = mode

                # 결과 출력
                y0 = 30
                for finger_name, is_extended in fingers.items():
                    text = f"{finger_name}: {'1' if is_extended else '0'}"
                    cv2.putText(
                        image,
                        text,
                        (10, y0),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.6,
                        (0, 255, 0),
                        2,
                    )
                    y0 += 25

                cv2.putText(
                    image,
                    f"Index Angle: {int(smoothed_index_angle)}",
                    (10, y0),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.6,
                    (255, 0, 0),
                    2,
                )
                y0 += 25

                # Display index status code (numeric 0..3) with color
                try:
                    isc = last_index_status_code
                except NameError:
                    isc = None

                index_colors = {
                    0: (0, 0, 255),
                    1: (0, 165, 255),
                    2: (0, 255, 255),
                    3: (0, 255, 0),
                }

                if isc in index_colors:
                    label = f"Index State: {isc}"
                    color = index_colors[isc]
                else:
                    label = "Index State: N/A"
                    color = (200, 200, 200)

                cv2.putText(
                    image,
                    label,
                    (10, y0),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.7,
                    color,
                    2,
                )
                y0 += 30

                # 모드 출력
                if mode == "mode0":
                    cv2.putText(
                        image,
                        "MODE 0",
                        (10, y0),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.8,
                        (255, 0, 255),
                        3,
                    )
                elif mode == "mode1":
                    cv2.putText(
                        image,
                        "MODE 1",
                        (10, y0),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.8,
                        (255, 255, 0),
                        3,
                    )
                elif mode == "mode2":
                    cv2.putText(
                        image,
                        "MODE 2",
                        (10, y0),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.8,
                        (0, 255, 255),
                        3,
                    )
                elif mode == "mode5":  # 새로 추가
                    cv2.putText(
                        image,
                        "MODE 5",
                        (10, y0),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.8,
                        (0, 0, 255),  # 빨간색으로 표시
                        3,
                    )

                # 손 랜드마크 시각화
                mp_drawing.draw_landmarks(
                    image, hand_landmarks, mp_hands.HAND_CONNECTIONS
                )

        # 화면에 표시
        cv2.imshow("Hand Tracking", image)

        # 창에 포커스 유지를 위해 창 속성 설정
        cv2.setWindowProperty("Hand Tracking", cv2.WND_PROP_TOPMOST, 1)

        # 키 입력 처리
        key = cv2.waitKey(1) & 0xFF

        # 디버그: 키 입력 감지 확인
        if key != 255:  # 키가 눌렸을 때만 출력
            print(
                f"Key pressed: {key} (char: {chr(key) if 32 <= key <= 126 else 'N/A'})"
            )

        if key == 27:  # ESC 키
            break
        elif key == 3:  # Ctrl+C
            print("Ctrl+C pressed. Exiting...")
            break
        elif key == ord("0"):  # '0' 키로 웹소켓 재연결
            if ws is None:
                try:
                    ws = websocket.create_connection(UNITY_WS_URL, timeout=2)
                    print("WebSocket reconnected.")
                except Exception as e:
                    print(f"WebSocket reconnection failed: {e}")
                    ws = None
            else:
                print("WebSocket is already connected.")

        # 웹소켓 전송 - 모드 변경 및 주기적 is 전송
        # 주기적으로 IS 값을 웹소켓으로 전송 (0.2초마다)
        PERIOD = 0.2  # 초 단위 (200ms)
        current_time = time.time()

        # mode 변경 체크: 확정된 모드가 마지막 전송 모드와 다를 때만 전송
        mode_changed = (
            last_confirmed_mode != last_sent_mode and last_confirmed_mode is not None
        )

        # is 값은 기존 index classification 사용
        is_val = last_index_status_code

        # 모드 변경 시 m과 is 함께 전송 (mode0과 mode2만)
        if mode_changed and mode in ["mode0", "mode2"]:
            m_val = 0 if mode == "mode0" else 2
            payload = {"m": m_val, "is": is_val}
            if ws is not None:
                try:
                    ws.send(json.dumps(payload))
                    last_sent_mode = mode
                    last_sent_is = is_val
                    print(f"Mode change sent: m={m_val}, is={is_val}")
                except Exception as e:
                    print(f"WebSocket send error: {e}")
                    ws = None
            else:
                print(f"Mode change (NO WS): m={m_val}, is={is_val}")
                last_sent_mode = mode
                last_sent_is = is_val

        # 주기적으로 is 값만 전송 (mode0과 mode2일 때만)
        elif mode in ["mode0", "mode2"]:
            if "last_periodic_send" not in globals():
                global last_periodic_send
                last_periodic_send = 0
            if current_time - last_periodic_send >= PERIOD:
                payload = {"is": is_val}
                if ws is not None:
                    try:
                        ws.send(json.dumps(payload))
                        last_periodic_send = current_time
                        last_sent_is = is_val
                        print(f"Periodic is sent: is={is_val}")
                    except Exception as e:
                        print(f"WebSocket send error: {e}")
                        ws = None
                else:
                    print(f"Periodic is (NO WS): is={is_val}")
                    last_periodic_send = current_time
                    last_sent_is = is_val


cap.release()
cv2.destroyAllWindows()
