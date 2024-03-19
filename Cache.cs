
namespace CacheCSharp
{
    public class CacheItem
    {
        public CacheItem(ulong sizeInBytes)
        {
            SizeInBytes = sizeInBytes;
        }

        public ulong SizeInBytes { get; private set; }

        // pinned item, do not remove
        public virtual bool Pinned { get; private set; } = false;
    }

    public class LeastRecentlyUsedCache
    {
        public LeastRecentlyUsedCache(ulong maximumCacheSizeInBytes)
        {
            MaximumCacheSizeInBytes = maximumCacheSizeInBytes;
            Items = new SortedSet<CacheItem>(Comparer<CacheItem>.Create((Item1, Item2) => CompareTime(Item1, Item2)));
        }

        ~LeastRecentlyUsedCache()
        {
            Clear();
        }

        public virtual void Clear()
        {
            CurrentCacheSizeInBytes = 0;
        }

        public virtual void Trim(ulong sizeInBytes = 0)
        {
            List<CacheItem> oldItemsToDelete;
            MakeRoomForSize(sizeInBytes, out oldItemsToDelete);
        }

        public void MakeRoomForSize(ulong sizeInBytes, out List<CacheItem> oldItemsToDelete)
        {
            oldItemsToDelete = Items.Where(f => ((CurrentCacheSizeInBytes -= f.SizeInBytes) > MaximumCacheSizeInBytes) && !f.Pinned).ToList();
            foreach (CacheItem item in oldItemsToDelete)
            {
                Items.Remove(item);
            }
        }

        protected void AddItemToCache(CacheItem item)
        {
            CurrentCacheSizeInBytes += item.SizeInBytes;

            Items.Add(item);
        }

        protected virtual int CompareTime(CacheItem Item1, CacheItem Item2)
        {
            return 0;
        }

        private ulong MaximumCacheSizeInBytes;
        private ulong CurrentCacheSizeInBytes = 0;
        private SortedSet<CacheItem> Items;
    }

    public class CacheFileItem : CacheItem
    {
        public CacheFileItem(string filePath, ulong sizeInBytes, DateTime lastWriteTime)
            : base(sizeInBytes)
        {
            FilePath = filePath;
            LastWriteTime = lastWriteTime;

            File = Win32File.Open(filePath);
        }

        public uint NumberOfLinks()
        {
            return Win32File.NumberOfLinks(File);
        }

        public override bool Pinned { get => NumberOfLinks() > 1; }
        public string FilePath { get; private set; }
        public DateTime LastWriteTime { get; private set; }
        private IntPtr File { get; set; }
    };

    public class LeastRecentlyUsedFileCache : LeastRecentlyUsedCache
    {
        public LeastRecentlyUsedFileCache(string rootPath, ulong maximumCacheSizeInBytes)
            : base(maximumCacheSizeInBytes)
        {
            RootPath = rootPath;

            var fileEnumerable = Directory.EnumerateFiles(RootPath, "*.*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true });
            foreach (string filePath in fileEnumerable)
            {
                var fileInfo = new FileInfo(filePath);
                AddItemToCache(new CacheFileItem(filePath, (ulong)fileInfo.Length, fileInfo.LastWriteTime));
            }
        }

        ~LeastRecentlyUsedFileCache()
        {
            Clear();
        }

        public override void Clear()
        {
            base.Clear();
            RootPath = String.Empty;
        }

        public override void Trim(ulong sizeInBytes = 0)
        {
            List<CacheItem> oldItemsToDelete;
            MakeRoomForSize(sizeInBytes, out oldItemsToDelete);
            foreach (CacheItem item in oldItemsToDelete)
            {
                DeleteFile(file: item as CacheFileItem);
            }
        }

        protected override int CompareTime(CacheItem Item1, CacheItem Item2)
        {
            var File1 = Item1 as CacheFileItem;
            var File2 = Item2 as CacheFileItem;

            long delta = File2!.LastWriteTime.Ticks - File1!.LastWriteTime.Ticks;
            return (int)delta;
        }

        private void DeleteFile(CacheFileItem? file)
        {
            if (file == null)
                return;

            // for the purpose of this test we will just print out the delete
            Console.WriteLine($"DeleteFile: {file.FilePath}");
        }

        public string RootPath { get; private set; }
    }
}
