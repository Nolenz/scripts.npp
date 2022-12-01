using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#pragma warning disable CS1591

namespace NppScripts
{
    /// <summary>
    /// A collection of terrible, terrible hacks to process script files before they are run.
    /// </summary>
    public class ScriptPreprocessor
    {
        private string[] files;
        private Dictionary<string, string> backups = new Dictionary<string, string>();
        public ScriptPreprocessor(ScriptInfo script)
        {
            files = script.GetAllFiles().ToArray();
        }

        /// <summary>
        /// Invoke some Action on preprocessed files, then restore them.
        /// </summary>
        public void Do(Action action)
        {
            try
            {
                Process();
                action.Invoke();
            }
            finally
            {
                Restore();
            }
            
        }



        #region Processors
        private string ApplyAll(string code)
        {
            code = ApplyStringInterpolation(code);
            code = ApplyWrapTryCatch(code);

            return code;
        }

        private static readonly Regex interpolatedStringsPattern = new Regex(@"(\$)(@?"".*?[^\\]"")", RegexOptions.Singleline);
        private static readonly Regex interpolatedExpressionPattern = new Regex(@"{\s*(.*?)\s*}", RegexOptions.Singleline);
        /// <summary>
        /// Swap all interpolated strings ($"", $@"") with string.Format calls.
        /// Because it's probably more fun to write this than trying to update to CSScript v4+.
        /// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/interpolated
        /// </summary>
        private static string ApplyStringInterpolation(string code)
        {
            return interpolatedStringsPattern.Replace(code, str =>
            {
                List<string> formatExpressions = new List<string>();
                string formatStr = interpolatedExpressionPattern.Replace(str.Groups[2].ToString(), expr =>
                {
                    string formatNum = $"{{{formatExpressions.Count}}}";
                    formatExpressions.Add(expr.Groups[1].ToString());
                    return formatNum;
                });
                return $"string.Format({formatStr}, {string.Join(", ", formatExpressions)})";
            });
        }


        private static readonly Regex wrapTryCatchPattern = new Regex(@"(public override void Run\(\)\s+{)(.*?)(\r?\n    })", RegexOptions.Singleline);
        private static readonly string tryStr = "try {";
        private static readonly string catchStr = "} catch (System.Exception e) {System.Windows.Forms.MessageBox.Show(e.Message + \"\\n\" + e.StackTrace); }";
        /// <summary>
        /// Wrap the Run fxn in a try catch that reports stacktrace.
        /// This is terrible, but I don't think stack trace can be grabbed any other way.
        /// </summary>
        private static string ApplyWrapTryCatch(string code)
        {
            return wrapTryCatchPattern.Replace(code, x => $"{x.Groups[1]}{tryStr}{x.Groups[2]}{catchStr}{x.Groups[3]}");
        }
        #endregion

        #region Helpers
        private static readonly string backupDir = Path.Combine(Plugin.ScriptsDir, "Backups");
        private void BackupFiles()
        {
            if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);
            foreach (string file in files)
            {
                backups[file] = File.ReadAllText(file);

                string backupPath = Path.Combine(backupDir, Path.GetFileName(file));
                FileInfo origin = new FileInfo(file);
                origin.CopyTo(backupPath, true);

                FileInfo dest = new FileInfo(backupPath);
                dest.CreationTime = origin.CreationTime;
                dest.LastWriteTime = origin.LastWriteTime;
                dest.LastAccessTime = origin.LastAccessTime;
            }
        }
        private void RestoreFiles()
        {
            foreach (string file in files)
            {
                string backupPath = Path.Combine(backupDir, Path.GetFileName(file));
                if (File.Exists(backupPath))
                {
                    FileInfo origin = new FileInfo(backupPath);
                    File.WriteAllText(file, backups[file]); // Write instead of copy to prevent file lock oddities.

                    FileInfo dest = new FileInfo(file);
                    dest.CreationTime = origin.CreationTime;
                    dest.LastWriteTime = origin.LastWriteTime;
                    dest.LastAccessTime = origin.LastAccessTime;
                }
            }
        }

        private void Process()
        {
            BackupFiles();
            foreach (string file in files)
            {
                string code = File.ReadAllText(file);
                code = ApplyAll(code);
                File.WriteAllText(file, code);
            }
        }

        private void Restore()
        {
            RestoreFiles();
        }
        #endregion
    }
}
