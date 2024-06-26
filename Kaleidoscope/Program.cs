using Kaleidoscope.Parser;

var source = new Source(null, "def f(x, y)\n  x + y\nf(1, 3.5)");

var lexer = new Lexer(source);

var token = lexer.Next();
while (token != null)
{
    Console.WriteLine($"{source[token.Value.Range]} at {token.Value.Range}");
    token = lexer.Next();
}