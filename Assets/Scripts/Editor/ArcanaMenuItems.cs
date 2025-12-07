#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Arcana.Editor
{
    /// <summary>
    /// Unity 에디터 메뉴에 Arcana 카테고리 추가
    /// </summary>
    public class ArcanaMenuItems
    {
        [MenuItem("Arcana/User Data Reset", false, 1)]
        public static void ResetUserData()
        {
            string persistentPath = Application.persistentDataPath;

            if (!Directory.Exists(persistentPath))
            {
                Debug.LogWarning($"PersistentDataPath does not exist: {persistentPath}");
                return;
            }

            // 확인 대화상자 표시
            bool confirmed = EditorUtility.DisplayDialog(
                "User Data Reset",
                $"Are you sure you want to delete all files in:\n{persistentPath}?",
                "Yes, Delete All",
                "Cancel"
            );

            if (!confirmed)
            {
                Debug.Log("User Data Reset cancelled by user.");
                return;
            }

            try
            {
                // 모든 파일과 디렉토리 삭제
                string[] files = Directory.GetFiles(persistentPath);
                string[] directories = Directory.GetDirectories(persistentPath);

                int fileCount = 0;
                int dirCount = 0;

                // 파일 삭제
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        fileCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to delete file {file}: {e.Message}");
                    }
                }

                // 디렉토리 삭제
                foreach (string directory in directories)
                {
                    try
                    {
                        Directory.Delete(directory, true);
                        dirCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to delete directory {directory}: {e.Message}");
                    }
                }

                Debug.Log($"User Data Reset completed. Deleted {fileCount} files and {dirCount} directories from {persistentPath}");
                EditorUtility.DisplayDialog(
                    "User Data Reset",
                    $"Successfully deleted {fileCount} files and {dirCount} directories.",
                    "OK"
                );
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to reset user data: {e.Message}");
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Failed to reset user data:\n{e.Message}",
                    "OK"
                );
            }
        }

        [MenuItem("Arcana/User Data Reset", true)]
        public static bool ValidateResetUserData()
        {
            // 메뉴 항목이 항상 활성화되도록 함
            return true;
        }
    }
}
#endif

