using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;

public class DataTransServer : MonoBehaviour
{
    public int port = 5678;
    private WebSocketServer wss;

    void Start()
    {   
        wss = new WebSocketServer(IPAddress.Any, port);
        wss.AddWebSocketService<WatchDataTransBehavior>("/watch");
        wss.AddWebSocketService<DOTDataTransBehavior>("/dot");
        
        // 기존 경로도 하위 호환성을 위해 유지 (선택사항)
        wss.AddWebSocketService<DataTransBehavior>("/");
        
        wss.Start();
        Debug.Log($"WebSocket 서버가 port {port}에서 /watch, /dot 경로로 시작되었습니다.");
    }

    void OnApplicationQuit()
    {
        if (wss != null)
        {
            wss.Stop();
        }
    }
    
    void Update()
    {
        // 모든 처리기의 메인 스레드 액션 실행
        DataTransBehavior.ExecuteMainThreadActions();
        WatchDataTransBehavior.ExecuteMainThreadActions();
        DOTDataTransBehavior.ExecuteMainThreadActions();
    }
    
    private string GetLocalIPAddress()
    {
        string localIP = "127.0.0.1";
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
        }
        catch (Exception e)
        {
            // Debug.LogError("Error getting local IP: " + e.Message);
        }
        return localIP;
    }
}


public class DataTransBehavior : WebSocketBehavior
{
    // 이벤트 정의는 그대로 유지
    public static event Action<float> OnNewGyroZ;
    public static event Action<float> OnNewGyroY;
    public static event Action<float> OnNewQuatX;
    public static event Action<int, int> OnNewHandGesture;
    public static event Action<float> OnNewYaw;
    public static event Action<float> OnNewPitch;
    public static event Action<float> OnNewRoll;   // 롤(Roll) - 필요시
    
    // qy, qz, qx 전용 이벤트 추가
    public static event Action<float> OnNewQy;
    public static event Action<float> OnNewQz;
    public static event Action<float> OnNewQx;
    
    // DOT 센서 가속도 이벤트 추가
    public static event Action<Vector3> OnNewAcceleration;  // 3축 가속도 통합
    public static event Action<float> OnNewAccelX;          // X축 가속도
    public static event Action<float> OnNewAccelY;          // Y축 가속도  
    public static event Action<float> OnNewAccelZ;          // Z축 가속도
    
    // m, d, a 값에 대한 이벤트 추가
    public static event Action<int> OnNewM;
    public static event Action<int> OnNewD;
    public static event Action<int> OnNewA;
    public static event Action<int> OnNewIS;  // IS 값 이벤트 추가
    
    // IB, FB 값에 대한 이벤트 추가
    public static event Action<int> OnNewIB;
    public static event Action<int> OnNewFB;
    
    // WebSocket 연결 상태 이벤트 추가
    public static event Action<bool> OnConnectionStateChanged;

