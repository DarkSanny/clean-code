using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markdown
{
	public class Fsm
	{

		private class State
		{
			
			public Action<State> ActiveState { get; set; }
			public Stack<StringBuilder> StackFields { get; }
			public string MarkdownLine { get; }
			public int Index { get; set; }
			public char? PreviousSymbol { get; set; }
			public string Result { get; set; }

			public State(string markdownLine, Action<State> startState)
			{
				MarkdownLine = markdownLine;
				ActiveState = startState;
				StackFields = new Stack<StringBuilder>();
				StackFields.Push(new StringBuilder());
			}

		}


		private static Dictionary<char, string> specialSymbols = new Dictionary<char, string>()
		{
			['\n'] = "<br>"
		};

		public static string GetResult(string markdown)
		{
			var currentState = new State(markdown, AddNextSymbolInSimpleField);
			while (currentState.Result == null)
				Update(currentState);
			return currentState.Result;
		}

		private static void AddNextSymbolInSimpleField(State currentState)
		{
			var isWasEscaped = TryGetNextEscapedSymbol(currentState, out var currentSymbol);
			if (isWasEscaped && currentSymbol != null)
				currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			else if (currentSymbol == null)
			{
				if (currentState.StackFields.Count == 1)
					currentState.Result = currentState.StackFields.Pop().ToString();
				else
					ConcatStackFields(currentState, currentState.StackFields.Pop().ToString());
			}
			else if (currentSymbol == '_')
			{
				if (IsShouldStartField(currentState, 2))
				{
					SetState(currentState, AddNextSymbolInBold);
					currentState.Index++;
					currentState.StackFields.Push(new StringBuilder());
				}
				else if (IsShouldStartField(currentState, 1))
				{
					SetState(currentState, AddNextSymbolInItalic);
					currentState.StackFields.Push(new StringBuilder());
				}
				else currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			}
			else currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			currentState.PreviousSymbol = currentSymbol;
		}

		private static void AddNextSymbolInItalic(State currentState)
		{
			var isWasEscaped = TryGetNextEscapedSymbol(currentState, out var currentSymbol);
			if (isWasEscaped && currentSymbol != null)
				currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			else if (currentSymbol == null)
			{
				SetState(currentState, AddNextSymbolInSimpleField);
				ConcatStackFields(currentState, "_" + currentState.StackFields.Pop());
			}
			else if (currentSymbol == '_' && IsShouldCloseField(currentState, 1))
			{
				SetState(currentState, AddNextSymbolInSimpleField);
				var currentField = currentState.StackFields.Pop();
				ConcatStackFields(currentState, int.TryParse(currentField.ToString(), out var _)
					? $"_{currentField}_"
					: currentField.ToHtmlContainer("em"));
			}
			else currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			currentState.PreviousSymbol = currentSymbol;
		}

		private static void AddNextSymbolInBold(State currentState)
		{
			var isWasEscaped = TryGetNextEscapedSymbol(currentState, out var currentSymbol);
			if (isWasEscaped && currentSymbol != null)
				currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			else if (currentSymbol == null)
			{
				SetState(currentState, AddNextSymbolInSimpleField);
				ConcatStackFields(currentState, "__" + currentState.StackFields.Pop());
			}
			else if (currentSymbol == '_')
			{
				if (IsShouldCloseField(currentState, 2))
				{
					SetState(currentState, AddNextSymbolInSimpleField);
					currentState.Index++;
					var currentField = currentState.StackFields.Pop();
					ConcatStackFields(currentState, int.TryParse(currentField.ToString(), out var _)
						? $"__{currentField}__"
						: currentField.ToHtmlContainer("strong"));
				}
				else if (IsShouldStartField(currentState, 1))
				{
					SetState(currentState, AddNextSymbolInItalicInsideBold);
					currentState.StackFields.Push(new StringBuilder());
				}
				else currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			}
			else currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			currentState.PreviousSymbol = currentSymbol;
		}

		private static void AddNextSymbolInItalicInsideBold(State currentState)
		{
			var isWasEscaped = TryGetNextEscapedSymbol(currentState, out var currentSymbol);
			if (isWasEscaped && currentSymbol != null)
				currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			else if (currentSymbol == null)
			{
				SetState(currentState, AddNextSymbolInBold);
				ConcatStackFields(currentState, "_" + currentState.StackFields.Pop());
			}
			else if (currentSymbol == '_' && IsShouldCloseField(currentState, 1))
			{
				SetState(currentState, AddNextSymbolInBold);
				var currentField = currentState.StackFields.Pop();
				ConcatStackFields(currentState, int.TryParse(currentField.ToString(), out var _)
					? $"_{currentField}_"
					: currentField.ToHtmlContainer("em"));
			}
			else currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			currentState.PreviousSymbol = currentSymbol;
		}

		private static void SetState(State currentState, Action<State> state)
		{
			currentState.ActiveState = state;
		}

		private static void Update(State currentState)
		{
			currentState.ActiveState?.Invoke(currentState);
		}

		private static void ConcatStackFields(State currentState, string lastField)
		{
			currentState.StackFields.Peek().Append(lastField);
		}

		private static bool IsShouldStartField(State currentState, int shift)
		{
			var previousSymbolIsCorrect = currentState.PreviousSymbol == null
				|| char.IsWhiteSpace(currentState.PreviousSymbol.Value)
				|| currentState.PreviousSymbol == '_';
			if (previousSymbolIsCorrect)
			{
				char? nextSymbol = null;
				for (var i = 1; i < shift; i++)
				{
					nextSymbol = TryLookNextSymbol(currentState, i);
					if (nextSymbol != '_') return false;
				}
				nextSymbol = TryLookNextSymbol(currentState, shift);
				if (nextSymbol != null && !char.IsWhiteSpace(nextSymbol.Value)) return true;
			}
			return false;
		}

		private static bool IsShouldCloseField(State currentState, int shift)
		{
			var previousSymbolIsCorrect = currentState.PreviousSymbol != null 
				&& !char.IsWhiteSpace(currentState.PreviousSymbol.Value);
			if (previousSymbolIsCorrect)
			{
				char? nextSymbol = null;
				for (var i = 1; i < shift; i++)
				{
					nextSymbol = TryLookNextSymbol(currentState, i);
					if (nextSymbol != '_') return false;
				}
				nextSymbol = TryLookNextSymbol(currentState, shift);
				if (nextSymbol == null || char.IsWhiteSpace(nextSymbol.Value) || nextSymbol == '_') return true;
			}
			return false;
		}

		private static char? TryLookNextSymbol(State currentState, int shift)
		{
			if (shift <= 0) throw new ArgumentException();
			var currentIndex = currentState.Index;
			char? result = null;
			for (var i = 0; i < shift; i++)
			{
				TryGetNextEscapedSymbol(currentState, out result);
			}
			currentState.Index = currentIndex;
			return result;
		}

		private static bool TryGetNextEscapedSymbol(State currentState, out char? symbol)
		{
			var currentSymbol = TryEscapeSymbol(currentState, out symbol);
			currentState.Index++;
			return currentSymbol;
		}

		private static bool TryEscapeSymbol(State currentState, out char? symbol)
		{
			symbol = null;
			if (currentState.Index >= currentState.MarkdownLine.Length) return false;
			if (currentState.MarkdownLine[currentState.Index] == '\\' 
				&& currentState.Index + 1 < currentState.MarkdownLine.Length)
			{
				symbol = currentState.MarkdownLine[++currentState.Index];
				return true;
			}
			symbol = currentState.MarkdownLine[currentState.Index];
			return false;
		}

		private static string TagOrSymbol(char symbol)
		{
			return specialSymbols.ContainsKey(symbol) ? specialSymbols[symbol] : symbol.ToString();
		}

	}
}
