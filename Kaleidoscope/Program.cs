using Kaleidoscope.Parser;

var parser = new Parser();
try
{
    var result = parser.ParseItem("1.337 * 4 < 42 - 3 * 4");
    Console.WriteLine($"{result}");
}
catch (ParseException e)
{
    Console.WriteLine(e.Message);
}