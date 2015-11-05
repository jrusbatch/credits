using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Dnx.Runtime;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Credits
{
    public class Program
    {
        public Program(IApplicationEnvironment appEnvironment, ILibraryManager libraryManager)
        {
            AppEnvironment = appEnvironment;
            LibraryManager = libraryManager;
        }

        public IApplicationEnvironment AppEnvironment { get; }

        public ILibraryManager LibraryManager { get; }

        public void Main(string[] args)
        {
            var authors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dependencies =
                LibraryManager.GetLibraries()
                    .SelectMany(x => x.Dependencies)
                    .Distinct()
                    .Select(x => LibraryManager.GetLibrary(x));

            foreach (var dependency in dependencies)
            {
                var projectFile = new FileInfo(dependency.Path);
                var directory = projectFile.Directory;

                var nuspecPath = Path.Combine(directory.FullName, dependency.Version, dependency.Name + ".nuspec");
                var nuspecFile = new FileInfo(nuspecPath);
                if (nuspecFile.Exists)
                {
                    var xmlDoc = XDocument.Load(nuspecPath);
                    var pkgAuthors = xmlDoc.Descendants().Where(x => string.Equals(x.Name?.LocalName, "authors", StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.Value)
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToArray() ?? Array.Empty<string>();

                    foreach (var author in pkgAuthors)
                    {
                        authors.Add(author);
                    }
                }
            }

            var projectDirectory = new DirectoryInfo(AppEnvironment.ApplicationBasePath);
            var outputPath = Path.Combine(projectDirectory.FullName, "THANKS.md");

            using (var outputFile = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var textWriter = new StreamWriter(outputFile, Encoding.UTF8))
            {
                foreach (var auth in authors)
                {
                    textWriter.WriteLine(auth);
                }

                textWriter.Flush();
            }
        }
    }
}
