using Kaleidoscope.Parser;

var parser = new Parser();
try
{
    var result = parser.ParseItem("def f(x, y) x + y * 4");
    Console.WriteLine($"{result}");
}
catch (ParseException e)
{
    Console.WriteLine(e.Message);
}