
namespace CacheCSharp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LeastRecentlyUsedCache Cache = new LeastRecentlyUsedFileCache(@"C:\Windows\System32", 4UL * 1024UL * 1024UL * 1024UL);
        	Cache.Trim(16UL * 1024UL * 1024UL);
        	Cache.Clear();

	        Console.WriteLine("Cache Terminated!");
        }
    }
}
