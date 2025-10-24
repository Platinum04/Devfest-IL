using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace GDGIlorin.Quiz
{
    [System.Serializable]
    public class QuizQuestion
    {
        public string question;
        public string[] options;
        public int correctAnswerIndex;
    }

    [System.Serializable]
    public class QuizData
    {
        public List<QuizQuestion> questions;
    }

    public class QuizLoader : MonoBehaviour
    {
        public List<QuizQuestion> loadedQuestions;

        // 👇 Event to notify when questions finish loading
        public event System.Action OnQuestionsLoaded;

        void Awake()
        {
            StartCoroutine(LoadQuestions());
        }

        private IEnumerator LoadQuestions()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "questions.json");
            string json = null;

#if UNITY_ANDROID && !UNITY_EDITOR
            // ✅ Works on Android/Quest builds
            UnityWebRequest request = UnityWebRequest.Get(path);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                json = request.downloadHandler.text;
            }
            else
            {
                Debug.LogError("❌ Failed to load JSON on device: " + request.error);
                yield break;
            }
#else
            // ✅ Works in Unity Editor / PC test
            if (File.Exists(path))
            {
                json = File.ReadAllText(path);
            }
            else
            {
                Debug.LogError("❌ questions.json not found in StreamingAssets!");
                yield break;
            }
#endif

            if (!string.IsNullOrEmpty(json))
            {
                QuizData data = JsonUtility.FromJson<QuizData>(json);
                if (data != null && data.questions != null)
                {
                    loadedQuestions = data.questions;
                    Debug.Log($"✅ Loaded {loadedQuestions.Count} quiz questions.");
                    OnQuestionsLoaded?.Invoke();
                }
                else
                {
                    Debug.LogError("⚠️ JSON parsed but no questions found.");
                }
            }
            else
            {
                Debug.LogError("⚠️ JSON string was empty.");
            }

            yield break; // ✅ Ensures coroutine always returns
        }
    }
}
