using System;
using System.Collections.Generic;
using System.Globalization;

namespace Minigames
{
	public class JavaInterpreter : IInterpreter
	{
		public InterpreterResult Run(string code)
		{
			var result = new InterpreterResult();
			try
			{
				var lexer = new Lexer(code ?? string.Empty);
				var tokens = lexer.Tokenize();
				var parser = new Parser(tokens);
				var program = parser.ParseProgram();

				var runtime = new Runtime(program);
				runtime.Execute();

				result.Success = true;
				result.Output.AddRange(runtime.Output);
			}
			catch (InterpreterException ex)
			{
				result.Success = false;
				result.ErrorMessage = ex.Message;
				result.ErrorLine = ex.Line;
				result.ErrorColumn = ex.Column;
			}
			catch (Exception ex)
			{
				result.Success = false;
				result.ErrorMessage = ex.Message;
				result.ErrorLine = 0;
				result.ErrorColumn = 0;
			}

			return result;
		}
	}

	public sealed class InterpreterResult
	{
		public bool Success;
		public readonly List<string> Output = new List<string>();
		public string ErrorMessage;
		public int ErrorLine;
		public int ErrorColumn;
	}

	internal sealed class Runtime
	{
		private readonly ProgramNode _program;
		private readonly Dictionary<string, FunctionDef> _functions = new Dictionary<string, FunctionDef>(StringComparer.Ordinal);
		private readonly ScopeStack _scopes = new ScopeStack();

		public Runtime(ProgramNode program)
		{
			_program = program;
			foreach (var func in program.Functions)
			{
				if (_functions.ContainsKey(func.Name))
				{
					throw new RuntimeException($"Duplicate function '{func.Name}'.", func.Line, func.Column);
				}

				_functions[func.Name] = func;
			}
		}

		public List<string> Output { get; } = new List<string>();

		public void Execute()
		{
			_scopes.Push();
			try
			{
				foreach (var stmt in _program.Statements)
				{
					stmt.Execute(this);
				}
			}
			catch (BreakSignal signal)
			{
				throw new RuntimeException("'break' used outside of a loop.", signal.Line, signal.Column);
			}
			catch (ContinueSignal signal)
			{
				throw new RuntimeException("'continue' used outside of a loop.", signal.Line, signal.Column);
			}
			finally
			{
				_scopes.Pop();
			}
		}

		public Value CallFunction(string name, List<Value> args, int line, int column)
		{
			if (name == "print" || name == "println" || name == "System.out.print" || name == "System.out.println")
			{
				var text = args.Count > 0 ? args[0].ToPrintString() : string.Empty;
				Output.Add(text);
				return Value.Void();
			}

			if (!_functions.TryGetValue(name, out var func))
			{
				throw new RuntimeException($"Unknown function '{name}'.", line, column);
			}

			if (func.Parameters.Count != args.Count)
			{
				throw new RuntimeException($"Function '{name}' expects {func.Parameters.Count} args, got {args.Count}.", line, column);
			}

			_scopes.Push();
			for (var i = 0; i < func.Parameters.Count; i++)
			{
				var param = func.Parameters[i];
				var value = TypeSystem.CastOrThrow(args[i], param.Type, line, column);
				_scopes.Define(param.Name, value);
			}

			try
			{
				func.Body.Execute(this);
			}
			catch (ReturnSignal signal)
			{
				_scopes.Pop();
				if (func.ReturnType == ValueType.Void)
				{
					return Value.Void();
				}

				return TypeSystem.CastOrThrow(signal.Value, func.ReturnType, signal.Line, signal.Column);
			}
			catch (BreakSignal signal)
			{
				_scopes.Pop();
				throw new RuntimeException("'break' used outside of a loop.", signal.Line, signal.Column);
			}
			catch (ContinueSignal signal)
			{
				_scopes.Pop();
				throw new RuntimeException("'continue' used outside of a loop.", signal.Line, signal.Column);
			}

			_scopes.Pop();

			if (func.ReturnType != ValueType.Void)
			{
				throw new RuntimeException($"Function '{name}' must return {func.ReturnType}.", func.Line, func.Column);
			}

			return Value.Void();
		}

		public ScopeStack Scopes => _scopes;
		public IReadOnlyDictionary<string, FunctionDef> Functions => _functions;
	}

	internal sealed class ScopeStack
	{
		private readonly Stack<Dictionary<string, Value>> _stack = new Stack<Dictionary<string, Value>>();

		public void Push()
		{
			_stack.Push(new Dictionary<string, Value>(StringComparer.Ordinal));
		}

		public void Pop()
		{
			_stack.Pop();
		}

		public void Define(string name, Value value)
		{
			if (_stack.Count == 0)
			{
				throw new InvalidOperationException("No scope to define variables.");
			}

			_stack.Peek()[name] = value;
		}

		public bool TryGet(string name, out Value value)
		{
			foreach (var scope in _stack)
			{
				if (scope.TryGetValue(name, out value))
				{
					return true;
				}
			}

			value = default;
			return false;
		}

		public void Assign(string name, Value value, int line, int column)
		{
			foreach (var scope in _stack)
			{
				if (scope.ContainsKey(name))
				{
					scope[name] = value;
					return;
				}
			}

			throw new RuntimeException($"Undefined variable '{name}'.", line, column);
		}
	}

	internal static class TypeSystem
	{
		public static Value CastOrThrow(Value value, ValueType targetType, int line, int column)
		{
			if (targetType == value.Type)
			{
				return value;
			}

			if (targetType == ValueType.Double && value.Type == ValueType.Int)
			{
				return Value.FromDouble(value.AsInt());
			}

			throw new RuntimeException($"Cannot convert {value.Type} to {targetType}.", line, column);
		}
	}

	internal enum ValueType
	{
		Int,
		Double,
		Bool,
		Char,
		String,
		IntArray,
		CharArray,
		StringArray,
		Void
	}

