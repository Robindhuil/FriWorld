using UnityEngine;
using System.Linq;

public class IdeMiniGame : Questable
{
	[Header("IDE Settings")]
	[SerializeField] private TextAsset defaultCodeFile;
	[SerializeField] private Minigames.InterpreterType selectedInterpreter = Minigames.InterpreterType.Java;

	[Header("Expected Output")]
	[SerializeField] private string expectedOutput = "";

	[SerializeField] private bool caseInsensitiveComparison = false;

	[SerializeField] private bool trimWhitespace = true;

	[SerializeField] private UIManager uiManager;
	private IdeUI ideUI;

	protected override void Interact()
	{
		base.Interact();

		if (!CanBeInteracted)
		{
			return;
		}

		if (uiManager == null)
		{
			Debug.LogError("[IdeMiniGame] UIManager reference not found on player.");
			return;
		}

		// Get IdeUI from UIManager
		ideUI = uiManager.GetIdeUI();
		
		if (ideUI == null)
		{
			Debug.LogError("[IdeMiniGame] IdeUI not initialized in UIManager.");
			return;
		}

		// Subscribe to code execution event
		ideUI.OnCodeExecuted += ValidateCodeOutput;
		
		// Initialize with this minigame's settings
		ideUI.Initialize(defaultCodeFile, selectedInterpreter);
		
		// Open the IDE window through UIManager
		uiManager.OpenIde();
	}

	private void ValidateCodeOutput(Minigames.InterpreterResult result)
	{
		if (result == null || !result.Success)
		{
			return;
		}

		// Combine all output lines into a single string
		string actualOutput = string.Join("\n", result.Output);

		// Normalize outputs based on settings
		string normalized_actual = NormalizeString(actualOutput);
		string normalized_expected = NormalizeString(expectedOutput);

		if (normalized_actual == normalized_expected)
		{
			Debug.Log("[IdeMiniGame] Code output matches expected output! Quest completed.");
			
			// Display success message in green in the terminal
			if (ideUI != null)
			{
				ideUI.AppendTerminalMessage("✓ Quest successfully completed!", true);
			}
			
			// Unsubscribe from the event
			if (ideUI != null)
			{
				ideUI.OnCodeExecuted -= ValidateCodeOutput;
			}

			// Complete the quest
			if (Journal != null)
			{
				CompleteQuest(Journal);
			}
			
		}
		else
		{
			Debug.Log($"[IdeMiniGame] Output mismatch.\nExpected:\n{normalized_expected}\n\nActual:\n{normalized_actual}");
			
			// Display failure message in red in the terminal
			if (ideUI != null)
			{
				ideUI.AppendTerminalMessage("✗ Code output is wrong. Try again!", false);
			}
		}
	}

	private string NormalizeString(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "";
		}

		string normalized = input;

		if (trimWhitespace)
		{
			// Trim each line and remove empty lines
			var lines = normalized.Split('\n');
			var trimmedLines = System.Linq.Enumerable.Where(lines, line => !string.IsNullOrWhiteSpace(line))
				.Select(line => line.Trim());
			normalized = string.Join("\n", trimmedLines);
		}

		if (caseInsensitiveComparison)
		{
			normalized = normalized.ToLower();
		}

		return normalized;
	}

	private void OnDestroy()
	{
		// Unsubscribe from the event when destroyed
		if (ideUI != null)
		{
			ideUI.OnCodeExecuted -= ValidateCodeOutput;
		}
	}

	private void OnDisable()
	{
		// Unsubscribe when disabled
		if (ideUI != null)
		{
			ideUI.OnCodeExecuted -= ValidateCodeOutput;
		}
	}
}
