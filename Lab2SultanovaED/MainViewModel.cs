using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lab2SultanovaED
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ArraySorter _threadSorter;
        private readonly ArraySorter _taskSorter;
        private readonly SynchronizationContext _uiContext;
        private readonly Stopwatch _threadBatchWatch = new Stopwatch();
        private readonly Stopwatch _taskBatchWatch = new Stopwatch();
        private readonly List<double> _threadBatchElapsedTimes = new List<double>();
        private readonly List<double> _taskBatchElapsedTimes = new List<double>();

        private int[]? _threadOriginalArray;
        private int[]? _taskOriginalArray;
        private CancellationTokenSource? _threadCancellationTokenSource;
        private CancellationTokenSource? _taskCancellationTokenSource;
        private int _threadRunningSorts;
        private int _taskRunningSorts;
        private bool _threadBatchCancelled;
        private bool _taskBatchCancelled;

        [ObservableProperty]
        private int _threadArraySize = 1000;

        [ObservableProperty]
        private string? _threadOriginalArrayString;

        [ObservableProperty]
        private string? _threadBubbleSortResult;

        [ObservableProperty]
        private string? _threadQuickSortResult;

        [ObservableProperty]
        private string? _threadInsertionSortResult;

        [ObservableProperty]
        private string? _threadShakerSortResult;

        [ObservableProperty]
        private string _threadTotalComparisons = "Общее число сравнений: 0";

        [ObservableProperty]
        private int _threadMaxParallelSorts = 4;

        [ObservableProperty]
        private bool _threadUseSharedArray;

        [ObservableProperty]
        private double _threadBubbleProgress;

        [ObservableProperty]
        private double _threadQuickProgress;

        [ObservableProperty]
        private double _threadInsertionProgress;

        [ObservableProperty]
        private double _threadShakerProgress;

        [ObservableProperty]
        private string _threadComparisonSummary = "Сравнение запусков: пока нет данных.";

        [ObservableProperty]
        private int _taskArraySize = 1000;

        [ObservableProperty]
        private string? _taskOriginalArrayString;

        [ObservableProperty]
        private string? _taskBubbleSortResult;

        [ObservableProperty]
        private string? _taskQuickSortResult;

        [ObservableProperty]
        private string? _taskInsertionSortResult;

        [ObservableProperty]
        private string? _taskHeapSortResult;

        [ObservableProperty]
        private string _taskTotalComparisons = "Общее число сравнений: 0";

        [ObservableProperty]
        private bool _taskUseSharedArray;

        [ObservableProperty]
        private double _taskBubbleProgress;

        [ObservableProperty]
        private double _taskQuickProgress;

        [ObservableProperty]
        private double _taskInsertionProgress;

        [ObservableProperty]
        private double _taskHeapProgress;

        [ObservableProperty]
        private string _taskComparisonSummary = "Сравнение запусков: пока нет данных.";

        public MainViewModel()
        {
            _threadSorter = new ArraySorter();
            _taskSorter = new ArraySorter();
            _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();

            _threadSorter.BubbleSortCompleted += OnThreadBubbleSortCompleted;
            _threadSorter.QuickSortCompleted += OnThreadQuickSortCompleted;
            _threadSorter.InsertionSortCompleted += OnThreadInsertionSortCompleted;
            _threadSorter.ShakerSortCompleted += OnThreadShakerSortCompleted;
            _threadSorter.SortProgressChanged += OnThreadSortProgressChanged;

            GenerateTaskArrayCommand = new AsyncRelayCommand(GenerateTaskArrayAsync, CanGenerateTaskArray);
            BubbleSortTaskCommand = new AsyncRelayCommand(BubbleSortTaskAsync, CanSortTaskBubble);
            QuickSortTaskCommand = new AsyncRelayCommand(QuickSortTaskAsync, CanSortTaskQuick);
            InsertionSortTaskCommand = new AsyncRelayCommand(InsertionSortTaskAsync, CanSortTaskInsertion);
            HeapSortTaskCommand = new AsyncRelayCommand(HeapSortTaskAsync, CanSortTaskHeap);
            CancelTaskSortsCommand = new RelayCommand(CancelTaskSorts, CanCancelTaskSorts);
        }

        public IAsyncRelayCommand GenerateTaskArrayCommand { get; }

        public IAsyncRelayCommand BubbleSortTaskCommand { get; }

        public IAsyncRelayCommand QuickSortTaskCommand { get; }

        public IAsyncRelayCommand InsertionSortTaskCommand { get; }

        public IAsyncRelayCommand HeapSortTaskCommand { get; }

        public IRelayCommand CancelTaskSortsCommand { get; }

        [RelayCommand(CanExecute = nameof(CanGenerateThreadArray))]
        private void GenerateThreadArray()
        {
            _threadCancellationTokenSource = new CancellationTokenSource();
            _threadOriginalArray = _threadSorter.GenerateRandomArray(ThreadArraySize);

            ThreadOriginalArrayString = BuildOriginalArrayString(_threadOriginalArray);
            ThreadBubbleSortResult = null;
            ThreadQuickSortResult = null;
            ThreadInsertionSortResult = null;
            ThreadShakerSortResult = null;
            ThreadBubbleProgress = 0;
            ThreadQuickProgress = 0;
            ThreadInsertionProgress = 0;
            ThreadShakerProgress = 0;
            ThreadComparisonSummary = "Сравнение запусков: пока нет данных.";
            _threadRunningSorts = 0;
            _threadBatchCancelled = false;
            _threadBatchElapsedTimes.Clear();
            _threadBatchWatch.Reset();
            _threadSorter.ResetTotalComparisons();
            UpdateThreadTotalComparisons();
            NotifyThreadCommands();
        }

        [RelayCommand(CanExecute = nameof(CanSortThreadBubble))]
        private void BubbleSortThread()
        {
            if (_threadOriginalArray == null)
            {
                return;
            }

            ThreadBubbleSortResult = "Сортируется...";
            ThreadBubbleProgress = 0;
            StartThreadSort(
                token => _threadSorter.BubbleSort(_threadOriginalArray, token),
                () => MarkThreadSortCancelled(ThreadSortKind.Bubble));
        }

        [RelayCommand(CanExecute = nameof(CanSortThreadQuick))]
        private void QuickSortThread()
        {
            if (_threadOriginalArray == null)
            {
                return;
            }

            ThreadQuickSortResult = "Сортируется...";
            ThreadQuickProgress = 0;
            StartThreadSort(
                token => _threadSorter.QuickSort(_threadOriginalArray, token),
                () => MarkThreadSortCancelled(ThreadSortKind.Quick));
        }

        [RelayCommand(CanExecute = nameof(CanSortThreadInsertion))]
        private void InsertionSortThread()
        {
            if (_threadOriginalArray == null)
            {
                return;
            }

            ThreadInsertionSortResult = "Сортируется...";
            ThreadInsertionProgress = 0;
            StartThreadSort(
                token => _threadSorter.InsertionSort(_threadOriginalArray, token),
                () => MarkThreadSortCancelled(ThreadSortKind.Insertion));
        }

        [RelayCommand(CanExecute = nameof(CanSortThreadShaker))]
        private void ShakerSortThread()
        {
            if (_threadOriginalArray == null)
            {
                return;
            }

            ThreadShakerSortResult = "Сортируется...";
            ThreadShakerProgress = 0;
            StartThreadSort(
                token => _threadSorter.ShakerSort(_threadOriginalArray, token),
                () => MarkThreadSortCancelled(ThreadSortKind.Shaker));
        }

        [RelayCommand(CanExecute = nameof(CanCancelThreadSorts))]
        private void CancelThreadSorts()
        {
            if (_threadRunningSorts == 0)
            {
                return;
            }

            _threadBatchCancelled = true;
            _threadCancellationTokenSource?.Cancel();
            MarkThreadSortCancelled(ThreadSortKind.Bubble);
            MarkThreadSortCancelled(ThreadSortKind.Quick);
            MarkThreadSortCancelled(ThreadSortKind.Insertion);
            MarkThreadSortCancelled(ThreadSortKind.Shaker);
            NotifyThreadCommands();
        }

        private bool CanGenerateThreadArray()
        {
            return _threadRunningSorts == 0;
        }

        private bool CanSortThreadBubble()
        {
            return _threadOriginalArray != null
                && ThreadBubbleSortResult != "Сортируется..."
                && _threadRunningSorts < ThreadMaxParallelSorts;
        }

        private bool CanSortThreadQuick()
        {
            return _threadOriginalArray != null
                && ThreadQuickSortResult != "Сортируется..."
                && _threadRunningSorts < ThreadMaxParallelSorts;
        }

        private bool CanSortThreadInsertion()
        {
            return _threadOriginalArray != null
                && ThreadInsertionSortResult != "Сортируется..."
                && _threadRunningSorts < ThreadMaxParallelSorts;
        }

        private bool CanSortThreadShaker()
        {
            return _threadOriginalArray != null
                && ThreadShakerSortResult != "Сортируется..."
                && _threadRunningSorts < ThreadMaxParallelSorts;
        }

        private bool CanCancelThreadSorts()
        {
            return _threadRunningSorts > 0;
        }

        private bool CanGenerateTaskArray()
        {
            return !GenerateTaskArrayCommand.IsRunning;
        }

        private bool CanSortTaskBubble()
        {
            return _taskOriginalArray != null
                && !BubbleSortTaskCommand.IsRunning;
        }

        private bool CanSortTaskQuick()
        {
            return _taskOriginalArray != null
                && !QuickSortTaskCommand.IsRunning;
        }

        private bool CanSortTaskInsertion()
        {
            return _taskOriginalArray != null
                && !InsertionSortTaskCommand.IsRunning;
        }

        private bool CanSortTaskHeap()
        {
            return _taskOriginalArray != null
                && !HeapSortTaskCommand.IsRunning;
        }

        private bool CanCancelTaskSorts()
        {
            return _taskRunningSorts > 0;
        }

        private async Task GenerateTaskArrayAsync()
        {
            await Task.Delay(100);

            _taskCancellationTokenSource = new CancellationTokenSource();
            _taskOriginalArray = _taskSorter.GenerateRandomArray(TaskArraySize);

            TaskOriginalArrayString = BuildOriginalArrayString(_taskOriginalArray);
            TaskBubbleSortResult = null;
            TaskQuickSortResult = null;
            TaskInsertionSortResult = null;
            TaskHeapSortResult = null;
            TaskBubbleProgress = 0;
            TaskQuickProgress = 0;
            TaskInsertionProgress = 0;
            TaskHeapProgress = 0;
            TaskComparisonSummary = "Сравнение запусков: пока нет данных.";
            _taskRunningSorts = 0;
            _taskBatchCancelled = false;
            _taskBatchElapsedTimes.Clear();
            _taskBatchWatch.Reset();
            _taskSorter.ResetTotalComparisons();
            UpdateTaskTotalComparisons();
            NotifyTaskCommands();
        }

        private Task BubbleSortTaskAsync()
        {
            return RunTaskSortAsync(
                "Пузырьковая",
                progress => TaskBubbleProgress = progress,
                text => TaskBubbleSortResult = text,
                (array, token, progress) => _taskSorter.BubbleSortAsync(array, token, progress));
        }

        private Task QuickSortTaskAsync()
        {
            return RunTaskSortAsync(
                "Быстрая",
                progress => TaskQuickProgress = progress,
                text => TaskQuickSortResult = text,
                (array, token, progress) => _taskSorter.QuickSortAsync(array, token, progress));
        }

        private Task InsertionSortTaskAsync()
        {
            return RunTaskSortAsync(
                "Вставками",
                progress => TaskInsertionProgress = progress,
                text => TaskInsertionSortResult = text,
                (array, token, progress) => _taskSorter.InsertionSortAsync(array, token, progress));
        }

        private Task HeapSortTaskAsync()
        {
            return RunTaskSortAsync(
                "Пирамидальная",
                progress => TaskHeapProgress = progress,
                text => TaskHeapSortResult = text,
                (array, token, progress) => _taskSorter.HeapSortAsync(array, token, progress));
        }

        private void CancelTaskSorts()
        {
            if (_taskRunningSorts == 0)
            {
                return;
            }

            _taskBatchCancelled = true;
            _taskCancellationTokenSource?.Cancel();
            MarkTaskSortCancelled(TaskSortKind.Bubble);
            MarkTaskSortCancelled(TaskSortKind.Quick);
            MarkTaskSortCancelled(TaskSortKind.Insertion);
            MarkTaskSortCancelled(TaskSortKind.Heap);
            NotifyTaskCommands();
        }

        partial void OnThreadUseSharedArrayChanged(bool value)
        {
            _threadSorter.UseSharedArray = value;
        }

        partial void OnTaskUseSharedArrayChanged(bool value)
        {
            _taskSorter.UseSharedArray = value;
        }

        partial void OnThreadMaxParallelSortsChanged(int value)
        {
            if (value < 1)
            {
                ThreadMaxParallelSorts = 1;
                return;
            }

            NotifyThreadCommands();
        }

        // Удалён метод OnTaskMaxParallelSortsChanged

        private void StartThreadSort(Action<CancellationToken> sortAction, Action markCancelled)
        {
            if (_threadCancellationTokenSource == null || _threadCancellationTokenSource.IsCancellationRequested)
            {
                _threadCancellationTokenSource = new CancellationTokenSource();
            }

            CancellationToken token = _threadCancellationTokenSource.Token;

            StartThreadBatchIfNeeded();
            _threadRunningSorts++;
            NotifyThreadCommands();

            Thread thread = new Thread(() =>
            {
                try
                {
                    sortAction(token);
                }
                finally
                {
                    _uiContext.Post(_ =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            markCancelled();
                        }

                        _threadRunningSorts = Math.Max(0, _threadRunningSorts - 1);
                        NotifyThreadCommands();
                        FinishThreadBatchIfNeeded();
                    }, null);
                }
            })
            {
                IsBackground = true
            };

            thread.Start();
        }

        private async Task RunTaskSortAsync(
            string algorithmName,
            Action<double> setProgress,
            Action<string?> setResult,
            Func<int[], CancellationToken, IProgress<double>, Task<SortExecutionResult>> sortMethod)
        {
            if (_taskOriginalArray == null)
            {
                return;
            }

            if (_taskCancellationTokenSource == null || _taskCancellationTokenSource.IsCancellationRequested)
            {
                _taskCancellationTokenSource = new CancellationTokenSource();
            }

            CancellationToken token = _taskCancellationTokenSource.Token;

            StartTaskBatchIfNeeded();
            _taskRunningSorts++;
            setProgress(0);
            setResult("Сортируется...");
            NotifyTaskCommands();

            try
            {
                Progress<double> progress = new Progress<double>(value => setProgress(Math.Clamp(value, 0, 100)));
                SortExecutionResult result = await sortMethod(_taskOriginalArray, token, progress);
                setProgress(100);
                setResult($"{algorithmName}: {FormatArray(result.SortedArray)}, время: {result.ElapsedMilliseconds:F2} мс, сравнений: {result.Comparisons}");
                _taskBatchElapsedTimes.Add(result.ElapsedMilliseconds);
                UpdateTaskTotalComparisons();
            }
            catch (OperationCanceledException)
            {
                markTaskResultCancelled(algorithmName);
            }
            finally
            {
                _taskRunningSorts = Math.Max(0, _taskRunningSorts - 1);
                NotifyTaskCommands();
                FinishTaskBatchIfNeeded();
            }

            void markTaskResultCancelled(string name)
            {
                switch (name)
                {
                    case "Пузырьковая":
                        MarkTaskSortCancelled(TaskSortKind.Bubble);
                        break;
                    case "Быстрая":
                        MarkTaskSortCancelled(TaskSortKind.Quick);
                        break;
                    case "Вставками":
                        MarkTaskSortCancelled(TaskSortKind.Insertion);
                        break;
                    case "Пирамидальная":
                        MarkTaskSortCancelled(TaskSortKind.Heap);
                        break;
                }
            }
        }

        private void OnThreadBubbleSortCompleted(int[] sortedArray, long comparisons, double elapsedMilliseconds)
        {
            _uiContext.Post(_ =>
            {
                if (ThreadBubbleSortResult == "Прервано")
                {
                    return;
                }

                ThreadBubbleProgress = 100;
                ThreadBubbleSortResult = $"Пузырьковая: {FormatArray(sortedArray)}, время: {elapsedMilliseconds:F2} мс, сравнений: {comparisons}";
                _threadBatchElapsedTimes.Add(elapsedMilliseconds);
                UpdateThreadTotalComparisons();
            }, null);
        }

        private void OnThreadQuickSortCompleted(int[] sortedArray, long comparisons, double elapsedMilliseconds)
        {
            _uiContext.Post(_ =>
            {
                if (ThreadQuickSortResult == "Прервано")
                {
                    return;
                }

                ThreadQuickProgress = 100;
                ThreadQuickSortResult = $"Быстрая: {FormatArray(sortedArray)}, время: {elapsedMilliseconds:F2} мс, сравнений: {comparisons}";
                _threadBatchElapsedTimes.Add(elapsedMilliseconds);
                UpdateThreadTotalComparisons();
            }, null);
        }

        private void OnThreadInsertionSortCompleted(int[] sortedArray, long comparisons, double elapsedMilliseconds)
        {
            _uiContext.Post(_ =>
            {
                if (ThreadInsertionSortResult == "Прервано")
                {
                    return;
                }

                ThreadInsertionProgress = 100;
                ThreadInsertionSortResult = $"Вставками: {FormatArray(sortedArray)}, время: {elapsedMilliseconds:F2} мс, сравнений: {comparisons}";
                _threadBatchElapsedTimes.Add(elapsedMilliseconds);
                UpdateThreadTotalComparisons();
            }, null);
        }

        private void OnThreadShakerSortCompleted(int[] sortedArray, long comparisons, double elapsedMilliseconds)
        {
            _uiContext.Post(_ =>
            {
                if (ThreadShakerSortResult == "Прервано")
                {
                    return;
                }

                ThreadShakerProgress = 100;
                ThreadShakerSortResult = $"Шейкерная: {FormatArray(sortedArray)}, время: {elapsedMilliseconds:F2} мс, сравнений: {comparisons}";
                _threadBatchElapsedTimes.Add(elapsedMilliseconds);
                UpdateThreadTotalComparisons();
            }, null);
        }

        private void OnThreadSortProgressChanged(string algorithm, double progress)
        {
            _uiContext.Post(_ =>
            {
                double clamped = Math.Clamp(progress, 0, 100);
                switch (algorithm)
                {
                    case "Bubble":
                        if (ThreadBubbleSortResult == "Сортируется...")
                        {
                            ThreadBubbleProgress = clamped;
                        }
                        break;
                    case "Quick":
                        if (ThreadQuickSortResult == "Сортируется...")
                        {
                            ThreadQuickProgress = clamped;
                        }
                        break;
                    case "Insertion":
                        if (ThreadInsertionSortResult == "Сортируется...")
                        {
                            ThreadInsertionProgress = clamped;
                        }
                        break;
                    case "Shaker":
                        if (ThreadShakerSortResult == "Сортируется...")
                        {
                            ThreadShakerProgress = clamped;
                        }
                        break;
                }
            }, null);
        }

        private void StartThreadBatchIfNeeded()
        {
            if (_threadRunningSorts == 0)
            {
                _threadBatchCancelled = false;
                _threadBatchElapsedTimes.Clear();
                _threadBatchWatch.Restart();
            }
        }

        private void FinishThreadBatchIfNeeded()
        {
            if (_threadRunningSorts != 0)
            {
                return;
            }

            _threadBatchWatch.Stop();
            ThreadComparisonSummary = BuildComparisonSummary(_threadBatchElapsedTimes, _threadBatchWatch.Elapsed.TotalMilliseconds, _threadBatchCancelled);
        }

        private void StartTaskBatchIfNeeded()
        {
            if (_taskRunningSorts == 0)
            {
                _taskBatchCancelled = false;
                _taskBatchElapsedTimes.Clear();
                _taskBatchWatch.Restart();
            }
        }

        private void FinishTaskBatchIfNeeded()
        {
            if (_taskRunningSorts != 0)
            {
                return;
            }

            _taskBatchWatch.Stop();
            TaskComparisonSummary = BuildComparisonSummary(_taskBatchElapsedTimes, _taskBatchWatch.Elapsed.TotalMilliseconds, _taskBatchCancelled);
        }

        private void UpdateThreadTotalComparisons()
        {
            ThreadTotalComparisons = $"Общее число сравнений: {_threadSorter.TotalComparisons}";
        }

        private void UpdateTaskTotalComparisons()
        {
            TaskTotalComparisons = $"Общее число сравнений: {_taskSorter.TotalComparisons}";
        }

        private void NotifyThreadCommands()
        {
            GenerateThreadArrayCommand.NotifyCanExecuteChanged();
            BubbleSortThreadCommand.NotifyCanExecuteChanged();
            QuickSortThreadCommand.NotifyCanExecuteChanged();
            InsertionSortThreadCommand.NotifyCanExecuteChanged();
            ShakerSortThreadCommand.NotifyCanExecuteChanged();
            CancelThreadSortsCommand.NotifyCanExecuteChanged();
        }

        private void NotifyTaskCommands()
        {
            GenerateTaskArrayCommand.NotifyCanExecuteChanged();
            BubbleSortTaskCommand.NotifyCanExecuteChanged();
            QuickSortTaskCommand.NotifyCanExecuteChanged();
            InsertionSortTaskCommand.NotifyCanExecuteChanged();
            HeapSortTaskCommand.NotifyCanExecuteChanged();
            CancelTaskSortsCommand.NotifyCanExecuteChanged();
        }

        private void MarkThreadSortCancelled(ThreadSortKind sortKind)
        {
            switch (sortKind)
            {
                case ThreadSortKind.Bubble:
                    if (ThreadBubbleSortResult == "Сортируется...")
                    {
                        ThreadBubbleSortResult = "Прервано";
                        ThreadBubbleProgress = 0;
                    }
                    break;
                case ThreadSortKind.Quick:
                    if (ThreadQuickSortResult == "Сортируется...")
                    {
                        ThreadQuickSortResult = "Прервано";
                        ThreadQuickProgress = 0;
                    }
                    break;
                case ThreadSortKind.Insertion:
                    if (ThreadInsertionSortResult == "Сортируется...")
                    {
                        ThreadInsertionSortResult = "Прервано";
                        ThreadInsertionProgress = 0;
                    }
                    break;
                case ThreadSortKind.Shaker:
                    if (ThreadShakerSortResult == "Сортируется...")
                    {
                        ThreadShakerSortResult = "Прервано";
                        ThreadShakerProgress = 0;
                    }
                    break;
            }
        }

        private void MarkTaskSortCancelled(TaskSortKind sortKind)
        {
            switch (sortKind)
            {
                case TaskSortKind.Bubble:
                    if (TaskBubbleSortResult == "Сортируется...")
                    {
                        TaskBubbleSortResult = "Прервано";
                        TaskBubbleProgress = 0;
                    }
                    break;
                case TaskSortKind.Quick:
                    if (TaskQuickSortResult == "Сортируется...")
                    {
                        TaskQuickSortResult = "Прервано";
                        TaskQuickProgress = 0;
                    }
                    break;
                case TaskSortKind.Insertion:
                    if (TaskInsertionSortResult == "Сортируется...")
                    {
                        TaskInsertionSortResult = "Прервано";
                        TaskInsertionProgress = 0;
                    }
                    break;
                case TaskSortKind.Heap:
                    if (TaskHeapSortResult == "Сортируется...")
                    {
                        TaskHeapSortResult = "Прервано";
                        TaskHeapProgress = 0;
                    }
                    break;
            }
        }

        private static string BuildOriginalArrayString(int[] array)
        {
            int previewCount = Math.Min(20, array.Length);
            int[] preview = new int[previewCount];
            Array.Copy(array, preview, previewCount);
            return "Исходный массив: " + string.Join(", ", preview) + (array.Length > 20 ? "..." : string.Empty);
        }

        private static string BuildComparisonSummary(IReadOnlyCollection<double> elapsedTimes, double batchElapsedMilliseconds, bool wasCancelled)
        {
            if (wasCancelled)
            {
                return "Сравнение запусков: выполнение было прервано.";
            }

            if (elapsedTimes.Count == 0)
            {
                return "Сравнение запусков: пока нет данных.";
            }

            double sumOfTimes = 0;
            foreach (double elapsed in elapsedTimes)
            {
                sumOfTimes += elapsed;
            }

            string mode = elapsedTimes.Count > 1 && batchElapsedMilliseconds < sumOfTimes ? "параллельный запуск выгоднее" : "последовательный запуск сопоставим";
            return $"Сравнение запусков: сумма отдельных времен {sumOfTimes:F2} мс, время группы {batchElapsedMilliseconds:F2} мс, вывод: {mode}.";
        }

        private static string FormatArray(int[] array)
        {
            if (array.Length <= 10)
            {
                return string.Join(", ", array);
            }

            int[] firstPart = new int[5];
            int[] lastPart = new int[5];
            Array.Copy(array, 0, firstPart, 0, 5);
            Array.Copy(array, array.Length - 5, lastPart, 0, 5);
            return string.Join(", ", firstPart) + " ... " + string.Join(", ", lastPart);
        }

        private enum ThreadSortKind
        {
            Bubble,
            Quick,
            Insertion,
            Shaker
        }

        private enum TaskSortKind
        {
            Bubble,
            Quick,
            Insertion,
            Heap
        }
    }
}