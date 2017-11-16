using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace Markdown
{
    public class Md
	{

        public static Dictionary<char, string> SpecialSymbols = new Dictionary<char, string>()
        {
            ['\n'] = "<br>"
        };

		public string RenderToHtml(string markdown)
		{  
		    var index = 0;
		    if (!TryEscapeSymbol(markdown, ref index, out var lastChar)) return "";
		    var token = lastChar == '_' ? GetItalicField(markdown, index + 1) : null;
		    if (token != null && token.Value == "__")
		        token = GetBoldField(markdown, index+2);
            var builder = new StringBuilder(token == null ? lastChar.ToString() : token.Value);
		    index = token?.GetIndexNextToToken() ?? index + 1;
            for ( ; index < markdown.Length; index++)
		    {
		        if (markdown[index] == '_')
		        {
		            token = GetItalicField(markdown, index + 1);
		            if (token.Value == "__")
		            {
		                token = GetBoldField(markdown, index + 2);
		                index = token.GetIndexNextToToken() - 1;
		                builder.Append(token.Value);
                        continue;
		            }
		            index = token.GetIndexNextToToken() - 1;
		            builder.Append(token.Value);
		            continue;
                }
		        if (!TryEscapeSymbol(markdown, ref index, out var symbol)) continue;
		        builder.Append(TagOrSymbol(symbol));
            }
			return builder.ToString();
		}

	    public static Token GetBoldField(string original, int startIndex)
	    {
            var index = startIndex;
	        if (!TryEscapeSymbol(original, ref index, out var lastChar) || char.IsWhiteSpace(lastChar))
	            return new Token("__", startIndex, index - startIndex);
	        var token = lastChar == '_' ? GetItalicField(original, index + 1) : null;
            if (token != null && token.Value == "__") return new Token("____", startIndex, token.Length + 1);
            var builder = new StringBuilder(token == null ? lastChar.ToString() : token.Value);
	        index = token?.GetIndexNextToToken() ?? index + 1;
	        for (; index < original.Length; index++)
	        {
	            if (original[index] == '_')
	            {
	                token = GetItalicField(original, index + 1);
                    if (token.Value == "__" && !char.IsWhiteSpace(lastChar))
                        if (int.TryParse(builder.ToString(), out var num))
                            return new Token($"__{builder}__", startIndex, index - startIndex + 2);
                        else
                            return new Token(GetHtmlContainer(builder.ToString(), "strong"), startIndex, index-startIndex + 2);
	                index = token.GetIndexNextToToken() - 1;
	                builder.Append(token.Value);
	                lastChar = default(char);
	                continue;
	            }
	            if (!TryEscapeSymbol(original, ref index, out var symbol)) continue;
	            builder.Append(TagOrSymbol(symbol));
	            lastChar = symbol;
            }
            return new Token($"__{builder}", startIndex, original.Length - startIndex);
	    }

        public static Token GetItalicField(string original, int startIndex) {
            var index = startIndex;
            if (!TryEscapeSymbol(original, ref index, out var lastChar) || char.IsWhiteSpace(lastChar))
                return new Token("_", startIndex, index - startIndex);
            var builder = new StringBuilder(TagOrSymbol(lastChar));
            if (lastChar == '_') return new Token($"_{builder}", startIndex, 1);
            for (index = index + 1; index < original.Length; index++)
            {
                if (original[index] == '_' && !char.IsWhiteSpace(lastChar))
                    if (int.TryParse(builder.ToString(), out var num))
                        return new Token($"_{builder}_", startIndex, index - startIndex + 1);
                    else
                        return new Token(GetHtmlContainer(builder.ToString(), "em"), startIndex, index - startIndex + 1);
                if (!TryEscapeSymbol(original, ref index, out var symbol)) continue;
                builder.Append(TagOrSymbol(symbol));
                lastChar = symbol;
            } 
            return new Token($"_{builder}", startIndex, original.Length - startIndex);
        }

	    public static string TagOrSymbol(char symbol)
	    {
	        return SpecialSymbols.ContainsKey(symbol) ? SpecialSymbols[symbol] : symbol.ToString();
	    }

	    public static string GetHtmlContainer(string content, string tag)
	    {
	        return $"<{tag}>{content}</{tag}>";
	    }

	    public static bool TryEscapeSymbol(string original, ref int index, out char symbol)
	    {
	        symbol = default(char);
	        if (index >= original.Length) return false;
	        symbol = original[index];
            if (symbol == '\\')
	        {
	            if (index + 1 == original.Length) return false;
	            else symbol = original[++index];
	        }
	        return true;
	    }

    }

	[TestFixture]
	public class Md_ShouldRender
	{

	    [Test]
	    public void GetHtmlContainer_Should()
	    {
	        Md.GetHtmlContainer("123", "em").Should().Be(@"<em>123</em>");
	    }

	    [Test]
	    public void Escaping_ShouldEscape_WhenSlash()
	    {

	        var original = "\\a";
	        var index = 0;
	        Md.TryEscapeSymbol(original, ref index, out var symbol);

	        index.Should().Be(1);
	        symbol.Should().Be('a');

	    }

	    [Test]
	    public void Escaping_ShouldEscape_WhenChar()
	    {
	        var original = "a";
	        var index = 0;
	        Md.TryEscapeSymbol(original, ref index, out var symbol);

	        index.Should().Be(0);
	        symbol.Should().Be('a');
        }

	    [Test]
	    public void Escaping_ShouldEscapeWithAnyIndex()
	    {
	        var original = "b\\a";
	        var index = 1;
	        Md.TryEscapeSymbol(original, ref index, out var symbol);

	        index.Should().Be(2);
	        symbol.Should().Be('a');
        }

	    [Test]
	    public void Escaping_ShouldNotEscapting_WhenAfetrSlashNoSymbols()
	    {
	        var original = "b\\";
	        var index = 1;

            Md.TryEscapeSymbol(original, ref index, out var symbol).Should().BeFalse();

        }

	    [Test]
	    public void Escaping_ShouldNotEscape_WhenIndexOutRange()
	    {
	        var original = "b\\a";
	        var index = 3;

	        Md.TryEscapeSymbol(original, ref index, out var symbol).Should().BeFalse();
        }

	    [Test]
	    public void TagOrSymbol_ShouldReturnTag()
	    {
	        var symbol = '\n';

	        Md.TagOrSymbol(symbol).Should().Be("<br>");
	    }

	    [Test]
	    public void TagOrSymbol_ShouldReturnSymbol()
	    {
	        var symbol = 'a';

	        Md.TagOrSymbol(symbol).Should().Be("a");
        }

	    [TestCase("", "_", TestName = "ShouldReturnUnderscore_WhenEmptyLine")]
        [TestCase("qwerty_", "<em>qwerty</em>", TestName = "ShouldReturnContainer_WhenCorrectLine")]
	    [TestCase("qwerty _", "_qwerty _", TestName = "ShouldReturnLine_WhenInvalidLine")]
	    [TestCase("\\_", "__", TestName = "WithEscape_ShouldEscape")]
	    [TestCase("a\n", "_a<br>", TestName = "WithSpecialSymbol_ShouldPutTag")]
	    [TestCase("_a", "__", TestName = "ShouldReturnLine_WhenFirstSymbolIsUnderscore")]
	    [TestCase(" a_", "_", TestName = "ShouldReturnLine_WhenFirstSymbolIsWhite")]
        public void ItalicField(string original, string expected)
	    {
	        Md.GetItalicField(original, 0).Value.Should().Be(expected);
        }

	    [TestCase("", "__", TestName = "ShouldReturnDoubleUnderscore_WhenEmptyLine")]
        [TestCase(" ", "__", TestName = "ShouldReturnLine_WhenFirstSymbolIsWhite")]
	    [TestCase("__", "____", TestName = "ShouldReturnLineWhen_StartWithDoubleUnderscore")]
	    [TestCase("a c", "__a c", TestName = "ShouldReturnLine_WhenNoEndUnderscore")]
	    [TestCase("\\\\ b__", "<strong>\\ b</strong>", TestName = "WithEscaping_ShouldEscape")]
	    [TestCase("a _b_c__", "<strong>a <em>b</em>c</strong>", TestName = "ShouldReturnToken_WhenContainsItalicField")]
        public void BoldField(string original, string expected)
	    {
	        Md.GetBoldField(original, 0).Value.Should().Be(expected);
        }

	    private Md md;

	    [SetUp]
	    public void SetUp()
	    {
	        md = new Md();
	    }

	    [TestCase("","", TestName = "ShouldReturnEmpty_WhenEmptyLine")]
	    [TestCase("abc", "abc", TestName = "ShouldReturnSameLine_WhenNoFields")]
	    [TestCase("\\\\abc", "\\abc", TestName = "WithEscaping_ShouldEscape")]
	    [TestCase("ab __c__", "ab <strong>c</strong>", TestName = "WithBoldField_ShouldContainsContainerStrong")]
	    [TestCase("ab _c_", "ab <em>c</em>", TestName = "WithItalicField_ShouldContainsContainerEm")]
	    [TestCase("__ab _c___", "<strong>ab <em>c</em></strong>", TestName = "ShouldContainsContainerEmInsideStrong")]
	    [TestCase("_ab __c___", "<em>ab _</em>c___", TestName = "ShouldNotContainsContainerStrongInsideEm")]
        public void RenderToHtml(string original, string expected)
	    {
	        md.RenderToHtml(original).Should().Be(expected);
	    }

	}
}