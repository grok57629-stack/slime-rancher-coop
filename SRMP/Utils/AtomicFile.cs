using System;
using System.IO;
using System.Text;

namespace SRMultiplayer
{
    internal static class AtomicFile
    {
        internal static void Write(string path, Action<Stream> write)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    write(stream);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        internal static void WriteText(string path, string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);
            Write(path, stream => stream.Write(data, 0, data.Length));
        }
    }
}
