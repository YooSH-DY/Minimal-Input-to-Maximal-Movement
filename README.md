# Dual-Sensor Real-Time Avatar Dance Control

> **MediaPipe Hands · Movella DOT IMU · WebSocket · Unity Humanoid IK**  
> 웹캠 기반 손가락 굽힘 입력과 손등 IMU의 움직임 데이터를 결합해, 최소한의 손 동작으로 Unity 아바타의 춤 형태·다리 높이·동작 속도를 실시간 제어하는 VR/HCI 프로토타입입니다.

<!-- 공개 가능한 실제 실행 GIF가 준비되면 아래 주석을 해제하세요.
<p align="center">
  <img src="assets/demo.gif" width="78%" alt="Dual-sensor avatar dance control demo">
</p>
-->

---

## What I Built

이 프로젝트는 **손가락 제스처로 춤 동작의 형태를 선택하고, IMU 기반 손목 움직임으로 모션 파라미터를 연속 제어하는 dual-sensor avatar interaction system**입니다.

Python은 웹캠 영상에서 MediaPipe Hands landmark를 추출하고, 검지 굽힘 각도를 네 단계로 분류해 Spongebob Shuffle의 기본 동작과 세 가지 변형을 선택합니다. 손등에 착용한 Movella DOT IMU는 손목 방향과 가속도 데이터를 제공하며, Unity에서는 이를 아바타의 다리 높이와 animation speed로 변환합니다.

이산적인 dance-mode selection과 연속적인 motion-parameter control을 분리해, 단순한 손 입력만으로도 여러 전신 동작 변형을 실시간 생성할 수 있도록 구성했습니다.

---

## System Overview

```text
Webcam
  ↓
MediaPipe Hands
  ↓
Index-finger flexion classification: IFF0 / IFF1 / IFF2 / IFF3
  ↓
WebSocket gesture message
  ↓
Unity dance-mode selection
  ↓
Basic Shuffle / Kick-out / Hop / Cross

Movella DOT IMU
  ↓
Wrist orientation + acceleration
  ↓
WebSocket sensor message
  ↓
Unity Humanoid IK / Animator
  ↓
Leg-height control + movement-velocity control
```

---

## Key Features

- **Discrete and continuous control**  
  손가락 굽힘 상태는 dance variation을 선택하고, IMU 입력은 다리 높이와 animation speed를 연속적으로 조절합니다.

- **Four-level finger-flexion mapping**  
  검지 굽힘 각도를 `IFF0–IFF3`의 네 단계로 분류해 기본 Spongebob Shuffle, Kick-out, Hop, Cross 동작으로 매핑합니다.

- **Occlusion-aware gesture design**  
  검지와 중지를 함께 움직이는 V-shaped gesture를 사용하되, 측면 카메라에서 발생하는 손가락 폐색을 고려해 검지 각도를 대표 분류값으로 사용합니다.

- **Real-time sensor fusion**  
  MediaPipe 기반 시각 입력과 Movella DOT의 quaternion·acceleration 입력을 WebSocket으로 Unity에 전달해 하나의 motion-control pipeline에서 처리합니다.

- **Humanoid motion parameterization**  
  Unity Animator와 Humanoid IK를 이용해 단순 animation switching을 넘어 다리 높이와 움직임 강도를 실시간으로 변경합니다.

- **Consumer-grade setup**  
  일반 웹캠과 손등 착용형 IMU만으로 구성해 full-body motion-capture 장비 없이도 동작하도록 설계했습니다.

---

## Gesture-to-Motion Mapping

| Finger state | Approximate flexion range | Avatar dance variation |
|---|---:|---|
| `IFF0` | 0°–25° | Basic Spongebob Shuffle |
| `IFF1` | 42°–70° | Kick-out |
| `IFF2` | 75°–95° | Hop |
| `IFF3` | 102° or greater | Cross |

| IMU input | Unity behavior |
|---|---|
| Wrist bends upward | Raises the avatar's leg height |
| Wrist bends downward | Lowers the avatar's leg height |
| Acceleration magnitude | Controls animation speed and movement intensity |

