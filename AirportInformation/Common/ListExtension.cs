//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ListExtension.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.Foundation;
    using Windows.UI.Xaml.Data;

    public static class ListExtension
    {
        public static void AddTo<T>(this ObservableCollection<T> itemsSource, ObservableCollection<T> itemsDest)
        {
            foreach (var item in itemsSource)
            {
                itemsDest.Add(item);
            }
        }
    }

    public class IncrementalLoadingCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
    {
        //delegate which populate the next items        
        private readonly Func<CancellationToken, uint, Task<ObservableCollection<T>>> _func;
        //Use by HasMoreItems
        //use to stop the virtualization and the incremental loading
        private CancellationToken _cts;
        private Boolean _isInfinite;
        private uint _maxItems;

        public IncrementalLoadingCollection(Func<CancellationToken, uint, Task<ObservableCollection<T>>> func)
            : this(func, 0)
        {
        }

        public IncrementalLoadingCollection(
            Func<CancellationToken, uint, Task<ObservableCollection<T>>> func,
            uint maxItems)
        {
            this._func = func;
            if (maxItems == 0) //Infinite
            {
                this._isInfinite = true;
            }
            else
            {
                this._maxItems = maxItems;
                this._isInfinite = false;
            }
        }

        public bool HasMoreItems
        {
            get
            {
                if (this._cts.IsCancellationRequested)
                {
                    return false;
                }

                if (this._isInfinite)
                {
                    return true;
                }
                return this.Count < this._maxItems;
            }
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            return AsyncInfo.Run(cts => this.InternalLoadMoreItemsAsync(cts, count));
        }

        private async Task<LoadMoreItemsResult> InternalLoadMoreItemsAsync(CancellationToken cts, uint count)
        {
            ObservableCollection<T> intermediate = null;
            this._cts = cts;
            var baseIndex = this.Count;
            uint numberOfitemsTogenerate = 0;

            if (!this._isInfinite)
            {
                if (baseIndex + count < this._maxItems)
                {
                    numberOfitemsTogenerate = count;
                }
                else
                {
                    //take the last items
                    numberOfitemsTogenerate = this._maxItems - (uint)(baseIndex);
                }
            }
            else
            {
                numberOfitemsTogenerate = count;
            }
            intermediate = await this._func(cts, numberOfitemsTogenerate);
            if (intermediate.Count == 0) //no more items stop the incremental loading 
            {
                this._maxItems = (uint)this.Count;
                this._isInfinite = false;
            }
            else
            {
                intermediate.AddTo(this);
            }
            return new LoadMoreItemsResult { Count = (uint)intermediate.Count };
        }
    }

    public class IncrementalLoadingCollection2<T> : ISupportIncrementalLoading, IList, INotifyCollectionChanged
    {
        //delegate which populate the next items
        //can be passed as a lambda
        private readonly Func<CancellationToken, uint, Task<List<T>>> _func;
        //Use by HasMoreItems
        private readonly List<T> _internalStorage;
        private Boolean _isInfinite;
        private uint _maxItems;

        public IncrementalLoadingCollection2(Func<CancellationToken, uint, Task<List<T>>> func) : this(func, 0)
        {
        }

        public IncrementalLoadingCollection2(Func<CancellationToken, uint, Task<List<T>>> func, uint maxItems)
        {
            this._func = func;
            this._internalStorage = new List<T>();

            if (maxItems == 0) //Infinite
            {
                this._isInfinite = true;
            }
            else
            {
                this._maxItems = maxItems;
                this._isInfinite = false;
            }
        }

        //#region IList

        public int Add(object value)
        {
            this._internalStorage.Add((T)value);
            //return the position
            return this._internalStorage.Count - 1;
        }

        public void Clear()
        {
            this._internalStorage.Clear();
        }

        public bool Contains(object value)
        {
            return this._internalStorage.Contains((T)value);
        }

        public int IndexOf(object value)
        {
            return this._internalStorage.IndexOf((T)value);
        }

        public void Insert(int index, object value)
        {
            this._internalStorage.Insert(index, (T)value);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public void Remove(object value)
        {
            this._internalStorage.Remove((T)value);
        }

        public void RemoveAt(int index)
        {
            this._internalStorage.RemoveAt(index);
        }

        public object this[int index]
        {
            get { return this._internalStorage[index]; }
            set { this._internalStorage[index] = (T)value; }
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)this._internalStorage).CopyTo(array, index);
        }

        public int Count
        {
            get { return this._internalStorage.Count; }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerator GetEnumerator()
        {
            return this._internalStorage.GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void NotifyOfInsertedItems(int baseIndex, int count)
        {
            if (this.CollectionChanged == null)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var args = new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    this._internalStorage[i + baseIndex],
                    i + baseIndex);
                this.CollectionChanged(this, args);
            }
        }

        #region IncrementalLoading

        private CancellationToken _cts;

        public bool HasMoreItems
        {
            get
            {
                if (this._cts.IsCancellationRequested)
                {
                    return false;
                }

                if (this._isInfinite)
                {
                    return true;
                }
                return this.Count < this._maxItems;
            }
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            return AsyncInfo.Run(c => this.InternalLoadMoreItemsAsync(c, count));
        }

        private async Task<LoadMoreItemsResult> InternalLoadMoreItemsAsync(CancellationToken c, uint count)
        {
            List<T> intermediate = null;

            this._cts = c;

            var baseIndex = this._internalStorage.Count;
            uint numberOfitemsTogenerate = 0;

            if (!this._isInfinite)
            {
                if (baseIndex + count < this._maxItems)
                {
                    numberOfitemsTogenerate = count;
                }
                else
                {
                    //take the last next items
                    numberOfitemsTogenerate = this._maxItems - (uint)(baseIndex);
                }
            }
            else
            {
                numberOfitemsTogenerate = count;
            }
            intermediate = await this._func(c, numberOfitemsTogenerate);
            if (intermediate.Count == 0) //no more items
            {
                this._maxItems = (uint)this.Count;
                //Stop the incremental loading
                this._isInfinite = false;
            }
            else
            {
                this._internalStorage.AddRange(intermediate);
            }

            this.NotifyOfInsertedItems(baseIndex, intermediate.Count);
            return new LoadMoreItemsResult { Count = (uint)intermediate.Count };
        }

        #endregion
    }
}
