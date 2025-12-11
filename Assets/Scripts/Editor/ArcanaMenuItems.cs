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
            // 확인 대화상자 표시
            bool confirmed = EditorUtility.DisplayDialog(
                "User Data Reset",
                "Are you sure you want to delete all user data?\n\nThis will delete:\n- PlayerPrefs (CharacterPool, tactics)\n- Resources/CharacterPool.json\n- Resources/tactics.json\n- All files in PersistentDataPath",
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
                int deletedCount = 0;

                // PlayerPrefs 삭제
                if (PlayerPrefs.HasKey("CharacterPool"))
                {
                    PlayerPrefs.DeleteKey("CharacterPool");
                    deletedCount++;
                    Debug.Log("Deleted PlayerPrefs: CharacterPool");
                }

                if (PlayerPrefs.HasKey("tactics"))
                {
                    PlayerPrefs.DeleteKey("tactics");
                    deletedCount++;
                    Debug.Log("Deleted PlayerPrefs: tactics");
                }

                PlayerPrefs.Save();

                // Resources/CharacterPool.json 삭제
                string resourcesPath = System.IO.Path.Combine(Application.dataPath, "Resources/CharacterPool.json");
                if (System.IO.File.Exists(resourcesPath))
                {
                    System.IO.File.Delete(resourcesPath);
                    deletedCount++;
                    Debug.Log("Deleted Resources/CharacterPool.json");
                }

                // Resources/tactics.json 삭제
                string tacticsPath = System.IO.Path.Combine(Application.dataPath, "Resources/tactics.json");
                if (System.IO.File.Exists(tacticsPath))
                {
                    System.IO.File.Delete(tacticsPath);
                    deletedCount++;
                    Debug.Log("Deleted Resources/tactics.json");
                }

                // PersistentDataPath의 모든 파일과 디렉토리 삭제
                string persistentPath = Application.persistentDataPath;
                if (Directory.Exists(persistentPath))
                {
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

                    deletedCount += fileCount + dirCount;
                    Debug.Log($"Deleted {fileCount} files and {dirCount} directories from {persistentPath}");
                }

                Debug.Log($"User Data Reset completed. Deleted {deletedCount} items total.");
                EditorUtility.DisplayDialog(
                    "User Data Reset",
                    $"Successfully deleted {deletedCount} items:\n- PlayerPrefs\n- Resources files\n- PersistentDataPath files",
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

