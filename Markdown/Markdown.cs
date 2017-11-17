using System.Collections.Generic;
using System.Text;

namespace Markdown
{
	public class Markdown
	{
		public static Dictionary<char, string> SpecialSymbols = new Dictionary<char, string>()
		{
			['\n'] = "<br>"
		};

		private string markdown;
		private int index;
		private char previousSymbol = default(char);

		private bool TryGetNextSymbol(out char symbol)
		{
			var result =  TryEscapeSymbol(markdown, ref index, out symbol);
			index++;
			return result;
		}

		public void SetMarkdown(string markdown)
		{
			this.markdown = markdown;
			index = 0;
		}

		public string RenderToHtml(string markdown)
		{
			SetMarkdown(markdown);
			var builder = new StringBuilder();
			while (TryGetNextSymbol(out var symbol))
			{
				if (symbol == '_')
				{
					var field = GetItalicField();
					if (field == "__") field = GetBoldField();
					builder.Append(field);
					continue;
				}
				builder.Append(TagOrSymbol(symbol));
				previousSymbol = symbol;
			}
			return builder.ToString();
		}

		public string GetItalicField()
		{
			if (!TryGetNextSymbol(out var firstSymbol)) return "_";
			if (firstSymbol == '_') return "__";
			previousSymbol = firstSymbol;
			if (char.IsWhiteSpace(firstSymbol)) return "_ ";
			var builder = new StringBuilder(firstSymbol.ToString());
			while (TryGetNextSymbol(out var symbol))
			{
				if (symbol == '_' && !char.IsWhiteSpace(previousSymbol))
					return int.TryParse(builder.ToString(), out var _) ? $"_{builder}_" 
						: builder.ToHtmlContainer("em");	
				builder.Append(TagOrSymbol(symbol));
				previousSymbol = symbol;
			}
			return $"_{builder}";
		}

		public string GetBoldField()
		{
			if (!TryGetNextSymbol(out var firstSymbol)) return "__";
			var firstField = firstSymbol == '_' ? GetItalicField() : firstSymbol.ToString();
			if (firstSymbol != '_') previousSymbol = firstSymbol;
			if (char.IsWhiteSpace(firstSymbol)) return "__ ";
			var builder = new StringBuilder(firstField);
			while (TryGetNextSymbol(out var symbol))
			{
				var nextField = symbol == '_' ? GetItalicField() : TagOrSymbol(symbol);
				if (nextField == "__" && !char.IsWhiteSpace(previousSymbol))
					return int.TryParse(builder.ToString(), out var _) ? $"__{builder}__"
						: builder.ToHtmlContainer("strong");
				builder.Append(nextField);
				previousSymbol = symbol;
			}
			return $"__{builder}";
		}

		public static string TagOrSymbol(char symbol)
		{
			return SpecialSymbols.ContainsKey(symbol) ? SpecialSymbols[symbol] : symbol.ToString();
		}

		public static bool TryEscapeSymbol(string original, ref int index, out char symbol)
		{
			symbol = default(char);
			if (index >= original.Length) return false;
			symbol = original[index];
			if (symbol != '\\') return true;
			if (index + 1 == original.Length) return false;
			symbol = original[++index];
			return true;
		}
	}
}