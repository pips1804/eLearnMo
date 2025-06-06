using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections; 

[System.Serializable]
public class Question
{
    public string questionText;
    public string[] answers;
    public int correctAnswerIndex;
    public string[] hints;
}

public class QuizHandler : MonoBehaviour
{
    public Text questionText;
    public Button[] answerButtons;
    public Text[] answerTexts;

    public Button nextButton;
    public Button backButton;

    public Color correctColor;
    public Color wrongColor;
    public Color defaultColor;

    public List<Question> questions;
    private int currentQuestionIndex = 0;
    private bool hasAnswered = false;

    public Text scoreText;
    public Slider progressBar;
    public Text questionNumberText;

    public Text hintDisplayText;
    public Button hintButton;
    private int hintIndex = 0;

    private List<int> selectedAnswers = new List<int>(); // -1 if unanswered

    public GameObject hintBubble; // Assign in inspector
    public Text hintBubbleText; // Assign in inspector

    public Text timerText; // Assign your Timer UI Text in the Inspector

    private float currentTime;
    private bool isTimerRunning = false;
    private List<int> twentySecondQuestions = new List<int>();
    private List<bool> questionExpired;

    int consecutiveWrongAnswers = 0; // Track consecutive wrongs

    private Coroutine hideBubbleCoroutine;

    public GameObject resultModal; // The modal panel
    public Text resultHeaderText;  // The "Congratulations" header text
    public Text resultScoreText;   // The "Your score is X/Y" text

    public Slider playerHealthSlider;
    public Slider enemyHealthSlider;

    public int playerHealth = 100;
    public int enemyHealth = 100;


    void Start()
    {

        playerHealthSlider.maxValue = playerHealth;
        playerHealthSlider.value = playerHealth;

        enemyHealthSlider.maxValue = enemyHealth;
        enemyHealthSlider.value = enemyHealth;

        if (hintButton != null)
            hintButton.onClick.AddListener(UseHint);

        // Initialize all answers as unanswered (-1)
        for (int i = 0; i < questions.Count; i++)
            selectedAnswers.Add(-1);

        questionExpired = new List<bool>(new bool[questions.Count]);

        LoadQuestion();

        progressBar.minValue = 0;
        progressBar.maxValue = questions.Count;

        PickRandomTwentySecondQuestions();


    }

    void Update()
    {
        if (isTimerRunning)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0f)
            {
                currentTime = 0f;
                isTimerRunning = false;
                TimeOut();
            }

