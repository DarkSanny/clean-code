using FluentAssertions;
using NUnit.Framework;

namespace Markdown
{
	[TestFixture]
	public class Markdown_ShouldRender
	{

		[Test]
		public void Escaping_ShouldEscape_WhenSlash()
		{
			var original = "\\a";
			var index = 0;
			Markdown.TryEscapeSymbol(original, ref index, out var symbol);

			index.Should().Be(1);
			symbol.Should().Be('a');
		}

		[Test]
		public void Escaping_ShouldEscape_WhenChar()
		{
			var original = "a";
			var index = 0;
			Markdown.TryEscapeSymbol(original, ref index, out var symbol);

			index.Should().Be(0);
			symbol.Should().Be('a');
		}

		[Test]
		public void Escaping_ShouldEscapeWithAnyIndex()
		{
			var original = "b\\a";
			var index = 1;
			Markdown.TryEscapeSymbol(original, ref index, out var symbol);

			index.Should().Be(2);
			symbol.Should().Be('a');
		}

		[Test]
		public void Escaping_ShouldNotEscapting_WhenAfetrSlashNoSymbols()
		{
			var original = "b\\";
			var index = 1;

			Markdown.TryEscapeSymbol(original, ref index, out var symbol).Should().BeFalse();
		}

		[Test]
		public void Escaping_ShouldNotEscape_WhenIndexOutRange()
		{
			var original = "b\\a";
			var index = 3;

			Markdown.TryEscapeSymbol(original, ref index, out var symbol).Should().BeFalse();
		}

		[Test]
		public void TagOrSymbol_ShouldReturnTag()
		{
			var symbol = '\n';

			Markdown.TagOrSymbol(symbol).Should().Be("<br>");
		}

		[Test]
		public void TagOrSymbol_ShouldReturnSymbol()
		{
			var symbol = 'a';

			Markdown.TagOrSymbol(symbol).Should().Be("a");
		}

		private Markdown md;

		[SetUp]
		public void SetUp()
		{
			md = new Markdown();
		}

		[TestCase("", "", TestName = "ShouldReturnEmpty_WhenEmptyLine")]
		[TestCase("abc", "abc", TestName = "ShouldReturnSameLine_WhenNoFields")]
		[TestCase("\\\\abc", "\\abc", TestName = "WithEscaping_ShouldEscape")]
		[TestCase("ab __c__", "ab <strong>c</strong>", TestName = "WithBoldField_ShouldContainsContainerStrong")]
		[TestCase("ab _c_", "ab <em>c</em>", TestName = "WithItalicField_ShouldContainsContainerEm")]
		[TestCase("__ab _c___", "<strong>ab <em>c</em></strong>", TestName = "ShouldContainsContainerEmInsideStrong")]
		[TestCase("_ab __c___", "<em>ab _</em>c___", TestName = "ShouldNotContainsContainerStrongInsideEm")]
		[TestCase("ab_c_", "ab<em>c</em>", TestName = "Test")]
		public void RenderToHtml(string original, string expected)
		{
			md.RenderToHtml(original).Should().Be(expected);
		}

		[TestCase("", "_", TestName = "ShouldReturnUnderscore_WhenEmptyLine")]
		[TestCase("qwerty_", "<em>qwerty</em>", TestName = "ShouldReturnContainer_WhenCorrectLine")]
		[TestCase("qwerty _", "_qwerty _", TestName = "ShouldReturnLine_WhenInvalidLine")]
		[TestCase("\\_", "__", TestName = "WithEscape_ShouldEscape")]
		[TestCase("a\n", "_a<br>", TestName = "WithSpecialSymbol_ShouldPutTag")]
		[TestCase("_a", "__", TestName = "ShouldReturnLine_WhenFirstSymbolIsUnderscore")]
		[TestCase(" a_", "_ ", TestName = "ShouldReturnLine_WhenFirstSymbolIsWhite")]
		[TestCase("5_", "_5_", TestName = "ShouldReturnline_WhenOnlyDigits")]
		public void ItalicField(string original, string expected)
		{
			md.SetMarkdown(original);

			md.GetItalicField().Should().Be(expected);
		}

		[TestCase("", "__", TestName = "ShouldReturnDoubleUnderscore_WhenEmptyLine")]
		[TestCase(" ", "__ ", TestName = "ShouldReturnLine_WhenFirstSymbolIsWhite")]
		[TestCase("__", "____", TestName = "ShouldReturnLineWhen_StartWithDoubleUnderscore")]
		[TestCase("a c", "__a c", TestName = "ShouldReturnLine_WhenNoEndUnderscore")]
		[TestCase("\\\\ b__", "<strong>\\ b</strong>", TestName = "WithEscaping_ShouldEscape")]
		[TestCase("a _b_c__", "<strong>a <em>b</em>c</strong>", TestName = "ShouldReturnToken_WhenContainsItalicField")]
		[TestCase("5__", "__5__", TestName = "ShouldReturnline_WhenOnlyDigits")]
		public void BoldField(string original, string expected)
		{
			md.SetMarkdown(original);

			md.GetBoldField().Should().Be(expected);
		}

	}
}