    // 스레드 안전한 큐 추가
    public static ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    protected override void OnOpen()
    {
        Debug.Log("DataTransBehavior 클라이언트 연결됨!");
        mainThreadActions.Enqueue(() => OnConnectionStateChanged?.Invoke(true));
    }
    protected override void OnMessage(MessageEventArgs e)
    {
        string message = e.Data;
        
        // 모든 수신 데이터 로그 출력
        Debug.Log($"[WebSocket 수신] {message}");

        // JSON 데이터 처리 (파이썬 프로그램)
        if (message.StartsWith("{") && !message.Contains("\"type\":"))
        {
            Debug.Log($"[JSON 조건 만족] 메시지: {message}");
            mainThreadActions.Enqueue(() => {
                Debug.Log($"[JSON 데이터] {message}");
                
                // m, d, a, IB, FB 값 개별 파싱 및 표시
                try
                {
                    var mMatch = Regex.Match(message, "\"m\"\\s*:\\s*(\\d+)");
                    var dMatch = Regex.Match(message, "\"d\"\\s*:\\s*(\\d+)");
                    var aMatch = Regex.Match(message, "\"a\"\\s*:\\s*(\\d+)");
                    var ibMatch = Regex.Match(message, "\"IB\"\\s*:\\s*(\\d+)");
                    var fbMatch = Regex.Match(message, "\"FB\"\\s*:\\s*(\\d+)");
                    var isMatch = Regex.Match(message, "\"IS\"\\s*:\\s*(\\d+)");
                    var isMatchLower = Regex.Match(message, "\"is\"\\s*:\\s*(\\d+)");
                    
                    Debug.Log($"[정규식 테스트] IS 패턴: \"IS\"\\s*:\\s*(\\d+)");
                    Debug.Log($"[정규식 테스트] IS 매칭 결과: {isMatch.Success}, 값: {(isMatch.Success ? isMatch.Groups[1].Value : "N/A")}");
                    
                    if (mMatch.Success || dMatch.Success || aMatch.Success || ibMatch.Success || fbMatch.Success || isMatch.Success || isMatchLower.Success)
                    {
                        Debug.Log($"[JSON 파싱] m:{mMatch.Success}, d:{dMatch.Success}, a:{aMatch.Success}, IB:{ibMatch.Success}, FB:{fbMatch.Success}, IS:{isMatch.Success}, is:{isMatchLower.Success}");
                        string parsedValues = "파싱된 값들: ";
                        
                        if (mMatch.Success) 
                        {
                            int mValue = int.Parse(mMatch.Groups[1].Value);
                            parsedValues += $"m={mValue} ";
                            OnNewM?.Invoke(mValue);
                        }
                        if (dMatch.Success) 
                        {
                            int dValue = int.Parse(dMatch.Groups[1].Value);
                            parsedValues += $"d={dValue} ";
                            OnNewD?.Invoke(dValue);
                        }
                        if (aMatch.Success) 
                        {
                            int aValue = int.Parse(aMatch.Groups[1].Value);
                            parsedValues += $"a={aValue} ";
                            OnNewA?.Invoke(aValue);
                        }
                        if (ibMatch.Success) 
                        {
                            int ibValue = int.Parse(ibMatch.Groups[1].Value);
                            parsedValues += $"IB={ibValue} ";
                            OnNewIB?.Invoke(ibValue);
                        }
                        if (fbMatch.Success) 
                        {
                            int fbValue = int.Parse(fbMatch.Groups[1].Value);
                            parsedValues += $"FB={fbValue} ";
                            OnNewFB?.Invoke(fbValue);
                        }
                        if (isMatch.Success) 
                        {
                            int isValue = int.Parse(isMatch.Groups[1].Value);
                            parsedValues += $"IS={isValue} ";
                            OnNewIS?.Invoke(isValue);
                        }
                        if (isMatchLower.Success) 
                        {
                            int isValue = int.Parse(isMatchLower.Groups[1].Value);
                            parsedValues += $"is={isValue}";
                            OnNewIS?.Invoke(isValue);
                        }
                        
                        Debug.Log(parsedValues);
                    }
                    else
                    {
                        Debug.Log($"[JSON 파싱 실패] 모든 정규식 매칭 실패 - 메시지: {message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"JSON m,d,a,IB,FB 파싱 실패: {ex.Message}");
                }
            });
        }
        else if (message.StartsWith("{") && message.Contains("\"type\":\"dotSensorData\""))
        {
            try
            {
                // deviceId 추출
                var idMatch = Regex.Match(message, "\"deviceId\":\"(.*?)\"");
                string deviceId = idMatch.Success ? idMatch.Groups[1].Value : "";

                mainThreadActions.Enqueue(() => {
                    Debug.Log($"[DOT JSON 전체] DeviceID: {deviceId}, Data: {message}");
                });

                if (deviceId == "DOT1")
                {
                    // DOT1 → Roll만 처리
                    var rollMatch = Regex.Match(message, "\"r\":\"(-?\\d+\\.?\\d*)\"");
                    if (rollMatch.Success && float.TryParse(rollMatch.Groups[1].Value, out float rollValue))
                    {
                        mainThreadActions.Enqueue(() => {
                            OnNewRoll?.Invoke(rollValue);
                            Debug.Log($"DOT1 Roll: {rollValue}°");
                        });
                    }
                }
                else if (deviceId == "DOT2")
                {
                    // DOT2 → Pitch만 처리
                    var pitchMatch = Regex.Match(message, "\"p\":\"(-?\\d+\\.?\\d*)\"");
                    if (pitchMatch.Success && float.TryParse(pitchMatch.Groups[1].Value, out float pitchValue))
                    {
                        mainThreadActions.Enqueue(() => {
                            OnNewPitch?.Invoke(pitchValue);
                            Debug.Log($"DOT2 Pitch: {pitchValue}°");
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                mainThreadActions.Enqueue(() =>
                    Debug.LogError($"JSON DOT 파싱 실패: {ex.Message}, 데이터: {message}"));
            }
        }
        else if (message.StartsWith("DOT:"))
        {
            try {
                string data = message.Substring(4).Trim();
                
                mainThreadActions.Enqueue(() => {
                    //Debug.Log($"[DOT 텍스트] {data}");
                });

                // 가속도 값 파싱 (ax, ay, az)
                var axMatch = Regex.Match(data, @"ax:(-?\d+\.?\d*)");
                var ayMatch = Regex.Match(data, @"ay:(-?\d+\.?\d*)");
                var azMatch = Regex.Match(data, @"az:(-?\d+\.?\d*)");
                
                if (axMatch.Success && ayMatch.Success && azMatch.Success)
                {
                    if (float.TryParse(axMatch.Groups[1].Value, out float axValue) &&
                        float.TryParse(ayMatch.Groups[1].Value, out float ayValue) &&
                        float.TryParse(azMatch.Groups[1].Value, out float azValue))
                    {
                        Vector3 acceleration = new Vector3(axValue, ayValue, azValue);
                        
                        mainThreadActions.Enqueue(() => {
                            OnNewAcceleration?.Invoke(acceleration);
                            OnNewAccelX?.Invoke(axValue);
                            OnNewAccelY?.Invoke(ayValue);
                            OnNewAccelZ?.Invoke(azValue);
                            //Debug.Log($"[DOT 가속도] X:{axValue:F3}, Y:{ayValue:F3}, Z:{azValue:F3}");
                        });
                    }
                }
                
                // qX, qY, qZ 값 파싱 추가
                var qxMatch = Regex.Match(data, @"qx:(-?\d+\.?\d*)");
                if (qxMatch.Success && float.TryParse(qxMatch.Groups[1].Value, out float qxValue))
                {
                    mainThreadActions.Enqueue(() => {
                        OnNewQx?.Invoke(qxValue);
                        //Debug.Log($"[DOT Qx] {qxValue}");
                    });
                }
                
                var qyMatch = Regex.Match(data, @"qy:(-?\d+\.?\d*)");
                if (qyMatch.Success && float.TryParse(qyMatch.Groups[1].Value, out float qyValue))
                {
                    mainThreadActions.Enqueue(() => {
                        OnNewQy?.Invoke(qyValue);
                        //Debug.Log($"[DOT Qy] {qyValue}");
                    });
                }
                
                var qzMatch = Regex.Match(data, @"qz:(-?\d+\.?\d*)");
                if (qzMatch.Success && float.TryParse(qzMatch.Groups[1].Value, out float qzValue))
                {
                    mainThreadActions.Enqueue(() => {
                        OnNewQz?.Invoke(qzValue);
                        //Debug.Log($"[DOT Qz] {qzValue}");
                    });
                }

                // 기존 Roll 파싱 유지
                // var rollMatch = Regex.Match(data, @"r:(-?\d+\.?\d*)");
                // if (rollMatch.Success && float.TryParse(rollMatch.Groups[1].Value, out float rollValue))
                // {
                //     mainThreadActions.Enqueue(() => {
                //         OnNewRoll?.Invoke(rollValue);
                //         Debug.Log($"[DOT Roll] {rollValue}°");
                //     });
                // }
                
                // 기존 시간 파싱 유지
                var timeMatch = Regex.Match(data, @"t:(\d+\.?\d*)");
                if (timeMatch.Success)
                {
                    // 필요 시 시간 값 처리
                    // float timeValue = float.Parse(timeMatch.Groups[1].Value);
                }
            }
            catch (Exception ex)
            {
                mainThreadActions.Enqueue(() => 
                    Debug.LogError($"DOT 데이터 파싱 실패: {ex.Message}, 데이터: {message}"));
            }
        }
        // DataTransBehavior 클래스의 OnMessage 메소드 내 W: 처리 부분 수정
        else if (message.StartsWith("W:"))
        {
            try
            {
                string data = message.Substring(2).Trim(); // "W:" 제거
                
                mainThreadActions.Enqueue(() => {
                    Debug.Log($"[워치 데이터] {data}");
                });
                
                // "W:qx:-0.003,qy:0.034,qz:0.686" 형식에서 qx, qy, qz 값 추출
                var qxMatch = Regex.Match(data, @"qx:(-?\d+\.?\d*)");
                if (qxMatch.Success && float.TryParse(qxMatch.Groups[1].Value, out float qxValue))
                {
                    mainThreadActions.Enqueue(() => {
                        OnNewQx?.Invoke(qxValue);
                        Debug.Log($"[워치 Qx] {qxValue}");
                    });
                }
                
                var qyMatch = Regex.Match(data, @"qy:(-?\d+\.?\d*)");
                if (qyMatch.Success && float.TryParse(qyMatch.Groups[1].Value, out float qyValue))
                {
                    mainThreadActions.Enqueue(() => {
                        OnNewQy?.Invoke(qyValue);
                        Debug.Log($"[워치 Qy] {qyValue}");
                    });
                }
                
                var qzMatch = Regex.Match(data, @"qz:(-?\d+\.?\d*)");
                if (qzMatch.Success && float.TryParse(qzMatch.Groups[1].Value, out float qzValue))
                {
                    mainThreadActions.Enqueue(() => {
                        OnNewQz?.Invoke(qzValue);
                        Debug.Log($"[워치 Qz] {qzValue}");
                    });
                }
                
                // 기존 y, r, p 처리도 유지 (하위 호환성)
                var yawMatch = Regex.Match(data, @"y:(-?\d+\.?\d*)");
                if (yawMatch.Success && float.TryParse(yawMatch.Groups[1].Value, out float yawValue))
                {
                    mainThreadActions.Enqueue(() => {
                        OnNewYaw?.Invoke(yawValue);
                        Debug.Log($"[워치 Yaw] {yawValue}");
                    });
                }
                
                var rollMatch = Regex.Match(data, @"r:(-?\d+\.?\d*)");
                if (rollMatch.Success && float.TryParse(rollMatch.Groups[1].Value, out float rollValue))
                {
                    mainThreadActions.Enqueue(() => {
                        OnNewRoll?.Invoke(rollValue);
                        Debug.Log($"[워치 Roll] {rollValue}");
                    });
                }
                
                var pitchMatch = Regex.Match(data, @"p:(-?\d+\.?\d*)");
                if (pitchMatch.Success && float.TryParse(pitchMatch.Groups[1].Value, out float pitchValue))
                {
                    mainThreadActions.Enqueue(() => {
                        OnNewPitch?.Invoke(pitchValue);
                        Debug.Log($"[워치 Pitch] {pitchValue}");
                    });
                }
            }
            catch (Exception ex)
            {
                mainThreadActions.Enqueue(() => 
                    Debug.LogError($"WATCH 데이터 파싱 실패: {ex.Message}, 데이터: {message}"));
            }
        }
        else if (message.StartsWith("{") && message.Contains("\"type\":\"watch\""))
        {
            try
            {
                // Watch에서는 Yaw만 사용
                var yawMatch = Regex.Match(message, "\"y\":\"(-?\\d+\\.?\\d*)\"");
                if (yawMatch.Success && float.TryParse(yawMatch.Groups[1].Value, out float yawValue))
                {
                    mainThreadActions.Enqueue(() => {
                        OnNewYaw?.Invoke(yawValue);
                        Debug.Log($"[워치 JSON Yaw] {yawValue}°");
                    });
                }
                mainThreadActions.Enqueue(() => {
                    Debug.Log($"[워치 JSON 전체] {message}");
                });
            }
            catch (Exception ex)
            {
                mainThreadActions.Enqueue(() =>
                    Debug.LogError($"WATCH JSON 파싱 실패: {ex.Message}, 데이터: {message}"));
            }
        }
        else if (message.StartsWith("HAND:"))
        {
            try {
                string data = message.Substring(5).Trim();
                mainThreadActions.Enqueue(() => {
                    Debug.Log($"[핸드 제스처] {data}");
                });
                
                string[] parts = data.Split(',');
                if (parts.Length >= 2 && 
                    int.TryParse(parts[0], out int fingerCount) && 
                    int.TryParse(parts[1], out int palmOrientation))
                {
                    mainThreadActions.Enqueue(() => {
                        OnNewHandGesture?.Invoke(fingerCount, palmOrientation);
                        Debug.Log($"[핸드 제스처 파싱] 손가락: {fingerCount}, 손바닥: {palmOrientation}");
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"HAND 데이터 파싱 실패: {ex.Message}");
            }
        }
        else
        {
            mainThreadActions.Enqueue(() => {
                Debug.Log($"[처리되지 않은 메시지] {message}");
            });
        }
    }

    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log("DataTransBehavior 클라이언트 연결 해제됨!");
        mainThreadActions.Enqueue(() => OnConnectionStateChanged?.Invoke(false));
    }

    protected override void OnError(WebSocketSharp.ErrorEventArgs e)
    {
        Debug.LogError($"DataTransBehavior WebSocket 오류: {e.Message}");
        mainThreadActions.Enqueue(() => OnConnectionStateChanged?.Invoke(false));
    }

    public static void ExecuteMainThreadActions()
    {
        // -- 변경 전 --
//    int processCount = 0;
//    while (processCount < 10 && mainThreadActions.TryDequeue(out var action))
//    {
//        try { action?.Invoke(); }
//        catch (Exception ex) { Debug.LogError($"워치 액션 실행 중 오류: {ex.Message}"); }
//        processCount++;
//    }

    // -- 변경 후: 큐에 남은 모든 액션을 처리하도록 제한 해제 --
        while (mainThreadActions.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"워치 액션 실행 중 오류: {ex.Message}");
            }
        }
    }
}
// 워치 데이터 전용 처리 클래스
public class WatchDataTransBehavior : WebSocketBehavior
{
    // 요(Yaw) 전용 이벤트
    public static event Action<float> OnNewYaw;
    
    // 스레드 안전한 큐
    public static ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    
    protected override void OnOpen()
    {
        Debug.Log("워치 클라이언트 연결됨!");
    }
    
    protected override void OnMessage(MessageEventArgs e)
    {
        string message = e.Data;
        Debug.Log($"[WATCH JSON] {message}");
        
        // 메시지 디버깅 로그 추가
        // mainThreadActions.Enqueue(() => {
        //     Debug.Log($"워치 메시지 수신: {message}");
        // });
        
        if (message.StartsWith("W:"))
        {
            string data = message.Substring(2); // "W:" 제거
            var matches = Regex.Matches(data, @"t:([\d\.]+),y:([\d\.\-]+)");
            
            if (matches.Count > 0)
            {
                var match = matches[0];
                float yaw = float.Parse(match.Groups[2].Value);
                
                // UI 스레드에서 실행되도록 큐에 추가
                mainThreadActions.Enqueue(() => {
                    OnNewYaw?.Invoke(yaw);
                    Debug.Log($"워치 Yaw: {yaw}°");
                });
            }
        }
        else if (message == "WATCH_SESSION_START" || message == "WATCH_SESSION_END")
        {
            Debug.Log($"워치 세션 이벤트: {message}");
        }
    }
    
    // 메인 스레드 액션 실행 메소드
    public static void ExecuteMainThreadActions()
    {
        int processCount = 0;
        while (processCount < 10 && mainThreadActions.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"워치 액션 실행 중 오류: {ex.Message}");
            }
            processCount++;
        }
    }
}

// DOT 데이터 전용 처리 클래스
public class DOTDataTransBehavior : WebSocketBehavior
{
    // 롤(Roll) 전용 이벤트
    public static event Action<float> OnNewRoll;
    
    // 스레드 안전한 큐
    public static ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    
    protected override void OnOpen()
    {
        Debug.Log("DOT 클라이언트 연결됨!");
    }
    
    protected override void OnMessage(MessageEventArgs e)
    {
        string message = e.Data;
        Debug.Log($"[WS Receive DOT] {message}");
        
        // JSON 형식 처리 (새로 추가)
        if (message.StartsWith("{") && message.Contains("\"type\":\"dotSensorData\""))
        {
            try
            {
                mainThreadActions.Enqueue(() => {
                    Debug.Log($"DOT 데이터 전체: {message}");
                });
                
                // Roll 값 추출
                var rollMatch = Regex.Match(message, "\"r\":(-?\\d+\\.?\\d*)");
                if (rollMatch.Success && float.TryParse(rollMatch.Groups[1].Value, out float rollValue))
                {
                    mainThreadActions.Enqueue(() => {
                        OnNewRoll?.Invoke(rollValue);
                        Debug.Log($"DOT Roll: {rollValue}°");
                    });
                }
                
                // Yaw와 Pitch도 추출
                var yawMatch = Regex.Match(message, "\"y\":(-?\\d+\\.?\\d*)");
                if (yawMatch.Success && float.TryParse(yawMatch.Groups[1].Value, out float yawValue))
                {
                    mainThreadActions.Enqueue(() => {
                        Debug.Log($"DOT Yaw: {yawValue}°");
                    });
                }
                
                var pitchMatch = Regex.Match(message, "\"p\":(-?\\d+\\.?\\d*)");
                if (pitchMatch.Success && float.TryParse(pitchMatch.Groups[1].Value, out float pitchValue))
                {
                    mainThreadActions.Enqueue(() => {
                        Debug.Log($"DOT Pitch: {pitchValue}°");
                    });
                }
            }
            catch (Exception ex)
            {
                mainThreadActions.Enqueue(() => 
                    Debug.LogError($"JSON DOT 데이터 파싱 실패: {ex.Message}, 데이터: {message}"));
            }
        }
        // 기존 "DOT:" 처리 코드 유지
        else if (message.StartsWith("DOT:"))
        {
            // 기존 코드
        }
        else if (message == "DOT_SESSION_START" || message == "DOT_SESSION_END")
        {
            Debug.Log($"DOT 세션 이벤트: {message}");
        }
    
    }
    
    // 메인 스레드 액션 실행 메소드
    public static void ExecuteMainThreadActions()
    {
        int processCount = 0;
        while (processCount < 10 && mainThreadActions.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"DOT 액션 실행 중 오류: {ex.Message}");
            }
            processCount++;
        }
    }
}
