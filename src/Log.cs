namespace ChatTCP;

public struct Log
{
    /// <summary>
    /// Logs information.
    /// </summary>
    /// <param name="msg">The message we wish to log.</param>
    public static void Info(string msg)
    {
        Console.WriteLine($"{DateTime.Now.ToShortTimeString()} [INFO] - {msg}");
    }

    /// <summary>
    /// Logs a warning.
    /// </summary>
    /// <param name="msg">The message we wish to log.</param>
    public static void Warn(string msg)
    {
        Console.WriteLine($"{DateTime.Now.ToShortTimeString()} [WARNING] - {msg}");
    }

    /// <summary>
    /// Logs an error.
    /// </summary>
    /// <param name="msg">The message we wish to log.</param>
    public static void Error(string msg)
    {
        Console.WriteLine($"{DateTime.Now.ToShortTimeString()} [ERROR] - {msg}");
    }
}