	internal readonly struct Value
	{
		private readonly object _value;
		public ValueType Type { get; }

		private Value(ValueType type, object value)
		{
			Type = type;
			_value = value;
		}

		public static Value FromInt(int value) => new Value(ValueType.Int, value);
		public static Value FromDouble(double value) => new Value(ValueType.Double, value);
		public static Value FromBool(bool value) => new Value(ValueType.Bool, value);
		public static Value FromChar(char value) => new Value(ValueType.Char, value);
		public static Value FromString(string value) => new Value(ValueType.String, value ?? string.Empty);
		public static Value FromIntArray(int[] value) => new Value(ValueType.IntArray, value ?? Array.Empty<int>());
		public static Value FromCharArray(char[] value) => new Value(ValueType.CharArray, value ?? Array.Empty<char>());
		public static Value FromStringArray(string[] value) => new Value(ValueType.StringArray, value ?? Array.Empty<string>());
		public static Value Void() => new Value(ValueType.Void, null);

		public int AsInt() => (int)_value;
		public double AsDouble() => (double)_value;
		public bool AsBool() => (bool)_value;
		public char AsChar() => (char)_value;
		public string AsString() => (string)_value;
		public int[] AsIntArray() => (int[])_value;
		public char[] AsCharArray() => (char[])_value;
		public string[] AsStringArray() => (string[])_value;

		public string ToPrintString()
		{
			switch (Type)
			{
				case ValueType.Int:
					return AsInt().ToString(CultureInfo.InvariantCulture);
				case ValueType.Double:
					return AsDouble().ToString(CultureInfo.InvariantCulture);
				case ValueType.Bool:
					return AsBool() ? "true" : "false";
				case ValueType.Char:
					return AsChar().ToString();
				case ValueType.String:
					return AsString();
				case ValueType.IntArray:
					return "[" + string.Join(", ", AsIntArray()) + "]";
				case ValueType.CharArray:
					return "[" + string.Join(", ", AsCharArray()) + "]";
				case ValueType.StringArray:
					return "[" + string.Join(", ", AsStringArray()) + "]";
				default:
					return string.Empty;
			}
		}
	}

	internal sealed class ProgramNode
	{
		public List<FunctionDef> Functions { get; } = new List<FunctionDef>();
		public List<Statement> Statements { get; } = new List<Statement>();
	}

	internal sealed class FunctionDef
	{
		public string Name { get; set; }
		public ValueType ReturnType { get; set; }
		public List<ParameterDef> Parameters { get; } = new List<ParameterDef>();
		public Statement Body { get; set; }
		public int Line { get; set; }
		public int Column { get; set; }
	}

	internal sealed class ParameterDef
	{
		public string Name { get; set; }
		public ValueType Type { get; set; }
	}

	internal abstract class Statement
	{
		public int Line { get; set; }
		public int Column { get; set; }
		public abstract void Execute(Runtime runtime);
	}

	internal sealed class BlockStatement : Statement
	{
		public List<Statement> Statements { get; } = new List<Statement>();

		public override void Execute(Runtime runtime)
		{
			runtime.Scopes.Push();
			foreach (var stmt in Statements)
			{
				stmt.Execute(runtime);
			}
			runtime.Scopes.Pop();
		}
	}

	internal sealed class VarDeclarationStatement : Statement
	{
		public string Name { get; set; }
		public ValueType DeclaredType { get; set; }
		public Expr Initializer { get; set; }

		public override void Execute(Runtime runtime)
		{
			Value value;
			if (Initializer == null)
			{
				value = GetDefaultValue(DeclaredType);
			}
			else
			{
				var initValue = Initializer.Evaluate(runtime);
				value = TypeSystem.CastOrThrow(initValue, DeclaredType, Line, Column);
			}

			runtime.Scopes.Define(Name, value);
		}

		private static Value GetDefaultValue(ValueType type)
		{
			switch (type)
			{
				case ValueType.Int:
					return Value.FromInt(0);
				case ValueType.Double:
					return Value.FromDouble(0.0);
				case ValueType.Bool:
					return Value.FromBool(false);
				case ValueType.Char:
					return Value.FromChar('\0');
				case ValueType.String:
					return Value.FromString(string.Empty);
				case ValueType.IntArray:
					return Value.FromIntArray(Array.Empty<int>());
				case ValueType.CharArray:
					return Value.FromCharArray(Array.Empty<char>());
				case ValueType.StringArray:
					return Value.FromStringArray(Array.Empty<string>());
				default:
					return Value.Void();
			}
		}
	}

	internal sealed class ArrayAssignmentStatement : Statement
	{
		public string Name { get; set; }
		public Expr Index { get; set; }
		public Expr ValueExpr { get; set; }

		public override void Execute(Runtime runtime)
		{
			if (!runtime.Scopes.TryGet(Name, out var arrayValue))
			{
				throw new RuntimeException($"Undefined variable '{Name}'.", Line, Column);
			}

			var indexValue = Index.Evaluate(runtime);
			if (indexValue.Type != ValueType.Int)
			{
				throw new RuntimeException("Array index must be int.", Line, Column);
			}

			var index = indexValue.AsInt();
			if (arrayValue.Type == ValueType.IntArray)
			{
				var array = arrayValue.AsIntArray();
				if (index < 0 || index >= array.Length)
				{
					throw new RuntimeException("Array index out of range.", Line, Column);
				}
				var value = TypeSystem.CastOrThrow(ValueExpr.Evaluate(runtime), ValueType.Int, Line, Column);
				array[index] = value.AsInt();
				return;
			}

			if (arrayValue.Type == ValueType.CharArray)
			{
				var array = arrayValue.AsCharArray();
				if (index < 0 || index >= array.Length)
				{
					throw new RuntimeException("Array index out of range.", Line, Column);
				}
				var value = TypeSystem.CastOrThrow(ValueExpr.Evaluate(runtime), ValueType.Char, Line, Column);
				array[index] = value.AsChar();
				return;
			}

			if (arrayValue.Type == ValueType.StringArray)
			{
				var array = arrayValue.AsStringArray();
				if (index < 0 || index >= array.Length)
				{
					throw new RuntimeException("Array index out of range.", Line, Column);
				}
				var value = TypeSystem.CastOrThrow(ValueExpr.Evaluate(runtime), ValueType.String, Line, Column);
				array[index] = value.AsString();
				return;
			}

			throw new RuntimeException("Target is not an array.", Line, Column);
		}
	}

	internal sealed class AssignmentStatement : Statement
	{
		public string Name { get; set; }
		public Expr ValueExpr { get; set; }

		public override void Execute(Runtime runtime)
		{
			if (!runtime.Scopes.TryGet(Name, out var current))
			{
				throw new RuntimeException($"Undefined variable '{Name}'.", Line, Column);
			}

			var newValue = ValueExpr.Evaluate(runtime);
			var castValue = TypeSystem.CastOrThrow(newValue, current.Type, Line, Column);
			runtime.Scopes.Assign(Name, castValue, Line, Column);
		}
	}

	internal sealed class ExpressionStatement : Statement
	{
		public Expr Expression { get; set; }

		public override void Execute(Runtime runtime)
		{
			Expression.Evaluate(runtime);
		}
	}

	internal sealed class IfStatement : Statement
	{
		public Expr Condition { get; set; }
		public Statement Then { get; set; }
		public Statement Else { get; set; }

