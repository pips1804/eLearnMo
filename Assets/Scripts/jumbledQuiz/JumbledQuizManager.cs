using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class JumbledQuizManager : MonoBehaviour
{
    [System.Serializable]
    public class JumbledQuestion
    {
        public string question;
        public string answer;
        public string explanationText;  // Add this for feedback, or you can modify accordingly
    }

    public List<JumbledQuestion> questions;

    // UI references for jumbled quiz
    public Text questionText;
    public GameObject letterButtonPrefab;
    public Transform letterContainer;
    public Text timerText;

    public GameObject resultPanel;
    public Button mainMenuButton;

    public RectTransform playerIcon;
    public RectTransform enemyIcon;

    private Vector3 playerStartPos;
    private Vector3 enemyStartPos;

    public BattleManager battleManager;

    private int currentQuestionIndex = 0;
    private float timer = 20f;  // Jumbled quiz timer
    private bool isTimerRunning = false;

    public Slider progressBar;
    private float targetProgress = 0f;
    public RectTransform progressHandle;

    public Text scoreText;
    private int score = 0;

    public Text finalScoreText;
    public Text scoreMessageText;

    public float hitChancePercent = 50f; // adjustable

    public Text missText;
    public Text damageText;

    public GameObject feedbackPanel;
    public Text feedbackText;
    private bool canAnswer = true;

    private List<Button> letterButtons = new List<Button>();
    private int selectedIndex = -1;

    private Color defaultColor;
    private Color selectedColor;

    private void Awake()
    {
        ColorUtility.TryParseHtmlString("#116530", out defaultColor);
        ColorUtility.TryParseHtmlString("#E8E8CC", out selectedColor);  

    }

    void Start()
    {
        playerStartPos = playerIcon.anchoredPosition;
        enemyStartPos = enemyIcon.anchoredPosition;

        if (progressBar != null)
        {
            progressBar.minValue = 0;
            progressBar.maxValue = questions.Count;
            progressBar.value = 0;
        }

        DisplayQuestion();
        UpdateScoreText();
    }

    void Update()
    {
        if (isTimerRunning)
        {
            timer -= Time.deltaTime;
            timerText.text = Mathf.CeilToInt(timer).ToString();

            if (timer <= 0)
            {
                isTimerRunning = false;
                timerText.text = "0";
                PlayerMissedAnswer();
            }
        }
    }

    void DisplayQuestion()
    {
        ClearLetters();
        var question = questions[currentQuestionIndex];
        questionText.text = question.question;

        string shuffled = new string(question.answer.ToCharArray().OrderBy(c => Random.value).ToArray());

        // Debug log the shuffled letters
        Debug.Log($"Shuffled letters: {shuffled}");

        for (int i = 0; i < shuffled.Length; i++)
        {
            GameObject letterObj = Instantiate(letterButtonPrefab, letterContainer);
            Button letterButton = letterObj.GetComponent<Button>();
            int index = i;
            letterButton.GetComponentInChildren<Text>().text = shuffled[i].ToString();
            letterButton.onClick.AddListener(() => OnLetterClick(index));
            letterButtons.Add(letterButton);
        }

        timer = 30f;
        isTimerRunning = true;
        canAnswer = true;
        selectedIndex = -1;
    }

    void OnLetterClick(int index)
    {
        if (!canAnswer) return;
        if (index < 0 || index >= letterButtons.Count)
            return;

        if (selectedIndex == -1)
        {
            selectedIndex = index;
            letterButtons[index].GetComponentInChildren<Text>().color = selectedColor;
        }
        else if (selectedIndex != index)
        {
            // Start the animation coroutine instead of instantly swapping
            StartCoroutine(AnimateSwap(selectedIndex, index));
        }
        else
        {
            letterButtons[selectedIndex].GetComponentInChildren<Text>().color = defaultColor;
            selectedIndex = -1;
        }
    }

    IEnumerator AnimateSwap(int firstIndex, int secondIndex)
    {
        Button firstButton = letterButtons[firstIndex];
        Button secondButton = letterButtons[secondIndex];

        RectTransform firstRect = firstButton.GetComponent<RectTransform>();
        RectTransform secondRect = secondButton.GetComponent<RectTransform>();

        Vector3 firstStartPos = firstRect.localPosition;
        Vector3 secondStartPos = secondRect.localPosition;

        float duration = 0.3f;
        float elapsed = 0f;

        // Highlight
        firstButton.GetComponentInChildren<Text>().color = selectedColor;
        secondButton.GetComponentInChildren<Text>().color = selectedColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            firstRect.localPosition = Vector3.Lerp(firstStartPos, secondStartPos, t);
            secondRect.localPosition = Vector3.Lerp(secondStartPos, firstStartPos, t);

            yield return null;
        }

        // Swap the text content
        string temp = firstButton.GetComponentInChildren<Text>().text;
        firstButton.GetComponentInChildren<Text>().text = secondButton.GetComponentInChildren<Text>().text;
        secondButton.GetComponentInChildren<Text>().text = temp;

        // Return buttons to original position
        firstRect.localPosition = firstStartPos;
        secondRect.localPosition = secondStartPos;

        // Reset colors
        firstButton.GetComponentInChildren<Text>().color = defaultColor;
        secondButton.GetComponentInChildren<Text>().color = defaultColor;

        selectedIndex = -1;
    }



    public void OnSubmitButton()
    {
        if (!canAnswer) return;
        isTimerRunning = false;
        SubmitAnswer();
    }

    void SubmitAnswer()
    {
        if (!canAnswer) return;
        canAnswer = false;

        var currentQ = questions[currentQuestionIndex];
        string userAnswer = string.Concat(letterButtons.Select(b => b.GetComponentInChildren<Text>().text.Trim()).Where(t => !string.IsNullOrEmpty(t)));
        string correctAnswer = questions[currentQuestionIndex].answer.Trim();
        bool isCorrect = string.Equals(userAnswer, correctAnswer, System.StringComparison.OrdinalIgnoreCase);



        if (isCorrect)
        {
            bool isHit = Random.value <= (hitChancePercent * 0.01f);

            Debug.Log($"User Answer: {userAnswer}");
            Debug.Log($"Correct Answer: {correctAnswer}");

            if (isHit)
            {
                ShowFeedback("Correct!", currentQ.explanationText);
                int damage = Random.Range(10, 16);
                battleManager.EnemyTakeDamage(damage);
                StartCoroutine(AttackAnimation(playerIcon, playerStartPos, new Vector3(250, 0, 0)));
                StartCoroutine(HitShake(enemyIcon));
                Color damageColor = new Color(1f, 0f, 0f); // Red
                StartCoroutine(ShowFloatingText(damageText, "-" + damage, enemyIcon.position, damageColor));
            }
            else
            {
                ShowFeedback("Correct!", currentQ.explanationText);
                StartCoroutine(AttackAnimation(playerIcon, playerStartPos, new Vector3(250, 0, 0)));
                StartCoroutine(DodgeAnimation(enemyIcon));
                Color missColor;
                ColorUtility.TryParseHtmlString("#5f9103", out missColor);
                StartCoroutine(ShowFloatingText(missText, "Miss!", enemyIcon.position, missColor));

            }

            score++;
        }
        else
        {
            bool isHit = Random.value <= (hitChancePercent * 0.01f);

            Debug.Log($"User Answer: {userAnswer}");
            Debug.Log($"Correct Answer: {correctAnswer}");

            if (isHit)
            {
                ShowFeedback("Wrong!", currentQ.explanationText);
                int damage = Random.Range(10, 16);
                battleManager.PlayerTakeDamage(damage);
                StartCoroutine(AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0)));
                StartCoroutine(HitShake(playerIcon));
                Color damageColor = new Color(1f, 0f, 0f); // Red
                StartCoroutine(ShowFloatingText(damageText, "-" + damage, playerIcon.position, damageColor));

            }
            else
            {
                ShowFeedback("Wrong!", currentQ.explanationText);
                StartCoroutine(AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0)));
                StartCoroutine(DodgeAnimation(playerIcon));
                Color missColor;
                ColorUtility.TryParseHtmlString("#5f9103", out missColor);
                StartCoroutine(ShowFloatingText(missText, "Miss!", playerIcon.position, missColor));
            }
        }

        UpdateScoreText();
    }

    void PlayerMissedAnswer()
    {
        canAnswer = false;
        int damage = Random.Range(10, 16);
        battleManager.PlayerTakeDamage(damage);
        StartCoroutine(AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0)));
        StartCoroutine(HitShake(playerIcon));
        Color damageColor = new Color(1f, 0f, 0f); // Red
        StartCoroutine(ShowFloatingText(damageText, "-" + damage, playerIcon.position, damageColor));
        ShowFeedback("Time's Up!", "You didn't answer in time.");
        StartCoroutine(WaitThenNextQuestion());
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    IEnumerator AnimateProgressBar()
    {
        float startValue = progressBar.value;
        float duration = 0.3f;
        float elapsed = 0f;

        // Scale effect variables
        Vector3 originalScale = progressHandle.localScale;
        Vector3 zoomedScale = originalScale * 1.3f; // adjust zoom level

        // Zoom in
        if (progressHandle != null)
            progressHandle.localScale = zoomedScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            progressBar.value = Mathf.Lerp(startValue, targetProgress, t);
            yield return null;
        }

        progressBar.value = targetProgress;

        // Optional delay before zoom out
        yield return new WaitForSeconds(0.05f);

        // Zoom out smoothly
        float shrinkTime = 0.2f;
        float shrinkElapsed = 0f;

        while (shrinkElapsed < shrinkTime)
        {
            shrinkElapsed += Time.deltaTime;
            float t = shrinkElapsed / shrinkTime;
            if (progressHandle != null)
                progressHandle.localScale = Vector3.Lerp(zoomedScale, originalScale, t);
            yield return null;
        }

        // Ensure final scale is exact
        if (progressHandle != null)
            progressHandle.localScale = originalScale;
    }

    IEnumerator DodgeAnimation(RectTransform defender)
    {
        Vector3 originalPos = defender.anchoredPosition;
        Vector3 dodgeOffset = new Vector3(80f, 0f, 0f);
        float dodgeTime = 0.35f;
        float elapsed = 0f;

        while (elapsed < dodgeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin((elapsed / dodgeTime) * Mathf.PI);
            defender.anchoredPosition = originalPos + dodgeOffset * t;
            yield return null;
        }

        yield return new WaitForSeconds(0.05f);
        defender.anchoredPosition = originalPos;
    }

    IEnumerator AttackAnimation(RectTransform attacker, Vector3 originalPos, Vector3 attackOffset)
    {
        Vector3 targetPos = originalPos + attackOffset;
        float duration = 0.35f;
        float elapsed = 0f;

        float tiltAngle = 25f;
        Quaternion startRotation = attacker.rotation;
        Quaternion tiltRotation = Quaternion.Euler(0, 0, attacker == playerIcon ? -tiltAngle : tiltAngle);

        Vector3 originalScale = attacker.localScale;
        Vector3 enlargedScale = originalScale * 1.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            attacker.anchoredPosition = Vector3.Lerp(originalPos, targetPos, t);
            attacker.rotation = Quaternion.Slerp(startRotation, tiltRotation, t);
            attacker.localScale = Vector3.Lerp(originalScale, enlargedScale, t);

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            attacker.anchoredPosition = Vector3.Lerp(targetPos, originalPos, t);
            attacker.rotation = Quaternion.Slerp(tiltRotation, startRotation, t);
            attacker.localScale = Vector3.Lerp(enlargedScale, originalScale, t);

            yield return null;
        }

        attacker.anchoredPosition = originalPos;
        attacker.rotation = startRotation;
        attacker.localScale = originalScale;

        yield return new WaitForSeconds(0.1f);
    }
    
    IEnumerator HitShake(RectTransform rectTransform)
    {
        Vector3 originalPos = rectTransform.anchoredPosition;
        float elapsed = 0f;
        float duration = 0.20f;
        float magnitude = 20f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * magnitude;
            rectTransform.anchoredPosition = originalPos + new Vector3(x, 0, 0);
            yield return null;
        }

        rectTransform.anchoredPosition = originalPos;
    }

    IEnumerator ShowFloatingText(Text textObj, string message, Vector3 worldPos, Color color)
    {
        textObj.text = message;
        textObj.color = color;
        textObj.transform.position = worldPos;
        textObj.gameObject.SetActive(true);

        Vector3 originalPos = textObj.transform.position;
        Vector3 targetPos = originalPos + new Vector3(0, 70, 0);
        float duration = 0.75f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            textObj.transform.position = Vector3.Lerp(originalPos, targetPos, elapsed / duration);
            yield return null;
        }

        textObj.gameObject.SetActive(false);
    }


    void ShowFeedback(string resultText, string explanation)
    {
        feedbackPanel.SetActive(true);
        StartCoroutine(TypeFeedback(resultText, explanation));
        StartCoroutine(WaitThenNextQuestion());
    }

    IEnumerator WaitThenNextQuestion()
    {
        yield return new WaitForSeconds(5f);
        feedbackPanel.SetActive(false);
        canAnswer = true;
        NextQuestionOrEnd();
    }

    IEnumerator TypeFeedback(string result, string explanation, float typeSpeed = 0.02f)
    {
        feedbackText.text = "";
        string fullText = $"{result}\n{explanation}";

        foreach (char c in fullText)
        {
            feedbackText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    void NextQuestionOrEnd()
    {
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }

        currentQuestionIndex++;

        if (progressBar != null)
        {
            targetProgress = currentQuestionIndex;
            StartCoroutine(AnimateProgressBar());
        }

        if (currentQuestionIndex < questions.Count)
        {
            DisplayQuestion();
        }
        else
        {
            ShowResult();
        }
    }

    void ShowResult()
    {
        resultPanel.SetActive(true);
        questionText.text = "";
        timerText.text = "";
        ClearLetters();

        string gradeMsg = "Good try!";
        if (score >= questions.Count * 0.8f)
            gradeMsg = "Excellent! You're a quiz master!";
        else if (score >= questions.Count * 0.5f)
            gradeMsg = "Good job! Keep practicing!";
        else
            gradeMsg = "Don't worry, try again and improve!";

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Your Score: {score}/{questions.Count}"; 
        }

        if (scoreMessageText != null)
        {
            scoreMessageText.text = gradeMsg;
        }
    }

    void ClearLetters()
    {
        foreach (Transform child in letterContainer)
        {
            Destroy(child.gameObject);
        }
        letterButtons.Clear();
    }

    public void OnMainMenuButton()
    {
        // Load main menu or reset game
        // Example: UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