            UpdateTimerDisplay();
        }
    }

    void LoadQuestion()
    {
        hintIndex = 0;
        hintDisplayText.text = "";

        ResetButtonColors();

        Question q = questions[currentQuestionIndex];
        questionText.text = q.questionText;
        questionNumberText.text = (currentQuestionIndex + 1).ToString();

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerTexts[i].text = q.answers[i];
            int index = i;
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => CheckAnswer(index));
        }

        int answeredIndex = selectedAnswers[currentQuestionIndex];
        hasAnswered = answeredIndex != -1 || questionExpired[currentQuestionIndex];

        if (hasAnswered)
        {
            // Show correct/previous answer
            ShowPreviousAnswer();
            isTimerRunning = false; // Stop timer on revisit
            currentTime = 0f;
            UpdateTimerDisplay();

            EnableButtons(false); // Disable answer buttons
            nextButton.interactable = currentQuestionIndex < questions.Count - 1;
        }
        else
        {
            EnableButtons(true);
            StartQuestionTimer(); // Start timer ONLY for fresh question
            nextButton.interactable = false;
        }

        backButton.interactable = currentQuestionIndex > 0;

        progressBar.value = currentQuestionIndex + 1;
        UpdateScoreDisplay();
        UpdateProgressBar();
    }




    void ShowPreviousAnswer()
    {
        int correct = questions[currentQuestionIndex].correctAnswerIndex;
        int selected = selectedAnswers[currentQuestionIndex];

        for (int i = 0; i < answerButtons.Length; i++)
        {
            Image btnImage = answerButtons[i].GetComponent<Image>();

            if (i == correct)
            {
                btnImage.color = correctColor;
            }
            else if (i == selected && selected != -1)
            {
                btnImage.color = wrongColor;
            }
            else
            {
                btnImage.color = defaultColor;
            }

            answerButtons[i].interactable = false;
        }

        nextButton.interactable = true;
    }


    void OnTimeExpired()
    {
        // If already answered, do nothing
        if (hasAnswered) return;

        // Mark question as expired
        questionExpired[currentQuestionIndex] = true;

        // Auto-show correct answer
        ShowCorrectAnswer();
    }

    void ShowCorrectAnswer()
    {
        hasAnswered = true;

        int correct = questions[currentQuestionIndex].correctAnswerIndex;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].interactable = false;
            answerButtons[i].GetComponent<Image>().color =
                (i == correct) ? correctColor : wrongColor;
        }

        nextButton.interactable = true;

        UpdateScoreDisplay();
    }



    void CheckAnswer(int index)
    {
        if (hasAnswered) return;

        hasAnswered = true;
        isTimerRunning = false; // Stop timer
        selectedAnswers[currentQuestionIndex] = index;

        int correct = questions[currentQuestionIndex].correctAnswerIndex;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].interactable = false;
            answerButtons[i].GetComponent<Image>().color =
                (i == correct) ? correctColor : wrongColor;
        }

        if (index == correct)
        {
            // Correct answer: damage the enemy
            int damage = 10;
            enemyHealth -= damage;
            enemyHealth = Mathf.Clamp(enemyHealth, 0, 100);
            enemyHealthSlider.value = enemyHealth;
            Debug.Log($"Correct! Enemy took {damage} damage. Remaining: {enemyHealth}");

            if (enemyHealth <= 0)
            {
                Debug.Log("Enemy defeated!");
            }

            consecutiveWrongAnswers = 0; // Reset on correct answer
        }
        else
        {
            consecutiveWrongAnswers++; // Increase consecutive count

            if (PlayerStats.Instance != null)
            {
                float totalHealth = 100;
                int damage = Mathf.RoundToInt(totalHealth * (0.05f * consecutiveWrongAnswers));

                playerHealth -= damage;
                playerHealth = Mathf.Clamp(playerHealth, 0, 100);
                playerHealthSlider.value = playerHealth;

                Debug.Log($"Wrong! Player took {damage} damage. Remaining: {playerHealth}");

                if (playerHealth <= 0)
                {
                    Debug.Log("Player died!");
                }

                bool isDead = PlayerStats.Instance.DamagePet(damage);

                if (isDead)
                {
                    // Set dead state true in Animator
                    PlayerStats.Instance.ShowDamageText(damage);
                    PetAnimationController.Instance.HurtPet(true);
                }
                else
                {
                    PlayerStats.Instance.ShowDamageText(damage);
                    PetAnimationController.Instance.HurtPet();
                }
            }
        }

        UpdateScoreDisplay();
        nextButton.interactable = true;
    }


    void EnableButtons(bool state)
    {
        foreach (Button btn in answerButtons)
            btn.interactable = state;
    }

    public void NextQuestion()
    {
        if (currentQuestionIndex < questions.Count - 1)
        {
            currentQuestionIndex++;
            LoadQuestion();
        }
        else
        {
            ShowResultModal(); // Show modal when quiz is done
        }
    }

    public void PreviousQuestion()
    {
        if (currentQuestionIndex > 0)
        {
            currentQuestionIndex--;
            LoadQuestion();
        }
    }

    void ResetButtonColors()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].GetComponent<Image>().color = defaultColor;
        }
    }

    void UpdateScoreDisplay()
    {
        int score = 0;
        for (int i = 0; i < selectedAnswers.Count; i++)
        {
            if (selectedAnswers[i] == questions[i].correctAnswerIndex)
                score++;
        }

        scoreText.text =  score.ToString();
    }


    void UpdateProgressBar()
    {
        progressBar.value = currentQuestionIndex + 1;
    }

    public void UseHint()
    {
        if (hasAnswered) return;

        Question q = questions[currentQuestionIndex];

        if (q.hints != null && hintIndex < q.hints.Length)
        {
            string hintMsg = q.hints[hintIndex];
            hintBubbleText.text = hintMsg;
            hintBubble.SetActive(true); // Show bubble

            hintIndex++;
        }
        else
        {
            hintBubbleText.text = "No more hints!";
            hintBubble.SetActive(true);
        }

        // Start the auto-hide countdown
        if (hideBubbleCoroutine != null)
            StopCoroutine(hideBubbleCoroutine); // Reset timer if pressed again

        hideBubbleCoroutine = StartCoroutine(HideHintBubbleAfterDelay(5f)); // 15 seconds
    }

    private IEnumerator HideHintBubbleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        hintBubble.SetActive(false);
    }

    void StartQuestionTimer()
    {
        // If current question is in the 20s list - 20 sec, else 30 sec
        if (twentySecondQuestions.Contains(currentQuestionIndex))
            currentTime = 20f;
        else
            currentTime = 30f;

        isTimerRunning = true;
        UpdateTimerDisplay();
    }


    void UpdateTimerDisplay()
    {
        int seconds = Mathf.CeilToInt(currentTime);
        timerText.text = seconds.ToString();

        // Turn red if 5 seconds or less
        if (seconds <= 5)
            timerText.color = Color.red;
        else
            timerText.color = Color.white; // Default color
    }


    void TimeOut()
    {
        if (hasAnswered) return;

        hasAnswered = true;

        // Mark question as expired
        questionExpired[currentQuestionIndex] = true;

        // Mark question as unanswered (-1)
        selectedAnswers[currentQuestionIndex] = -1;

        // Show correct answer automatically
        ShowCorrectAnswer();

        isTimerRunning = false;
    }

    void PickRandomTwentySecondQuestions()
    {
        List<int> indexes = new List<int>();
        for (int i = 0; i < questions.Count; i++)
            indexes.Add(i);

        // Shuffle the list
        for (int i = 0; i < indexes.Count; i++)
        {
            int temp = indexes[i];
            int randomIndex = Random.Range(i, indexes.Count);
            indexes[i] = indexes[randomIndex];
            indexes[randomIndex] = temp;
        }

        // Take first 3 random indexes
        twentySecondQuestions.Clear();
        for (int i = 0; i < 3 && i < indexes.Count; i++)
        {
            twentySecondQuestions.Add(indexes[i]);
        }
    }

    void ShowResultModal()
    {
        int totalQuestions = questions.Count;
        int correctAnswers = 0;

        for (int i = 0; i < selectedAnswers.Count; i++)
        {
            if (selectedAnswers[i] == questions[i].correctAnswerIndex)
                correctAnswers++;
        }

        // Set the header based on performance
        float percentage = (float)correctAnswers / totalQuestions;

        if (percentage == 1f)
        {
            resultHeaderText.text = "Perfect Score! ";
        }
        else if (percentage >= 0.7f)
        {
            resultHeaderText.text = "Congratulations! ";
        }
        else if (percentage >= 0.4f)
        {
            resultHeaderText.text = "Good Try! Keep Practicing!";
        }
        else
        {
            resultHeaderText.text = "Don't Give Up! Try Again!";
        }

        // Display score text
        resultScoreText.text = "Your Score: " + "\n" + correctAnswers + " / " + totalQuestions;

        // Show the modal
        resultModal.SetActive(true);
    }

}
