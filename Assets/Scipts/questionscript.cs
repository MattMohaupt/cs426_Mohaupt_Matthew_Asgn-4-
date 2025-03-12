using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;
using UnityEditor;

public class questionscript : NetworkBehaviour
{
    [TextArea(2, 5)]
    public string[] questions; // Array of questions

    // Reference to the TextMeshPro object for the billboard
    public TextMeshPro billboardText;

    // Array of answer targets in the scene
    public Target[] targets;

    // ✅ Manually assigned correct answers for each question
    public Target[] correctAnswers;

    // the door to the treasure room
    public GameObject treasuredoor;

    // NetworkVariable to synchronize the current question index
    private NetworkVariable<int> currentQuestionIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // NetworkVariable to track if the game is won
    private NetworkVariable<bool> isGameWon = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
     public GameObject winPrompt;
    void Start()
    {
        // Only the server should initialize the first question
        if (IsServer)
        {
            DisplayQuestion(); // Display the first question
            SetCorrectTarget(); // Set the correct target for the first question
        }
        if (winPrompt != null)
        {
            winPrompt.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        // Subscribe to the OnValueChanged event for currentQuestionIndex
        currentQuestionIndex.OnValueChanged += OnQuestionChanged;

        // Display the initial question and set the correct target for all clients
        if (IsClient)
        {
            OnQuestionChanged(0, currentQuestionIndex.Value);
        }

        // Subscribe to the OnValueChanged event for isGameWon
        isGameWon.OnValueChanged += OnGameWonChanged;
    }

    private void OnQuestionChanged(int previousValue, int newValue)
    {
        // Update the question and correct target whenever the index changes
        DisplayQuestion();
        SetCorrectTarget();
    }

    private void OnGameWonChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("🎉 Both players win!");
            ShowWinPrompt();
            // Add logic to display a "You Win!" message or stop the game.
        }
    }
    private void ShowWinPrompt()
    {
        if (winPrompt != null)
        {
            winPrompt.SetActive(true);
            Debug.Log("🏆 You Win!");
        }
        else
        {
            Debug.LogError("❌ Win Prompt not assigned!");
        }
    }

    public void OnCorrectAnswer(ulong playerId)
    {
        // Only the server can answer
        if (!IsServer || playerId != NetworkManager.ServerClientId) return;

        Debug.Log($"✅ Correct answer detected for server: {playerId}");

        // Move to the next question
        currentQuestionIndex.Value = (currentQuestionIndex.Value + 1) % questions.Length;
        Debug.Log($"📢 Moving to question index: {currentQuestionIndex.Value}");

        // Check if the game is won (e.g., after answering a certain number of questions)
        if (currentQuestionIndex.Value == questions.Length - 1)
        {
            isGameWon.Value = true; // Trigger win condition

            //destroy the the treasure door
            Destroy(treasuredoor);
        }
    }

    // Method to display the current question on the billboard
    private void DisplayQuestion()
    {
        if (billboardText != null)
        {
            billboardText.text = questions[currentQuestionIndex.Value];
            Debug.Log($"📢 Displaying question: {questions[currentQuestionIndex.Value]}");
        }
        else
        {
            Debug.LogError("❌ Billboard Text not set in the inspector!");
        }
    }

    // ✅ Method to set the correct target based on manual assignment
    private void SetCorrectTarget()
    {
        Debug.Log($"🔹 Setting correct target for question: {questions[currentQuestionIndex.Value]}");

        // Ensure correctAnswers array matches the number of questions
        if (correctAnswers.Length != questions.Length)
        {
            Debug.LogError($"❌ CorrectAnswers array size ({correctAnswers.Length}) does not match Questions array size ({questions.Length})!");
            return;
        }

        // Reset all targets to incorrect
        foreach (var target in targets)
        {
            target.isCorrect = false;
        }

        // ✅ Manually assign the correct target
        Target correctTarget = correctAnswers[currentQuestionIndex.Value];

        if (correctTarget == null)
        {
            Debug.LogError($"❌ Correct target is NULL for question {currentQuestionIndex.Value}! Make sure all CorrectAnswers[] are assigned in Unity.");
            return;
        }

        correctTarget.isCorrect = true;
        Debug.Log($"✅ Correct target set: {correctTarget.name}");
    }
}