namespace MeowBot;

internal static class Utils
{
    public static readonly HttpClient GlobalHttpClient = new();
    public static void PressAnyKeyToContinue()
    {
        Console.WriteLine("Press any key to continue");
        Console.ReadKey(true);
    }
}