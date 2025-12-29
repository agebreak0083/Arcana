using UnityEngine;

/// <summary>
/// Web 빌드에서 Development Build가 아닐 때 Debug.Log를 필터링하는 유틸리티 클래스
/// </summary>
public static class GameLogger
{
    /// <summary>
    /// Web 빌드에서 Development Build가 아닐 때는 로그를 출력하지 않음
    /// </summary>
    private static bool ShouldLog()
    {
        #if UNITY_WEBGL && !DEVELOPMENT_BUILD && !UNITY_EDITOR
            return false;
        #else
            return true;
        #endif
    }

    /// <summary>
    /// 일반 로그 출력 (Debug.Log 대체)
    /// </summary>
    public static void Log(object message)
    {
        if (ShouldLog())
        {
            Debug.Log(message);
        }
    }

    /// <summary>
    /// 일반 로그 출력 (컨텍스트 포함)
    /// </summary>
    public static void Log(object message, Object context)
    {
        if (ShouldLog())
        {
            Debug.Log(message, context);
        }
    }

    /// <summary>
    /// 경고 로그 출력 (항상 출력)
    /// </summary>
    public static void LogWarning(object message)
    {
        if (ShouldLog())
        {
            Debug.LogWarning(message);
        }
    }

    /// <summary>
    /// 경고 로그 출력 (컨텍스트 포함)
    /// </summary>
    public static void LogWarning(object message, Object context)
    {
        if (ShouldLog())
        {
            Debug.LogWarning(message, context);
        }
    }

    /// <summary>
    /// 에러 로그 출력 (항상 출력)
    /// </summary>
    public static void LogError(object message)
    {
        // 에러는 항상 출력
        Debug.LogError(message);
    }

    /// <summary>
    /// 에러 로그 출력 (컨텍스트 포함)
    /// </summary>
    public static void LogError(object message, Object context)
    {
        // 에러는 항상 출력
        Debug.LogError(message, context);
    }
}

