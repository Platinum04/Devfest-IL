using GDGIlorin.Quiz;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    [Header("Dependencies")]
    public QuizLoader quizLoader;

    [Header("Quiz UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI questionNumberText;
    public TextMeshProUGUI timerText;
    public Button[] optionButtons;
    public Button skipButton;

    [Header("Panels")]
    public GameObject splashPanel;
    public GameObject mainMenuPanel;
    public GameObject quizPanel;
    public GameObject resultPanel;
    public GameObject leaderboardPanel;

    [Header("Main Menu UI")]
    public TMP_InputField playerNameInput;
    public Button startButton;
    public Button viewLeaderboardButton;

    [Header("Result UI")]
    public TextMeshProUGUI resultText;
    public Button playAgainButton;
    public Button backToMenuButton;

    [Header("Leaderboard UI")]
    public TMP_Text leaderboardTitle;
    public TMP_Text leaderboardText;
    public Button leaderboardBackButton;

    private List<QuizQuestion> currentQuestions;
    private int currentQuestionIndex = 0;
    private int selectedAnswerIndex = -1;
    private int score = 0;
    private bool answerSubmitted = false;

    // Timer settings
    private float timePerQuestion = 15f;
    private float timeRemaining;
    private bool timerRunning = false;

    private List<LeaderboardEntry> leaderboard = new List<LeaderboardEntry>();
    private string leaderboardFilePath;

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string playerName;
        public int score;
    }

    void Start()
    {
        leaderboardFilePath = Path.Combine(Application.persistentDataPath, "leaderboard.json");
        LoadLeaderboard();

        // Default visibility
        splashPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        quizPanel.SetActive(false);
        resultPanel.SetActive(false);
        leaderboardPanel.SetActive(false);

        // Splash transition
        Invoke(nameof(ShowMainMenu), 5f);

        // Hook up buttons
        startButton.onClick.AddListener(OnStartQuizClicked);
        viewLeaderboardButton.onClick.AddListener(OnViewLeaderboardClicked);
        playAgainButton.onClick.AddListener(ReturnToMainMenu);
        backToMenuButton.onClick.AddListener(ReturnToMainMenu);
        leaderboardBackButton.onClick.AddListener(ReturnToMainMenu);
        skipButton.onClick.AddListener(SkipQuestion);
    }

    private void ShowMainMenu()
    {
        splashPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OnStartQuizClicked()
    {
        string playerName = playerNameInput.text.Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            playerNameInput.placeholder.GetComponent<TextMeshProUGUI>().text = "Please enter your name!";
            Debug.LogWarning("⚠️ Name required before starting quiz!");
            return;
        }

        mainMenuPanel.SetActive(false);
        StartQuiz();
    }

    public void OnViewLeaderboardClicked()
    {
        mainMenuPanel.SetActive(false);
        leaderboardPanel.SetActive(true);
        ShowLeaderboardPanel();
    }

    private void ReturnToMainMenu()
    {
        resultPanel.SetActive(false);
        leaderboardPanel.SetActive(false);
        quizPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void StartQuiz()
    {
        score = 0;
        currentQuestionIndex = 0;
        answerSubmitted = false;

        if (quizLoader.loadedQuestions == null || quizLoader.loadedQuestions.Count == 0)
        {
            Debug.LogError("❌ No questions loaded.");
            return;
        }

        currentQuestions = new List<QuizQuestion>(quizLoader.loadedQuestions);
        ShuffleList(currentQuestions);
        currentQuestions = currentQuestions.Take(10).ToList();

        quizPanel.SetActive(true);
        resultPanel.SetActive(false);
        leaderboardPanel.SetActive(false);

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        answerSubmitted = false;
        selectedAnswerIndex = -1;

        if (currentQuestionIndex >= currentQuestions.Count)
        {
            ShowResults();
            return;
        }

        QuizQuestion question = currentQuestions[currentQuestionIndex];
        questionText.text = question.question;
        questionNumberText.text = $"Question {currentQuestionIndex + 1} of {currentQuestions.Count}";

        // Reset timer
        timeRemaining = timePerQuestion;
        timerRunning = true;

        // Set options
        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            var label = btn.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            label.text = $"{(char)(65 + i)}. {question.options[i]}";
            btn.image.color = Color.white;

            int index = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectAnswer(index));
        }
    }

    void Update()
    {
        // Run timer if active
        if (timerRunning)
        {
            timeRemaining -= Time.deltaTime;
            if (timerText != null)
                timerText.text = $"Time Left: {Mathf.Ceil(timeRemaining)}s";

            if (timeRemaining <= 0f)
            {
                timerRunning = false;
                SkipQuestion();
            }
        }
    }

    private void SelectAnswer(int index)
    {
        if (answerSubmitted) return;

        answerSubmitted = true;
        timerRunning = false;

        bool isCorrect = index == currentQuestions[currentQuestionIndex].correctAnswerIndex;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i == currentQuestions[currentQuestionIndex].correctAnswerIndex)
                optionButtons[i].image.color = Color.green;
            else if (i == index)
                optionButtons[i].image.color = Color.red;
        }

        if (isCorrect) score++;
        Invoke(nameof(NextQuestion), 1.5f);
    }

    private void SkipQuestion()
    {
        if (answerSubmitted) return;

        answerSubmitted = true;
        timerRunning = false;

        Invoke(nameof(NextQuestion), 0.5f);
    }

    private void NextQuestion()
    {
        currentQuestionIndex++;
        if (currentQuestionIndex < currentQuestions.Count)
            ShowQuestion();
        else
            ShowResults();
    }

    private void ShowResults()
    {
        quizPanel.SetActive(false);
        resultPanel.SetActive(true);
        timerRunning = false;

        resultText.text = $"You scored {score} out of {currentQuestions.Count}!";

        string playerName = !string.IsNullOrEmpty(playerNameInput.text)
            ? playerNameInput.text
            : "Guest";

        AddToLeaderboard(playerName, score);
        SaveLeaderboard();
    }

    private void AddToLeaderboard(string playerName, int score)
    {
        leaderboard.Add(new LeaderboardEntry { playerName = playerName, score = score });
        leaderboard = leaderboard.OrderByDescending(entry => entry.score).Take(10).ToList();
    }

    private void ShowLeaderboardPanel()
    {
        leaderboardText.text = "";
        int rank = 1;
        foreach (var entry in leaderboard)
        {
            leaderboardText.text += $"{rank}. {entry.playerName} — {entry.score}\n";
            rank++;
        }

        leaderboardTitle.text = "🏆 Leaderboard 🏆";
        leaderboardPanel.SetActive(true);
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void SaveLeaderboard()
    {
        try
        {
            string json = JsonUtility.ToJson(new LeaderboardWrapper { entries = leaderboard }, true);
            File.WriteAllText(leaderboardFilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Failed to save leaderboard: " + e.Message);
        }
    }

    private void LoadLeaderboard()
    {
        if (File.Exists(leaderboardFilePath))
        {
            string json = File.ReadAllText(leaderboardFilePath);
            LeaderboardWrapper wrapper = JsonUtility.FromJson<LeaderboardWrapper>(json);
            if (wrapper != null && wrapper.entries != null)
                leaderboard = wrapper.entries;
        }
        else
        {
            leaderboard = new List<LeaderboardEntry>();
        }
    }

    [System.Serializable]
    private class LeaderboardWrapper
    {
        public List<LeaderboardEntry> entries;
    }
}
