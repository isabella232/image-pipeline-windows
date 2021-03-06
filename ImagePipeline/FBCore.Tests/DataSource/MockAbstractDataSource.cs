﻿using FBCore.Concurrency;
using FBCore.DataSource;
using System;
using System.Collections.Generic;

namespace FBCore.Tests.DataSource
{
    /// <summary>
    /// Mock AbstractDataSource
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MockAbstractDataSource<T> : AbstractDataSource<T>
    {
        private bool _setState;
        private bool _isClosed;
        private bool _hasResult;
        private T _value;
        private bool _isFinished;
        private bool _hasFailed;
        private Exception _failureCause;

        // Mock
        private IDictionary<string, IList<int>> _methodInvocations;
        private int _inOrderCount;
        private int _response;

        /// <summary>
        /// Instantiates the <see cref="MockAbstractDataSource{T}"/>
        /// </summary>
        public MockAbstractDataSource()
        {
            _setState = false;
            _methodInvocations = new Dictionary<string, IList<int>>(9);
            _inOrderCount = 0;
            _response = -1;
        }

        /// <summary>
        /// Sets <see cref="IDataSource{T}"/> states
        /// </summary>
        public void SetState(
            bool isClosed,
            bool isFinished,
            bool hasResult,
            T value,
            bool hasFailed,
            Exception failureCause)
        {
            _setState = true;
            _isClosed = isClosed;
            _isFinished = isFinished;
            _hasResult = hasResult;
            _value = value;
            _hasFailed = hasFailed;
            _failureCause = failureCause;
        }

        /// <summary>
        /// <returns>true if the data source is closed, false otherwise</returns>
        /// </summary>
        public override bool IsClosed()
        {
            AddMethodInvocation("IsClosed");
            return _setState ? _isClosed : base.IsClosed();
        }

        /// <summary>
        /// The most recent result of the asynchronous computation.
        ///
        /// <para />The caller gains ownership of the object and is responsible for releasing it.
        /// Note that subsequent calls to getResult might give different results. Later results should be
        /// considered to be of higher quality.
        ///
        /// <para />This method will return null in the following cases:
        /// when the DataSource does not have a result (<code> HasResult</code> returns false).
        /// when the last result produced was null.
        /// <returns>current best result</returns>
        /// </summary>
        public override T GetResult()
        {
            AddMethodInvocation("GetResult");
            return _setState ? _value : base.GetResult();
        }

        /// <summary>
        /// <returns>true if any result (possibly of lower quality) is available right now, false otherwise</returns>
        /// </summary>
        public override bool HasResult()
        {
            AddMethodInvocation("HasResult");
            return _setState ? _hasResult : base.HasResult();
        }

        /// <summary>
        /// <returns>true if request is finished, false otherwise</returns>
        /// </summary>
        public override bool IsFinished()
        {
            AddMethodInvocation("IsFinished");
            return _setState ? _isFinished : base.IsFinished();
        }

        /// <summary>
        /// <returns>true if request finished due to error</returns>
        /// </summary>
        public override bool HasFailed()
        {
            AddMethodInvocation("HasFailed");
            return _setState ? _hasFailed : base.HasFailed();
        }

        /// <summary>
        /// <returns>failure cause if the source has failed, else null</returns>
        /// </summary>
        public override Exception GetFailureCause()
        {
            AddMethodInvocation("GetFailureCause");
            return _setState ? _failureCause : base.GetFailureCause();
        }

        /// <summary>
        /// <returns>progress in range [0, 1]</returns>
        /// </summary>
        public override float GetProgress()
        {
            AddMethodInvocation("GetProgress");
            return base.GetProgress();
        }

        /// <summary>
        /// Cancels the ongoing request and releases all associated resources.
        ///
        /// <para />Subsequent calls to <see cref="GetResult"/> will return null.
        /// <returns>true if the data source is closed for the first time</returns>
        /// </summary>
        public override bool Close()
        {
            AddMethodInvocation("Close");
            return _setState ? _isClosed : base.Close();
        }

        /// <summary>
        /// Subscribe for notifications whenever the state of the DataSource changes.
        ///
        /// <para />All changes will be observed on the provided executor.
        /// <param name="dataSubscriber"></param>
        /// <param name="executor"></param>
        /// </summary>
        public override void Subscribe(IDataSubscriber<T> dataSubscriber, IExecutorService executor)
        {
            AddMethodInvocation("Subscribe");
            DataSubscriber = dataSubscriber;

            if (_setState)
            {
                switch (_response)
                {
                    case DataSourceTestUtils.NO_INTERACTIONS:
                        break;

                    case DataSourceTestUtils.ON_NEW_RESULT:
                        dataSubscriber.OnNewResult(this);
                        break;

                    case DataSourceTestUtils.ON_FAILURE:
                        dataSubscriber.OnFailure(this);
                        break;

                    case DataSourceTestUtils.ON_CANCELLATION:
                        dataSubscriber.OnCancellation(this);
                        break;
                }
            }
            else
            {
                base.Subscribe(dataSubscriber, executor);
            }
        }

        internal IDataSubscriber<T> DataSubscriber { get; set; }

        internal bool VerifyMethodInvocation(string methodName, int minNumberOfInvocations)
        {
            IList<int> methodInvocation = default(IList<int>);
            if (_methodInvocations.TryGetValue(methodName, out methodInvocation))
            {
                _methodInvocations.Remove(methodName);
                return methodInvocation[0] >= minNumberOfInvocations;
            }

            return (minNumberOfInvocations == 0) || false;
        }

        internal bool VerifyMethodInvocationOrder(string methodName, int order)
        {
            IList<int> methodInvocation = default(IList<int>);
            if (_methodInvocations.TryGetValue(methodName, out methodInvocation))
            {
                return methodInvocation[1] == order;
            }

            return false;
        }

        internal bool VerifyMethodInvocationOrder(string methodName)
        {
            IList<int> methodInvocation = default(IList<int>);
            if (_methodInvocations.TryGetValue(methodName, out methodInvocation))
            {
                return true;
            }

            return false;
        }

        internal bool HasNoMoreInteraction
        {
            get
            {
                return _methodInvocations.Count == 0;
            }
        }

        internal bool HasZeroInteractions
        {
            get
            {
                return _inOrderCount == 0;
            }
        }

        internal void RespondOnSubscribe(int response)
        {
            _response = response;
        }

        private void AddMethodInvocation(string methodName)
        {
            IList<int> methodInvocation = default(IList<int>);
            if (_methodInvocations.TryGetValue(methodName, out methodInvocation))
            {
                ++methodInvocation[0];
                methodInvocation[1] = _inOrderCount;
            }
            else
            {
                methodInvocation = new List<int>();
                methodInvocation.Add(1); // number of invocations
                methodInvocation.Add(_inOrderCount); // order
                _methodInvocations.Add(methodName, methodInvocation);
            }

            ++_inOrderCount;
        }
    }
}
