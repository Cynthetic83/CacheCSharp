
/*
Question 2- Open Source Project
  In this scenario, you have forked an open source project from GitHub, and there is a specific source file that
  contains multiple issues. Please identify and fix the problems in the source file, and prepare a GitHub pull
  request to submit a fix back to the open source project.
  Attached you’ll find a project labeled “CacheCSharp”. The issues are in “cache.cs”. 
  Please resubmit “cache.cs” with your fixes and provide a pull request comment. 
*/


namespace CacheCSharp
{
    public class CacheItem
    {
        public CacheItem(ulong sizeInBytes, DateTime lastWriteTime)
        {
            SizeInBytes = sizeInBytes;
            LastWriteTime = lastWriteTime;
        }

        // this property used in all derived/child classes, so moved higher in the parent/child hierarchy.
        public DateTime LastWriteTime { get; private set; }


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

        //
        // We don't need the destructor in C#, specially here in managed code.
        // If we need to clean up something significant here, then we can Implement IDisposable.
        // Based on what it does currently, we even don't need IDisposable.
        // Additionally, below we've 'virtual void Clear()' method -- which effectively has the clean up code.
        // This is called from Program.cs 
        // So, code commented out.
        //
        // ~LeastRecentlyUsedCache()
        // {
        //    Clear();
        // }
        //

        public virtual void Clear()
        {
            Items.Clear(); // Assuming obvious business meaning of 'Clear'.
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
            CurrentCacheSizeInBytes += item!.SizeInBytes;

            Items.Add(item!);
        }

        protected virtual int CompareTime(CacheItem Item1, CacheItem Item2)
        {
            // return 0;
            
            return (int)(Item2!.LastWriteTime.Ticks - Item1!.LastWriteTime.Ticks);

            return DateTime.Compare(Item1.LastWriteTime, Item2.LastWriteTime);  // we may use this if better suited
        }

        private ulong MaximumCacheSizeInBytes;
        private ulong CurrentCacheSizeInBytes = 0;


        // We should be able to access these in derived/child classes for better cleanup.
        public SortedSet<CacheItem> Items { get; private set; }
    }

    public class CacheFileItem : CacheItem
    {
        public CacheFileItem(string filePath, ulong sizeInBytes, DateTime lastWriteTime) : base(sizeInBytes, lastWriteTime)
        {
            FilePath = filePath;
            
            // LastWriteTime = lastWriteTime;

            File = Win32File.Open(filePath);
        }

        public uint NumberOfLinks()
        {
            return Win32File.NumberOfLinks(File);
        }

        // This can be called by the child class for cleanup purpose.
        public void CloseFile()
        {
            Win32File.Close(File);
        }

        public override bool Pinned { get => NumberOfLinks() > 1; }
        public string FilePath { get; private set; }
        // public DateTime LastWriteTime { get; private set; }  // Moved to higher/parent/base class, because this could be used everywhere in the hierarchy.
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

        // 
        // We don't need the destructor in C#, specially here in managed code.
        // If we need to clean up something significant here, then we can Implement IDisposable.
        // 
        // Additionally, below we've 'virtual void Clear()' method -- which effectively has the clean up code
        // and being called from Program.cs
        // 
        // So, code commented out.
        // ~LeastRecentlyUsedFileCache()
        // {            
        //    Clear();
        // }
        //

        // For cleanup
        public override void Clear()
        {
            foreach (var aItem in Items)
            {
                var cf = aItem as CacheFileItem;
                cf!.CloseFile();
            }
            RootPath = String.Empty;
            base.Clear();            
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

        // Used higher/base/parent's implementation, because that implementation is sufficient for this condition.
        // So, following code commented out.
        // protected override int CompareTime(CacheItem Item1, CacheItem Item2)
        // {
        //    var File1 = Item1 as CacheFileItem;
        //    var File2 = Item2 as CacheFileItem;
        // 
        //    long delta = File2!.LastWriteTime.Ticks - File1!.LastWriteTime.Ticks;
        //    return (int)delta;
        // }

        private void DeleteFile(CacheFileItem? file)
        {
            if (file == null || file.Pinned) // don't delete if Pinned !?
                return;

            // for the purpose of this test we will just print out the delete
            Console.WriteLine($"DeleteFile: {file.FilePath}");
        }

        public string RootPath { get; private set; }
    }
}
