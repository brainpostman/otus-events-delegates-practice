using System.Security.AccessControl;

namespace otus_events_delegates_practice
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CatalogIterator iter = new CatalogIterator();
            CatalogIteratorSubscriber cis = new CatalogIteratorSubscriber(iter);
            CatalogIteratorSubscriber cis2 = new CatalogIteratorSubscriber(iter);
            iter.CatalogPath = "../../..";
            iter.FileFound += cis.FileFoundHandler;
            iter.FileFound += cis2.FileFoundHaltingHandler;
            iter.StartFileSearch();
            ICollection<string> list = new List<string>()
            {
                "1,23",
                "3.45",
                "2,54",
                "23,53",
                "12.412",
                "1231,23",
                "adasd",
                "-123.123",
            };
            var max = list.GetMax((string val) =>
            {
                if (float.TryParse(val, out var result))
                {
                    return result;
                } else
                {
                    return float.MinValue;
                }
            });
            Console.WriteLine(max);
        }
    }

    public class CatalogIteratorSubscriber
    {
        public CatalogIterator iter { get; set; }

        public Guid Id { get; private set; }

        private CatalogIteratorSubscriber()
        {
        }

        public CatalogIteratorSubscriber(CatalogIterator iterator)
        {
            iter = iterator;
            Id = Guid.NewGuid();
        }

        public void FileFoundHandler(object sender, FileEventArgs e)
        {
            Console.WriteLine($"[{Id}] Найден файл: {e.FileName}");
        }

        public void FileFoundHaltingHandler(object sender, FileEventArgs e)
        {
            Console.WriteLine($"[{Id}] Найден файл: {e.FileName}");
            if (e.FileName.Contains('4'))
            {
                iter.StopFileSearch($"[{Id}] Нашли файл с четвёркой, заканчиваю поиск...");
            }
        }
    }

    public static class CollectionExtensions
    {
        public static float GetMax<T>(this IEnumerable<T> collection, Func<T, float> convertToNumber) where T : class
        {
            return collection.Select(convertToNumber).Max();
        }
    }

    public class CatalogIterator
    {
        public event EventHandler<FileEventArgs> FileFound;

        public bool isIterating { get; private set; } = false;

        private string _path = string.Empty;

        public string CatalogPath
        {
            get => _path;
            set
            {
                if (!isIterating)
                {
                    _path = value;
                }
            }
        }

        private void OnFileFound(string fileName)
        {
            FileFound.Invoke(this, new FileEventArgs(fileName));
        }

        public void StartFileSearch()
        {
            if (isIterating) return;
            isIterating = true;
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(_path);

                FileSystemInfo[] members = dirInfo.GetFileSystemInfos();

                foreach (FileSystemInfo member in members)
                {
                    if (!member.Attributes.HasFlag(FileAttributes.Directory))
                    {
                        FileInfo fileInfo = member as FileInfo;
                        if (fileInfo != null)
                        {
                            OnFileFound(fileInfo.Name);
                        }
                    }
                }
                FinishSearch();
            }
            catch (FileSearchHaltedException e)
            {
                string msg = "Поиск файлов остановлен.";
                if (e.Message != string.Empty)
                {
                    msg += $" Причина: {e.Message}";
                }
                Console.WriteLine(msg);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Возникла ошибка: {e.Message}");
            }
            finally
            {
                FinishSearch();
            }
        }

        public void StopFileSearch(string reason)
        {
            if (!isIterating) return;
            throw new FileSearchHaltedException(reason);
        }

        private void FinishSearch()
        {
            Console.WriteLine("Поиск файлов завершён.");
            isIterating = false;
        }

        public class FileSearchHaltedException : Exception
        {
            public FileSearchHaltedException(string reason) : base(reason)
            {
            }
        }
    }

    public class FileEventArgs : EventArgs
    {
        public string FileName { get; private set; }

        public FileEventArgs(string fileName)
        {
            FileName = fileName;
        }
    }
}
