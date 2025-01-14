﻿using System.Diagnostics;
using System.Reflection;
using Esprima;
using Jint;
using Jint.Native;
using Jint.Native.Json;
using Jint.Runtime;

var engine = new Engine(cfg => cfg
    .AllowClr()
);

engine
    .SetValue("print", new Action<object>(Console.WriteLine))
    .SetValue("load", new Func<string, object>(
        path => engine.Evaluate(File.ReadAllText(path)))
    );

var filename = args.Length > 0 ? args[0] : "";
if (!string.IsNullOrEmpty(filename))
{
    if (!File.Exists(filename))
    {
        Console.WriteLine("Could not find file: {0}", filename);
    }

    var script = File.ReadAllText(filename);
    engine.Evaluate(script, "repl");
    return;
}

var assembly = Assembly.GetExecutingAssembly();
var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
var version = fvi.FileVersion;

Console.WriteLine("Welcome to Jint ({0})", version);
Console.WriteLine("Type 'exit' to leave, " +
                  "'print()' to write on the console, " +
                  "'load()' to load scripts.");
Console.WriteLine();

var defaultColor = Console.ForegroundColor;
var parserOptions = new ParserOptions
{
    Tolerant = true,
    RegExpParseMode = RegExpParseMode.AdaptToInterpreted
};

var serializer = new JsonSerializer(engine);

while (true)
{
    Console.ForegroundColor = defaultColor;
    Console.Write("jint> ");
    var input = Console.ReadLine();
    if (input is "exit" or ".exit")
    {
        return;
    }

    try
    {
        var result = engine.Evaluate(input, parserOptions);
        JsValue str = result;
        if (!result.IsPrimitive() && result is not IPrimitiveInstance)
        {
            str = serializer.Serialize(result, JsValue.Undefined, "  ");
            if (str == JsValue.Undefined)
            {
                str = result;
            }
        }
        else if (result.IsString())
        {
            str = serializer.Serialize(result, JsValue.Undefined, JsValue.Undefined);
        }
        Console.WriteLine(str);
    }
    catch (JavaScriptException je)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(je.ToString());
    }
    catch (Exception e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(e.Message);
    }
}