		public override void Execute(Runtime runtime)
		{
			var cond = Condition.Evaluate(runtime);
			if (cond.Type != ValueType.Bool)
			{
				throw new RuntimeException("Condition must be boolean.", Line, Column);
			}

			if (cond.AsBool())
			{
				Then.Execute(runtime);
			}
			else if (Else != null)
			{
				Else.Execute(runtime);
			}
		}
	}

	internal sealed class WhileStatement : Statement
	{
		public Expr Condition { get; set; }
		public Statement Body { get; set; }

		public override void Execute(Runtime runtime)
		{
			while (true)
			{
				var cond = Condition.Evaluate(runtime);
				if (cond.Type != ValueType.Bool)
				{
					throw new RuntimeException("Condition must be boolean.", Line, Column);
				}

				if (!cond.AsBool())
				{
					break;
				}

				try
				{
					Body.Execute(runtime);
				}
				catch (ContinueSignal)
				{
					continue;
				}
				catch (BreakSignal)
				{
					break;
				}
			}
		}
	}

	internal sealed class ForStatement : Statement
	{
		public Statement Init { get; set; }
		public Expr Condition { get; set; }
		public Statement Post { get; set; }
		public Statement Body { get; set; }

		public override void Execute(Runtime runtime)
		{
			runtime.Scopes.Push();
			Init?.Execute(runtime);

			while (true)
			{
				if (Condition != null)
				{
					var cond = Condition.Evaluate(runtime);
					if (cond.Type != ValueType.Bool)
					{
						throw new RuntimeException("Condition must be boolean.", Line, Column);
					}

					if (!cond.AsBool())
					{
						break;
					}
				}

				try
				{
					Body.Execute(runtime);
				}
				catch (ContinueSignal)
				{
					Post?.Execute(runtime);
					continue;
				}
				catch (BreakSignal)
				{
					break;
				}

				Post?.Execute(runtime);
			}

			runtime.Scopes.Pop();
		}
	}

	internal sealed class BreakStatement : Statement
	{
		public override void Execute(Runtime runtime)
		{
			throw new BreakSignal(Line, Column);
		}
	}

	internal sealed class ContinueStatement : Statement
	{
		public override void Execute(Runtime runtime)
		{
			throw new ContinueSignal(Line, Column);
		}
	}

	internal sealed class ReturnStatement : Statement
	{
		public Expr Expression { get; set; }

		public override void Execute(Runtime runtime)
		{
			var value = Expression != null ? Expression.Evaluate(runtime) : Value.Void();
			throw new ReturnSignal(value, Line, Column);
		}
	}

	internal abstract class Expr
	{
		public int Line { get; set; }
		public int Column { get; set; }
		public abstract Value Evaluate(Runtime runtime);
	}

	internal sealed class LiteralExpr : Expr
	{
		public Value Value { get; set; }
		public override Value Evaluate(Runtime runtime) => Value;
	}

	internal sealed class VariableExpr : Expr
	{
		public string Name { get; set; }

		public override Value Evaluate(Runtime runtime)
		{
			if (!runtime.Scopes.TryGet(Name, out var value))
			{
				throw new RuntimeException($"Undefined variable '{Name}'.", Line, Column);
			}

			return value;
		}
	}

	internal sealed class ArrayAccessExpr : Expr
	{
		public string Name { get; set; }
		public Expr Index { get; set; }

		public override Value Evaluate(Runtime runtime)
		{
			if (!runtime.Scopes.TryGet(Name, out var arrayValue))
			{
				throw new RuntimeException($"Undefined variable '{Name}'.", Line, Column);
			}

			var indexValue = Index.Evaluate(runtime);
			if (indexValue.Type != ValueType.Int)
			{
				throw new RuntimeException("Array index must be int.", Line, Column);
			}

			var index = indexValue.AsInt();
			if (arrayValue.Type == ValueType.IntArray)
			{
				var array = arrayValue.AsIntArray();
				if (index < 0 || index >= array.Length)
				{
					throw new RuntimeException("Array index out of range.", Line, Column);
				}
				return Value.FromInt(array[index]);
			}

			if (arrayValue.Type == ValueType.CharArray)
			{
				var array = arrayValue.AsCharArray();
				if (index < 0 || index >= array.Length)
				{
					throw new RuntimeException("Array index out of range.", Line, Column);
				}
				return Value.FromChar(array[index]);
			}

			if (arrayValue.Type == ValueType.StringArray)
			{
				var array = arrayValue.AsStringArray();
				if (index < 0 || index >= array.Length)
				{
					throw new RuntimeException("Array index out of range.", Line, Column);
				}
				return Value.FromString(array[index]);
			}

			throw new RuntimeException("Target is not an array.", Line, Column);
		}
	}

	internal sealed class ArrayCreationExpr : Expr
	{
		public ValueType ElementType { get; set; }
		public Expr Length { get; set; }

		public override Value Evaluate(Runtime runtime)
		{
			var lengthValue = Length.Evaluate(runtime);
			if (lengthValue.Type != ValueType.Int)
			{
				throw new RuntimeException("Array length must be int.", Line, Column);
			}

			var length = lengthValue.AsInt();
			if (length < 0)
			{
				throw new RuntimeException("Array length cannot be negative.", Line, Column);
			}

			switch (ElementType)
			{
				case ValueType.Int:
					return Value.FromIntArray(new int[length]);
				case ValueType.Char:
					return Value.FromCharArray(new char[length]);
				case ValueType.String:
					return Value.FromStringArray(new string[length]);
				default:
					throw new RuntimeException("Only int[], char[], and string[] are supported.", Line, Column);
			}
		}
	}

	internal sealed class CallExpr : Expr
	{
		public string Name { get; set; }
		public List<Expr> Args { get; } = new List<Expr>();

		public override Value Evaluate(Runtime runtime)
		{
			var values = new List<Value>();
			foreach (var arg in Args)
			{
				values.Add(arg.Evaluate(runtime));
			}

			return runtime.CallFunction(Name, values, Line, Column);
		}
	}

	internal sealed class UnaryExpr : Expr
	{
		public string Op { get; set; }
		public Expr Right { get; set; }

		public override Value Evaluate(Runtime runtime)
		{
			var value = Right.Evaluate(runtime);
			switch (Op)
			{
				case "!":
					if (value.Type != ValueType.Bool)
					{
						throw new RuntimeException("'!' expects boolean.", Line, Column);
					}
					return Value.FromBool(!value.AsBool());
				case "-":
					if (value.Type == ValueType.Int)
					{
						return Value.FromInt(-value.AsInt());
					}
					if (value.Type == ValueType.Double)
					{
						return Value.FromDouble(-value.AsDouble());
					}
					throw new RuntimeException("Unary '-' expects number.", Line, Column);
				default:
					throw new RuntimeException("Unknown unary operator.", Line, Column);
			}
		}
	}

