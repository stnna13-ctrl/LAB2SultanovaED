using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lab2SultanovaED
{
    public sealed class SortExecutionResult
    {
        public SortExecutionResult(int[] sortedArray, long comparisons, double elapsedMilliseconds)
        {
            SortedArray = sortedArray;
            Comparisons = comparisons;
            ElapsedMilliseconds = elapsedMilliseconds;
        }

        public int[] SortedArray { get; }

        public long Comparisons { get; }

        public double ElapsedMilliseconds { get; }
    }

    public class ArraySorter
    {
        private long _totalComparisons;
        private readonly object _locker = new object();

        public bool UseSharedArray { get; set; }

        public delegate void SortCompletedHandler(int[] sortedArray, long comparisons, double elapsedMilliseconds);
        public delegate void SortProgressHandler(string algorithm, double progress);

        public event SortCompletedHandler? BubbleSortCompleted;
        public event SortCompletedHandler? QuickSortCompleted;
        public event SortCompletedHandler? InsertionSortCompleted;
        public event SortCompletedHandler? ShakerSortCompleted;
        public event SortProgressHandler? SortProgressChanged;

        public long TotalComparisons => _totalComparisons;

        public void ResetTotalComparisons()
        {
            lock (_locker)
            {
                _totalComparisons = 0;
            }
        }

        public int[] GenerateRandomArray(int size)
        {
            Random random = new Random();
            int[] array = new int[size];

            for (int i = 0; i < size; i++)
            {
                array[i] = random.Next(1000);
            }

            return array;
        }

        public void BubbleSort(int[] originalArray, CancellationToken token)
        {
            try
            {
                SortExecutionResult result = BubbleSortCore(originalArray, token, CreateThreadProgress("Bubble"));
                BubbleSortCompleted?.Invoke(result.SortedArray, result.Comparisons, result.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void QuickSort(int[] originalArray, CancellationToken token)
        {
            try
            {
                SortExecutionResult result = QuickSortCore(originalArray, token, CreateThreadProgress("Quick"));
                QuickSortCompleted?.Invoke(result.SortedArray, result.Comparisons, result.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void InsertionSort(int[] originalArray, CancellationToken token)
        {
            try
            {
                SortExecutionResult result = InsertionSortCore(originalArray, token, CreateThreadProgress("Insertion"));
                InsertionSortCompleted?.Invoke(result.SortedArray, result.Comparisons, result.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void ShakerSort(int[] originalArray, CancellationToken token)
        {
            try
            {
                SortExecutionResult result = ShakerSortCore(originalArray, token, CreateThreadProgress("Shaker"));
                ShakerSortCompleted?.Invoke(result.SortedArray, result.Comparisons, result.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public Task<SortExecutionResult> BubbleSortAsync(int[] originalArray, CancellationToken token, IProgress<double>? progress = null)
        {
            return Task.Run(() => BubbleSortCore(originalArray, token, progress), token);
        }

        public Task<SortExecutionResult> QuickSortAsync(int[] originalArray, CancellationToken token, IProgress<double>? progress = null)
        {
            return Task.Run(() => QuickSortCore(originalArray, token, progress), token);
        }

        public Task<SortExecutionResult> InsertionSortAsync(int[] originalArray, CancellationToken token, IProgress<double>? progress = null)
        {
            return Task.Run(() => InsertionSortCore(originalArray, token, progress), token);
        }

        public Task<SortExecutionResult> HeapSortAsync(int[] originalArray, CancellationToken token, IProgress<double>? progress = null)
        {
            return Task.Run(() => HeapSortCore(originalArray, token, progress), token);
        }

        private SortExecutionResult BubbleSortCore(int[] originalArray, CancellationToken token, IProgress<double>? progress)
        {
            int[] array = PrepareArray(originalArray);
            long comparisons = 0;
            Stopwatch watch = Stopwatch.StartNew();

            if (array.Length <= 1)
            {
                watch.Stop();
                AddComparisons(comparisons);
                progress?.Report(100);
                return new SortExecutionResult(array, comparisons, watch.Elapsed.TotalMilliseconds);
            }

            for (int i = 0; i < array.Length - 1; i++)
            {
                token.ThrowIfCancellationRequested();

                for (int j = 0; j < array.Length - 1 - i; j++)
                {
                    token.ThrowIfCancellationRequested();
                    comparisons++;
                    if (array[j] > array[j + 1])
                    {
                        Swap(array, j, j + 1);
                    }
                }

                progress?.Report((i + 1) * 100.0 / (array.Length - 1));
            }

            watch.Stop();
            AddComparisons(comparisons);
            progress?.Report(100);
            return new SortExecutionResult(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }

        private SortExecutionResult QuickSortCore(int[] originalArray, CancellationToken token, IProgress<double>? progress)
        {
            int[] array = PrepareArray(originalArray);
            long comparisons = 0;
            int partitionSteps = 0;
            int totalSteps = Math.Max(1, array.Length);
            Stopwatch watch = Stopwatch.StartNew();

            if (array.Length > 1)
            {
                QuickSortRecursive(array, 0, array.Length - 1, ref comparisons, token, progress, ref partitionSteps, totalSteps);
            }

            watch.Stop();
            AddComparisons(comparisons);
            progress?.Report(100);
            return new SortExecutionResult(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }

        private void QuickSortRecursive(
            int[] array,
            int left,
            int right,
            ref long comparisons,
            CancellationToken token,
            IProgress<double>? progress,
            ref int partitionSteps,
            int totalSteps)
        {
            token.ThrowIfCancellationRequested();

            if (left >= right)
            {
                return;
            }

            int pivotIndex = Partition(array, left, right, ref comparisons, token);
            partitionSteps++;
            progress?.Report(Math.Min(100, partitionSteps * 100.0 / totalSteps));

            QuickSortRecursive(array, left, pivotIndex - 1, ref comparisons, token, progress, ref partitionSteps, totalSteps);
            QuickSortRecursive(array, pivotIndex + 1, right, ref comparisons, token, progress, ref partitionSteps, totalSteps);
        }

        private SortExecutionResult InsertionSortCore(int[] originalArray, CancellationToken token, IProgress<double>? progress)
        {
            int[] array = PrepareArray(originalArray);
            long comparisons = 0;
            Stopwatch watch = Stopwatch.StartNew();

            if (array.Length <= 1)
            {
                watch.Stop();
                AddComparisons(comparisons);
                progress?.Report(100);
                return new SortExecutionResult(array, comparisons, watch.Elapsed.TotalMilliseconds);
            }

            for (int i = 1; i < array.Length; i++)
            {
                token.ThrowIfCancellationRequested();

                int key = array[i];
                int j = i - 1;

                while (j >= 0 && array[j] > key)
                {
                    token.ThrowIfCancellationRequested();
                    comparisons++;
                    array[j + 1] = array[j];
                    j--;
                }

                comparisons++;
                array[j + 1] = key;
                progress?.Report(i * 100.0 / (array.Length - 1));
            }

            watch.Stop();
            AddComparisons(comparisons);
            progress?.Report(100);
            return new SortExecutionResult(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }

        private SortExecutionResult ShakerSortCore(int[] originalArray, CancellationToken token, IProgress<double>? progress)
        {
            int[] array = PrepareArray(originalArray);
            long comparisons = 0;
            Stopwatch watch = Stopwatch.StartNew();

            if (array.Length <= 1)
            {
                watch.Stop();
                AddComparisons(comparisons);
                progress?.Report(100);
                return new SortExecutionResult(array, comparisons, watch.Elapsed.TotalMilliseconds);
            }

            int left = 0;
            int right = array.Length - 1;
            bool swapped = true;
            int completedPasses = 0;
            int totalPasses = Math.Max(1, array.Length - 1);

            while (left < right && swapped)
            {
                token.ThrowIfCancellationRequested();
                swapped = false;

                for (int i = left; i < right; i++)
                {
                    token.ThrowIfCancellationRequested();
                    comparisons++;
                    if (array[i] > array[i + 1])
                    {
                        Swap(array, i, i + 1);
                        swapped = true;
                    }
                }

                right--;
                completedPasses++;
                progress?.Report(Math.Min(100, completedPasses * 100.0 / totalPasses));

                for (int i = right; i > left; i--)
                {
                    token.ThrowIfCancellationRequested();
                    comparisons++;
                    if (array[i - 1] > array[i])
                    {
                        Swap(array, i - 1, i);
                        swapped = true;
                    }
                }

                left++;
                completedPasses++;
                progress?.Report(Math.Min(100, completedPasses * 100.0 / totalPasses));
            }

            watch.Stop();
            AddComparisons(comparisons);
            progress?.Report(100);
            return new SortExecutionResult(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }

        private SortExecutionResult HeapSortCore(int[] originalArray, CancellationToken token, IProgress<double>? progress)
        {
            int[] array = PrepareArray(originalArray);
            long comparisons = 0;
            Stopwatch watch = Stopwatch.StartNew();

            if (array.Length <= 1)
            {
                watch.Stop();
                AddComparisons(comparisons);
                progress?.Report(100);
                return new SortExecutionResult(array, comparisons, watch.Elapsed.TotalMilliseconds);
            }

            int length = array.Length;

            for (int i = length / 2 - 1; i >= 0; i--)
            {
                token.ThrowIfCancellationRequested();
                Heapify(array, length, i, ref comparisons, token);
                progress?.Report((length / 2.0 - i) * 50.0 / Math.Max(1, length / 2));
            }

            for (int i = length - 1; i > 0; i--)
            {
                token.ThrowIfCancellationRequested();
                Swap(array, 0, i);
                Heapify(array, i, 0, ref comparisons, token);
                progress?.Report(50 + (length - i) * 50.0 / Math.Max(1, length - 1));
            }

            watch.Stop();
            AddComparisons(comparisons);
            progress?.Report(100);
            return new SortExecutionResult(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }

        private static int Partition(int[] array, int left, int right, ref long comparisons, CancellationToken token)
        {
            int pivot = array[right];
            int i = left - 1;

            for (int j = left; j < right; j++)
            {
                token.ThrowIfCancellationRequested();
                comparisons++;
                if (array[j] < pivot)
                {
                    i++;
                    Swap(array, i, j);
                }
            }

            Swap(array, i + 1, right);
            return i + 1;
        }

        private static void Heapify(int[] array, int heapSize, int rootIndex, ref long comparisons, CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                int largest = rootIndex;
                int left = 2 * rootIndex + 1;
                int right = 2 * rootIndex + 2;

                if (left < heapSize)
                {
                    comparisons++;
                    if (array[left] > array[largest])
                    {
                        largest = left;
                    }
                }

                if (right < heapSize)
                {
                    comparisons++;
                    if (array[right] > array[largest])
                    {
                        largest = right;
                    }
                }

                if (largest == rootIndex)
                {
                    return;
                }

                Swap(array, rootIndex, largest);
                rootIndex = largest;
            }
        }

        private int[] PrepareArray(int[] originalArray)
        {
            if (UseSharedArray)
            {
                return originalArray;
            }

            int[] copy = new int[originalArray.Length];
            Array.Copy(originalArray, copy, originalArray.Length);
            return copy;
        }

        private void AddComparisons(long comparisons)
        {
            lock (_locker)
            {
                _totalComparisons += comparisons;
            }
        }

        private IProgress<double> CreateThreadProgress(string algorithm)
        {
            return new Progress<double>(value => SortProgressChanged?.Invoke(algorithm, value));
        }

        private static void Swap(int[] array, int left, int right)
        {
            int temp = array[left];
            array[left] = array[right];
            array[right] = temp;
        }
    }
}
