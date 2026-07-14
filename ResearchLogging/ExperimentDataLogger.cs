using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class ExperimentDataLogger : MonoBehaviour
{
    [Header("Experiment Settings")]
    public string participantID = "P001";
    public FootGlideController footController;
    public DataTransBehavior dataTransBehavior;
    
    [Header("Task Configuration")]
    public List<TaskConfig> tasks = new List<TaskConfig>();
    
    [System.Serializable]
    public class TaskConfig
    {
        public int taskID;
        public string taskType; // "지시형" 또는 "자유형"
        public string controlType; // "통합제어" 또는 "세부제어"
        public string targetStyle; // 목표 스타일 설명
    }
    
    // 실험 상태 관리
    private bool isExperimentActive = false;
    private bool isTaskActive = false;
    private bool isDataRecording = true;
    private int currentTaskIndex = -1;
    private TaskConfig currentTask;
    
    // 타임스탬프 관리
    private float experimentStartTime;
    private float taskStartTime;
    private DateTime realStartTime;
    
    // 데이터 수집 리스트들
    private List<FrameData> frameDataList = new List<FrameData>();
    private List<TaskEvent> taskEventList = new List<TaskEvent>();
    private List<CommandResponse> commandResponseList = new List<CommandResponse>();
    
    // 프레임 데이터 구조체
    [System.Serializable]
    public struct FrameData
    {
        public float timestamp;
        public int taskID;
        public string taskType;
        public string controlType;
        
        // 아바타 조인트 위치/회전
        public Vector3 leftAnklePos, rightAnklePos;
        public Vector3 leftKneePos, rightKneePos;
        public Vector3 leftHipPos, rightHipPos;
        public Vector3 pelvisPos;
        
        public Vector3 leftAnkleRot, rightAnkleRot;
        public Vector3 leftKneeRot, rightKneeRot;
        public Vector3 leftHipRot, rightHipRot;
        public Vector3 pelvisRot;
        
        // 파라미터 값들
        public float spacingRatio;
        public float depthRatio;
        public float bodyLeanAmount;
        public float adjustSpeed;
        public float glideAmount;
        
        // 센서 값들
        public float qx, qy, qz;
        public int mValue, aValue, dValue;
        
        // 애니메이션 상태
        public string currentAnimationState;
        public bool isJumpInProgress;
    }
    
    [System.Serializable]
    public struct TaskEvent
    {
        public float timestamp;
        public int taskID;
        public string eventType; // "START", "END"
        public string taskType;
        public string controlType;
        public string targetStyle;
    }
    
    [System.Serializable]
    public struct CommandResponse
    {
        public float timestamp;
        public int taskID;
        public string commandType; // "M", "A", "D", "QX", "QY", "QZ"
        public float commandValue;
        public float responseDelay; // 명령과 실제 파라미터 변화 간의 지연시간
    }
    
    // 명령 반응 추적을 위한 변수들
    private float lastMCommand = -1;
    private float lastACommand = -1;
    private float lastDCommand = -1;
    private float lastQxCommand = 0;
    private float lastQyCommand = 0;
    private float lastQzCommand = 0;
    
    private float lastMTime, lastATime, lastDTime, lastQxTime, lastQyTime, lastQzTime;

    void Start()
    {
        // DataTransBehavior 자동 찾기 (Inspector에서 할당되지 않은 경우)
        if (dataTransBehavior == null)
        {
            // WebSocket GameObject에서 DataTransServer 컴포넌트 찾기
            var webSocketObj = GameObject.Find("WebSocket");
            if (webSocketObj != null)
            {
                var dataTransServer = webSocketObj.GetComponent<DataTransServer>();
                if (dataTransServer != null)
                {
                    Debug.Log("DataTransBehavior를 자동으로 찾았습니다 (WebSocket GameObject의 DataTransServer 컴포넌트)");
                    // DataTransBehavior는 서버 내부에서 관리되므로 직접 참조할 수 없습니다.
                    // 대신 이벤트 구독은 정적 이벤트를 통해 처리됩니다.
                }
            }
        }
        
        // 기본 태스크 설정 (예시)
        if (tasks.Count == 0)
        {
            SetupDefaultTasks();
        }
        
        // 이벤트 구독 (정적 이벤트 사용)
        DataTransBehavior.OnNewM += OnMValueReceived;
        DataTransBehavior.OnNewA += OnAValueReceived;
        DataTransBehavior.OnNewD += OnDValueReceived;
        DataTransBehavior.OnNewQx += OnQxValueReceived;
        DataTransBehavior.OnNewQy += OnQyValueReceived;
        DataTransBehavior.OnNewQz += OnQzValueReceived;
        
        Debug.Log("=== 실험 데이터 로거 초기화 완료 ===");
        Debug.Log("1키: 태스크 번호 증가");
        Debug.Log("2키: 현재 태스크 녹화 시작");
        Debug.Log("3키: 녹화 저장 및 종료");
        Debug.Log("4키: 데이터 기록 일시정지/재개");
    }
    
    void SetupDefaultTasks()
    {
        // 태스크 1-4
        tasks.Add(new TaskConfig { taskID = 1, taskType = "지시형", controlType = "통합제어", targetStyle = "태스크 1" });
        tasks.Add(new TaskConfig { taskID = 2, taskType = "자유형", controlType = "통합제어", targetStyle = "태스크 2" });
        tasks.Add(new TaskConfig { taskID = 3, taskType = "지시형", controlType = "세부제어", targetStyle = "태스크 3" });
        tasks.Add(new TaskConfig { taskID = 4, taskType = "자유형", controlType = "세부제어", targetStyle = "태스크 4" });
        
        // 태스크 5-8 (추가)
        tasks.Add(new TaskConfig { taskID = 5, taskType = "지시형", controlType = "통합제어", targetStyle = "태스크 5" });
        tasks.Add(new TaskConfig { taskID = 6, taskType = "자유형", controlType = "통합제어", targetStyle = "태스크 6" });
        tasks.Add(new TaskConfig { taskID = 7, taskType = "지시형", controlType = "세부제어", targetStyle = "태스크 7" });
        tasks.Add(new TaskConfig { taskID = 8, taskType = "자유형", controlType = "세부제어", targetStyle = "태스크 8" });
    }

    void Update()
    {
        // 키 입력 처리
        HandleKeyInput();
        
        // 데이터 수집 (태스크가 활성화되고 기록이 활성화된 경우에만)
        // WebSocket 데이터가 실제로 활성화된 경우에만 기록
        if (isTaskActive && isDataRecording && IsWebSocketDataActive())
        {
            CollectFrameData();
        }
    }

    void HandleKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            IncrementTaskNumber();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartRecordingCurrentTask();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            EndExperiment();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ToggleDataRecording();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ResetTaskNumber();
        }
    }
    
    // 이전 로직: 태스크 번호 증가와 녹화 시작이 1키 하나로 이루어짐
    // 새 로직에서는 IncrementTaskNumber와 StartRecordingCurrentTask로 분리됨
    /*
    void StartNextTask()
    {
        if (isTaskActive)
        {
            Debug.LogWarning("현재 태스크가 진행 중입니다. 먼저 2키로 종료하세요.");
            return;
        }
        
        currentTaskIndex++;
        if (currentTaskIndex >= tasks.Count)
        {
            Debug.Log("모든 태스크가 완료되었습니다.");
            return;
        }
        
        currentTask = tasks[currentTaskIndex];
        isTaskActive = true;
        taskStartTime = Time.time;
        
        if (!isExperimentActive)
        {
            isExperimentActive = true;
            experimentStartTime = Time.time;
            realStartTime = DateTime.Now;
        }
        
        // 제어 모드 설정
        if (footController != null)
        {
            bool integratedMode = currentTask.controlType == "통합제어";
            // V키 상태를 프로그램matically 설정하는 것은 제한적이므로, 
            // 사용자에게 안내 메시지를 출력합니다.
            Debug.Log($"=== V키를 눌러 {currentTask.controlType} 모드로 전환하세요 ===");
        }
        
        // 태스크 시작 이벤트 기록
        TaskEvent startEvent = new TaskEvent
        {
            timestamp = Time.time - experimentStartTime,
            taskID = currentTask.taskID,
            eventType = "START",
            taskType = currentTask.taskType,
            controlType = currentTask.controlType,
            targetStyle = currentTask.targetStyle
        };
        taskEventList.Add(startEvent);
        
        Debug.Log($"=== 태스크 {currentTask.taskID} 시작 ===");
        Debug.Log($"타입: {currentTask.taskType}");
        Debug.Log($"제어: {currentTask.controlType}");
        Debug.Log($"목표: {currentTask.targetStyle}");
    }
    */
    
    // 이전 로직: 2키로 태스크 종료 및 저장
    // 새 로직에서는 3키로 녹화 저장 및 종료로 변경됨
    /*
    void EndCurrentTask()
    {
        if (!isTaskActive)
        {
            Debug.LogWarning("진행 중인 태스크가 없습니다.");
            return;
        }
        
        // 태스크 종료 이벤트 기록
        TaskEvent endEvent = new TaskEvent
        {
            timestamp = Time.time - experimentStartTime,
            taskID = currentTask.taskID,
            eventType = "END",
            taskType = currentTask.taskType,
            controlType = currentTask.controlType,
            targetStyle = currentTask.targetStyle
        };
        taskEventList.Add(endEvent);
        
        float taskDuration = Time.time - taskStartTime;
        Debug.Log($"=== 태스크 {currentTask.taskID} 종료 ===");
        Debug.Log($"소요 시간: {taskDuration:F2}초");
        
        // 태스크별 데이터 저장
        SaveTaskData();
        
        isTaskActive = false;
    }
    */
    
    void EndExperiment()
    {
        if (isTaskActive)
        {
            // 태스크 종료 이벤트 기록
            TaskEvent endEvent = new TaskEvent
            {
                timestamp = Time.time - experimentStartTime,
                taskID = currentTask.taskID,
                eventType = "END",
                taskType = currentTask.taskType,
                controlType = currentTask.controlType,
                targetStyle = currentTask.targetStyle
            };
            taskEventList.Add(endEvent);
            
            float taskDuration = Time.time - taskStartTime;
            Debug.Log($"=== 태스크 {currentTask.taskID} 녹화 종료 ===");
            Debug.Log($"소요 시간: {taskDuration:F2}초");
            
            // 태스크별 데이터 저장
            SaveTaskData();
            
            isTaskActive = false;
        }
        
        if (isExperimentActive)
        {
            float totalDuration = Time.time - experimentStartTime;
            Debug.Log($"=== 실험 종료 ===");
            Debug.Log($"총 소요 시간: {totalDuration:F2}초");
            
            // 전체 실험 데이터 저장
            SaveAllExperimentData();
            
            isExperimentActive = false;
        }
    }
    
    void ToggleDataRecording()
    {
        isDataRecording = !isDataRecording;
        Debug.Log($"데이터 기록: {(isDataRecording ? "재개" : "일시정지")}");
    }
    
    // WebSocket 데이터가 실제로 활성화되었는지 확인
    bool IsWebSocketDataActive()
    {
        if (footController == null) return false;
        
        // FootGlideController의 isWebSocketDataEnabled 필드 확인
        var isWebSocketEnabled = GetFieldValue<bool>(footController, "isWebSocketDataEnabled");
        
        if (!isWebSocketEnabled)
        {
            // WebSocket이 비활성화된 경우 사용자에게 알림 (5초마다)
            if (Time.time % 5f < Time.deltaTime)
            {
                Debug.LogWarning("CSV 기록 대기 중: 스페이스바를 눌러 WebSocket 데이터를 활성화하세요!");
            }
            return false;
        }
        
        return true;
    }
    
    void CollectFrameData()
    {
        if (footController == null) return;
        
        FrameData frame = new FrameData();
        
        // 기본 정보
        frame.timestamp = Time.time - experimentStartTime;
        frame.taskID = currentTask.taskID;
        frame.taskType = currentTask.taskType;
        frame.controlType = currentTask.controlType;
        
        // 아바타 조인트 데이터 수집
        Animator animator = footController.GetComponent<Animator>();
        if (animator != null && animator.isHuman)
        {
            // 발목 (Ankle)
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftFoot != null)
            {
                frame.leftAnklePos = leftFoot.position;
                frame.leftAnkleRot = leftFoot.eulerAngles;
            }
            if (rightFoot != null)
            {
                frame.rightAnklePos = rightFoot.position;
                frame.rightAnkleRot = rightFoot.eulerAngles;
            }
            
            // 무릎 (Knee)
            Transform leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Transform rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            if (leftLowerLeg != null)
            {
                frame.leftKneePos = leftLowerLeg.position;
                frame.leftKneeRot = leftLowerLeg.eulerAngles;
            }
            if (rightLowerLeg != null)
            {
                frame.rightKneePos = rightLowerLeg.position;
                frame.rightKneeRot = rightLowerLeg.eulerAngles;
            }
            
            // 엉덩이 (Hip)
            Transform leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            if (leftUpperLeg != null)
            {
                frame.leftHipPos = leftUpperLeg.position;
                frame.leftHipRot = leftUpperLeg.eulerAngles;
            }
            if (rightUpperLeg != null)
            {
                frame.rightHipPos = rightUpperLeg.position;
                frame.rightHipRot = rightUpperLeg.eulerAngles;
            }
            
            // 골반 (Pelvis)
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null)
            {
                frame.pelvisPos = hips.position;
                frame.pelvisRot = hips.eulerAngles;
            }
        }
        
        // 파라미터 값들 - 리플렉션을 사용하여 FootGlideController의 필드 값 가져오기
        var footControllerType = typeof(FootGlideController);
        
        frame.spacingRatio = GetFieldValue<float>(footController, "spacingRatio");
        frame.depthRatio = GetFieldValue<float>(footController, "depthRatio");
        frame.bodyLeanAmount = GetFieldValue<float>(footController, "bodyLeanAmount");
        frame.adjustSpeed = GetFieldValue<float>(footController, "adjustSpeed");
        frame.glideAmount = GetFieldValue<float>(footController, "glideAmount");
        
        // 센서 값들
        frame.qx = GetFieldValue<float>(footController, "currentQx");
        frame.qy = GetFieldValue<float>(footController, "currentQy");
        frame.qz = GetFieldValue<float>(footController, "currentQz");
        frame.mValue = GetFieldValue<int>(footController, "lastMValue");
        frame.aValue = GetFieldValue<int>(footController, "currentAValue");
        frame.dValue = GetFieldValue<int>(footController, "currentDValue");
        
        // 애니메이션 상태
        if (animator != null)
        {
            var currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.IsName("walking")) frame.currentAnimationState = "walking";
            else if (currentState.IsName("RunningmanRecentSensing_jin")) frame.currentAnimationState = "runningman";
            else if (currentState.IsName("SpongebobRecentsension_jin")) frame.currentAnimationState = "spongebob";
            else if (currentState.IsName("Spin")) frame.currentAnimationState = "spin";
            else if (currentState.IsName("Jump")) frame.currentAnimationState = "jump";
            else frame.currentAnimationState = "unknown";
        }
        
        frame.isJumpInProgress = GetFieldValue<bool>(footController, "isJumpInProgress");
        
        frameDataList.Add(frame);
    }
    
    // 리플렉션을 사용하여 private 필드 값 가져오기
    // 리플렉션을 사용하여 public과 private 필드 값 모두 가져오기
    T GetFieldValue<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        
        // 필드를 찾지 못한 경우 디버그 로그 출력
        Debug.LogWarning($"필드 '{fieldName}'을 찾을 수 없습니다.");
        return default(T);
    }
    
    // 센서 값 변화 이벤트 핸들러들
    void OnMValueReceived(int value)
    {
        if (isTaskActive && value != lastMCommand)
        {
            CommandResponse response = new CommandResponse
            {
                timestamp = Time.time - experimentStartTime,
                taskID = currentTask.taskID,
                commandType = "M",
                commandValue = value,
                responseDelay = Time.time - lastMTime
            };
            commandResponseList.Add(response);
            
            lastMCommand = value;
            lastMTime = Time.time;
        }
    }
    
    void OnAValueReceived(int value)
    {
        if (isTaskActive && value != lastACommand)
        {
            CommandResponse response = new CommandResponse
            {
                timestamp = Time.time - experimentStartTime,
                taskID = currentTask.taskID,
                commandType = "A",
                commandValue = value,
                responseDelay = Time.time - lastATime
            };
            commandResponseList.Add(response);
            
            lastACommand = value;
            lastATime = Time.time;
        }
    }
    
    void OnDValueReceived(int value)
    {
        if (isTaskActive && value != lastDCommand)
        {
            CommandResponse response = new CommandResponse
            {
                timestamp = Time.time - experimentStartTime,
                taskID = currentTask.taskID,
                commandType = "D",
                commandValue = value,
                responseDelay = Time.time - lastDTime
            };
            commandResponseList.Add(response);
            
            lastDCommand = value;
            lastDTime = Time.time;
        }
    }
    
    void OnQxValueReceived(float value)
    {
        if (isTaskActive && Mathf.Abs(value - lastQxCommand) > 0.01f)
        {
            CommandResponse response = new CommandResponse
            {
                timestamp = Time.time - experimentStartTime,
                taskID = currentTask.taskID,
                commandType = "QX",
                commandValue = value,
                responseDelay = Time.time - lastQxTime
            };
            commandResponseList.Add(response);
            
            lastQxCommand = value;
            lastQxTime = Time.time;
        }
    }
    
    void OnQyValueReceived(float value)
    {
        if (isTaskActive && Mathf.Abs(value - lastQyCommand) > 0.01f)
        {
            CommandResponse response = new CommandResponse
            {
                timestamp = Time.time - experimentStartTime,
                taskID = currentTask.taskID,
                commandType = "QY",
                commandValue = value,
                responseDelay = Time.time - lastQyTime
            };
            commandResponseList.Add(response);
            
            lastQyCommand = value;
            lastQyTime = Time.time;
        }
    }
    
    void OnQzValueReceived(float value)
    {
        if (isTaskActive && Mathf.Abs(value - lastQzCommand) > 0.01f)
        {
            CommandResponse response = new CommandResponse
            {
                timestamp = Time.time - experimentStartTime,
                taskID = currentTask.taskID,
                commandType = "QZ",
                commandValue = value,
                responseDelay = Time.time - lastQzTime
            };
            commandResponseList.Add(response);
            
            lastQzCommand = value;
            lastQzTime = Time.time;
        }
    }
    
    void SaveTaskData()
    {
        string baseFileName = $"{participantID}_{currentTask.taskID:D3}";
        
        // 태스크별 프레임 데이터 필터링
        var taskFrameData = frameDataList.FindAll(f => f.taskID == currentTask.taskID);
        var taskEvents = taskEventList.FindAll(e => e.taskID == currentTask.taskID);
        var taskCommands = commandResponseList.FindAll(c => c.taskID == currentTask.taskID);
        
        // 프레임 데이터 저장
        SaveFrameDataToCSV(taskFrameData, $"{baseFileName}_FrameData.csv");
        
        // 이벤트 데이터 저장
        SaveTaskEventsToCSV(taskEvents, $"{baseFileName}_Events.csv");
        
        // 명령 반응 데이터 저장
        SaveCommandResponseToCSV(taskCommands, $"{baseFileName}_Commands.csv");
        
        Debug.Log($"태스크 {currentTask.taskID} 데이터 저장 완료: {baseFileName}");
    }
    
    void SaveAllExperimentData()
    {
        string baseFileName = $"{participantID}_All";
        
        // 전체 데이터 저장
        SaveFrameDataToCSV(frameDataList, $"{baseFileName}_FrameData.csv");
        SaveTaskEventsToCSV(taskEventList, $"{baseFileName}_Events.csv");
        SaveCommandResponseToCSV(commandResponseList, $"{baseFileName}_Commands.csv");
        
        // 요약 통계 저장
        SaveExperimentSummary($"{baseFileName}_Summary.csv");
        
        Debug.Log($"전체 실험 데이터 저장 완료: {baseFileName}");
    }
    
    void SaveFrameDataToCSV(List<FrameData> data, string fileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // 헤더 작성
            writer.WriteLine("Timestamp,TaskID," +
                           "LeftAnklePosX,LeftAnklePosY,LeftAnklePosZ,LeftAnkleRotX,LeftAnkleRotY,LeftAnkleRotZ," +
                           "RightAnklePosX,RightAnklePosY,RightAnklePosZ,RightAnkleRotX,RightAnkleRotY,RightAnkleRotZ," +
                           "LeftKneePosX,LeftKneePosY,LeftKneePosZ,LeftKneeRotX,LeftKneeRotY,LeftKneeRotZ," +
                           "RightKneePosX,RightKneePosY,RightKneePosZ,RightKneeRotX,RightKneeRotY,RightKneeRotZ," +
                           "LeftHipPosX,LeftHipPosY,LeftHipPosZ,LeftHipRotX,LeftHipRotY,LeftHipRotZ," +
                           "RightHipPosX,RightHipPosY,RightHipPosZ,RightHipRotX,RightHipRotY,RightHipRotZ," +
                           "PelvisPosX,PelvisPosY,PelvisPosZ,PelvisRotX,PelvisRotY,PelvisRotZ," +
                           "SpacingRatio,DepthRatio,BodyLeanAmount,AdjustSpeed,GlideAmount," +
                           "QX,QY,QZ,MValue,AValue,DValue," +
                           "AnimationState,IsJumpInProgress");
            
            // 데이터 작성
            foreach (var frame in data)
            {
                writer.WriteLine($"{frame.timestamp:F3},{frame.taskID}," +
                               $"{frame.leftAnklePos.x:F6},{frame.leftAnklePos.y:F6},{frame.leftAnklePos.z:F6}," +
                               $"{frame.leftAnkleRot.x:F3},{frame.leftAnkleRot.y:F3},{frame.leftAnkleRot.z:F3}," +
                               $"{frame.rightAnklePos.x:F6},{frame.rightAnklePos.y:F6},{frame.rightAnklePos.z:F6}," +
                               $"{frame.rightAnkleRot.x:F3},{frame.rightAnkleRot.y:F3},{frame.rightAnkleRot.z:F3}," +
                               $"{frame.leftKneePos.x:F6},{frame.leftKneePos.y:F6},{frame.leftKneePos.z:F6}," +
                               $"{frame.leftKneeRot.x:F3},{frame.leftKneeRot.y:F3},{frame.leftKneeRot.z:F3}," +
                               $"{frame.rightKneePos.x:F6},{frame.rightKneePos.y:F6},{frame.rightKneePos.z:F6}," +
                               $"{frame.rightKneeRot.x:F3},{frame.rightKneeRot.y:F3},{frame.rightKneeRot.z:F3}," +
                               $"{frame.leftHipPos.x:F6},{frame.leftHipPos.y:F6},{frame.leftHipPos.z:F6}," +
                               $"{frame.leftHipRot.x:F3},{frame.leftHipRot.y:F3},{frame.leftHipRot.z:F3}," +
                               $"{frame.rightHipPos.x:F6},{frame.rightHipPos.y:F6},{frame.rightHipPos.z:F6}," +
                               $"{frame.rightHipRot.x:F3},{frame.rightHipRot.y:F3},{frame.rightHipRot.z:F3}," +
                               $"{frame.pelvisPos.x:F6},{frame.pelvisPos.y:F6},{frame.pelvisPos.z:F6}," +
                               $"{frame.pelvisRot.x:F3},{frame.pelvisRot.y:F3},{frame.pelvisRot.z:F3}," +
                               $"{frame.spacingRatio:F6},{frame.depthRatio:F6},{frame.bodyLeanAmount:F6}," +
                               $"{frame.adjustSpeed:F6},{frame.glideAmount:F6}," +
                               $"{frame.qx:F6},{frame.qy:F6},{frame.qz:F6}," +
                               $"{frame.mValue},{frame.aValue},{frame.dValue}," +
                               $"{frame.currentAnimationState},{frame.isJumpInProgress}");
            }
        }
        
        Debug.Log($"프레임 데이터 저장: {filePath}");
    }
    
    void SaveTaskEventsToCSV(List<TaskEvent> events, string fileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("Timestamp,TaskID,EventType,TargetStyle");
            
            foreach (var evt in events)
            {
                writer.WriteLine($"{evt.timestamp:F3},{evt.taskID},{evt.eventType}," +
                               $"{evt.targetStyle}");
            }
        }
        
        Debug.Log($"이벤트 데이터 저장: {filePath}");
    }
    
    void SaveCommandResponseToCSV(List<CommandResponse> commands, string fileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("Timestamp,TaskID,CommandType,CommandValue,ResponseDelay");
            
            foreach (var cmd in commands)
            {
                writer.WriteLine($"{cmd.timestamp:F3},{cmd.taskID},{cmd.commandType}," +
                               $"{cmd.commandValue:F6},{cmd.responseDelay:F6}");
            }
        }
        
        Debug.Log($"명령 반응 데이터 저장: {filePath}");
    }
    
    void SaveExperimentSummary(string fileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("ExperimentSummary");
            writer.WriteLine($"ParticipantID,{participantID}");
            writer.WriteLine($"ExperimentStartTime,{realStartTime:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"TotalDuration,{Time.time - experimentStartTime:F2}");
            writer.WriteLine($"TotalTasks,{currentTaskIndex + 1}");
            writer.WriteLine($"TotalFrames,{frameDataList.Count}");
            writer.WriteLine($"TotalEvents,{taskEventList.Count}");
            writer.WriteLine($"TotalCommands,{commandResponseList.Count}");
            
            writer.WriteLine("\nTaskSummary");
            writer.WriteLine("TaskID,TargetStyle,FrameCount,Duration");
            
            foreach (var task in tasks.GetRange(0, Mathf.Min(currentTaskIndex + 1, tasks.Count)))
            {
                var taskFrames = frameDataList.FindAll(f => f.taskID == task.taskID);
                var taskEvents = taskEventList.FindAll(e => e.taskID == task.taskID);
                
                float duration = 0f;
                if (taskEvents.Count >= 2)
                {
                    var startEvent = taskEvents.Find(e => e.eventType == "START");
                    var endEvent = taskEvents.Find(e => e.eventType == "END");
                    if (startEvent.timestamp > 0 && endEvent.timestamp > 0)
                    {
                        duration = endEvent.timestamp - startEvent.timestamp;
                    }
                }
                
                writer.WriteLine($"{task.taskID},{task.targetStyle},{taskFrames.Count},{duration:F2}");
            }
        }
        
        Debug.Log($"실험 요약 저장: {filePath}");
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (dataTransBehavior != null)
        {
            DataTransBehavior.OnNewM -= OnMValueReceived;
            DataTransBehavior.OnNewA -= OnAValueReceived;
            DataTransBehavior.OnNewD -= OnDValueReceived;
            DataTransBehavior.OnNewQx -= OnQxValueReceived;
            DataTransBehavior.OnNewQy -= OnQyValueReceived;
            DataTransBehavior.OnNewQz -= OnQzValueReceived;
        }
    }

    void ResetTaskNumber()
    {
        if (isTaskActive)
        {
            Debug.LogWarning("현재 태스크가 진행 중입니다. 먼저 3키로 종료하세요.");
            return;
        }
        
        currentTaskIndex = -1;
        currentTask = null;
        
        Debug.Log("=== 태스크 번호 초기화 ===");
        Debug.Log("1키를 눌러 태스크 1부터 다시 시작할 수 있습니다.");
    }

    void IncrementTaskNumber()
    {
        if (isTaskActive)
        {
            Debug.LogWarning("현재 태스크가 진행 중입니다. 먼저 3키로 종료하세요.");
            return;
        }
        
        currentTaskIndex++;
        if (currentTaskIndex >= tasks.Count)
        {
            Debug.LogWarning("모든 태스크가 완료되었습니다. 5키를 눌러 태스크를 초기화하거나 3키로 실험을 종료하세요.");
            currentTaskIndex = tasks.Count - 1;
            return;
        }
        
        currentTask = tasks[currentTaskIndex];
        
        // 제어 모드 설정
        if (footController != null)
        {
            bool integratedMode = currentTask.controlType == "통합제어";
            // V키 상태를 프로그램matically 설정하는 것은 제한적이므로, 
            // 사용자에게 안내 메시지를 출력합니다.
            Debug.Log($"=== V키를 눌러 {currentTask.controlType} 모드로 전환하세요 ===");
        }
        
        Debug.Log($"=== 태스크 {currentTask.taskID} 선택됨 ===");
        Debug.Log($"타입: {currentTask.taskType}");
        Debug.Log($"제어: {currentTask.controlType}");
        Debug.Log($"목표: {currentTask.targetStyle}");
        Debug.Log("2키를 눌러 녹화를 시작하세요.");
    }
    
    void StartRecordingCurrentTask()
    {
        if (isTaskActive)
        {
            Debug.LogWarning("이미 태스크가 녹화 중입니다. 먼저 3키로 종료하세요.");
            return;
        }
        
        if (currentTaskIndex < 0 || currentTaskIndex >= tasks.Count)
        {
            Debug.LogWarning("선택된 태스크가 없습니다. 먼저 1키로 태스크를 선택하세요.");
            return;
        }
        
        isTaskActive = true;
        taskStartTime = Time.time;
        
        if (!isExperimentActive)
        {
            isExperimentActive = true;
            experimentStartTime = Time.time;
            realStartTime = DateTime.Now;
        }
        
        // 태스크 시작 이벤트 기록
        TaskEvent startEvent = new TaskEvent
        {
            timestamp = Time.time - experimentStartTime,
            taskID = currentTask.taskID,
            eventType = "START",
            taskType = currentTask.taskType,
            controlType = currentTask.controlType,
            targetStyle = currentTask.targetStyle
        };
        taskEventList.Add(startEvent);
        
        Debug.Log($"=== 태스크 {currentTask.taskID} 녹화 시작 ===");
    }
}