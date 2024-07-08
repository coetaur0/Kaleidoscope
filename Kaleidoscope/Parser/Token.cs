namespace Kaleidoscope.Parser;

/// <summary>
/// A token's kind.
/// </summary>
internal enum TokenKind
{
    Def,
    Extern,
    If,
    Then,
    Else,
    For,
    In,
    Identifier,
    Number,
    Equal,
    Less,
    Plus,
    Minus,
    Times,
    LeftParen,
    RightParen,
    Comma,
    Eof,
    Unknown
}

/// <summary>
/// A lexical token.
/// </summary>
/// <param name="Kind">The token's kind.</param>
/// <param name="Range">The token's source range.</param>
internal readonly record struct Token(TokenKind Kind, Syntax.Range Range);