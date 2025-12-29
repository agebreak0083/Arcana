using UnityEngine;

/// <summary>
/// Web 빌드에서 Development Build가 아닐 때 Debug.Log를 필터링하는 커스텀 로그 핸들러
/// </summary>
public class FilteredLogHandler : ILogHandler
{
    private ILogHandler defaultLogHandler;

    public FilteredLogHandler(ILogHandler defaultLogHandler)
    {
        this.defaultLogHandler = defaultLogHandler;
    }

    public void LogFormat(LogType logType, Object context, string format, params object[] args)
    {
        #if UNITY_WEBGL && !DEVELOPMENT_BUILD && !UNITY_EDITOR
            // Web 빌드에서 Development Build가 아닐 때는 LogType.Log만 필터링
            // LogError, LogWarning, Exception은 유지
            if (logType == LogType.Log)
            {
                return; // Debug.Log는 출력하지 않음
            }
        #endif
        
        // 그 외의 경우는 기본 핸들러로 전달
        defaultLogHandler.LogFormat(logType, context, format, args);
    }

    public void LogException(System.Exception exception, Object context)
    {
        // Exception은 항상 출력
        defaultLogHandler.LogException(exception, context);
    }
}

/// <summary>
/// Web 빌드에서 Development Build가 아닐 때 Debug.Log를 필터링하는 초기화 스크립트
/// </summary>
public static class LoggingInitializer
{
    /// <summary>
    /// 게임 시작 시 자동으로 호출되어 로깅 설정을 초기화
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeLogging()
    {
        #if UNITY_WEBGL && !DEVELOPMENT_BUILD && !UNITY_EDITOR
            // Web 빌드에서 Development Build가 아닐 때는 커스텀 로그 핸들러 사용
            ILogHandler defaultHandler = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = new FilteredLogHandler(defaultHandler);
            // LogWarning으로 출력하여 필터링되지 않도록 함
            Debug.LogWarning("LoggingInitializer: Web 빌드에서 Debug.Log 필터링이 활성화되었습니다.");
        #else
            // Development Build나 Editor에서는 정상적으로 모든 로그 출력
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
        #endif
    }
}