손 제스처는 **어떤 동작을 수행할지**를 선택하고, IMU 입력은 **선택된 동작을 어떻게 표현할지**를 조절합니다. 이 구조를 통해 하나의 기본 dance animation에서 여러 형태와 강도의 동작을 생성합니다.

---

## Core Implementation

### Python: `VRST_handtest.py`

- 웹캠에서 MediaPipe Hands landmark 추출
- 검지의 interphalangeal joint를 이용해 flexion angle 계산
- 검지 굽힘 상태를 `IFF0–IFF3` 단계로 변환
- gesture mode와 index-flexion state를 WebSocket 메시지로 전송
- 프레임 단위 입력 변화가 모드 흔들림으로 이어지지 않도록 상태 안정화 처리

### Unity: Sensor and Avatar Control

- `Communication/Datatrans.cs`
  - WebSocketSharp server 구성
  - gesture·quaternion·acceleration message parsing
  - Unity main-thread event dispatch

- `AvatarControl/FootIKController.cs`
  - 손가락 상태에 따른 dance variation 전환
  - 손목 방향에 따른 leg-height parameter 제어
  - acceleration magnitude 기반 animation speed 조절
  - Animator와 Humanoid IK를 통한 실시간 avatar motion 적용

- `AvatarControl/SpinExitBehaviour.cs`
  - Animator state 종료 후 이전 dance state 복원

- `ResearchLogging/ExperimentDataLogger.cs`
  - 입력 명령, 반응 시간, avatar state 및 실험 이벤트 기록

---

## Demonstration Setup

- **Camera:** Logitech C920 webcam
- **IMU:** Movella DOT worn on the back of the hand
- **Sensor rate:** 20 Hz
- **Runtime:** Unity3D + MediaPipe + WebSocket

예비 시연에서는 세 명의 미디어아트 전공자가 시스템을 사용했으며, consumer-grade 장비에서도 입력과 아바타 동작이 지연 없이 연결되는 실시간 제어 가능성을 확인했습니다.

---

## Repository Structure

```text
.
├── VRST_handtest.py                    # MediaPipe gesture and flexion-state sender
├── Communication/
│   └── Datatrans.cs                    # WebSocket server and Unity event bridge
├── AvatarControl/
│   ├── FootIKController.cs             # Dance variation, speed, and humanoid IK control
│   └── SpinExitBehaviour.cs            # Restores the previous dance state
├── ResearchLogging/
│   └── ExperimentDataLogger.cs         # Command, response, and avatar-state logging
├── SCRIPT_GUIDE.md                     # Runtime path and dependency details
└── README.md
```

---

## Tech Stack

`Python` · `OpenCV` · `MediaPipe Hands` · `Movella DOT` · `IMU` · `WebSocket` · `Unity` · `C#` · `WebSocketSharp` · `Animator` · `Humanoid IK`

---

## Related Publication

This repository is a portfolio-oriented implementation summary based on the following published poster.

**Minimal Input to Maximal Movement: Real-Time Avatar Control with Dual-sensor**  
Poster, 31st ACM Symposium on Virtual Reality Software and Technology (VRST 2025)

DOI: [10.1145/3756884.3768392](https://doi.org/10.1145/3756884.3768392)

---

## Portfolio Repository Scope

This repository is a **source-only portfolio bundle** intended to show the multimodal input-to-avatar movement pipeline and implementation structure.

Included:

- MediaPipe-based finger-bend classification
- WebSocket bridge for gesture and sensor messages
- Unity dance-variation and motion-parameter control
- Animator and Humanoid IK integration with experiment logging

Excluded:

- Unity scenes, prefabs, avatar models, Animator Controller, and animation assets
- Raw Movella DOT sensor recordings and experiment CSV files
- Participant videos, participant data, and build outputs
- Unity-generated `Library/`, `Temp/`, `Logs/`, and `Obj/` folders

This repository is not independently runnable without the original Unity scene references, Animator Controller, humanoid rig, IK targets, Movella DOT data source, and WebSocketSharp dependency. See [SCRIPT_GUIDE.md](SCRIPT_GUIDE.md) for the verified runtime flow and file dependencies.

For Unity running on another device, set the WebSocket address before starting the Python sender.

```bash
export UNITY_WS_URL="ws://<unity-host>:5678"
python3 VRST_handtest.py
```
