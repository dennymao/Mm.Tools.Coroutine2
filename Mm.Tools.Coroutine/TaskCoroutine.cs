using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mm.Tools.Coroutine
{
    public partial class TaskCoroutine : IEnumerable
    {
        List<Task> prList;
        bool isRun = false;
        public TaskCoroutine()
        {
            
        }

        public void AddTask(Task t)
        {
            if (isRun)
                throw new Exception("Coroutine already begin.");

            prList.Add(t);

        }
        public void RemoveTask(Task t)
        {
            if(isRun)
                throw new Exception("Coroutine already begin.");
            prList.Remove(t);
        }

        public void StartCoroutine()
        {
            foreach(Task _t in this)
            {

            }
        }

        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i<prList.Count; i++)
            {

            }
        }
    }
}
