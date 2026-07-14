# VRST 2025 Unity Gesture Bridge

## Scope

This source-only portfolio bundle is extracted from the `dollmove 통합 복사본` Unity project. It contains the active WebSocket bridge, the extended hand-gesture command handler, the animator Spin exit behavior, and experiment logging code. Scenes, prefabs, animation assets, avatar models, generated CSV files, and editor tooling are intentionally excluded.

## Verified Runtime Transport

The only enabled build scene is `Assets/Scenes/SampleScene.unity`.

- Transport: WebSocket server using `WebSocketSharp`.
- Active scene object: `WebSocket`, with `DataTransServer` enabled.
- Configured port: `5678`.
- Routes: `/` for general gesture messages, `/watch` for watch data, `/dot` for DOT data.
- UDP: no active UDP receiver, sender, or `UdpClient` execution path was found.

The copied server binds with `IPAddress.Any`, so no machine-specific network address is included. Port `5678` is a development configuration value, not a secret.

`VRST_handtest.py` reads `UNITY_WS_URL` and defaults to `ws://127.0.0.1:5678`. Set that environment variable only when the Unity server runs on another machine.

## Runtime Flow

```text
Python gesture sender
  -> WebSocket message
  -> DataTransServer / DataTransBehavior
  -> main-thread events
  -> FootGlideController
  -> Animator state, humanoid IK, movement direction, and gesture submodes

Animator Spin exit
  -> SpinExitBehaviour
  -> restore saved Walking / Runningman / Spongebob state
```

`DataTransBehavior` parses JSON gesture fields `m`, `d`, `a`, `IB`, `FB`, and both `IS` / `is`. It also parses `qx`, `qy`, `qz`, and accelerometer values from the `W:` and `DOT:` formats. Events are queued before Unity's main thread invokes the avatar controller.

## Gesture-to-Avatar Mapping

| Input | Active behavior in `FootGlideController` |
|---|---|
| `m = 0` | Switches to walking |
| `m = 1` | Switches to Runningman |
| `m = 2` | Switches to Spongebob |
| `m = 5` | Starts the jump sequence |
| `d` | Controls stride: Runningman front-back depth or Spongebob left-right spacing |
| `a` | Controls upper-body forward lean |
| `qx` | Controls animation speed and integrated-motion posture / stride behavior |
| `qz` | Controls direction rotation in the non-integrated control path |
| `IB` | Enables conditional speed control when the FB state is a middle state |
| `FB = 1 / 4` | Requests 90-degree left / right direction turns; `FB = 2 / 3` re-arms the next turn command |
| `IS = 0 / 1 / 2 / 3 / 4` | Selects Hop2 / Kick-out / Front-cross / Hop / default Spongebob submodes |

`qy` is received and stored, but this variant explicitly removes qy/qz-driven Spin initiation. The included `SpinExitBehaviour` remains an Animator state dependency and restores the last dance if Spin is entered by another route. Accelerometer values are received for logging/state capture but do not directly control the avatar in the current controller.

## Copied Files

| Bundle path | Role | Original project-relative path |
|---|---|---|
| `Communication/Datatrans.cs` | Starts the WebSocket server, registers routes, parses gesture and sensor messages, manages connection state, and raises main-thread events. | `Assets/Scirpts/Datatrans.cs` |
| `AvatarControl/FootIKController.cs` | Contains `FootGlideController`, the active extended gesture command handler. Maps `m/d/a/qx/qz/IB/FB/IS` to animation, stride, posture, direction, and Spongebob submodes. | `Assets/FootIKController.cs` |
| `AvatarControl/SpinExitBehaviour.cs` | Animator state-machine behavior that restores the previous dance when Spin exits. | `Assets/SpinExitBehaviour.cs` |
| `ResearchLogging/ExperimentDataLogger.cs` | Active experiment logger. Records task events, received command values, response timing, and avatar joint state to CSV under `Application.persistentDataPath`. | `Assets/Scirpts/ExperimentDataLogger.cs` |

## Dependencies

```text
Datatrans.cs
  -> WebSocketSharp / WebSocketSharp.Server (external library)
  -> DataTransBehavior events
      -> FootGlideController (inside FootIKController.cs)
          -> Unity Animator, humanoid avatar rig, Rigidbody, UI and IK targets
          -> SpongebobController animator asset
              -> SpinExitBehaviour
      -> ExperimentDataLogger
          -> FootGlideController and Animator
```

`SampleScene` has enabled `DataTransServer`, `FootGlideController`, and `ExperimentDataLogger` components. The avatar script depends on Animator triggers and states such as `ToWalking`, `ToRunningman`, `ToSpongebob`, `ToSpin`, `walking`, `RunningmanRecentSensing_jin`, and `SpongebobRecentsension_jin`.

## Recommended Reading Order

1. `Communication/Datatrans.cs`
2. `AvatarControl/FootIKController.cs`
3. `AvatarControl/SpinExitBehaviour.cs`
4. `ResearchLogging/ExperimentDataLogger.cs`

## Major Exclusions

| Excluded item | Reason |
|---|---|
| `Assets/Scenes/SampleScene.unity` | Scene serialization and asset references are not required for the source portfolio. |
| `Assets/SpongebobController.controller` and animation assets | Unity animation assets rather than implementation source. |
| `Assets/StyleManager.cs`, `StyleProfile.cs`, `IKMotionController.cs`, `IKUIBinder.cs`, `FootSpacingController.cs` | No active scene reference or direct dependency from the verified command path. |
| `Assets/Editor/`, Tutorial scripts, frame counter utilities | Editor-only, tutorial, or non-core support code. |
| `Library/`, `Temp/`, `Logs/`, `Packages/`, build output | Generated files, package contents, and non-source material. |
| CSV/log output and avatar assets | May contain experiment data or licensed/non-public assets. |

## Public Release Checklist

- Keep the WebSocket bind and port configuration appropriate for the deployment environment; this copy uses `IPAddress.Any` and port `5678`.
- Do not publish generated CSV files, participant labels, device identifiers, local paths, screen captures, or raw sensor recordings.
- `ExperimentDataLogger.cs` defaults to participant ID `P001`; use only anonymized study IDs at runtime.
- Confirm whether the `WebSocketSharp` binary/license can be redistributed; otherwise list it as an installation prerequisite.
- This bundle is not independently runnable. It requires the original Unity Animator Controller, humanoid rig, animations, Rigidbody/UI/IK target scene references, and WebSocketSharp dependency.
