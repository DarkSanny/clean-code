using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markdown
{
	public class Fsm
	{

		public string Result = null;

		private Action activeState;
		private Stack<StringBuilder> stackFields;

		private string markdown;
		private int index;
		private char? previousSymbol;

		public static Dictionary<char, string> SpecialSymbols = new Dictionary<char, string>()
		{
			['\n'] = "<br>"
		};

		public void SetUp(string markdown)
		{
			activeState = SimpleField;
			this.markdown = markdown;
			index = 0;
			previousSymbol = null;
			stackFields = new Stack<StringBuilder>();
			stackFields.Push(new StringBuilder());
		}

		private void SimpleField()
		{
			var isWasEscaped = TryGetNextEscapedSymbol(out var currentSymbol);
			if (isWasEscaped && currentSymbol != null)
				stackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			else if (currentSymbol == null)
			{
				if (stackFields.Count == 1) Result = stackFields.Pop().ToString();
				else ConcatStackFields(stackFields.Pop().ToString());
			}
			else if (currentSymbol == '_')
			{
				if (IsShouldStartField(2))
				{
					SetState(BoldField);
					index++;
					stackFields.Push(new StringBuilder());
				}
				else if (IsShouldStartField(1))
				{
					SetState(ItailicField);
					stackFields.Push(new StringBuilder());
				}
				else stackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			}
			else stackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			previousSymbol = currentSymbol;
		}

		private void ItailicField()
		{
			var isWasEscaped = TryGetNextEscapedSymbol(out var currentSymbol);
			if (isWasEscaped && currentSymbol != null)
				stackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			else if (currentSymbol == null)
			{
				SetState(SimpleField);
				ConcatStackFields("_" + stackFields.Pop());
			}
			else if (currentSymbol == '_' && IsShouldCloseField(1))
			{
				SetState(SimpleField);
				var currentField = stackFields.Pop();
				ConcatStackFields(int.TryParse(currentField.ToString(), out var _)
					? $"_{currentField}_"
					: currentField.ToHtmlContainer("em"));
			}
			else stackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			previousSymbol = currentSymbol;
		}

		private void BoldField()
		{
			var isWasEscaped = TryGetNextEscapedSymbol(out var currentSymbol);
			if (isWasEscaped && currentSymbol != null)
				stackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			else if (currentSymbol == null)
			{
				SetState(SimpleField);
				ConcatStackFields("__" + stackFields.Pop());
			}
			else if (currentSymbol == '_')
			{
				if (IsShouldCloseField(2))
				{
					SetState(SimpleField);
					index++;
					var currentField = stackFields.Pop();
					ConcatStackFields(int.TryParse(currentField.ToString(), out var _)
						? $"__{currentField}__"
						: currentField.ToHtmlContainer("strong"));
				}
				else if (IsShouldStartField(1))
				{
					SetState(ItailcFieldInsideBold);
					stackFields.Push(new StringBuilder());
				}
				else stackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			}
			else stackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			previousSymbol = currentSymbol;
		}

		private void ItailcFieldInsideBold()
		{
			var isWasEscaped = TryGetNextEscapedSymbol(out var currentSymbol);
			if (isWasEscaped && currentSymbol != null)
				stackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			else if (currentSymbol == null)
			{
				SetState(BoldField);
				ConcatStackFields("_" + stackFields.Pop());
			}
			else if (currentSymbol == '_' && IsShouldCloseField(1))
			{
				SetState(BoldField);
				var currentField = stackFields.Pop();
				ConcatStackFields(int.TryParse(currentField.ToString(), out var _)
					? $"_{currentField}_"
					: currentField.ToHtmlContainer("em"));
			}
			else stackFields.Peek().Append(TagOrSymbol(currentSymbol.Value));
			previousSymbol = currentSymbol;
		}

		private void SetState(Action state)
		{
			activeState = state;
		}

		public void Updete()
		{
			activeState?.Invoke();
		}

		private void ConcatStackFields(string lastField)
		{
			stackFields.Peek().Append(lastField);
		}

		private bool IsShouldStartField(int shift)
		{
			var previousSymbolIsCorrect = previousSymbol == null
				|| char.IsWhiteSpace(previousSymbol.Value)
				|| previousSymbol == '_';
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
			var previousSymbolIsCorrect = previousSymbol != null && !char.IsWhiteSpace(previousSymbol.Value);
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
			var currentIndex = index;
			char? result = null;
			for (var i = 0; i < shift; i++)
			{
				TryGetNextEscapedSymbol(out result);
			}
			index = currentIndex;
			return result;
		}

		private bool TryGetNextEscapedSymbol(out char? symbol)
		{
			var currentSymbol = TryEscapeSymbol(out symbol);
			index++;
			return currentSymbol;
		}

		private bool TryEscapeSymbol(out char? symbol)
		{
			symbol = null;
			if (index >= markdown.Length) return false;
			if (markdown[index] == '\\' && index + 1 < markdown.Length)
			{
				symbol = markdown[++index];
				return true;
			}
			symbol = markdown[index];
			return false;
		}

		public static string TagOrSymbol(char symbol)
		{
			return SpecialSymbols.ContainsKey(symbol) ? SpecialSymbols[symbol] : symbol.ToString();
		}

	}
}
