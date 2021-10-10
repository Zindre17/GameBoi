using System.IO;
using System.Text;

namespace GB_Emulator.Gameboi.Hardware
{

    public class LogFileWriter
    {
        private const string filepath = "bin/instructions.log";

        private static FileStream logFile;

        public LogFileWriter()
        {
            if (logFile is not null)
            {
                logFile.Close();
                File.Delete(filepath);
            }
            logFile = File.OpenWrite(filepath);
        }

        public void Log(string line)
        {
            logFile?.Write(Encoding.ASCII.GetBytes(line), 0, line.Length);
        }

        public void LogLine(string line)
        {
            Log(line + "\n");
        }

    }
}