	internal sealed class BinaryExpr : Expr
	{
		public string Op { get; set; }
		public Expr Left { get; set; }
		public Expr Right { get; set; }

		public override Value Evaluate(Runtime runtime)
		{
			if (Op == "&&" || Op == "||")
			{
				var leftVal = Left.Evaluate(runtime);
				if (leftVal.Type != ValueType.Bool)
				{
					throw new RuntimeException("Logical operators require boolean.", Line, Column);
				}

				if (Op == "&&")
				{
					if (!leftVal.AsBool())
					{
						return Value.FromBool(false);
					}
					var rightVal = Right.Evaluate(runtime);
					if (rightVal.Type != ValueType.Bool)
					{
						throw new RuntimeException("Logical operators require boolean.", Line, Column);
					}
					return Value.FromBool(rightVal.AsBool());
				}
				else
				{
					if (leftVal.AsBool())
					{
						return Value.FromBool(true);
					}
					var rightVal = Right.Evaluate(runtime);
					if (rightVal.Type != ValueType.Bool)
					{
						throw new RuntimeException("Logical operators require boolean.", Line, Column);
					}
					return Value.FromBool(rightVal.AsBool());
				}
			}

			var left = Left.Evaluate(runtime);
			var right = Right.Evaluate(runtime);

			switch (Op)
			{
				case "+":
					if (left.Type == ValueType.String || right.Type == ValueType.String)
					{
						return Value.FromString(left.ToPrintString() + right.ToPrintString());
					}
					return NumericOp(left, right, (a, b) => a + b, (a, b) => a + b);
				case "-":
					return NumericOp(left, right, (a, b) => a - b, (a, b) => a - b);
				case "*":
					return NumericOp(left, right, (a, b) => a * b, (a, b) => a * b);
				case "/":
					return NumericOp(left, right, (a, b) => a / b, (a, b) => a / b);
				case "==":
					return Value.FromBool(Equals(left, right));
				case "!=":
					return Value.FromBool(!Equals(left, right));
				case "<":
					return CompareOp(left, right, (a, b) => a < b, (a, b) => a < b);
				case "<=":
					return CompareOp(left, right, (a, b) => a <= b, (a, b) => a <= b);
				case ">":
					return CompareOp(left, right, (a, b) => a > b, (a, b) => a > b);
				case ">=":
					return CompareOp(left, right, (a, b) => a >= b, (a, b) => a >= b);
				default:
					throw new RuntimeException("Unknown binary operator.", Line, Column);
			}
		}

		private Value NumericOp(Value left, Value right, Func<int, int, int> intOp, Func<double, double, double> doubleOp)
		{
			if (left.Type == ValueType.Int && right.Type == ValueType.Int)
			{
				return Value.FromInt(intOp(left.AsInt(), right.AsInt()));
			}

			if ((left.Type == ValueType.Double || left.Type == ValueType.Int) &&
				(right.Type == ValueType.Double || right.Type == ValueType.Int))
			{
				var a = left.Type == ValueType.Double ? left.AsDouble() : left.AsInt();
				var b = right.Type == ValueType.Double ? right.AsDouble() : right.AsInt();
				return Value.FromDouble(doubleOp(a, b));
			}

			throw new RuntimeException("Numeric operator expects numbers.", Line, Column);
		}

		private Value CompareOp(Value left, Value right, Func<int, int, bool> intOp, Func<double, double, bool> doubleOp)
		{
			if (left.Type == ValueType.Int && right.Type == ValueType.Int)
			{
				return Value.FromBool(intOp(left.AsInt(), right.AsInt()));
			}

			if (left.Type == ValueType.Char && right.Type == ValueType.Char)
			{
				return Value.FromBool(intOp(left.AsChar(), right.AsChar()));
			}

			if ((left.Type == ValueType.Double || left.Type == ValueType.Int) &&
				(right.Type == ValueType.Double || right.Type == ValueType.Int))
			{
				var a = left.Type == ValueType.Double ? left.AsDouble() : left.AsInt();
				var b = right.Type == ValueType.Double ? right.AsDouble() : right.AsInt();
				return Value.FromBool(doubleOp(a, b));
			}

			throw new RuntimeException("Comparison expects numbers.", Line, Column);
		}

		private static bool Equals(Value left, Value right)
		{
			if (left.Type != right.Type)
			{
				if (left.Type == ValueType.Int && right.Type == ValueType.Double)
				{
					return Math.Abs(left.AsInt() - right.AsDouble()) < 0.0000001;
				}
				if (left.Type == ValueType.Double && right.Type == ValueType.Int)
				{
					return Math.Abs(left.AsDouble() - right.AsInt()) < 0.0000001;
				}
				return false;
			}

			switch (left.Type)
			{
				case ValueType.Int:
					return left.AsInt() == right.AsInt();
				case ValueType.Double:
					return Math.Abs(left.AsDouble() - right.AsDouble()) < 0.0000001;
				case ValueType.Bool:
					return left.AsBool() == right.AsBool();
				case ValueType.Char:
					return left.AsChar() == right.AsChar();
				case ValueType.String:
					return string.Equals(left.AsString(), right.AsString(), StringComparison.Ordinal);
				case ValueType.IntArray:
					return ReferenceEquals(left.AsIntArray(), right.AsIntArray());
				case ValueType.StringArray:
					return ReferenceEquals(left.AsStringArray(), right.AsStringArray());
				default:
					return true;
			}
		}
	}

	internal sealed class Parser
	{
		private readonly List<Token> _tokens;
		private int _index;

		public Parser(List<Token> tokens)
		{
			_tokens = tokens;
		}

