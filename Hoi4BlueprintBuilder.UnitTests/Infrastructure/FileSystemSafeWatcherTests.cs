using Hoi4BlueprintBuilder.Core.Infrastructure;

namespace Hoi4BlueprintBuilder.UnitTests.Infrastructure;

[TestFixture(TestOf = typeof(FileSystemSafeWatcher))]
public sealed class FileSystemSafeWatcherTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = TestHelper.CreateUniqueTempDirectory();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Constructor

    [Test]
    public void Constructor_Default_CreatesInstance()
    {
        using var watcher = new FileSystemSafeWatcher();
        Assert.That(watcher.EnableRaisingEvents, Is.False);
    }

    [Test]
    public void Constructor_WithPath_SetsPath()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir);
        Assert.That(watcher.Path, Is.EqualTo(_tempDir));
    }

    [Test]
    public void Constructor_WithPathAndFilter_SetsBoth()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir, "*.txt");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(watcher.Path, Is.EqualTo(_tempDir));
            Assert.That(watcher.Filter, Is.EqualTo("*.txt"));
        }
    }

    #endregion

    #region Properties

    [Test]
    public void Filter_SetAndGet_ReturnsSetValue()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir);
        watcher.Filter = "*.log";
        Assert.That(watcher.Filter, Is.EqualTo("*.log"));
    }

    [Test]
    public void Filters_ReturnsCollection()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir);
        Assert.That(watcher.Filters, Is.Not.Null);
    }

    [Test]
    public void IncludeSubdirectories_SetAndGet_ReturnsSetValue()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir);
        watcher.IncludeSubdirectories = true;
        Assert.That(watcher.IncludeSubdirectories, Is.True);
    }

    [Test]
    public void InternalBufferSize_SetAndGet_ReturnsSetValue()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir);
        watcher.InternalBufferSize = 16384;
        Assert.That(watcher.InternalBufferSize, Is.EqualTo(16384));
    }

    [Test]
    public void NotifyFilter_SetAndGet_ReturnsSetValue()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir);
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
        Assert.That(watcher.NotifyFilter, Is.EqualTo(NotifyFilters.FileName | NotifyFilters.Size));
    }

    [Test]
    public void Path_SetAndGet_ReturnsSetValue()
    {
        using var watcher = new FileSystemSafeWatcher();
        watcher.Path = _tempDir;
        Assert.That(watcher.Path, Is.EqualTo(_tempDir));
    }

    [Test]
    public void SynchronizingObject_DefaultIsNull()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir);
        Assert.That(watcher.SynchronizingObject, Is.Null);
    }

    [Test]
    public void ConsolidationInterval_Default_Is600()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir);
        Assert.That(watcher.ConsolidationInterval, Is.EqualTo(600));
    }

    [Test]
    public void ConsolidationInterval_SetAndGet_ReturnsSetValue()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir);
        watcher.ConsolidationInterval = 200;
        Assert.That(watcher.ConsolidationInterval, Is.EqualTo(200));
    }

    #endregion

    #region Dispose

    [Test]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        var watcher = new FileSystemSafeWatcher(_tempDir);
        watcher.Dispose();
        Assert.DoesNotThrow(() => watcher.Dispose());
    }

    #endregion

    #region WaitForChanged

    [Test]
    public void WaitForChanged_ThrowsNotSupportedException()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir);
        Assert.Throws<NotSupportedException>(() => watcher.WaitForChanged(WatcherChangeTypes.All));
    }

    [Test]
    public void WaitForChanged_WithTimeout_ThrowsNotSupportedException()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir);
        Assert.Throws<NotSupportedException>(() => watcher.WaitForChanged(WatcherChangeTypes.All, 1000));
    }

    #endregion

    #region File System Events

    [Test]
    public void Created_FileCreated_RaisesCreatedEvent()
    {
        using var resetEvent = new ManualResetEventSlim(false);
        string? receivedPath = null;

        using var watcher = new FileSystemSafeWatcher(_tempDir) { ConsolidationInterval = 100 };
        watcher.Created += (_, e) =>
        {
            receivedPath = e.FullPath;
            resetEvent.Set();
        };
        watcher.EnableRaisingEvents = true;

        var filePath = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(filePath, "hello");

        bool signaled = resetEvent.Wait(TimeSpan.FromSeconds(5));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signaled, Is.True, "Created event was not raised");
            Assert.That(receivedPath, Is.EqualTo(filePath));
        }
    }

    [Test]
    public void Deleted_FileDeleted_RaisesDeletedEvent()
    {
        var filePath = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(filePath, "hello");

        using var resetEvent = new ManualResetEventSlim(false);
        string? receivedPath = null;

        using var watcher = new FileSystemSafeWatcher(_tempDir) { ConsolidationInterval = 100 };
        watcher.Deleted += (_, e) =>
        {
            receivedPath = e.FullPath;
            resetEvent.Set();
        };
        watcher.EnableRaisingEvents = true;

        File.Delete(filePath);

        bool signaled = resetEvent.Wait(TimeSpan.FromSeconds(5));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signaled, Is.True, "删除事件未触发");
            Assert.That(receivedPath, Is.EqualTo(filePath));
        }
    }

    [Test]
    public void Changed_FileModified_RaisesChangedEvent()
    {
        var filePath = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(filePath, "initial");

        using var resetEvent = new ManualResetEventSlim(false);

        using var watcher = new FileSystemSafeWatcher(_tempDir)
        {
            ConsolidationInterval = 100,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
        };
        watcher.Changed += (_, _) => resetEvent.Set();
        watcher.EnableRaisingEvents = true;

        Thread.Sleep(50);
        File.WriteAllText(filePath, "modified content");

        bool signaled = resetEvent.Wait(TimeSpan.FromSeconds(5));
        Assert.That(signaled, Is.True, "Changed event was not raised");
    }

    [Test]
    public void Renamed_FileRenamed_RaisesRenamedEvent()
    {
        var filePath = Path.Combine(_tempDir, "original.txt");
        var newPath = Path.Combine(_tempDir, "renamed.txt");
        File.WriteAllText(filePath, "hello");

        using var resetEvent = new ManualResetEventSlim(false);
        string? oldFullPath = null;
        string? newFullPath = null;

        using var watcher = new FileSystemSafeWatcher(_tempDir) { ConsolidationInterval = 100 };
        watcher.Renamed += (_, e) =>
        {
            oldFullPath = e.OldFullPath;
            newFullPath = e.FullPath;
            resetEvent.Set();
        };
        watcher.EnableRaisingEvents = true;

        File.Move(filePath, newPath);

        bool signaled = resetEvent.Wait(TimeSpan.FromSeconds(5));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signaled, Is.True, "Renamed event was not raised");
            Assert.That(oldFullPath, Is.EqualTo(filePath));
            Assert.That(newFullPath, Is.EqualTo(newPath));
        }
    }

    [Test]
    public void EnableRaisingEvents_SetToFalse_NoEventsRaised()
    {
        using var watcher = new FileSystemSafeWatcher(_tempDir) { ConsolidationInterval = 100 };

        int eventCount = 0;
        watcher.Created += (_, _) => Interlocked.Increment(ref eventCount);

        watcher.EnableRaisingEvents = true;
        watcher.EnableRaisingEvents = false;

        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), "hello");

        Thread.Sleep(500);

        Assert.That(eventCount, Is.Zero);
    }

    #endregion

    #region Event Deduplication

    [Test]
    public void DuplicateChangedEvents_AreConsolidated()
    {
        using var firstEvent = new ManualResetEventSlim(false);
        int consolidatedCount = 0;
        int rawCount = 0;

        // Use a long consolidation interval so rapid writes are batched
        using var watcher = new FileSystemSafeWatcher(_tempDir)
        {
            ConsolidationInterval = 1000,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
        };

        // Also track raw events from a standard FileSystemWatcher for comparison
        using var rawWatcher = new FileSystemWatcher(_tempDir)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };
        rawWatcher.Changed += (_, _) => Interlocked.Increment(ref rawCount);

        watcher.Changed += (_, _) =>
        {
            Interlocked.Increment(ref consolidatedCount);
            firstEvent.Set();
        };
        watcher.EnableRaisingEvents = true;

        var filePath = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(filePath, "initial");

        // Wait for initial events to settle
        Thread.Sleep(2500);
        Interlocked.Exchange(ref consolidatedCount, 0);
        Interlocked.Exchange(ref rawCount, 0);

        // Rapidly modify the file multiple times
        for (int i = 0; i < 10; i++)
        {
            File.WriteAllText(filePath, $"content {i}");
            Thread.Sleep(10);
        }

        firstEvent.Wait(TimeSpan.FromSeconds(5));
        Thread.Sleep(2500);

        // Consolidated watcher should produce fewer events than raw watcher
        using (Assert.EnterMultipleScope())
        {
            Assert.That(consolidatedCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(
                consolidatedCount,
                Is.LessThanOrEqualTo(rawCount),
                "Consolidated events should not exceed raw events"
            );
        }
    }

    [Test]
    public void DuplicateChangedEvents_SameFile_DeduplicatedToSingleEvent()
    {
        // 此测试验证同一文件的重复 Changed 事件在同一个定时器周期内被合并为一个
        // 之前 IsDuplicate 中错误地使用了 reO2?.Name (RenamedEventArgs) 而不是 eO2.Name，
        // 导致非 Rename 事件的去重完全失效，但宽松的断言没有捕获到这个问题
        using var allEventsReceived = new ManualResetEventSlim(false);
        int changedCount = 0;

        using var watcher = new FileSystemSafeWatcher(_tempDir)
        {
            // 长间隔确保所有快速写入都落在同一个定时器周期被一起处理
            ConsolidationInterval = 1500,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
        };
        watcher.Changed += (_, _) =>
        {
            Interlocked.Increment(ref changedCount);
            allEventsReceived.Set();
        };
        watcher.EnableRaisingEvents = true;

        var filePath = Path.Combine(_tempDir, "dedup_test.txt");
        File.WriteAllText(filePath, "initial");

        // 等待初始事件处理完毕
        Thread.Sleep(3500);
        Interlocked.Exchange(ref changedCount, 0);

        // 在一个很短的时间窗口内快速写入同一文件多次
        // 所有写入应落在同一个 consolidation 周期内
        for (int i = 0; i < 5; i++)
        {
            File.WriteAllText(filePath, $"content {i}");
            Thread.Sleep(15);
        }

        allEventsReceived.Wait(TimeSpan.FromSeconds(7));
        // 等待足够长时间让所有定时器周期处理完毕
        Thread.Sleep(3500);

        // 同一文件的重复 Changed 事件应当被去重，最终只触发 1 次
        Assert.That(changedCount, Is.EqualTo(1),
            "同一文件在同一个 consolidation 周期内的重复 Changed 事件应被去重为 1 次");
    }

    [Test]
    public void Filter_OnlyMatchingFilesRaiseEvents()
    {
        using var matchedEvent = new ManualResetEventSlim(false);
        int txtCount = 0;
        int totalCount = 0;

        using var watcher = new FileSystemSafeWatcher(_tempDir, "*.txt") { ConsolidationInterval = 100, };
        watcher.Created += (_, e) =>
        {
            Interlocked.Increment(ref totalCount);
            if (e.Name?.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) == true)
            {
                Interlocked.Increment(ref txtCount);
            }

            matchedEvent.Set();
        };
        watcher.EnableRaisingEvents = true;

        // .log should not trigger event due to filter
        File.WriteAllText(Path.Combine(_tempDir, "test.log"), "log");
        Thread.Sleep(500);

        // .txt should trigger event
        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), "txt");

        matchedEvent.Wait(TimeSpan.FromSeconds(5));
        Thread.Sleep(300);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(txtCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(totalCount, Is.EqualTo(txtCount), "Non-matching files should not raise events");
        }
    }

    #endregion
}
