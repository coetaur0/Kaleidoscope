using Kaleidoscope;

var interpreter = new Interpreter();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (input is null or "")
    {
        break;
    }

    try
    {
        var (ir, result) = interpreter.Run(input);
        Console.WriteLine($"LLVM IR:\n{ir}");
        if (result is not null)
        {
            Console.WriteLine($"Result: {result}");
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
}

Console.WriteLine(interpreter.Module.ToString());