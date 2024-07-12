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
    Binary,
    Unary,
    Identifier,
    Number,
    Op,
    LeftParen,
    RightParen,
    Comma,
    Eof
}

/// <summary>
/// A lexical token.
/// </summary>
/// <param name="Kind">The token's kind.</param>
/// <param name="Range">The token's source range.</param>
internal readonly record struct Token(TokenKind Kind, Syntax.Range Range);