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
			
			public Action ActiveState { get; set; }
			public Stack<StringBuilder> StackFields { get; }
			public string MarkdownLine { get; }
			public int Index { get; set; }
			public char? PreviousSymbol { get; set; }
			public string Result { get; set; }

			public State(string markdownLine, Action startState)
			{
				MarkdownLine = markdownLine;
				ActiveState = startState;
				StackFields = new Stack<StringBuilder>();
				StackFields.Push(new StringBuilder());
			}

		}

		private State currentState;

		private static Dictionary<char, string> specialSymbols = new Dictionary<char, string>()
		{
			['\n'] = "<br>"
		};

		public string GetResult(string markdown)
		{
			currentState = new State(markdown, AddNextSymbolInSimpleField);
			while (currentState.Result == null)
				Update();
			return currentState.Result;
		}

		private void AddNextSymbolInSimpleField()
		{
			var isWasEscaped = TryGetNextEscapedSymbol(out var currentSymbol);
			if (isWasEscaped && currentSymbol != null)
				currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			else if (currentSymbol == null)
			{
				if (currentState.StackFields.Count == 1)
					currentState.Result = currentState.StackFields.Pop().ToString();
				else
					ConcatStackFields(currentState.StackFields.Pop().ToString());
			}
			else if (currentSymbol == '_')
			{
				if (IsShouldStartField(2))
				{
					SetState(AddNextSymbolInBold);
					currentState.Index++;
					currentState.StackFields.Push(new StringBuilder());
				}
				else if (IsShouldStartField(1))
				{
					SetState(AddNextSymbolInItalic);
					currentState.StackFields.Push(new StringBuilder());
				}
				else currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			}
			else currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			currentState.PreviousSymbol = currentSymbol;
		}

		private void AddNextSymbolInItalic()
		{
			var isWasEscaped = TryGetNextEscapedSymbol(out var currentSymbol);
			if (isWasEscaped && currentSymbol != null)
				currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			else if (currentSymbol == null)
			{
				SetState(AddNextSymbolInSimpleField);
				ConcatStackFields("_" + currentState.StackFields.Pop());
			}
			else if (currentSymbol == '_' && IsShouldCloseField(1))
			{
				SetState(AddNextSymbolInSimpleField);
				var currentField = currentState.StackFields.Pop();
				ConcatStackFields(int.TryParse(currentField.ToString(), out var _)
					? $"_{currentField}_"
					: currentField.ToHtmlContainer("em"));
			}
			else currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			currentState.PreviousSymbol = currentSymbol;
		}

		private void AddNextSymbolInBold()
		{
			var isWasEscaped = TryGetNextEscapedSymbol(out var currentSymbol);
			if (isWasEscaped && currentSymbol != null)
				currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			else if (currentSymbol == null)
			{
				SetState(AddNextSymbolInSimpleField);
				ConcatStackFields("__" + currentState.StackFields.Pop());
			}
			else if (currentSymbol == '_')
			{
				if (IsShouldCloseField(2))
				{
					SetState(AddNextSymbolInSimpleField);
					currentState.Index++;
					var currentField = currentState.StackFields.Pop();
					ConcatStackFields(int.TryParse(currentField.ToString(), out var _)
						? $"__{currentField}__"
						: currentField.ToHtmlContainer("strong"));
				}
				else if (IsShouldStartField(1))
				{
					SetState(AddNextSymbolInItalicInsideBold);
					currentState.StackFields.Push(new StringBuilder());
				}
				else currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			}
			else currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			currentState.PreviousSymbol = currentSymbol;
		}

		private void AddNextSymbolInItalicInsideBold()
		{
			var isWasEscaped = TryGetNextEscapedSymbol(out var currentSymbol);
			if (isWasEscaped && currentSymbol != null)
				currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			else if (currentSymbol == null)
			{
				SetState(AddNextSymbolInBold);
				ConcatStackFields("_" + currentState.StackFields.Pop());
			}
			else if (currentSymbol == '_' && IsShouldCloseField(1))
			{
				SetState(AddNextSymbolInBold);
				var currentField = currentState.StackFields.Pop();
				ConcatStackFields(int.TryParse(currentField.ToString(), out var _)
					? $"_{currentField}_"
					: currentField.ToHtmlContainer("em"));
			}
			else currentState.StackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			currentState.PreviousSymbol = currentSymbol;
		}

		private void SetState(Action state)
		{
			currentState.ActiveState = state;
		}

		private void Update()
		{
			currentState.ActiveState?.Invoke();
		}

		private void ConcatStackFields(string lastField)
		{
			currentState.StackFields.Peek().Append(lastField);
		}

		private bool IsShouldStartField(int shift)
		{
			var previousSymbolIsCorrect = currentState.PreviousSymbol == null
				|| char.IsWhiteSpace(currentState.PreviousSymbol.Value)
				|| currentState.PreviousSymbol == '_';
			if (previousSymbolIsCorrect)
			{
				char? nextSymbol = null;
				for (var i = 1; i < shift; i++)
				{
					nextSymbol = TryLookNextSymbol(i);
					if (nextSymbol != '_') return false;
				}
				nextSymbol = TryLookNextSymbol(shift);
				if (nextSymbol != null && !char.IsWhiteSpace(nextSymbol.Value)) return true;
			}
			return false;
		}

		private bool IsShouldCloseField(int shift)
		{
			var previousSymbolIsCorrect = currentState.PreviousSymbol != null 
				&& !char.IsWhiteSpace(currentState.PreviousSymbol.Value);
			if (previousSymbolIsCorrect)
			{
				char? nextSymbol = null;
				for (var i = 1; i < shift; i++)
				{
					nextSymbol = TryLookNextSymbol(i);
					if (nextSymbol != '_') return false;
				}
				nextSymbol = TryLookNextSymbol(shift);
				if (nextSymbol == null || char.IsWhiteSpace(nextSymbol.Value) || nextSymbol == '_') return true;
			}
			return false;
		}

		private char? TryLookNextSymbol(int shift)
		{
			if (shift <= 0) throw new ArgumentException();
			var currentIndex = currentState.Index;
			char? result = null;
			for (var i = 0; i < shift; i++)
			{
				TryGetNextEscapedSymbol(out result);
			}
			currentState.Index = currentIndex;
			return result;
		}

		private bool TryGetNextEscapedSymbol(out char? symbol)
		{
			var currentSymbol = TryEscapeSymbol(out symbol);
			currentState.Index++;
			return currentSymbol;
		}

		private bool TryEscapeSymbol(out char? symbol)
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
