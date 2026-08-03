using System;
using System.Collections.Generic;

namespace Minigames
{
	/// <summary>
	/// Interface for all interpreters. Implement this to add new interpreter types.
	/// </summary>
	public interface IInterpreter
	{
		/// <summary>
		/// Executes the provided code and returns the result.
		/// </summary>
		/// <param name="code">The source code to execute.</param>
		/// <returns>An InterpreterResult containing execution results or errors.</returns>
		InterpreterResult Run(string code);
	}

	/// <summary>
	/// Enum for selecting which interpreter to use.
	/// Add more types here as new interpreters are created.
	/// </summary>
	public enum InterpreterType
	{
		Java = 0
		// Add more interpreter types here as needed:
		// Python = 1,
		// CSharp = 2,
		// etc.
	}
}
