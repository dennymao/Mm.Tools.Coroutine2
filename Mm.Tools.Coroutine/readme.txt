1.1.1	增加了CTask<T> RunCoroutineItemAsync<T1,T>(Func<T1,T> func, T1 args = default, Action<Exception> exAct =null)方法
		该方法进一步简化了函数异步封装，进一步方便了前期未使用Mm.Coroutine组件的项目使用协程的需求。

1.1.0	增加了CTask<T> RunCoroutineItemAsync<T>(Func<CoroutineItem,object, IEnumerator<CoroutineControl>> func, object args = null) 方法
		该方法用于支持使用await等待在自定义协程中函数的

1.0.0	历史版本