

namespace SLLibrary
{
    public class IO
    {
        public static void CreateDirIfNotExist(string dir)
        {

            var dirs = dir.Split(Path.DirectorySeparatorChar).Where(m => !string.IsNullOrEmpty(m)).ToList();
            string endpoint = dirs[0];
            dirs.RemoveAt(0);
            foreach (var path in dirs)
            {
                endpoint = Path.Combine(endpoint, path);
                if (!Directory.Exists(endpoint))
                    Directory.CreateDirectory(endpoint);
            }
        }
    }
}