		public ProgramNode ParseProgram()
		{
			var program = new ProgramNode();

			while (!Check(TokenType.Eof))
			{
				if (IsTypeKeyword(Peek()))
				{
					var typeToken = Peek();
					var type = ParseType();
					var name = Consume(TokenType.Identifier, "Expected identifier.");

					if (Match(TokenType.LeftParen))
					{
						var func = new FunctionDef
						{
							Name = name.Lexeme,
							ReturnType = type,
							Line = name.Line,
							Column = name.Column
						};

						if (!Check(TokenType.RightParen))
						{
							do
							{
								var paramType = ParseType();
								var paramName = Consume(TokenType.Identifier, "Expected parameter name.");
								func.Parameters.Add(new ParameterDef { Name = paramName.Lexeme, Type = paramType });
							}
							while (Match(TokenType.Comma));
						}

						Consume(TokenType.RightParen, "Expected ')'.");
						func.Body = ParseBlock();
						program.Functions.Add(func);
					}
					else
					{
						var decl = new VarDeclarationStatement
						{
							DeclaredType = type,
							Name = name.Lexeme,
							Line = name.Line,
							Column = name.Column
						};

						if (Match(TokenType.Equal))
						{
							decl.Initializer = ParseExpression();
						}

						Consume(TokenType.Semicolon, "Expected ';'.");
						program.Statements.Add(decl);
					}
				}
				else
				{
					program.Statements.Add(ParseStatement());
				}
			}

			return program;
		}

		private Statement ParseStatement()
		{
			if (Match(TokenType.LeftBrace))
			{
				return ParseBlock(true);
			}

			if (Match(TokenType.If))
			{
				return ParseIf();
			}

			if (Match(TokenType.While))
			{
				return ParseWhile();
			}

			if (Match(TokenType.For))
			{
				return ParseFor();
			}

			if (Match(TokenType.Return))
			{
				var stmt = new ReturnStatement { Line = Previous().Line, Column = Previous().Column };
				if (!Check(TokenType.Semicolon))
				{
					stmt.Expression = ParseExpression();
				}
				Consume(TokenType.Semicolon, "Expected ';'.");
				return stmt;
			}

			if (Match(TokenType.Break))
			{
				var stmt = new BreakStatement { Line = Previous().Line, Column = Previous().Column };
				Consume(TokenType.Semicolon, "Expected ';'.");
				return stmt;
			}

			if (Match(TokenType.Continue))
			{
				var stmt = new ContinueStatement { Line = Previous().Line, Column = Previous().Column };
				Consume(TokenType.Semicolon, "Expected ';'.");
				return stmt;
			}

			if (IsTypeKeyword(Peek()))
			{
				return ParseVarDeclaration();
			}

			return ParseExpressionOrAssignment();
		}

		private Statement ParseExpressionOrAssignment()
		{
			var start = Peek();

			if (IsArrayAssignmentStart())
			{
				return ParseArrayAssignment(true);
			}

			if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Equal)
			{
				var name = Consume(TokenType.Identifier, "Expected identifier.");
				Consume(TokenType.Equal, "Expected '='.");
				var expr = ParseExpression();
				Consume(TokenType.Semicolon, "Expected ';'.");
				return new AssignmentStatement
				{
					Name = name.Lexeme,
					ValueExpr = expr,
					Line = name.Line,
					Column = name.Column
				};
			}

			var exprStmt = new ExpressionStatement
			{
				Expression = ParseExpression(),
				Line = start.Line,
				Column = start.Column
			};
			Consume(TokenType.Semicolon, "Expected ';'.");
			return exprStmt;
		}

		private Statement ParseVarDeclaration()
		{
			var type = ParseType();
			var name = Consume(TokenType.Identifier, "Expected identifier.");
			var stmt = new VarDeclarationStatement
			{
				DeclaredType = type,
				Name = name.Lexeme,
				Line = name.Line,
				Column = name.Column
			};

			if (Match(TokenType.Equal))
			{
				stmt.Initializer = ParseExpression();
			}

			Consume(TokenType.Semicolon, "Expected ';'.");
			return stmt;
		}

		private Statement ParseIf()
		{
			Consume(TokenType.LeftParen, "Expected '('.");
			var condition = ParseExpression();
			Consume(TokenType.RightParen, "Expected ')'.");
			var thenStmt = ParseStatement();
			Statement elseStmt = null;
			if (Match(TokenType.Else))
			{
				elseStmt = ParseStatement();
			}

			return new IfStatement
			{
				Condition = condition,
				Then = thenStmt,
				Else = elseStmt,
				Line = condition.Line,
				Column = condition.Column
			};
		}

		private Statement ParseWhile()
		{
			Consume(TokenType.LeftParen, "Expected '('.");
			var condition = ParseExpression();
			Consume(TokenType.RightParen, "Expected ')'.");
			var body = ParseStatement();
			return new WhileStatement
			{
				Condition = condition,
				Body = body,
				Line = condition.Line,
				Column = condition.Column
			};
		}

		private Statement ParseFor()
		{
			var forToken = Previous();
			Consume(TokenType.LeftParen, "Expected '('.");

			Statement init = null;
			if (!Check(TokenType.Semicolon))
			{
				if (IsTypeKeyword(Peek()))
				{
					init = ParseVarDeclarationInFor();
				}
				else
				{
					init = ParseAssignmentOrExpressionInFor();
				}
			}
			Consume(TokenType.Semicolon, "Expected ';'.");

			Expr condition = null;
			if (!Check(TokenType.Semicolon))
			{
				condition = ParseExpression();
			}
			Consume(TokenType.Semicolon, "Expected ';'.");

			Statement post = null;
			if (!Check(TokenType.RightParen))
			{
				post = ParseAssignmentOrExpressionInFor();
			}
			Consume(TokenType.RightParen, "Expected ')'.");

			var body = ParseStatement();
			return new ForStatement
			{
				Init = init,
				Condition = condition,
				Post = post,
				Body = body,
				Line = forToken.Line,
				Column = forToken.Column
			};
		}

		private Statement ParseVarDeclarationInFor()
		{
			var type = ParseType();
			var name = Consume(TokenType.Identifier, "Expected identifier.");
			var stmt = new VarDeclarationStatement
			{
				DeclaredType = type,
				Name = name.Lexeme,
				Line = name.Line,
				Column = name.Column
			};

			if (Match(TokenType.Equal))
			{
				stmt.Initializer = ParseExpression();
			}

			return stmt;
		}

		private Statement ParseAssignmentOrExpressionInFor()
		{
			var start = Peek();
			if (IsArrayAssignmentStart())
			{
				return ParseArrayAssignment(false);
			}
			if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Equal)
			{
				var name = Consume(TokenType.Identifier, "Expected identifier.");
				Consume(TokenType.Equal, "Expected '='.");
				var expr = ParseExpression();
				return new AssignmentStatement
				{
					Name = name.Lexeme,
					ValueExpr = expr,
					Line = name.Line,
					Column = name.Column
				};
			}

