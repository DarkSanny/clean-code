using System;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;

namespace Markdown
{
	[TestFixture]
	public class Markdown_ShouldRender
	{

		private Markdown md;

		[SetUp]
		public void SetUp()
		{
			md = new Markdown();
		}

		[TestCase("", "", TestName = "Empty_WhenMarkdownEmpty")]
		[TestCase("a", "a", TestName = "SameLine_WhenMarkdownHaveNoFields")]
		[TestCase("a_", "a_", TestName = "SameLine_WhenMarkdownHaveNoStartUnderscore")]
		[TestCase("_a_", "<em>a</em>", TestName = "ContainerEm_WhenItalicField")]
		[TestCase("__a__", "<strong>a</strong>", TestName = "ContainerStrong_WhenBoldField")]
		[TestCase("___a___", "<strong><em>a</em></strong>", TestName = "ContainerEmInsideStrong_WhenItalicInsideBoldField")]
		[TestCase("_a __b__ c_", "<em>a __b</em>_ c_", TestName = "StrongShouldNotBeInsideEm")]
		[TestCase("\\\\a", "\\a", TestName = "Escape symbol")]
		[TestCase("\\__a__", "_<em>a</em>_", TestName = "Escape metasymbol")]
		[TestCase("_14_", "_14_", TestName = "SameLine_WhenOnlyDigitsInsideItalicField")]
		[TestCase("__14__", "__14__", TestName = "SameLine_WhenOnlyDigitsInsideBoldField")]
		[TestCase("___14___", "<strong>_14_</strong>", TestName = "ContainerStrong_InsideTripleUnderscoreOnlyDigits")]
		[TestCase("ab\nc", "ab<br>c", TestName = "Tag_WhenSpecialSymbol")]
		[TestCase("ab _c_", "ab <em>c</em>", TestName = "ContainContainerEm_WhenLineHaveItalicField")]
		[TestCase("ab __c__", "ab <strong>c</strong>", TestName = "ContainContainerEm_WhenLineHaveBoldField")]
		[TestCase("_a _", "_a _", TestName = "SameLine_WhenNoClosingUnderscore")]
		[TestCase("\\", "\\", TestName = "NotEscaping_WhenNoSymbolAfterSlash")]
		[TestCase("a_b_c", "a_b_c", TestName = "SameLine_WhenUnderscoreInsideWord")]
		public void MarkdownParser_Should(string original, string expected)
		{
			md.RenderToHtml(original).Should().Be(expected);
		}

	}
}