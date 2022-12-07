using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mm.Tools.Coroutine
{
    public class Task : System.Threading.Tasks.Task, IAsyncResult
    {
        public Task(Action action) : base(action)
        {

        }

        public Task(Action action, CancellationToken cancellationToken) : base(action, cancellationToken)
        {
        }

        public Task(Action action, TaskCreationOptions creationOptions) : base(action, creationOptions)
        {
        }

        public Task(Action<object> action, object state) : base(action, state)
        {
        }

        public Task(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : base(action, cancellationToken, creationOptions)
        {
        }

        public Task(Action<object> action, object state, CancellationToken cancellationToken) : base(action, state, cancellationToken)
        {
        }

        public Task(Action<object> action, object state, TaskCreationOptions creationOptions) : base(action, state, creationOptions)
        {
        }

        public Task(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : base(action, state, cancellationToken, creationOptions)
        {
        }

        public object AsyncState => throw new NotImplementedException();

        public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

        public bool CompletedSynchronously => throw new NotImplementedException();

        public bool IsCompleted => throw new NotImplementedException();
    }
}