			return new ExpressionStatement
			{
				Expression = ParseExpression(),
				Line = start.Line,
				Column = start.Column
			};
		}

		private Statement ParseArrayAssignment(bool consumeSemicolon)
		{
			var name = Consume(TokenType.Identifier, "Expected identifier.");
			Consume(TokenType.LeftBracket, "Expected '['.");
			var index = ParseExpression();
			Consume(TokenType.RightBracket, "Expected ']'.");
			Consume(TokenType.Equal, "Expected '='.");
			var expr = ParseExpression();
			if (consumeSemicolon)
			{
				Consume(TokenType.Semicolon, "Expected ';'.");
			}
			return new ArrayAssignmentStatement
			{
				Name = name.Lexeme,
				Index = index,
				ValueExpr = expr,
				Line = name.Line,
				Column = name.Column
			};
		}

		private BlockStatement ParseBlock(bool alreadyOpened = false)
		{
			if (!alreadyOpened)
			{
				Consume(TokenType.LeftBrace, "Expected '{'.");
			}

			var block = new BlockStatement();
			while (!Check(TokenType.RightBrace) && !Check(TokenType.Eof))
			{
				block.Statements.Add(ParseStatement());
			}
			Consume(TokenType.RightBrace, "Expected '}'.");
			return block;
		}

		private Expr ParseExpression()
		{
			return ParseLogicalOr();
		}

		private Expr ParseLogicalOr()
		{
			var expr = ParseLogicalAnd();
			while (Match(TokenType.OrOr))
			{
				var op = Previous();
				var right = ParseLogicalAnd();
				expr = new BinaryExpr { Left = expr, Right = right, Op = op.Lexeme, Line = op.Line, Column = op.Column };
			}
			return expr;
		}

		private Expr ParseLogicalAnd()
		{
			var expr = ParseEquality();
			while (Match(TokenType.AndAnd))
			{
				var op = Previous();
				var right = ParseEquality();
				expr = new BinaryExpr { Left = expr, Right = right, Op = op.Lexeme, Line = op.Line, Column = op.Column };
			}
			return expr;
		}

		private Expr ParseEquality()
		{
			var expr = ParseComparison();
			while (Match(TokenType.EqualEqual, TokenType.BangEqual))
			{
				var op = Previous();
				var right = ParseComparison();
				expr = new BinaryExpr { Left = expr, Right = right, Op = op.Lexeme, Line = op.Line, Column = op.Column };
			}
			return expr;
		}

		private Expr ParseComparison()
		{
			var expr = ParseTerm();
			while (Match(TokenType.Less, TokenType.LessEqual, TokenType.Greater, TokenType.GreaterEqual))
			{
				var op = Previous();
				var right = ParseTerm();
				expr = new BinaryExpr { Left = expr, Right = right, Op = op.Lexeme, Line = op.Line, Column = op.Column };
			}
			return expr;
		}

		private Expr ParseTerm()
		{
			var expr = ParseFactor();
			while (Match(TokenType.Plus, TokenType.Minus))
			{
				var op = Previous();
				var right = ParseFactor();
				expr = new BinaryExpr { Left = expr, Right = right, Op = op.Lexeme, Line = op.Line, Column = op.Column };
			}
			return expr;
		}

		private Expr ParseFactor()
		{
			var expr = ParseUnary();
			while (Match(TokenType.Star, TokenType.Slash))
			{
				var op = Previous();
				var right = ParseUnary();
				expr = new BinaryExpr { Left = expr, Right = right, Op = op.Lexeme, Line = op.Line, Column = op.Column };
			}
			return expr;
		}

		private Expr ParseUnary()
		{
			if (Match(TokenType.Bang, TokenType.Minus))
			{
				var op = Previous();
				var right = ParseUnary();
				return new UnaryExpr { Op = op.Lexeme, Right = right, Line = op.Line, Column = op.Column };
			}

			return ParsePrimary();
		}

		private Expr ParsePrimary()
		{
			if (Match(TokenType.False))
			{
				return new LiteralExpr { Value = Value.FromBool(false), Line = Previous().Line, Column = Previous().Column };
			}
			if (Match(TokenType.True))
			{
				return new LiteralExpr { Value = Value.FromBool(true), Line = Previous().Line, Column = Previous().Column };
			}
			if (Match(TokenType.Number))
			{
				var token = Previous();
				if (token.Literal is int intValue)
				{
					return new LiteralExpr { Value = Value.FromInt(intValue), Line = token.Line, Column = token.Column };
				}
				return new LiteralExpr { Value = Value.FromDouble((double)token.Literal), Line = token.Line, Column = token.Column };
			}
			if (Match(TokenType.String))
			{
				var token = Previous();
				return new LiteralExpr { Value = Value.FromString((string)token.Literal), Line = token.Line, Column = token.Column };
			}
			if (Match(TokenType.Char))
			{
				var token = Previous();
				return new LiteralExpr { Value = Value.FromChar((char)token.Literal), Line = token.Line, Column = token.Column };
			}
			if (Match(TokenType.Identifier))
			{
				var name = Previous();
				var fullName = name.Lexeme;
				while (Match(TokenType.Dot))
				{
					var part = Consume(TokenType.Identifier, "Expected identifier after '.'.");
					fullName = fullName + "." + part.Lexeme;
				}

				if (Match(TokenType.LeftParen))
				{
					var call = new CallExpr { Name = fullName, Line = name.Line, Column = name.Column };
					if (!Check(TokenType.RightParen))
					{
						do
						{
							call.Args.Add(ParseExpression());
						}
						while (Match(TokenType.Comma));
					}
					Consume(TokenType.RightParen, "Expected ')'.");
					return call;
				}

				if (Match(TokenType.LeftBracket))
				{
					if (fullName.Contains("."))
					{
						throw Error(name, "Array access cannot be qualified.");
					}
					var index = ParseExpression();
					Consume(TokenType.RightBracket, "Expected ']'.");
					return new ArrayAccessExpr { Name = fullName, Index = index, Line = name.Line, Column = name.Column };
				}

				if (fullName.Contains("."))
				{
					throw Error(name, "Qualified name must be a function call.");
				}

				return new VariableExpr { Name = fullName, Line = name.Line, Column = name.Column };
			}
			if (Match(TokenType.New))
			{
				var elementType = ParseElementType();
				Consume(TokenType.LeftBracket, "Expected '['.");
				var lengthExpr = ParseExpression();
				Consume(TokenType.RightBracket, "Expected ']'.");
				return new ArrayCreationExpr
				{
					ElementType = elementType,
					Length = lengthExpr,
					Line = lengthExpr.Line,
					Column = lengthExpr.Column
				};
			}
			if (Match(TokenType.LeftParen))
			{
				var expr = ParseExpression();
				Consume(TokenType.RightParen, "Expected ')'.");
				return expr;
			}

			throw Error(Peek(), "Expected expression.");
		}

		private ValueType ParseType()
		{
			if (Match(TokenType.Int))
			{
				if (Match(TokenType.LeftBracket))
				{
					Consume(TokenType.RightBracket, "Expected ']'.");
					return ValueType.IntArray;
				}
				return ValueType.Int;
			}
			if (Match(TokenType.Double)) return ValueType.Double;
			if (Match(TokenType.Bool)) return ValueType.Bool;
			if (Match(TokenType.CharType))
			{
				if (Match(TokenType.LeftBracket))
				{
					Consume(TokenType.RightBracket, "Expected ']'.");
					return ValueType.CharArray;
				}
				return ValueType.Char;
			}
			if (Match(TokenType.StringType))
			{
				if (Match(TokenType.LeftBracket))
				{
					Consume(TokenType.RightBracket, "Expected ']'.");
					return ValueType.StringArray;
				}
				return ValueType.String;
			}
			if (Match(TokenType.Void)) return ValueType.Void;

			throw Error(Peek(), "Expected type.");
		}

		private ValueType ParseElementType()
		{
			if (Match(TokenType.Int)) return ValueType.Int;
			if (Match(TokenType.StringType)) return ValueType.String;
			if (Match(TokenType.Double)) return ValueType.Double;
			if (Match(TokenType.Bool)) return ValueType.Bool;
			if (Match(TokenType.CharType)) return ValueType.Char;
			throw Error(Peek(), "Expected element type.");
		}

		private bool IsTypeKeyword(Token token)
		{
			return token.Type == TokenType.Int || token.Type == TokenType.Double || token.Type == TokenType.Bool ||
			       token.Type == TokenType.CharType || token.Type == TokenType.StringType || token.Type == TokenType.Void;
		}

		private bool IsArrayAssignmentStart()
		{
			if (!Check(TokenType.Identifier))
			{
				return false;
			}

			var i = _index + 1;
			if (i >= _tokens.Count || _tokens[i].Type != TokenType.LeftBracket)
			{
				return false;
			}

			var depth = 0;
			for (; i < _tokens.Count; i++)
			{
				var type = _tokens[i].Type;
				if (type == TokenType.LeftBracket)
				{
					depth++;
				}
				else if (type == TokenType.RightBracket)
				{
					depth--;
					if (depth == 0)
					{
						i++;
						break;
					}
				}
			}

			if (depth != 0 || i >= _tokens.Count)
			{
				return false;
			}

			return _tokens[i].Type == TokenType.Equal;
		}

		private bool Match(params TokenType[] types)
		{
			foreach (var type in types)
			{
				if (Check(type))
				{
					Advance();
					return true;
				}
			}

			return false;
		}

		private Token Consume(TokenType type, string message)
		{
			if (Check(type)) return Advance();
			throw Error(Peek(), message);
		}

		private bool Check(TokenType type)
		{
			if (IsAtEnd())
			{
				return type == TokenType.Eof;
			}
			return Peek().Type == type;
		}

		private Token Advance()
		{
			if (!IsAtEnd()) _index++;
			return Previous();
		}

		private bool IsAtEnd() => Peek().Type == TokenType.Eof;

		private Token Peek() => _tokens[_index];

		private Token PeekNext()
		{
			if (_index + 1 >= _tokens.Count) return _tokens[_tokens.Count - 1];
			return _tokens[_index + 1];
		}

		private Token Previous() => _tokens[_index - 1];

		private InterpreterException Error(Token token, string message)
		{
			return new ParseException(message, token.Line, token.Column);
		}
	}

	internal enum TokenType
	{
		Identifier,
		Number,
		String,
		Char,
		Int,
		Double,
		Bool,
		CharType,
		StringType,
		Void,
		New,
		Break,
		Continue,
		If,
		Else,
		While,
		For,
		Return,
		True,
		False,
		LeftParen,
		RightParen,
		LeftBrace,
		RightBrace,
		LeftBracket,
		RightBracket,
		Semicolon,
		Comma,
		Dot,
		Plus,
		Minus,
		Star,
		Slash,
		Equal,
		EqualEqual,
		Bang,
		BangEqual,
		Less,
		LessEqual,
		Greater,
		GreaterEqual,
		AndAnd,
		OrOr,
		Eof
	}

	internal sealed class Token
	{
		public TokenType Type { get; }
		public string Lexeme { get; }
		public object Literal { get; }
		public int Line { get; }
		public int Column { get; }

		public Token(TokenType type, string lexeme, object literal, int line, int column)
		{
			Type = type;
			Lexeme = lexeme;
			Literal = literal;
			Line = line;
			Column = column;
		}
	}

	internal sealed class Lexer
	{
		private readonly string _source;
		private readonly List<Token> _tokens = new List<Token>();
		private int _start;
		private int _current;
		private int _line = 1;
		private int _column = 1;

		private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>(StringComparer.Ordinal)
		{
			{ "int", TokenType.Int },
			{ "double", TokenType.Double },
			{ "boolean", TokenType.Bool },
			{ "char", TokenType.CharType },
			{ "String", TokenType.StringType },
			{ "void", TokenType.Void },
			{ "new", TokenType.New },
			{ "break", TokenType.Break },
			{ "continue", TokenType.Continue },
			{ "if", TokenType.If },
			{ "else", TokenType.Else },
			{ "while", TokenType.While },
			{ "for", TokenType.For },
			{ "return", TokenType.Return },
			{ "true", TokenType.True },
			{ "false", TokenType.False }
		};

		public Lexer(string source)
		{
			_source = source ?? string.Empty;
		}

		public List<Token> Tokenize()
		{
			while (!IsAtEnd())
			{
				_start = _current;
				ScanToken();
			}

			_tokens.Add(new Token(TokenType.Eof, string.Empty, null, _line, _column));
			return _tokens;
		}

		private void ScanToken()
		{
			var c = Advance();
			switch (c)
			{
				case '(':
					AddToken(TokenType.LeftParen);
					break;
				case ')':
					AddToken(TokenType.RightParen);
					break;
				case '{':
					AddToken(TokenType.LeftBrace);
					break;
				case '}':
					AddToken(TokenType.RightBrace);
					break;
				case '[':
					AddToken(TokenType.LeftBracket);
					break;
				case ']':
					AddToken(TokenType.RightBracket);
					break;
				case ';':
					AddToken(TokenType.Semicolon);
					break;
				case ',':
					AddToken(TokenType.Comma);
					break;
				case '.':
					AddToken(TokenType.Dot);
					break;
				case '+':
					AddToken(TokenType.Plus);
					break;
				case '-':
					AddToken(TokenType.Minus);
					break;
				case '*':
					AddToken(TokenType.Star);
					break;
				case '/':
					if (Match('/'))
					{
						while (Peek() != '\n' && !IsAtEnd()) Advance();
					}
					else if (Match('*'))
					{
						SkipBlockComment();
					}
					else
					{
						AddToken(TokenType.Slash);
					}
					break;
				case '!':
					AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
					break;
				case '=':
					AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
					break;
				case '<':
					AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
					break;
				case '>':
					AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
					break;
				case '&':
					if (Match('&')) AddToken(TokenType.AndAnd);
					else throw new ParseException("Unexpected '&'.", _line, _column);
					break;
				case '|':
					if (Match('|')) AddToken(TokenType.OrOr);
					else throw new ParseException("Unexpected '|'.", _line, _column);
					break;
				case ' ':
				case '\r':
				case '\t':
					break;
				case '\n':
					_line++;
					_column = 1;
					break;
				case '"':
					String();
					break;
				case '\'':
					CharLiteral();
					break;
				default:
					if (char.IsDigit(c))
					{
						Number();
					}
					else if (IsAlpha(c))
					{
						Identifier();
					}
					else
					{
						throw new ParseException($"Unexpected character '{c}'.", _line, _column);
					}
					break;
			}
		}

		private void Identifier()
		{
			while (IsAlphaNumeric(Peek())) Advance();
			var text = _source.Substring(_start, _current - _start);
			if (Keywords.TryGetValue(text, out var type))
			{
				AddToken(type);
			}
			else
			{
				AddToken(TokenType.Identifier);
			}
		}

		private void Number()
		{
			while (char.IsDigit(Peek())) Advance();

			var isDouble = false;
			if (Peek() == '.' && char.IsDigit(PeekNext()))
			{
				isDouble = true;
				Advance();
				while (char.IsDigit(Peek())) Advance();
			}

			var text = _source.Substring(_start, _current - _start);
			if (isDouble)
			{
				var value = double.Parse(text, CultureInfo.InvariantCulture);
				AddToken(TokenType.Number, value);
			}
			else
			{
				var value = int.Parse(text, CultureInfo.InvariantCulture);
				AddToken(TokenType.Number, value);
			}
		}

		private void String()
		{
			var startLine = _line;
			var startColumn = _column;
			var buffer = new List<char>();
			while (!IsAtEnd() && Peek() != '"')
			{
				var c = Advance();
				if (c == '\\')
				{
					var next = Advance();
					switch (next)
					{
						case 'n':
							buffer.Add('\n');
							break;
						case 't':
							buffer.Add('\t');
							break;
						case '"':
							buffer.Add('"');
							break;
						case '\\':
							buffer.Add('\\');
							break;
						default:
							buffer.Add(next);
							break;
					}
				}
				else
				{
					if (c == '\n')
					{
						_line++;
						_column = 1;
					}
					buffer.Add(c);
				}
			}

			if (IsAtEnd())
			{
				throw new ParseException("Unterminated string.", startLine, startColumn);
			}

			Advance();
			AddToken(TokenType.String, new string(buffer.ToArray()));
		}

		private void CharLiteral()
		{
			var startLine = _line;
			var startColumn = _column;
			if (IsAtEnd() || Peek() == '\n')
			{
				throw new ParseException("Unterminated char literal.", startLine, startColumn);
			}

			char value;
			var c = Advance();
			if (c == '\\')
			{
				if (IsAtEnd())
				{
					throw new ParseException("Unterminated char literal.", startLine, startColumn);
				}
				var next = Advance();
				switch (next)
				{
					case 'n':
						value = '\n';
						break;
					case 't':
						value = '\t';
						break;
					case '\'':
						value = '\'';
						break;
					case '\\':
						value = '\\';
						break;
					default:
						value = next;
						break;
				}
			}
			else
			{
				value = c;
			}

			if (IsAtEnd() || Peek() != '\'')
			{
				throw new ParseException("Unterminated char literal.", startLine, startColumn);
			}
			Advance();
			AddToken(TokenType.Char, value);
		}

		private void SkipBlockComment()
		{
			while (!IsAtEnd())
			{
				if (Peek() == '*' && PeekNext() == '/')
				{
					Advance();
					Advance();
					return;
				}

				var c = Advance();
				if (c == '\n')
				{
					_line++;
					_column = 1;
				}
			}

			throw new ParseException("Unterminated block comment.", _line, _column);
		}

		private bool Match(char expected)
		{
			if (IsAtEnd()) return false;
			if (_source[_current] != expected) return false;
			_current++;
			_column++;
			return true;
		}

		private char Peek() => IsAtEnd() ? '\0' : _source[_current];
		private char PeekNext() => _current + 1 >= _source.Length ? '\0' : _source[_current + 1];

		private bool IsAtEnd() => _current >= _source.Length;

		private char Advance()
		{
			var c = _source[_current++];
			_column++;
			return c;
		}

		private void AddToken(TokenType type) => AddToken(type, null);

		private void AddToken(TokenType type, object literal)
		{
			var text = _source.Substring(_start, _current - _start);
			_tokens.Add(new Token(type, text, literal, _line, _column - ( _current - _start )));
		}

		private static bool IsAlpha(char c) => char.IsLetter(c) || c == '_';
		private static bool IsAlphaNumeric(char c) => IsAlpha(c) || char.IsDigit(c);
	}

	internal abstract class InterpreterException : Exception
	{
		public int Line { get; }
		public int Column { get; }

		protected InterpreterException(string message, int line, int column) : base(message)
		{
			Line = line;
			Column = column;
		}
	}

	internal sealed class ParseException : InterpreterException
	{
		public ParseException(string message, int line, int column) : base(message, line, column)
		{
		}
	}

	internal sealed class RuntimeException : InterpreterException
	{
		public RuntimeException(string message, int line, int column) : base(message, line, column)
		{
		}
	}

	internal sealed class BreakSignal : Exception
	{
		public int Line { get; }
		public int Column { get; }

		public BreakSignal(int line, int column)
		{
			Line = line;
			Column = column;
		}
	}

	internal sealed class ContinueSignal : Exception
	{
		public int Line { get; }
		public int Column { get; }

		public ContinueSignal(int line, int column)
		{
			Line = line;
			Column = column;
		}
	}

	internal sealed class ReturnSignal : Exception
	{
		public Value Value { get; }
		public int Line { get; }
		public int Column { get; }

		public ReturnSignal(Value value, int line, int column)
		{
			Value = value;
			Line = line;
			Column = column;
		}
	}
}
